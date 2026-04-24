using Scraps.Configs;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Scraps.Database.LocalFiles
{
    /// <summary>
    /// Схема файлового хранилища JSON.
    /// </summary>
    public class LocalDatabaseSchema : IDatabaseSchema
    {
        private string GetPath(string tableName) => Path.Combine(ScrapsConfig.LocalDataPath, tableName + ".json");

        /// <summary>Получить список таблиц из JSON-файлов в LocalDataPath.</summary>
        public List<string> GetTables(bool includeSystem = false)
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

        /// <summary>Получить имена колонок таблицы.</summary>
        public List<string> GetTableColumns(string tableName)
        {
            var table = JsonTableSerializer.Load(GetPath(tableName));
            return table.Schema.Select(s => s.Name).ToList();
        }

        /// <summary>Получить схему таблицы в виде DataTable.</summary>
        public DataTable GetTableSchema(string tableName)
        {
            var dt = new DataTable(tableName);
            dt.Columns.Add("ColumnName", typeof(string));
            dt.Columns.Add("DataType", typeof(string));

            var table = JsonTableSerializer.Load(GetPath(tableName));
            foreach (var col in table.Schema)
            {
                var row = dt.NewRow();
                row["ColumnName"] = col.Name;
                row["DataType"] = MapNetTypeToSqlType(col.Type);
                dt.Rows.Add(row);
            }
            return dt;
        }

        private static string MapNetTypeToSqlType(string netType)
        {
            switch (netType?.ToLowerInvariant())
            {
                case "int32": return "int";
                case "int64": return "bigint";
                case "int16": return "smallint";
                case "byte": return "tinyint";
                case "string": return "nvarchar";
                case "boolean": return "bit";
                case "datetime": return "datetime";
                case "double": return "float";
                case "decimal": return "decimal";
                case "guid": return "uniqueidentifier";
                default: return "nvarchar";
            }
        }

        /// <summary>Определить, является ли колонка identity (по соглашению: имя оканчивается на ID).</summary>
        public bool IsIdentityColumn(string tableName, string columnName)
        {
            // В файловом хранилище нет автоинкрементных колонок по умолчанию.
            // Можно считать первую колонку с именем, оканчивающимся на "ID", как identity.
            return columnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Определить, допускает ли колонка null (в JSON-режиме все колонки nullable).</summary>
        public bool IsNullableColumn(string tableName, string columnName)
        {
            var table = JsonTableSerializer.Load(GetPath(tableName));
            if (!table.Schema.Any(s => s.Name == columnName))
                throw new InvalidOperationException($"Колонка '{columnName}' не найдена в таблице '{tableName}'.");
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
                table.Schema.Add(new SchemaEntry { Name = col.Key, Type = col.Value });
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
