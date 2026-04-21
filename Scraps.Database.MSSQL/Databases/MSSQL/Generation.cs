using Scraps.Configs;
using Scraps.Databases.Utilities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace Scraps.Databases
{
    public static partial class MSSQL
    {
        /// <summary>
        /// Инициализация: найти сервер, создать БД и базовые таблицы.
        /// Требуется только ScrapsConfig.DatabaseName.
        /// </summary>
        public static void Initialize(string databaseName = null, DatabaseGenerationMode mode = DatabaseGenerationMode.Full)
        {
            if (!string.IsNullOrWhiteSpace(databaseName))
                ScrapsConfig.DatabaseName = databaseName;

            // Синхронизируем UseRoleIdMapping с режимом
            ScrapsConfig.UseRoleIdMapping = mode >= DatabaseGenerationMode.Standard;

            if (string.IsNullOrWhiteSpace(ScrapsConfig.ConnectionString))
            {
                ScrapsConfig.ConnectionString = ConnectionStringBuilder();
                if (string.IsNullOrWhiteSpace(ScrapsConfig.ConnectionString))
                {
                    throw new InvalidOperationException("ScrapsConfig.ConnectionString не задан. Автоматически строку подобрать не удалось. Укажите её вручную (можно через MSSQL.ConnectionStringBuilder).");
                }
            }

            GenerateIfNotExists(new DatabaseGenerationOptions { Mode = mode });
        }

        /// <summary>
        /// Создать базовую структуру БД, если её нет.
        /// </summary>
        public static void GenerateIfNotExists(DatabaseGenerationOptions options = null)
        {
            options = options ?? DatabaseGenerationOptions.Default();

            if (!CheckConnection())
                throw new Exception("Ошибка подключения к базе данных!");

            if (string.IsNullOrEmpty(options.DatabaseName))
                throw new InvalidOperationException("Не задано DatabaseName (укажите в ScrapsConfig.DatabaseName или через параметр).");

            if (options.ApplyUsersMappingToScrapsConfig)
            {
                ScrapsConfig.DatabaseName = options.DatabaseName;
                ScrapsConfig.UsersTableName = string.IsNullOrWhiteSpace(options.UsersTableName)
                    ? ScrapsConfig.UsersTableName
                    : options.UsersTableName;

                if (options.UsersTableColumnsNames != null)
                {
                    ScrapsConfig.UsersTableColumnsNames = new Dictionary<string, string>(options.UsersTableColumnsNames);
                }
            }

            // Синхронизируем UseRoleIdMapping даже при прямом вызове GenerateIfNotExists(...),
            // чтобы поведение Users/RoleManager соответствовало выбранному режиму генерации.
            ScrapsConfig.UseRoleIdMapping = options.Mode >= DatabaseGenerationMode.Standard;

            using (var conn = new SqlConnection(GetMasterConnectionString()))
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = @DbName) " +
                    $"CREATE DATABASE {QuoteIdentifier(options.DatabaseName)}", conn);
                cmd.Parameters.AddWithValue("@DbName", options.DatabaseName);
                cmd.ExecuteNonQuery();
            }

            using (var conn = new SqlConnection(GetDatabaseConnectionString(options.DatabaseName)))
            {
                conn.Open();
                if (options.Mode == DatabaseGenerationMode.None)
                    return;

                bool createRoles = options.Mode >= DatabaseGenerationMode.Standard;
                bool createPermissions = options.Mode >= DatabaseGenerationMode.Full;
                bool useRoleIdMapping = options.Mode >= DatabaseGenerationMode.Standard;

                if (createRoles)
                    CreateRolesTable(conn, options);

                if (createPermissions)
                    CreateRolePermissionsTable(conn);

                CreateUsersTable(conn, options, useRoleIdMapping);
            }
        }

        private static void CreateRolesTable(SqlConnection conn, DatabaseGenerationOptions options)
        {
            new SqlCommand(
                "IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Roles') " +
                "BEGIN " +
                "CREATE TABLE [Roles] (" +
                "[RoleID] int IDENTITY(1,1) PRIMARY KEY, " +
                "[RoleName] nvarchar(64) NOT NULL UNIQUE); " +
                "END", conn).ExecuteNonQuery();

            var cmdDefault = new SqlCommand(
                "SET IDENTITY_INSERT [Roles] ON; " +
                "IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [RoleID] = 0) " +
                "INSERT INTO [Roles]([RoleID], [RoleName]) VALUES (0, @RoleName); " +
                "SET IDENTITY_INSERT [Roles] OFF;", conn);
            cmdDefault.Parameters.AddWithValue("@RoleName", options.DefaultRoleName ?? "default");
            cmdDefault.ExecuteNonQuery();

            if (options.SeedRoles != null)
            {
                foreach (var roleName in options.SeedRoles)
                {
                    var cmdSeed = new SqlCommand(
                        "IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [RoleName] = @RoleName) " +
                        "INSERT INTO [Roles]([RoleName]) VALUES (@RoleName)", conn);
                    cmdSeed.Parameters.AddWithValue("@RoleName", roleName);
                    cmdSeed.ExecuteNonQuery();
                }
            }
        }

        private static void CreateRolePermissionsTable(SqlConnection conn)
        {
            new SqlCommand(
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
                "END", conn).ExecuteNonQuery();

            new SqlCommand(
                "IF NOT EXISTS (SELECT 1 FROM [RolePermissions] WHERE [RoleID] = 0 AND [TableName] = '*') " +
                "INSERT INTO [RolePermissions]([RoleID], [TableName], [CanRead], [CanWrite], [CanDelete], [CanExport], [CanImport]) " +
                "VALUES (0, '*', 0, 0, 0, 0, 0)", conn).ExecuteNonQuery();
        }

        private static void CreateUsersTable(SqlConnection conn, DatabaseGenerationOptions options, bool useRoleIdMapping)
        {
            string tableName = options.UsersTableName ?? "Users";
            var cols = options.UsersTableColumnsNames;
            string quotedTable = QuoteIdentifier(tableName);
            string quotedUserId = QuoteIdentifier(cols?["UserID"] ?? "UserID");
            string quotedLogin = QuoteIdentifier(cols?["Login"] ?? "Login");
            string quotedPassword = QuoteIdentifier(cols?["Password"] ?? "Password");
            string quotedRole = QuoteIdentifier(cols?["Role"] ?? "Role");
            string objectSuffix = BuildSqlObjectSafeSuffix(tableName);
            string fkUsersRoles = QuoteIdentifier("FK_" + objectSuffix + "_Roles");
            string ixUsersLogin = QuoteIdentifier("IX_" + objectSuffix + "_Login");

            var createCmd = new SqlCommand(
                $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName) " +
                "BEGIN " +
                $"CREATE TABLE {quotedTable} (" +
                $"{quotedUserId} int IDENTITY(1,1) PRIMARY KEY, " +
                $"{quotedLogin} nvarchar(64) NOT NULL, " +
                $"{quotedPassword} nvarchar(64) NOT NULL, " +
                (useRoleIdMapping
                    ? $"{quotedRole} int NOT NULL); " +
                      $"ALTER TABLE {quotedTable} ADD CONSTRAINT {fkUsersRoles} FOREIGN KEY ({quotedRole}) REFERENCES [Roles]([RoleID]); "
                    : $"{quotedRole} nvarchar(64) NOT NULL); ") +
                $"CREATE INDEX {ixUsersLogin} ON {quotedTable}({quotedLogin}); " +
                "END", conn);
            createCmd.Parameters.AddWithValue("@TableName", tableName);
            createCmd.ExecuteNonQuery();
        }

        private static string BuildSqlObjectSafeSuffix(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Users";

            var sb = new StringBuilder(value.Length);
            foreach (var ch in value)
                sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');

            var safe = sb.ToString().Trim('_');
            if (safe.Length == 0)
                safe = "Users";
            if (safe.Length > 90)
                safe = safe.Substring(0, 90);

            return safe;
        }
    }
}








