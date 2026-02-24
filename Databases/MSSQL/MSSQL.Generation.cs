using Scraps.Configs;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Scraps.Databases
{
    public static partial class MSSQL
    {
        /// <summary>Совместимость с текущей конфигурацией (ScrapsConfig).</summary>
        public static void PreCheck()
        {
            GenerateIfNotExists(new DatabaseGenerationOptions
            {
                DatabaseName = ScrapsConfig.DatabaseName,
                ConnectionString = ScrapsConfig.ConnectionString,
                UsersTableName = ScrapsConfig.UsersTableName,
                UsersTableColumnsNames = ScrapsConfig.UsersTableColumnsNames,
                UsersRequiredColumnKeys = ScrapsConfig.UsersRequiredColumnKeys,
                UseRoleIdMapping = ScrapsConfig.UseRoleIdMapping,
                DefaultRoleName = ScrapsConfig.DefaultRoleName,
                SeedRoles = ScrapsConfig.SeedRoles,
                CreateRolesTable = true,
                CreateRolePermissionsTable = true,
                CreateUsersTable = true
            });
        }

        /// <summary>Создать базовую структуру БД, если её нет.</summary>
        public static void GenerateIfNotExists(DatabaseGenerationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (!CheckConnection())
                throw new Exception("Ошибка подключения к базе данных!");

            if (string.IsNullOrEmpty(options.DatabaseName))
                throw new SystemException("Не задано DatabaseName.");

            if (string.IsNullOrWhiteSpace(options.UsersTableName))
                throw new SystemException("Не задано UsersTableName.");

            if (options.UsersTableColumnsNames == null || options.UsersRequiredColumnKeys == null)
                throw new SystemException("Не задана схема UsersTableColumnsNames/UsersRequiredColumnKeys.");

            var missingKeys = new List<string>();
            foreach (var key in options.UsersRequiredColumnKeys)
            {
                if (!options.UsersTableColumnsNames.ContainsKey(key) ||
                    string.IsNullOrWhiteSpace(options.UsersTableColumnsNames[key]))
                {
                    missingKeys.Add(key);
                }
            }
            if (missingKeys.Count > 0)
            {
                throw new SystemException("UsersTableColumnsNames: отсутствуют ключи: " +
                    string.Join(", ", missingKeys));
            }

            using (var conn = new SqlConnection(ConnectionStringBuilder("master")))
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = @DbName) " +
                    $"CREATE DATABASE {QuoteIdentifier(options.DatabaseName)}", conn);
                cmd.Parameters.AddWithValue("@DbName", options.DatabaseName);
                cmd.ExecuteNonQuery();
            }

            using (var conn = new SqlConnection(ConnectionStringBuilder(options.DatabaseName)))
            {
                conn.Open();

                if (options.CreateRolesTable)
                {
                    var rolesCmd = new SqlCommand(
                        "IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Roles') " +
                        "BEGIN " +
                        "CREATE TABLE [Roles] (" +
                        "[RoleID] int IDENTITY(1,1) PRIMARY KEY, " +
                        "[RoleName] nvarchar(64) NOT NULL UNIQUE); " +
                        "END", conn);
                    rolesCmd.ExecuteNonQuery();

                    var checkDefaultRole = new SqlCommand(
                        "SELECT COUNT(1) FROM [Roles] WHERE [RoleID] = 0", conn);
                    var hasDefaultRole = Convert.ToInt32(checkDefaultRole.ExecuteScalar()) > 0;
                    if (!hasDefaultRole)
                    {
                        var insertDefault = new SqlCommand(
                            "SET IDENTITY_INSERT [Roles] ON; " +
                            "IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [RoleID] = 0) " +
                            "INSERT INTO [Roles]([RoleID], [RoleName]) VALUES (0, @RoleName); " +
                            "SET IDENTITY_INSERT [Roles] OFF;", conn);
                        insertDefault.Parameters.AddWithValue("@RoleName", options.DefaultRoleName ?? "default");
                        insertDefault.ExecuteNonQuery();
                    }

                    if (options.SeedRoles != null)
                    {
                        foreach (var roleName in options.SeedRoles)
                        {
                            var insertRole = new SqlCommand(
                                "IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [RoleName] = @RoleName) " +
                                "INSERT INTO [Roles]([RoleName]) VALUES (@RoleName)", conn);
                            insertRole.Parameters.AddWithValue("@RoleName", roleName);
                            insertRole.ExecuteNonQuery();
                        }
                    }
                }

                if (options.CreateRolePermissionsTable)
                {
                    var rolePermCmd = new SqlCommand(
                        "IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RolePermissions') " +
                        "BEGIN " +
                        "CREATE TABLE [RolePermissions] (" +
                        "[RoleID] int NOT NULL, " +
                        "[TableName] nvarchar(128) NOT NULL, " +
                        "[CanRead] bit NOT NULL, " +
                        "[CanWrite] bit NOT NULL, " +
                        "[CanDelete] bit NOT NULL, " +
                        "[CanExport] bit NOT NULL, " +
                        "[CanImport] bit NOT NULL, " +
                        "CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RoleID], [TableName]), " +
                        "CONSTRAINT [FK_RolePermissions_Roles] FOREIGN KEY ([RoleID]) REFERENCES [Roles]([RoleID])" +
                        "); " +
                        "END", conn);
                    rolePermCmd.ExecuteNonQuery();

                    var defaultPerm = new SqlCommand(
                        "IF NOT EXISTS (SELECT 1 FROM [RolePermissions] WHERE [RoleID] = 0 AND [TableName] = '*') " +
                        "INSERT INTO [RolePermissions]([RoleID], [TableName], [CanRead], [CanWrite], [CanDelete], [CanExport], [CanImport]) " +
                        "VALUES (0, '*', 0, 0, 0, 0, 0)", conn);
                    defaultPerm.ExecuteNonQuery();
                }

                if (options.CreateUsersTable)
                {
                    string tableName = options.UsersTableName;
                    var cols = options.UsersTableColumnsNames;
                    string quotedTable = QuoteIdentifier(tableName);
                    string quotedUserId = QuoteIdentifier(cols["UserID"]);
                    string quotedLogin = QuoteIdentifier(cols["Login"]);
                    string quotedPassword = QuoteIdentifier(cols["Password"]);
                    string quotedRole = QuoteIdentifier(cols["Role"]);

                    var createCmd = new SqlCommand(
                        $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName) " +
                        "BEGIN " +
                        $"CREATE TABLE {quotedTable} (" +
                        $"{quotedUserId} int IDENTITY(1,1) PRIMARY KEY, " +
                        $"{quotedLogin} nvarchar(64) NOT NULL, " +
                        $"{quotedPassword} nvarchar(64) NOT NULL, " +
                        (options.UseRoleIdMapping
                            ? $"{quotedRole} int NOT NULL); " +
                              $"ALTER TABLE {quotedTable} ADD CONSTRAINT [FK_Users_Roles] FOREIGN KEY ({quotedRole}) REFERENCES [Roles]([RoleID]); "
                            : $"{quotedRole} nvarchar(64) NOT NULL); ") +
                        $"CREATE INDEX [IX_Users_Login] ON {quotedTable}({quotedLogin}); " +
                        "END", conn);
                    createCmd.Parameters.AddWithValue("@TableName", tableName);
                    createCmd.ExecuteNonQuery();
                }
            }
        }
    }
}
