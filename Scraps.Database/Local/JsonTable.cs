using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Scraps.Database.LocalFiles
{
    /// <summary>Описание одной колонки в JSON-схеме.</summary>
    public class SchemaEntry
    {
        /// <summary>Имя колонки.</summary>
        public string Name { get; set; }
        /// <summary>Тип данных (.NET: Int32, String, Boolean, DateTime и т.д.).</summary>
        public string Type { get; set; }
    }

    /// <summary>
    /// JSON-представление таблицы для файлового хранилища.
    /// </summary>
    public class JsonTable
    {
        /// <summary>Колонки таблицы.</summary>
        public List<SchemaEntry> Schema { get; set; } = new List<SchemaEntry>();

        /// <summary>Строки таблицы: список словарей имя_колонки -> значение_строкой.</summary>
        public List<Dictionary<string, string>> Rows { get; set; } = new List<Dictionary<string, string>>();
    }

    /// <summary>
    /// Сериализация/десериализация JsonTable в файл (Newtonsoft.Json).
    /// </summary>
    public static class JsonTableSerializer
    {
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>Сохранить таблицу в JSON-файл.</summary>
        public static void Save(string filePath, JsonTable table)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonConvert.SerializeObject(table, _settings);
            File.WriteAllText(filePath, json);
        }

        /// <summary>Загрузить таблицу из JSON-файла. Если файл не существует — создаёт пустую таблицу.</summary>
        public static JsonTable Load(string filePath)
        {
            if (!File.Exists(filePath))
                return new JsonTable();

            var json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json))
                return new JsonTable();

            var table = new JsonTable();

            try
            {
                var jObj = Newtonsoft.Json.Linq.JObject.Parse(json);

                var schemaArr = jObj["Schema"] as Newtonsoft.Json.Linq.JArray;
                if (schemaArr != null)
                {
                    foreach (var item in schemaArr)
                    {
                        table.Schema.Add(new SchemaEntry
                        {
                            Name = item["Name"]?.ToString(),
                            Type = item["Type"]?.ToString() ?? "String"
                        });
                    }
                }

                var rowsArr = jObj["Rows"] as Newtonsoft.Json.Linq.JArray;
                if (rowsArr != null)
                {
                    foreach (var rowObj in rowsArr.OfType<Newtonsoft.Json.Linq.JObject>())
                    {
                        var dict = new Dictionary<string, string>();
                        foreach (var prop in rowObj.Properties())
                            dict[prop.Name] = prop.Value?.ToString() ?? "";
                        table.Rows.Add(dict);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"[JsonTableSerializer.Load] Failed to parse '{filePath}'. JSON ({json.Length} chars): {json.Substring(0, Math.Min(json.Length, 300))}", ex);
            }

            if ((table.Schema?.Count ?? 0) == 0 && (table.Rows?.Count ?? 0) > 0)
            {
                table.Schema = table.Rows[0].Keys
                    .Select(k => new SchemaEntry { Name = k, Type = "String" })
                    .ToList();
            }

            return table;
        }

        /// <summary>Преобразовать JsonTable в DataTable.</summary>
        public static DataTable ToDataTable(JsonTable table, string tableName)
        {
            var dt = new DataTable(tableName);
            var schema = table.Schema ?? new List<SchemaEntry>();

            foreach (var col in schema)
            {
                Type colType = ResolveType(col.Type);
                dt.Columns.Add(col.Name, colType);
            }

            foreach (var rowDict in table.Rows ?? new List<Dictionary<string, string>>())
            {
                var row = dt.NewRow();
                foreach (var col in schema)
                {
                    if (rowDict.ContainsKey(col.Name))
                    {
                        row[col.Name] = ConvertValue(rowDict[col.Name], dt.Columns[col.Name].DataType);
                    }
                }
                dt.Rows.Add(row);
            }

            return dt;
        }

        /// <summary>Преобразовать DataTable в JsonTable.</summary>
        public static JsonTable FromDataTable(DataTable dt)
        {
            var table = new JsonTable
            {
                Schema = new List<SchemaEntry>(),
                Rows = new List<Dictionary<string, string>>()
            };

            foreach (DataColumn col in dt.Columns)
            {
                string typeName = col.DataType == typeof(int) ? "Int32"
                    : col.DataType == typeof(string) ? "String"
                    : col.DataType == typeof(bool) ? "Boolean"
                    : col.DataType == typeof(DateTime) ? "DateTime"
                    : col.DataType == typeof(double) ? "Double"
                    : col.DataType == typeof(decimal) ? "Decimal"
                    : col.DataType == typeof(Guid) ? "Guid"
                    : col.DataType == typeof(float) ? "Single"
                    : col.DataType == typeof(byte) ? "Byte"
                    : col.DataType == typeof(short) ? "Int16"
                    : col.DataType == typeof(long) ? "Int64"
                    : "String";
                table.Schema.Add(new SchemaEntry { Name = col.ColumnName, Type = typeName });
            }

            foreach (DataRow row in dt.Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;
                var dict = new Dictionary<string, string>();
                foreach (DataColumn col in dt.Columns)
                {
                    var value = row[col];
                    dict[col.ColumnName] = value == DBNull.Value || value == null ? "" : value.ToString();
                }
                table.Rows.Add(dict);
            }

            return table;
        }

        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return typeof(string);

            var type = Type.GetType(typeName);
            if (type != null) return type;

            var wellKnown = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                ["Int32"] = typeof(int),
                ["Int64"] = typeof(long),
                ["String"] = typeof(string),
                ["Boolean"] = typeof(bool),
                ["DateTime"] = typeof(DateTime),
                ["Double"] = typeof(double),
                ["Decimal"] = typeof(decimal),
                ["Guid"] = typeof(Guid),
                ["Single"] = typeof(float),
                ["Byte"] = typeof(byte),
                ["Int16"] = typeof(short)
            };

            if (wellKnown.TryGetValue(typeName, out var wellKnownType))
                return wellKnownType;

            return typeof(string);
        }

        private static object ConvertValue(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
            {
                var underlying = Nullable.GetUnderlyingType(targetType);
                if (underlying != null || !targetType.IsValueType)
                    return DBNull.Value;
                return Activator.CreateInstance(targetType);
            }

            var type = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (type == typeof(string))
                return value;
            if (type == typeof(int))
                return int.Parse(value);
            if (type == typeof(long))
                return long.Parse(value);
            if (type == typeof(bool))
                return bool.Parse(value);
            if (type == typeof(DateTime))
                return DateTime.Parse(value);
            if (type == typeof(double))
                return double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            if (type == typeof(decimal))
                return decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            if (type == typeof(Guid))
                return Guid.Parse(value);
            if (type == typeof(float))
                return float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            if (type == typeof(byte))
                return byte.Parse(value);
            if (type == typeof(short))
                return short.Parse(value);

            return Convert.ChangeType(value, type);
        }
    }
}
