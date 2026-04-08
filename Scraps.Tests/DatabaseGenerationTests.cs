using Scraps.Configs;
using Scraps.Databases;
using Scraps.Databases.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Xunit;

namespace Scraps.Tests
{
    [Collection("DbGeneration")]
    public class DatabaseGenerationTests
    {
        internal static void CleanupCurrentRunDatabases()
        {
            TempDb.CleanupRegisteredDatabases();
        }

        internal static void CleanupOrphanedTestDatabases()
        {
            TempDb.CleanupDatabasesByPrefix("Scraps_Test_");
        }

        [DbFact]
        public void GenerateIfNotExists_Simple_CreatesUsersOnly()
        {
            using (var db = TempDb.Create(DatabaseGenerationMode.Simple, viaInitialize: false))
            {
                var tables = MSSQL.GetTables();
                Assert.Contains("Users", tables);
                Assert.DoesNotContain("Roles", tables);
                Assert.DoesNotContain("RolePermissions", tables);

                var schema = MSSQL.GetTableSchema("Users");
                Assert.Equal("nvarchar", schema["Role"]);
            }
        }

        [DbFact]
        public void GenerateIfNotExists_None_CreatesDatabaseOnly()
        {
            using (var db = TempDb.Create(DatabaseGenerationMode.None, viaInitialize: false))
            {
                var tables = MSSQL.GetTables();
                Assert.DoesNotContain("Users", tables);
                Assert.DoesNotContain("Roles", tables);
                Assert.DoesNotContain("RolePermissions", tables);
            }
        }

        [DbFact]
        public void GenerateIfNotExists_Standard_CreatesUsersAndRoles()
        {
            using (var db = TempDb.Create(DatabaseGenerationMode.Standard, viaInitialize: false))
            {
                var tables = MSSQL.GetTables();
                Assert.Contains("Users", tables);
                Assert.Contains("Roles", tables);
                Assert.DoesNotContain("RolePermissions", tables);

                var schema = MSSQL.GetTableSchema("Users");
                Assert.Equal("int", schema["Role"]);
            }
        }

        [DbFact]
        public void GenerateIfNotExists_Full_CreatesAllTables()
        {
            using (var db = TempDb.Create(DatabaseGenerationMode.Full, viaInitialize: false))
            {
                var tables = MSSQL.GetTables();
                Assert.Contains("Users", tables);
                Assert.Contains("Roles", tables);
                Assert.Contains("RolePermissions", tables);

                var schema = MSSQL.GetTableSchema("Users");
                Assert.Equal("int", schema["Role"]);
            }
        }

        [DbFact]
        public void GenerateIfNotExists_CanRunTwice()
        {
            using (var db = TempDb.Create(DatabaseGenerationMode.Full, viaInitialize: false))
            {
                MSSQL.GenerateIfNotExists(new DatabaseGenerationOptions { DatabaseName = db.DatabaseName, Mode = DatabaseGenerationMode.Full });
                var tables = MSSQL.GetTables();
                Assert.Contains("Users", tables);
                Assert.Contains("Roles", tables);
                Assert.Contains("RolePermissions", tables);
            }
        }

        [DbFact]
        public void GenerateIfNotExists_UsesCustomUsersTableAndColumns()
        {
            using (var db = TempDb.Create(DatabaseGenerationMode.Full, viaInitialize: false))
            {
                var options = new DatabaseGenerationOptions
                {
                    DatabaseName = db.DatabaseName,
                    Mode = DatabaseGenerationMode.Full,
                    UsersTableName = "AppUsers",
                    UsersTableColumnsNames = new Dictionary<string, string>
                    {
                        ["UserID"] = "AppUserId",
                        ["Login"] = "UserLogin",
                        ["Password"] = "UserPassword",
                        ["Role"] = "UserRole"
                    }
                };

                MSSQL.GenerateIfNotExists(options);

                var schema = MSSQL.GetTableSchema("AppUsers");
                Assert.True(schema.ContainsKey("AppUserId"));
                Assert.True(schema.ContainsKey("UserLogin"));
                Assert.True(schema.ContainsKey("UserPassword"));
                Assert.True(schema.ContainsKey("UserRole"));

                // По умолчанию mapping должен применяться в ScrapsConfig.
                Assert.Equal("AppUsers", ScrapsConfig.UsersTableName);
                Assert.Equal("AppUserId", ScrapsConfig.UsersTableColumnsNames["UserID"]);
                Assert.Equal("UserLogin", ScrapsConfig.UsersTableColumnsNames["Login"]);
                Assert.Equal("UserPassword", ScrapsConfig.UsersTableColumnsNames["Password"]);
                Assert.Equal("UserRole", ScrapsConfig.UsersTableColumnsNames["Role"]);
            }
        }

        [DbFact]
        public void GenerateIfNotExists_CanSkipUsersMappingReassign()
        {
            using (var db = TempDb.Create(DatabaseGenerationMode.Full, viaInitialize: false))
            {
                // Базовые значения до custom-генерации.
                var defaultTable = ScrapsConfig.UsersTableName;
                var defaultColumns = new Dictionary<string, string>(ScrapsConfig.UsersTableColumnsNames);

                var options = new DatabaseGenerationOptions
                {
                    DatabaseName = db.DatabaseName,
                    Mode = DatabaseGenerationMode.Full,
                    UsersTableName = "CustomUsers",
                    UsersTableColumnsNames = new Dictionary<string, string>
                    {
                        ["UserID"] = "CustomUserId",
                        ["Login"] = "CustomLogin",
                        ["Password"] = "CustomPassword",
                        ["Role"] = "CustomRole"
                    },
                    ApplyUsersMappingToScrapsConfig = false
                };

                MSSQL.GenerateIfNotExists(options);

                var schema = MSSQL.GetTableSchema("CustomUsers");
                Assert.True(schema.ContainsKey("CustomUserId"));
                Assert.True(schema.ContainsKey("CustomLogin"));

                // Глобальный конфиг не должен быть перезаписан.
                Assert.Equal(defaultTable, ScrapsConfig.UsersTableName);
                Assert.Equal(defaultColumns["UserID"], ScrapsConfig.UsersTableColumnsNames["UserID"]);
                Assert.Equal(defaultColumns["Login"], ScrapsConfig.UsersTableColumnsNames["Login"]);
                Assert.Equal(defaultColumns["Password"], ScrapsConfig.UsersTableColumnsNames["Password"]);
                Assert.Equal(defaultColumns["Role"], ScrapsConfig.UsersTableColumnsNames["Role"]);
            }
        }

        [DbFact]
        public void GenerateIfNotExists_SeedsRoles()
        {
            using (var db = TempDb.Create(DatabaseGenerationMode.Standard, viaInitialize: false))
            {
                var options = new DatabaseGenerationOptions
                {
                    DatabaseName = db.DatabaseName,
                    Mode = DatabaseGenerationMode.Standard,
                    SeedRoles = new[] { "Teacher", "Manager" }
                };

                MSSQL.GenerateIfNotExists(options);

                Assert.NotNull(MSSQL.Roles.GetRoleIdByName("Teacher"));
                Assert.NotNull(MSSQL.Roles.GetRoleIdByName("Manager"));
            }
        }

        [DbFact]
        public void Initialize_SetsUseRoleIdMapping_ByMode()
        {
            using (var db = TempDb.Create(DatabaseGenerationMode.None, viaInitialize: false))
            {
                MSSQL.Initialize(db.DatabaseName, DatabaseGenerationMode.Simple);
                Assert.False(ScrapsConfig.UseRoleIdMapping);

                MSSQL.Initialize(db.DatabaseName, DatabaseGenerationMode.Standard);
                Assert.True(ScrapsConfig.UseRoleIdMapping);

                MSSQL.Initialize(db.DatabaseName, DatabaseGenerationMode.Full);
                Assert.True(ScrapsConfig.UseRoleIdMapping);
            }
        }

        [DbFact]
        public void SimpleMode_UsersStoreRoleAsString()
        {
            using (var db = TempDb.Create(DatabaseGenerationMode.Simple, viaInitialize: false))
            {
                ScrapsConfig.UseRoleIdMapping = false;
                var login = "user_" + Guid.NewGuid().ToString("N");

                MSSQL.Users.Create(login, "Pass1!", "Student");
                var role = MSSQL.Users.GetUserStatus(login);

                Assert.Equal("Student", role);

                MSSQL.Users.Delete(login);
            }
        }

        [DbFact]
        public void StandardMode_UsersStoreRoleAsId()
        {
            using (var db = TempDb.Create(DatabaseGenerationMode.Standard, viaInitialize: false))
            {
                ScrapsConfig.UseRoleIdMapping = true;
                var login = "user_" + Guid.NewGuid().ToString("N");

                MSSQL.Users.Create(login, "Pass1!", "default");
                var role = MSSQL.Users.GetUserStatus(login);

                Assert.Equal("default", role);

                MSSQL.Users.Delete(login);
            }
        }

        [DbFact]
        public void GenerateIfNotExists_ThrowsWhenDatabaseNameMissing()
        {
            using (var db = TempDb.Create(DatabaseGenerationMode.Full, viaInitialize: false))
            {
                Assert.Throws<InvalidOperationException>(() =>
                    MSSQL.GenerateIfNotExists(new DatabaseGenerationOptions { DatabaseName = "" }));
            }
        }

        private sealed class TempDb : IDisposable
        {
            private static readonly object ConnectionCacheLock = new object();
            private static string _cachedBaseConnectionString;
            private static readonly object CleanupLock = new object();
            private static readonly HashSet<string> DatabasesToCleanup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            private readonly string _prevDatabaseName;
            private readonly string _prevConnectionString;
            private readonly string _prevUsersTableName;
            private readonly Dictionary<string, string> _prevUsersTableColumnsNames;
            private readonly string[] _prevUsersRequiredColumnKeys;
            private readonly bool _prevUseRoleIdMapping;
            private readonly string _prevDefaultRoleName;
            private readonly string[] _prevSeedRoles;
            private readonly bool _prevAuthHashPasswords;
            private readonly HashAlgorithm _prevAuthHashAlgorithm;
            private readonly string _prevExplicitServerName;
            private readonly int _prevServerDiscoveryTimeout;
            private readonly bool _prevUseParallelServerDiscovery;
            private readonly int _prevMaxParallelConnections;

            public string DatabaseName { get; }

            private TempDb()
            {
                _prevDatabaseName = ScrapsConfig.DatabaseName;
                _prevConnectionString = ScrapsConfig.ConnectionString;
                _prevUsersTableName = ScrapsConfig.UsersTableName;
                _prevUsersTableColumnsNames = ScrapsConfig.UsersTableColumnsNames != null
                    ? new Dictionary<string, string>(ScrapsConfig.UsersTableColumnsNames)
                    : null;
                _prevUsersRequiredColumnKeys = ScrapsConfig.UsersRequiredColumnKeys != null
                    ? (string[])ScrapsConfig.UsersRequiredColumnKeys.Clone()
                    : null;
                _prevUseRoleIdMapping = ScrapsConfig.UseRoleIdMapping;
                _prevDefaultRoleName = ScrapsConfig.DefaultRoleName;
                _prevSeedRoles = ScrapsConfig.SeedRoles != null
                    ? (string[])ScrapsConfig.SeedRoles.Clone()
                    : null;
                _prevAuthHashPasswords = ScrapsConfig.AuthHashPasswords;
                _prevAuthHashAlgorithm = ScrapsConfig.AuthHashAlgorithm;
                _prevExplicitServerName = ScrapsConfig.ExplicitServerName;
                _prevServerDiscoveryTimeout = ScrapsConfig.ServerDiscoveryTimeout;
                _prevUseParallelServerDiscovery = ScrapsConfig.UseParallelServerDiscovery;
                _prevMaxParallelConnections = ScrapsConfig.MaxParallelConnections;

                DatabaseName = "Scraps_Test_" + Guid.NewGuid().ToString("N");
            }

            public static TempDb Create(DatabaseGenerationMode mode, bool viaInitialize)
            {
                var temp = new TempDb();
                try
                {
                    RegisterDatabaseForCleanup(temp.DatabaseName);

                    ScrapsConfig.DatabaseName = temp.DatabaseName;
                    ScrapsConfig.ConnectionString = BuildConnectionStringForDatabase(temp.DatabaseName);
                    if (string.IsNullOrWhiteSpace(ScrapsConfig.ConnectionString))
                        throw new InvalidOperationException("Не удалось подобрать строку подключения к SQL Server для тестов.");

                    if (!MSSQL.CheckConnection())
                        throw new InvalidOperationException("SQL Server недоступен для запуска DB-тестов.");

                    if (viaInitialize)
                    {
                        MSSQL.Initialize(temp.DatabaseName, mode);
                    }
                    else
                    {
                        MSSQL.GenerateIfNotExists(new DatabaseGenerationOptions { DatabaseName = temp.DatabaseName, Mode = mode });
                    }
                }
                catch (Exception ex)
                {
                    temp.Dispose();
                    throw new InvalidOperationException("Не удалось создать/инициализировать временную БД для теста.", ex);
                }

                return temp;
            }

            private static void RegisterDatabaseForCleanup(string databaseName)
            {
                lock (CleanupLock)
                {
                    DatabasesToCleanup.Add(databaseName);
                }
            }

            private static string BuildConnectionStringForDatabase(string databaseName)
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    throw new ArgumentException("Database name cannot be empty.", nameof(databaseName));

                lock (ConnectionCacheLock)
                {
                    if (string.IsNullOrWhiteSpace(_cachedBaseConnectionString))
                    {
                        var discovered = MSSQL.ConnectionStringBuilder("master");
                        if (string.IsNullOrWhiteSpace(discovered))
                            throw new InvalidOperationException("Не удалось получить базовую строку подключения к SQL Server.");

                        var baseBuilder = new SqlConnectionStringBuilder(discovered)
                        {
                            InitialCatalog = "master"
                        };
                        _cachedBaseConnectionString = baseBuilder.ToString();
                    }

                    var builder = new SqlConnectionStringBuilder(_cachedBaseConnectionString)
                    {
                        InitialCatalog = databaseName
                    };
                    return builder.ToString();
                }
            }

            internal static void CleanupRegisteredDatabases()
            {
                string[] dbs;
                lock (CleanupLock)
                {
                    dbs = DatabasesToCleanup.ToArray();
                    DatabasesToCleanup.Clear();
                }

                if (dbs.Length == 0)
                    return;

                CleanupDatabases(dbs);
            }

            internal static void CleanupDatabasesByPrefix(string prefix)
            {
                if (string.IsNullOrWhiteSpace(prefix))
                    return;

                var dbs = new List<string>();
                var connStr = BuildConnectionStringForDatabase("master");
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT [name] FROM [sys].[databases] WHERE [name] LIKE @prefix", conn))
                    {
                        cmd.Parameters.AddWithValue("@prefix", prefix + "%");
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                dbs.Add(reader.GetString(0));
                            }
                        }
                    }
                }

                if (dbs.Count > 0)
                    CleanupDatabases(dbs);
            }

            private static void CleanupDatabases(IEnumerable<string> databaseNames)
            {
                var names = databaseNames?.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                if (names == null || names.Length == 0)
                    return;

                var connStr = BuildConnectionStringForDatabase("master");
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    foreach (var db in names)
                    {
                        try
                        {
                            var escaped = db.Replace("]", "]]");
                            var cmd = new SqlCommand(
                                "IF DB_ID(@DbName) IS NOT NULL " +
                                "BEGIN " +
                                "ALTER DATABASE [" + escaped + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                                "DROP DATABASE [" + escaped + "]; " +
                                "END", conn);
                            cmd.Parameters.AddWithValue("@DbName", db);
                            cmd.ExecuteNonQuery();
                        }
                        catch
                        {
                            // ignore cleanup errors
                        }
                    }
                }
            }

            public void Dispose()
            {
                // DB cleanup выполняется fixture-ом в конце класса, чтобы не тормозить каждый тест.
                ScrapsConfig.DatabaseName = _prevDatabaseName;
                ScrapsConfig.ConnectionString = _prevConnectionString;
                ScrapsConfig.UsersTableName = _prevUsersTableName;
                ScrapsConfig.UsersTableColumnsNames = _prevUsersTableColumnsNames != null
                    ? new Dictionary<string, string>(_prevUsersTableColumnsNames)
                    : new Dictionary<string, string>();
                ScrapsConfig.UsersRequiredColumnKeys = _prevUsersRequiredColumnKeys ?? new string[0];
                ScrapsConfig.UseRoleIdMapping = _prevUseRoleIdMapping;
                ScrapsConfig.DefaultRoleName = _prevDefaultRoleName;
                ScrapsConfig.SeedRoles = _prevSeedRoles ?? new string[0];
                ScrapsConfig.AuthHashPasswords = _prevAuthHashPasswords;
                ScrapsConfig.AuthHashAlgorithm = _prevAuthHashAlgorithm;
                ScrapsConfig.ExplicitServerName = _prevExplicitServerName;
                ScrapsConfig.ServerDiscoveryTimeout = _prevServerDiscoveryTimeout;
                ScrapsConfig.UseParallelServerDiscovery = _prevUseParallelServerDiscovery;
                ScrapsConfig.MaxParallelConnections = _prevMaxParallelConnections;
            }
        }
    }
}
