using Scraps.Configs;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Scraps.Database.Local
{
    /// <summary>
    /// Схема файлового хранилища JSON.
    /// </summary>
    internal class LocalDatabaseSchema : IDatabaseSchema
    {
        private string GetPath(string tableName) => Path.Combine(ScrapsConfig.LocalDataPath, tableName + ".json");

        public List<string> GetTables(bool includeViews = false, bool includeSystem = false)
        {
            var path = ScrapsConfig.LocalDataPath;
            if (!Directory.Exists(path))
                return new List<string>();

            var files = Directory.GetFiles(path, "*.json");
            return files
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(n => !n.StartsWith("_"))
                .OrderBy(n => n)
                .ToList();
        }

        public List<string> GetTableColumns(string tableName)
        {
            var table = JsonTableSerializer.Load(GetPath(tableName));
            return table.Schema.Keys.ToList();
        }

        public DataTable GetTableSchema(string tableName)
        {
            var dt = new DataTable(tableName);
            var table = JsonTableSerializer.Load(GetPath(tableName));
            foreach (var col in table.Schema)
            {
                var type = JsonTableSerializer.ToDataTable(new JsonTable { Schema = { [col.Key] = col.Value } }, tableName).Columns[col.Key].DataType;
                dt.Columns.Add(col.Key, type);
            }
            return dt;
        }

        public bool IsIdentityColumn(string tableName, string columnName)
        {
            // В файловом хранилище нет автоинкрементных колонок по умолчанию.
            // Можно считать первую колонку с именем, оканчивающимся на "ID", как identity.
            return columnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsNullableColumn(string tableName, string columnName)
        {
            return true; // В JSON все колонки nullable
        }

        /// <summary>
        /// Создать новую таблицу в файловом хранилище.
        /// </summary>
        /// <param name="tableName">Имя таблицы.</param>
        /// <param name="columns">Колонки: имя -> тип (String, Int32, Boolean, DateTime, Double, Decimal, Guid и т.д.).</param>
        public void CreateTable(string tableName, Dictionary<string, string> columns)
        {
            var path = GetPath(tableName);
            if (File.Exists(path))
                throw new InvalidOperationException($"Таблица '{tableName}' уже существует.");

            var table = new JsonTable();
            foreach (var col in columns)
            {
                table.Schema[col.Key] = col.Value;
            }
            JsonTableSerializer.Save(path, table);
        }

        /// <summary>
        /// Удалить таблицу из файлового хранилища.
        /// </summary>
        public void DropTable(string tableName)
        {
            var path = GetPath(tableName);
            if (File.Exists(path))
                File.Delete(path);
        }

        /// <summary>
        /// Проверить существование таблицы.
        /// </summary>
        public bool TableExists(string tableName)
        {
            return File.Exists(GetPath(tableName));
        }
    }
}
