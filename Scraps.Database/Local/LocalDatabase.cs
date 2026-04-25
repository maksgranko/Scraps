using Scraps.Configs;
using System;
using System.IO;

namespace Scraps.Database.Local
{
    /// <summary>
    /// Локальная база данных (LocalFiles режим).
    /// Хранит данные в JSON-файлах в папке ScrapsConfig.LocalDataPath.
    /// </summary>
    public class LocalDatabase : DatabaseBase
    {
        /// <summary>Провайдер базы данных.</summary>
        public override DatabaseProvider Provider => DatabaseProvider.LocalFiles;

        static LocalDatabase()
        {
            DatabaseProviderFactory.Register(DatabaseProvider.LocalFiles, () => new LocalDatabase());
        }

        /// <summary>Создать экземпляр LocalDatabase.</summary>
        public LocalDatabase()
        {
            Connection = new LocalDatabaseConnection();
            Schema = new LocalDatabaseSchema();
            Data = new LocalDatabaseData();
            Users = new LocalDatabaseUsers();
            Roles = new LocalDatabaseRoles();
            RolePermissions = new LocalDatabaseRolePermissions();
            RowEditor = new LocalRowEditor();
            VirtualTables = new LocalVirtualTableRegistry();
            ForeignKeys = new LocalForeignKeyProvider();
        }

        /// <inheritdoc/>
        public override bool TestConnection()
        {
            return Connection?.TestConnection() ?? false;
        }

        /// <inheritdoc/>
        public override void Initialize(DatabaseGenerationOptions options)
        {
            var path = ScrapsConfig.LocalDataPath;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            // Генерируем базовые таблицы если их нет
            EnsureUsersTable(options);
            if (options.Mode == DatabaseGenerationMode.Standard || options.Mode == DatabaseGenerationMode.Full)
            {
                EnsureRolesTable();
            }
            if (options.Mode == DatabaseGenerationMode.Full)
            {
                EnsureRolePermissionsTable();
            }
        }

        private static void EnsureUsersTable(DatabaseGenerationOptions options)
        {
            var tableName = options.UsersTableName ?? ScrapsConfig.UsersTableName;
            var filePath = Path.Combine(ScrapsConfig.LocalDataPath, tableName + ".json");
            if (File.Exists(filePath)) return;

            var table = new JsonTable();
            var columns = options.UsersTableColumnsNames ?? ScrapsConfig.UsersTableColumnsNames;

            foreach (var col in columns)
            {
                var type = col.Key == "UserID" ? "Int32" : "String";
                table.Schema.Add(new SchemaEntry { Name = col.Value, Type = type });
            }

            // Создаём default пользователя
            var defaultRow = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var col in columns)
            {
                defaultRow[col.Value] = col.Key == "UserID" ? "1" : (col.Key == "Login" ? "admin" : (col.Key == "Password" ? "" : (col.Key == "Role" ? options.DefaultRoleName : "")));
            }
            table.Rows.Add(defaultRow);

            JsonTableSerializer.Save(filePath, table);
        }

        private static void EnsureRolesTable()
        {
            var filePath = Path.Combine(ScrapsConfig.LocalDataPath, "Roles.json");
            if (File.Exists(filePath)) return;

            var table = new JsonTable
            {
                Schema = new System.Collections.Generic.List<SchemaEntry>
                {
                    new SchemaEntry { Name = "RoleID", Type = "Int32" },
                    new SchemaEntry { Name = "RoleName", Type = "String" }
                }
            };

            table.Rows.Add(new System.Collections.Generic.Dictionary<string, string> { ["RoleID"] = "0", ["RoleName"] = ScrapsConfig.DefaultRoleName });
            foreach (var role in ScrapsConfig.SeedRoles ?? new string[0])
            {
                table.Rows.Add(new System.Collections.Generic.Dictionary<string, string> { ["RoleID"] = table.Rows.Count.ToString(), ["RoleName"] = role });
            }

            JsonTableSerializer.Save(filePath, table);
        }

        private static void EnsureRolePermissionsTable()
        {
            var filePath = Path.Combine(ScrapsConfig.LocalDataPath, "RolePermissions.json");
            if (File.Exists(filePath)) return;

            var table = new JsonTable
            {
                Schema = new System.Collections.Generic.List<SchemaEntry>
                {
                    new SchemaEntry { Name = "RoleID", Type = "Int32" },
                    new SchemaEntry { Name = "TableName", Type = "String" },
                    new SchemaEntry { Name = "Flags", Type = "Int32" }
                }
            };

            JsonTableSerializer.Save(filePath, table);
        }

        #region --- Public static helpers ---

        /// <summary>
        /// Создать новую таблицу в файловом хранилище.
        /// </summary>
        /// <param name="tableName">Имя таблицы.</param>
        /// <param name="columns">Колонки: имя -> тип (String, Int32, Boolean, DateTime, Double, Decimal, Guid).</param>
        public static void CreateTable(string tableName, System.Collections.Generic.Dictionary<string, string> columns)
        {
            new LocalDatabaseSchema().CreateTable(tableName, columns);
        }

        /// <summary>
        /// Удалить таблицу из файлового хранилища.
        /// </summary>
        public static void DropTable(string tableName)
        {
            new LocalDatabaseSchema().DropTable(tableName);
        }

        /// <summary>
        /// Проверить существование таблицы.
        /// </summary>
        public static bool TableExists(string tableName)
        {
            return new LocalDatabaseSchema().TableExists(tableName);
        }

        /// <summary>
        /// Инициализировать базу данных, если она ещё не инициализирована (аналог MSSQL.GenerateIfNotExists).
        /// </summary>
        public static void GenerateIfNotExists(DatabaseGenerationOptions options = null)
        {
            var db = new LocalDatabase();
            db.Initialize(options ?? DatabaseGenerationOptions.Default());
        }

        #endregion
    }
}
