using Scraps.Configs;
using Scraps.Databases;
using Scraps.Databases.Utilities;
using System;
using System.Data.SqlClient;
using Xunit.Sdk;

namespace Scraps.Tests
{
    public class DatabaseFixture : IDisposable
    {
        public string DatabaseName { get; }

        private readonly string _prevDatabaseName;
        private readonly string _prevConnectionString;
        private readonly string _prevUsersTableName;
        private readonly System.Collections.Generic.Dictionary<string, string> _prevUsersTableColumnsNames;
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

        public DatabaseFixture()
        {
            _prevDatabaseName = ScrapsConfig.DatabaseName;
            _prevConnectionString = ScrapsConfig.ConnectionString;
            _prevUsersTableName = ScrapsConfig.UsersTableName;
            _prevUsersTableColumnsNames = ScrapsConfig.UsersTableColumnsNames != null
                ? new System.Collections.Generic.Dictionary<string, string>(ScrapsConfig.UsersTableColumnsNames)
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
            ScrapsConfig.DatabaseName = DatabaseName;

            if (string.IsNullOrWhiteSpace(ScrapsConfig.ConnectionString))
            {
                ScrapsConfig.ConnectionString = MSSQL.ConnectionStringBuilder(DatabaseName);
                if (string.IsNullOrWhiteSpace(ScrapsConfig.ConnectionString))
                    throw new SkipException("SQL Server не найден. Установите ScrapsConfig.ConnectionString вручную, чтобы запустить DB-тесты.");
            }

            if (!MSSQL.CheckConnection())
                throw new SkipException("Нет доступа к SQL Server. Пропускаем DB-тесты.");

            try
            {
                MSSQL.Initialize(DatabaseName, DatabaseGenerationMode.Full);
            }
            catch (Exception ex)
            {
                throw new SkipException("Не удалось создать/инициализировать тестовую БД. Пропускаем DB-тесты. " + ex.Message);
            }

            // Таблица с пробелом в названии
            MSSQL.ExecuteNonQuery(
                "IF OBJECT_ID(N'[Таблица 1]','U') IS NULL " +
                "CREATE TABLE [Таблица 1] ([Id] int IDENTITY(1,1) PRIMARY KEY, [Name] nvarchar(50) NOT NULL);");

            // Таблица для импорта без identity
            MSSQL.ExecuteNonQuery(
                "IF OBJECT_ID(N'[ImportTest]','U') IS NULL " +
                "CREATE TABLE [ImportTest] ([Name] nvarchar(50) NOT NULL);");

            MSSQL.ExecuteNonQuery("INSERT INTO [Таблица 1]([Name]) VALUES ('Ivan');");
        }

        public void Dispose()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(ScrapsConfig.ConnectionString)
                {
                    InitialCatalog = "master"
                };

                using (var conn = new SqlConnection(builder.ToString()))
                {
                    conn.Open();
                    var cmd = new SqlCommand(
                        $"IF DB_ID(@DbName) IS NOT NULL " +
                        $"BEGIN " +
                        $"ALTER DATABASE [" + DatabaseName + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                        $"DROP DATABASE [" + DatabaseName + "]; " +
                        $"END", conn);
                    cmd.Parameters.AddWithValue("@DbName", DatabaseName);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // ignore cleanup errors
            }

            ScrapsConfig.DatabaseName = _prevDatabaseName;
            ScrapsConfig.ConnectionString = _prevConnectionString;
            ScrapsConfig.UsersTableName = _prevUsersTableName;
            ScrapsConfig.UsersTableColumnsNames = _prevUsersTableColumnsNames != null
                ? new System.Collections.Generic.Dictionary<string, string>(_prevUsersTableColumnsNames)
                : new System.Collections.Generic.Dictionary<string, string>();
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




