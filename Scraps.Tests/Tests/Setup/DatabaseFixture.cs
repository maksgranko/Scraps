using Scraps.Configs;
using Scraps.Database;
using Scraps.Databases.Utilities;
using Scraps.Tests.Setup;
using System;

namespace Scraps.Tests.Setup
{
    /// <summary>
    /// Fixture для DB-тестов. Автоматически подстраивается под выбранный TestDatabaseConfig.Provider.
    /// </summary>
    public class DatabaseFixture : IDisposable
    {
        public string DatabaseName { get; }
        private readonly string _fixtureConnectionString;
        private readonly ITestDatabaseSetup _setup;

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
        private readonly DatabaseProvider _prevDatabaseProvider;

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
            _prevDatabaseProvider = ScrapsConfig.DatabaseProvider;

            _setup = TestDatabaseSetupFactory.Create();
            DatabaseName = _setup.CreateDatabase();
            _fixtureConnectionString = ScrapsConfig.ConnectionString;

            try
            {
                _setup.Initialize(DatabaseGenerationMode.Full);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось инициализировать тестовую БД через {_setup.ProviderName}.", ex);
            }

            // Таблица с пробелом в названии
            _setup.ExecuteNonQuery(
                "IF OBJECT_ID(N'[Таблица 1]','U') IS NULL " +
                "CREATE TABLE [Таблица 1] ([Id] int IDENTITY(1,1) PRIMARY KEY, [Name] nvarchar(50) NOT NULL);");

            // Таблица для импорта без identity
            _setup.ExecuteNonQuery(
                "IF OBJECT_ID(N'[ImportTest]','U') IS NULL " +
                "CREATE TABLE [ImportTest] ([Name] nvarchar(50) NOT NULL);");

            _setup.ExecuteNonQuery("INSERT INTO [Таблица 1]([Name]) VALUES ('Ivan');");
        }

        public void Dispose()
        {
            _setup.DropDatabase(DatabaseName, _fixtureConnectionString);

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
            ScrapsConfig.DatabaseProvider = _prevDatabaseProvider;
            Scraps.Database.DatabaseProviderFactory.Reset();
        }
    }
}
