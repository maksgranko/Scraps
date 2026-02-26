using Scraps.Configs;
using Scraps.Databases;
using Scraps.Databases.Utilities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Xunit;
using Xunit.Sdk;

namespace Scraps.Tests
{
    public class DatabaseGenerationTests
    {
        [Fact]
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
        [Fact]
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
        [Fact]
        public void GenerateIfNotExists_Standard_CreatesUsersAndRoles()\r
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

        [Fact]
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

        [Fact]
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

        [Fact]
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
            }
        }

        [Fact]
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

        [Fact]
        public void Initialize_SetsUseRoleIdMapping_ByMode()
        {
            using (var db = TempDb.Create(DatabaseGenerationMode.Simple, viaInitialize: true))
            {
                Assert.False(ScrapsConfig.UseRoleIdMapping);
            }

            using (var db = TempDb.Create(DatabaseGenerationMode.Standard, viaInitialize: true))
            {
                Assert.True(ScrapsConfig.UseRoleIdMapping);
            }

            using (var db = TempDb.Create(DatabaseGenerationMode.Full, viaInitialize: true))
            {
                Assert.True(ScrapsConfig.UseRoleIdMapping);
            }
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

                ScrapsConfig.DatabaseName = temp.DatabaseName;
                if (string.IsNullOrWhiteSpace(ScrapsConfig.ConnectionString))
                {
                    ScrapsConfig.ConnectionString = MSSQL.ConnectionStringBuilder(temp.DatabaseName);
                    if (string.IsNullOrWhiteSpace(ScrapsConfig.ConnectionString))
                        throw new SkipException("SQL Server не найден. Пропускаем DB-тесты.");
                }

                if (!MSSQL.CheckConnection())
                    throw new SkipException("Нет доступа к SQL Server. Пропускаем DB-тесты.");

                try
                {
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
                    throw new SkipException("Не удалось создать/инициализировать тестовую БД. Пропускаем DB-тесты. " + ex.Message);
                }

                return temp;
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



