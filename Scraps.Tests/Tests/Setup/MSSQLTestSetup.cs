using Scraps.Configs;
using Scraps.Database;
using Scraps.Database.MSSQL;
using Scraps.Database.MSSQL.Utilities;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace Scraps.Tests.Setup
{
    /// <summary>
    /// Настройка тестовой БД для MSSQL.
    /// </summary>
    public class MSSQLTestSetup : ITestDatabaseSetup
    {
        public string ProviderName => "MSSQL";

        public bool IsAvailable
        {
            get
            {
                try
                {
                    var cs = BuildConnectionString("master");
                    if (string.IsNullOrWhiteSpace(cs)) return false;
                    using (var conn = new SqlConnection(cs))
                    {
                        conn.Open();
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        public string CreateDatabase()
        {
            var dbName = "Scraps_Test_" + Guid.NewGuid().ToString("N");
            ScrapsConfig.DatabaseName = dbName;
            var connStr = BuildConnectionString(dbName);
            ScrapsConfig.ConnectionString = connStr;
            ScrapsConfig.DatabaseProvider = DatabaseProvider.MSSQL;

            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(MSSQLDatabase).TypeHandle);
            Scraps.Database.DatabaseProviderFactory.Reset();

            if (string.IsNullOrWhiteSpace(connStr))
                throw new InvalidOperationException("Не удалось подобрать строку подключения к SQL Server для тестов.");

            if (!MSSQL.CheckConnection())
                throw new InvalidOperationException("SQL Server недоступен для запуска DB-тестов.");

            return dbName;
        }

        public void Initialize(DatabaseGenerationMode mode)
        {
            var dbName = ScrapsConfig.DatabaseName;
            if (string.IsNullOrWhiteSpace(dbName))
                throw new InvalidOperationException("DatabaseName не установлен.");
            MSSQL.Initialize(dbName, mode);
        }

        public void ExecuteNonQuery(string sql)
        {
            MSSQL.ExecuteNonQuery(sql);
        }

        public void DropDatabase(string databaseName, string connectionString)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString)
                {
                    InitialCatalog = "master"
                };

                using (var conn = new SqlConnection(builder.ToString()))
                {
                    conn.Open();
                    var cmd = new SqlCommand(
                        "IF DB_ID(@DbName) IS NOT NULL " +
                        "BEGIN " +
                        "ALTER DATABASE [" + databaseName.Replace("]", "]]") + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                        "DROP DATABASE [" + databaseName.Replace("]", "]]") + "]; " +
                        "END", conn);
                    cmd.Parameters.AddWithValue("@DbName", databaseName);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // ignore cleanup errors
            }
        }

        public string BuildConnectionString(string databaseName)
        {
            return MSSQL.ConnectionStringBuilder(databaseName);
        }

        public System.Collections.Generic.IEnumerable<string> FindDatabasesByPrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                yield break;

            var connStr = BuildConnectionString("master");
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT [name] FROM [sys].[databases] WHERE [name] LIKE @prefix", conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", prefix + "%");
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            yield return reader.GetString(0);
                    }
                }
            }
        }

        public void CleanupDatabases(System.Collections.Generic.IEnumerable<string> databaseNames)
        {
            var names = databaseNames?.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(System.StringComparer.OrdinalIgnoreCase).ToArray();
            if (names == null || names.Length == 0)
                return;

            var connStr = BuildConnectionString("master");
            var options = new System.Threading.Tasks.ParallelOptions
            {
                MaxDegreeOfParallelism = ResolveCleanupParallelism()
            };

            System.Threading.Tasks.Parallel.ForEach(names, options, db =>
            {
                try
                {
                    using (var conn = new SqlConnection(connStr))
                    {
                        conn.Open();
                        var escaped = db.Replace("]", "]]");
                        using (var cmd = new SqlCommand(
                            "IF DB_ID(@DbName) IS NOT NULL " +
                            "BEGIN " +
                            "ALTER DATABASE [" + escaped + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                            "DROP DATABASE [" + escaped + "]; " +
                            "END", conn))
                        {
                            cmd.CommandTimeout = 30;
                            cmd.Parameters.AddWithValue("@DbName", db);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch
                {
                    // ignore cleanup errors
                }
            });
        }

        private static int ResolveCleanupParallelism()
        {
            var env = System.Environment.GetEnvironmentVariable("SCRAPS_TEST_DB_CLEANUP_PARALLELISM");
            if (int.TryParse(env, out var configured) && configured > 0)
                return configured;

            var cpu = System.Environment.ProcessorCount;
            if (cpu <= 2) return 1;
            return System.Math.Min(4, cpu);
        }
    }
}
