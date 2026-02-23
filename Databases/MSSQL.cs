using Scraps.Configs;
using Scraps.Localization;
using Scraps.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Linq;

namespace Scraps.Databases
{
    /// <summary>
    /// Опции генерации схемы базы данных.
    /// </summary>
    public class DatabaseGenerationOptions
    {
        /// <summary>Название базы данных.</summary>
        public string DatabaseName { get; set; }
        /// <summary>Строка подключения.</summary>
        public string ConnectionString { get; set; }
        /// <summary>Название таблицы пользователей.</summary>
        public string UsersTableName { get; set; }
        /// <summary>Сопоставление логических ключей колонок с реальными именами.</summary>
        public Dictionary<string, string> UsersTableColumnsNames { get; set; }
        /// <summary>Обязательные логические ключи для таблицы пользователей.</summary>
        public string[] UsersRequiredColumnKeys { get; set; }
        /// <summary>Если true, Users.Role хранит RoleID (int). Если false, хранит RoleName (string).</summary>
        public bool UseRoleIdMapping { get; set; }
        /// <summary>Название роли по умолчанию (RoleID = 0).</summary>
        public string DefaultRoleName { get; set; }
        /// <summary>Роли для первичного заполнения.</summary>
        public string[] SeedRoles { get; set; }
        /// <summary>Создать таблицу Roles.</summary>
        public bool CreateRolesTable { get; set; }
        /// <summary>Создать таблицу RolePermissions.</summary>
        public bool CreateRolePermissionsTable { get; set; }
        /// <summary>Создать таблицу Users.</summary>
        public bool CreateUsersTable { get; set; }
    }

    /// <summary>
    /// Утилиты работы с Microsoft SQL Server.
    /// </summary>
    public static class MSSQL
    {
        /// <summary>Модель роли (ID + название).</summary>
        public class RoleInfo
        {
            /// <summary>Идентификатор роли.</summary>
            public int Id { get; set; }
            /// <summary>Название роли.</summary>
            public string Name { get; set; }
        }

        /// <summary>Модель прав роли по таблице.</summary>
        public class RolePermissionInfo
        {
            /// <summary>Идентификатор роли.</summary>
            public int RoleId { get; set; }
            /// <summary>Имя таблицы.</summary>
            public string TableName { get; set; }
            /// <summary>Права (флаги).</summary>
            public TablePermission Permission { get; set; }
            /// <summary>Права (флаги).</summary>
            public PermissionFlags Flags => Permission?.Flags ?? PermissionFlags.None;
        }

        /// <summary>Операции с таблицей Roles.</summary>
        public static class Roles
        {
            /// <summary>Название таблицы ролей.</summary>
            public static string RolesTableName = "Roles";
            /// <summary>Название колонки RoleID.</summary>
            public static string RoleIdColumnName = "RoleID";
            /// <summary>Название колонки RoleName.</summary>
            public static string RoleNameColumnName = "RoleName";

            /// <summary>Получить имя роли по ID.</summary>
            public static string GetRoleNameById(int roleId)
            {
                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    string query = $"SELECT {RoleNameColumnName} FROM {RolesTableName} WHERE {RoleIdColumnName} = @RoleID";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@RoleID", roleId);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return result?.ToString();
                }
            }

            /// <summary>Получить ID роли по названию.</summary>
            public static int? GetRoleIdByName(string roleName)
            {
                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    string query = $"SELECT {RoleIdColumnName} FROM {RolesTableName} WHERE {RoleNameColumnName} = @RoleName";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@RoleName", roleName);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value) return null;
                    return Convert.ToInt32(result);
                }
            }

            /// <summary>Получить список всех ролей.</summary>
            public static List<RoleInfo> GetAll()
            {
                var result = new List<RoleInfo>();
                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    string query = $"SELECT {RoleIdColumnName}, {RoleNameColumnName} FROM {RolesTableName}";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new RoleInfo
                            {
                                Id = Convert.ToInt32(reader[RoleIdColumnName]),
                                Name = reader[RoleNameColumnName].ToString()
                            });
                        }
                    }
                }
                return result;
            }
        }

        /// <summary>Операции с таблицей Users.</summary>
        public static class Users
        {
            /// <summary>Название таблицы пользователей.</summary>
            public static string UsersTableName => ScrapsConfig.UsersTableName;
            /// <summary>Сопоставление логических ключей колонок с реальными именами.</summary>
            public static Dictionary<string, string> UsersTableColumnsNames => ScrapsConfig.UsersTableColumnsNames;

            /// <summary>Получить пользователя по логину.</summary>
            public static DataRow GetByLogin(string login)
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    string query = $"SELECT * FROM {UsersTableName} WHERE {UsersTableColumnsNames["Login"]} = @Login";
                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    da.SelectCommand.Parameters.AddWithValue("@Login", login);
                    try
                    {
                        da.Fill(dt);
                    }
                    catch
                    {
                        return null;
                    }
                }
                return dt.Rows.Count > 0 ? dt.Rows[0] : null;
            }

            /// <summary>Получить название роли пользователя.</summary>
            public static string GetUserStatus(string login)
            {
                var user = GetByLogin(login);
                var roleObj = user?[UsersTableColumnsNames["Role"]];
                if (roleObj == null || roleObj == DBNull.Value) return null;

                if (!ScrapsConfig.UseRoleIdMapping)
                    return roleObj.ToString();

                int roleId = Convert.ToInt32(roleObj);
                return Roles.GetRoleNameById(roleId);
            }

            /// <summary>Создать пользователя.</summary>
            public static bool Create(string login, string password, string role)
            {
                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    string query = $"INSERT INTO {UsersTableName}" +
                                   $"({UsersTableColumnsNames["Login"]}, " +
                                   $"{UsersTableColumnsNames["Password"]}, " +
                                   $"{UsersTableColumnsNames["Role"]}) " +
                                   $"VALUES (@Login, @Password, @Role)";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Login", login);
                    cmd.Parameters.AddWithValue("@Password", password);

                    if (ScrapsConfig.UseRoleIdMapping)
                    {
                        var roleId = Roles.GetRoleIdByName(role);
                        if (roleId == null) throw new Exception("Role not found: " + role);
                        cmd.Parameters.AddWithValue("@Role", roleId.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@Role", role);
                    }

                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }        /// <summary>Операции с таблицей RolePermissions.</summary>
        public static class RolePermissions
        {
            /// <summary>Название таблицы прав ролей.</summary>
            public static string RolePermissionsTableName = "RolePermissions";

            /// <summary>Получить все правила прав.</summary>
            public static List<RolePermissionInfo> GetAll()
            {
                var result = new List<RolePermissionInfo>();
                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    string query = $"SELECT RoleID, TableName, CanRead, CanWrite, CanDelete, CanExport, CanImport FROM {RolePermissionsTableName}";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var tableName = reader["TableName"].ToString();
                            var flags = PermissionFlags.None;
                            if (Convert.ToBoolean(reader["CanRead"])) flags |= PermissionFlags.Read;
                            if (Convert.ToBoolean(reader["CanWrite"])) flags |= PermissionFlags.Write;
                            if (Convert.ToBoolean(reader["CanDelete"])) flags |= PermissionFlags.Delete;
                            if (Convert.ToBoolean(reader["CanExport"])) flags |= PermissionFlags.Export;
                            if (Convert.ToBoolean(reader["CanImport"])) flags |= PermissionFlags.Import;
                            result.Add(new RolePermissionInfo
                            {
                                RoleId = Convert.ToInt32(reader["RoleID"]),
                                TableName = tableName,
                                Permission = new TablePermission
                                {
                                    TableName = tableName,
                                    Flags = flags
                                }
                            });
                        }
                    }
                }
                return result;
            }
        }

        /// <summary>Сформировать строку подключения (с автопоиском).</summary>
        public static string ConnectionStringBuilder(string databaseName, bool auto = true)
        {
            if (auto)
            {
                string result = ParseFirstSQLServer(databaseName);
                if (result != null) return result;
            }
            return $"Data Source={Environment.MachineName};Initial Catalog={databaseName};Integrated Security=True;Encrypt=False;Connection Timeout=3;";
        }

        /// <summary>Попытаться найти SQL Server среди популярных вариантов.</summary>
        public static string ParseFirstSQLServer(string databaseName)
        {
            string[] defaultServers = {
                ".\\SQLEXPRESS",
                "localhost",
                ".",
                ".\\SQLSERVER01",
                Environment.MachineName,
                $"{Environment.MachineName}\\SQLEXPRESS",
                $"{Environment.MachineName}\\SQLSERVER01",
            };

            foreach (var server in defaultServers)
            {
                try
                {
                    using (var conn = new SqlConnection($"Data Source={server};Initial Catalog=master;Integrated Security=True;Connection Timeout=3;"))
                    {
                        conn.Open();
                        return $"Data Source={server};Initial Catalog={databaseName};Integrated Security=True;Encrypt=False";
                    }
                }
                catch { }
            }

            try
            {
                var instances = SqlDataSourceEnumerator.Instance.GetDataSources();
                foreach (DataRow row in instances.Rows)
                {
                    string serverName = row["ServerName"].ToString();
                    string instanceName = row["InstanceName"].ToString();
                    string fullServerName = string.IsNullOrEmpty(instanceName)
                        ? serverName
                        : $"{serverName}\\{instanceName}";

                    try
                    {
                        using (var conn = new SqlConnection($"Data Source={fullServerName};Initial Catalog=master;Integrated Security=True;Connection Timeout=3;"))
                        {
                            conn.Open();
                            return $"Data Source={fullServerName};Initial Catalog={databaseName};Integrated Security=True;Encrypt=False";
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return null;
        }

        /// <summary>Проверить соединение с SQL Server.</summary>
        public static bool CheckConnection()
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionStringBuilder("master")))
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

        /// <summary>Выполнить SQL-команду без возврата данных.</summary>
        public static int ExecuteNonQuery(string query)
        {
            using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Выполнить SQL-команду и вернуть скалярный результат.</summary>
        public static object ExecuteScalar(string query)
        {
            using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                return cmd.ExecuteScalar();
            }
        }

        /// <summary>Получить DataTable из SQL-запроса.</summary>
        public static DataTable GetDataTableFromSQL(string sqlRequest)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    new SqlDataAdapter(sqlRequest, conn).Fill(dt);
                }
                return dt;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Получить все записи из таблицы.</summary>
        public static DataTable GetTableData(string tableName)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                SqlDataAdapter da = new SqlDataAdapter($"SELECT * FROM {tableName}", conn);
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>Получить все записи из таблицы с проверкой прав.</summary>
        public static DataTable GetTableData(string tableName, string roleName, PermissionFlags required)
        {
            if (!RoleManager.CheckAccess(roleName, tableName, required))
                throw new UnauthorizedAccessException($"Нет доступа для роли '{roleName}' к таблице '{tableName}'.");

            return GetTableData(tableName);
        }

        /// <summary>Получить все записи из таблицы с переводом названий колонок.</summary>
        public static DataTable GetTableDataTranslated(string tableName)
        {
            var dt = GetTableData(tableName);
            return TranslationManager.TranslateDataTable(dt, tableName);
        }

        /// <summary>Найти записи по значению колонки (LIKE для строк, = для остальных).</summary>
        public static DataTable FindByColumn(string tableName, string columnName, object value, bool useLike = true)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                if (value == null)
                {
                    string queryNull = $"SELECT * FROM [{tableName}] WHERE [{columnName}] IS NULL";
                    SqlDataAdapter daNull = new SqlDataAdapter(queryNull, conn);
                    daNull.Fill(dt);
                    return dt;
                }

                bool isString = value is string;
                string op = (useLike && isString) ? "LIKE" : "=";

                string query = $"SELECT * FROM [{tableName}] WHERE [{columnName}] {op} @Value";
                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                object paramValue = (useLike && isString) ? $"%{value}%" : value;
                da.SelectCommand.Parameters.AddWithValue("@Value", paramValue);

                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>Применить изменения DataTable в БД (insert/update/delete).</summary>
        public static int ApplyTableChanges(string tableName, DataTable data)
        {
            using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                SqlDataAdapter da = new SqlDataAdapter($"SELECT * FROM {tableName}", conn);
                SqlCommandBuilder cb = new SqlCommandBuilder(da);
                da.UpdateCommand = cb.GetUpdateCommand();
                da.InsertCommand = cb.GetInsertCommand();
                da.DeleteCommand = cb.GetDeleteCommand();

                conn.Open();
                return da.Update(data);
            }
        }

        /// <summary>Получить список таблиц базы данных.</summary>
        public static string[] GetTables(bool includeSystemTables = false)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                string query = @"SELECT TABLE_NAME
                                FROM INFORMATION_SCHEMA.TABLES
                                WHERE TABLE_TYPE = 'BASE TABLE'";

                if (!includeSystemTables)
                {
                    query += " AND TABLE_CATALOG = @DatabaseName";
                }

                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                da.SelectCommand.Parameters.AddWithValue("@DatabaseName", ScrapsConfig.DatabaseName);
                da.Fill(dt);
            }
            return dt.Rows.Cast<DataRow>().Select(r => r[0].ToString()).ToArray();
        }

        /// <summary>Получить список колонок таблицы.</summary>
        public static string[] GetTableColumns(string tableName)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                string query = @"
                    SELECT COLUMN_NAME
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = @TableName
                    ORDER BY ORDINAL_POSITION";

                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                da.SelectCommand.Parameters.AddWithValue("@TableName", tableName);
                da.Fill(dt);
            }
            return dt.Rows.Cast<DataRow>().Select(r => r[0].ToString()).ToArray();
        }

        /// <summary>Получить схему таблицы (ColumnName -> DataType).</summary>
        public static Dictionary<string, string> GetTableSchema(string tableName)
        {
            var schema = new Dictionary<string, string>();
            using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                string query = @"
                    SELECT
                        COLUMN_NAME,
                        DATA_TYPE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = @TableName";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TableName", tableName);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        schema[reader["COLUMN_NAME"].ToString()] =
                            reader["DATA_TYPE"].ToString();
                    }
                }
            }
            return schema;
        }

        /// <summary>Проверить, является ли колонка identity.</summary>
        public static bool IsIdentityColumn(string tableName, string columnName)
        {
            try
            {
                using (var conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    string query = @"
                        SELECT COLUMNPROPERTY(OBJECT_ID(@TableName), @ColumnName, 'IsIdentity') AS IsIdentity";

                    var cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@TableName", tableName);
                    cmd.Parameters.AddWithValue("@ColumnName", columnName);

                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return result != DBNull.Value && Convert.ToInt32(result) == 1;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Проверить, допускает ли колонка NULL.</summary>
        public static bool IsNullableColumn(string tableName, string columnName)
        {
            using (var connection = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName", connection);
                command.Parameters.AddWithValue("@tableName", tableName);
                command.Parameters.AddWithValue("@columnName", columnName);

                var isNullable = command.ExecuteScalar();
                return isNullable != null && isNullable.ToString().ToLower() == "yes";
            }
        }

        /// <summary>Массовая вставка (SqlBulkCopy) с учётом переводов.</summary>
        public static int BulkInsert(string tableName, DataTable data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            DataTable importData = data.Copy();
            TranslationManager.UntranslateDataTable(importData, tableName);

            using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
            {
                conn.Open();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                {
                    bulkCopy.DestinationTableName = tableName;

                    foreach (DataColumn column in importData.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }

                    bulkCopy.WriteToServer(importData);
                    return importData.Rows.Count;
                }
            }
        }

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
                    $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{options.DatabaseName}') " +
                    $"CREATE DATABASE {options.DatabaseName}", conn);
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

                    var createCmd = new SqlCommand(
                        $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}') " +
                        "BEGIN " +
                        $"CREATE TABLE [{tableName}] (" +
                        $"[{cols["UserID"]}] int IDENTITY(1,1) PRIMARY KEY, " +
                        $"[{cols["Login"]}] nvarchar(64) NOT NULL, " +
                        $"[{cols["Password"]}] nvarchar(64) NOT NULL, " +
                        (options.UseRoleIdMapping
                            ? $"[{cols["Role"]}] int NOT NULL); " +
                              $"ALTER TABLE [{tableName}] ADD CONSTRAINT [FK_Users_Roles] FOREIGN KEY ([{cols["Role"]}]) REFERENCES [Roles]([RoleID]); "
                            : $"[{cols["Role"]}] nvarchar(64) NOT NULL); ") +
                        $"CREATE INDEX [IX_Users_Login] ON [{tableName}]([{cols["Login"]}]); " +
                        "END", conn);
                    createCmd.ExecuteNonQuery();
                }
            }
        }
    }
}