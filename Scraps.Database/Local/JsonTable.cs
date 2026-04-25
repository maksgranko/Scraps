using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace Scraps.Database.Local
{
    /// <summary>Описание одной колонки в JSON-схеме.</summary>
    public class SchemaEntry
    {
        public string Name { get; set; }
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
    /// Сериализация/десериализация JsonTable в файл (ручной JSON — работает на любой платформе).
    /// </summary>
    public static class JsonTableSerializer
    {
        /// <summary>Сохранить таблицу в JSON-файл.</summary>
        public static void Save(string filePath, JsonTable table)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"Schema\": [");
            for (int i = 0; i < (table.Schema?.Count ?? 0); i++)
            {
                var col = table.Schema[i];
                sb.Append($"    {{ \"Name\": {Escape(col.Name)}, \"Type\": {Escape(col.Type)} }}");
                if (i < table.Schema.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  ],");
            sb.AppendLine("  \"Rows\": [");
            for (int r = 0; r < (table.Rows?.Count ?? 0); r++)
            {
                var row = table.Rows[r];
                sb.Append("    {");
                var keys = row.Keys.ToList();
                for (int k = 0; k < keys.Count; k++)
                {
                    sb.Append($" {Escape(keys[k])}: {Escape(row[keys[k]])}");
                    if (k < keys.Count - 1) sb.Append(",");
                }
                sb.Append(" }");
                if (r < table.Rows.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>Загрузить таблицу из JSON-файла. Если файл не существует — создаёт пустую таблицу.</summary>
        public static JsonTable Load(string filePath)
        {
            if (!File.Exists(filePath))
                return new JsonTable();

            var json = File.ReadAllText(filePath, Encoding.UTF8);
            return Parse(json);
        }

        private static JsonTable Parse(string json)
        {
            var table = new JsonTable();
            json = json.Trim();
            if (!json.StartsWith("{")) return table;

            int pos = 1;
            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] == '}') break;

                string key = ReadString(json, ref pos);
                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ':') pos++;
                SkipWhitespace(json, ref pos);

                if (key == "Schema")
                {
                    table.Schema = ReadSchemaArray(json, ref pos);
                }
                else if (key == "Rows")
                {
                    table.Rows = ReadRowsArray(json, ref pos);
                }
                else
                {
                    SkipValue(json, ref pos);
                }

                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ',') pos++;
            }

            return table;
        }

        private static List<SchemaEntry> ReadSchemaArray(string json, ref int pos)
        {
            var list = new List<SchemaEntry>();
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '[') return list;
            pos++;

            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ']') { pos++; break; }

                var entry = new SchemaEntry();
                if (json[pos] == '{')
                {
                    pos++;
                    while (pos < json.Length)
                    {
                        SkipWhitespace(json, ref pos);
                        if (pos < json.Length && json[pos] == '}') { pos++; break; }

                        string k = ReadString(json, ref pos);
                        SkipWhitespace(json, ref pos);
                        if (pos < json.Length && json[pos] == ':') pos++;
                        SkipWhitespace(json, ref pos);
                        string v = ReadString(json, ref pos);

                        if (k == "Name") entry.Name = v;
                        else if (k == "Type") entry.Type = v;

                        SkipWhitespace(json, ref pos);
                        if (pos < json.Length && json[pos] == ',') pos++;
                    }
                }
                list.Add(entry);

                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ',') pos++;
            }
            return list;
        }

        private static List<Dictionary<string, string>> ReadRowsArray(string json, ref int pos)
        {
            var list = new List<Dictionary<string, string>>();
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '[') return list;
            pos++;

            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ']') { pos++; break; }

                var dict = new Dictionary<string, string>();
                if (json[pos] == '{')
                {
                    pos++;
                    while (pos < json.Length)
                    {
                        SkipWhitespace(json, ref pos);
                        if (pos < json.Length && json[pos] == '}') { pos++; break; }

                        string k = ReadString(json, ref pos);
                        SkipWhitespace(json, ref pos);
                        if (pos < json.Length && json[pos] == ':') pos++;
                        SkipWhitespace(json, ref pos);
                        string v = ReadString(json, ref pos);
                        dict[k] = v;

                        SkipWhitespace(json, ref pos);
                        if (pos < json.Length && json[pos] == ',') pos++;
                    }
                }
                list.Add(dict);

                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ',') pos++;
            }
            return list;
        }

        private static void SkipWhitespace(string json, ref int pos)
        {
            while (pos < json.Length && char.IsWhiteSpace(json[pos])) pos++;
        }

        private static string ReadString(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length) return "";
            if (json[pos] != '\"') return "";
            pos++;
            var sb = new StringBuilder();
            while (pos < json.Length && json[pos] != '\"')
            {
                if (json[pos] == '\\' && pos + 1 < json.Length)
                {
                    pos++;
                    switch (json[pos])
                    {
                        case '\"': sb.Append('\"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        default: sb.Append(json[pos]); break;
                    }
                }
                else
                {
                    sb.Append(json[pos]);
                }
                pos++;
            }
            if (pos < json.Length && json[pos] == '\"') pos++;
            return sb.ToString();
        }

        private static void SkipValue(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length) return;
            if (json[pos] == '\"') { ReadString(json, ref pos); return; }
            if (json[pos] == '[')
            {
                pos++;
                while (pos < json.Length && json[pos] != ']')
                {
                    SkipValue(json, ref pos);
                    SkipWhitespace(json, ref pos);
                    if (pos < json.Length && json[pos] == ',') pos++;
                }
                if (pos < json.Length && json[pos] == ']') pos++;
                return;
            }
            if (json[pos] == '{')
            {
                pos++;
                while (pos < json.Length && json[pos] != '}')
                {
                    SkipWhitespace(json, ref pos);
                    ReadString(json, ref pos);
                    SkipWhitespace(json, ref pos);
                    if (pos < json.Length && json[pos] == ':') pos++;
                    SkipValue(json, ref pos);
                    SkipWhitespace(json, ref pos);
                    if (pos < json.Length && json[pos] == ',') pos++;
                }
                if (pos < json.Length && json[pos] == '}') pos++;
                return;
            }
            // number/true/false/null
            while (pos < json.Length && !char.IsWhiteSpace(json[pos]) && json[pos] != ',' && json[pos] != ']' && json[pos] != '}')
                pos++;
        }

        private static string Escape(string s)
        {
            if (s == null) return "\"\"";
            var sb = new StringBuilder();
            sb.Append('\"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            sb.Append('\"');
            return sb.ToString();
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
