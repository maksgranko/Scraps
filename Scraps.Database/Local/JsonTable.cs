using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Scraps.Database.Local
{
    /// <summary>
    /// JSON-представление таблицы для файлового хранилища.
    /// </summary>
    [DataContract]
    public class JsonTable
    {
        /// <summary>Колонки таблицы: имя -> тип (AssemblyQualifiedName).</summary>
        [DataMember]
        public Dictionary<string, string> Schema { get; set; } = new Dictionary<string, string>();

        /// <summary>Строки таблицы: список словарей имя_колонки -> значение_строкой.</summary>
        [DataMember]
        public List<Dictionary<string, string>> Rows { get; set; } = new List<Dictionary<string, string>>();
    }

    /// <summary>
    /// Сериализация/десериализация JsonTable в файл.
    /// </summary>
    public static class JsonTableSerializer
    {
        private static readonly DataContractJsonSerializer _serializer = new DataContractJsonSerializer(typeof(JsonTable));

        /// <summary>Сохранить таблицу в JSON-файл.</summary>
        public static void Save(string filePath, JsonTable table)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                _serializer.WriteObject(stream, table);
            }
        }

        /// <summary>Загрузить таблицу из JSON-файла. Если файл не существует — создаёт пустую таблицу.</summary>
        public static JsonTable Load(string filePath)
        {
            if (!File.Exists(filePath))
                return new JsonTable();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                return (JsonTable)_serializer.ReadObject(stream);
            }
        }

        /// <summary>Преобразовать JsonTable в DataTable.</summary>
        public static DataTable ToDataTable(JsonTable table, string tableName)
        {
            var dt = new DataTable(tableName);

            foreach (var col in table.Schema)
            {
                Type colType = ResolveType(col.Value);
                dt.Columns.Add(col.Key, colType);
            }

            foreach (var rowDict in table.Rows)
            {
                var row = dt.NewRow();
                foreach (var kv in rowDict)
                {
                    if (dt.Columns.Contains(kv.Key))
                    {
                        row[kv.Key] = ConvertValue(kv.Value, dt.Columns[kv.Key].DataType);
                    }
                }
                dt.Rows.Add(row);
            }

            return dt;
        }

        /// <summary>Преобразовать DataTable в JsonTable.</summary>
        public static JsonTable FromDataTable(DataTable dt)
        {
            var table = new JsonTable();

            foreach (DataColumn col in dt.Columns)
            {
                table.Schema[col.ColumnName] = col.DataType.AssemblyQualifiedName;
            }

            foreach (DataRow row in dt.Rows)
            {
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

            // Fallback на короткие имена (Int32, String и т.д.)
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
                // Для не-nullable value types возвращаем default
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
