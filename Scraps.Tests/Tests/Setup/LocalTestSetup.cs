using Scraps.Configs;
using Scraps.Database;
using Scraps.Database.Local;
using Scraps.Databases.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Scraps.Tests.Setup
{
    public class LocalTestSetup : ITestDatabaseSetup
    {
        private string _dbPath;

        public string ProviderName => "LocalFiles";
        public bool IsAvailable => true;

        public string CreateDatabase()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "Scraps_Test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dbPath);

            ScrapsConfig.LocalDataPath = _dbPath;
            ScrapsConfig.DatabaseProvider = DatabaseProvider.LocalFiles;
            ScrapsConfig.DatabaseName = "TestDb";
            ScrapsConfig.ConnectionString = _dbPath;

            DatabaseProviderFactory.Reset();
            DatabaseProviderFactory.Register(DatabaseProvider.LocalFiles, () => new LocalDatabase());

            return _dbPath;
        }

        public void Initialize(DatabaseGenerationMode mode)
        {
            var db = new LocalDatabase();
            db.Initialize(DatabaseGenerationOptions.ForDatabase("TestDb", mode));
        }

        public void ExecuteNonQuery(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return;
            var trimmed = sql.Trim();

            if (trimmed.StartsWith("IF OBJECT_ID", StringComparison.OrdinalIgnoreCase))
            {
                var createMatch = Regex.Match(trimmed, @"CREATE\s+TABLE\s+\[(?<table>[^\]]+)\]\s*\(", RegexOptions.IgnoreCase);
                if (createMatch.Success)
                {
                    var tableName = createMatch.Groups["table"].Value;
                    var cols = ExtractParenContent(trimmed, createMatch.Index + createMatch.Length - 1);
                    if (cols != null)
                        CreateTableFromSql(tableName, cols);
                }
                return;
            }

            if (trimmed.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(trimmed, @"CREATE\s+TABLE\s+\[(?<table>[^\]]+)\]\s*\(", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var tableName = match.Groups["table"].Value;
                    var cols = ExtractParenContent(trimmed, match.Index + match.Length - 1);
                    if (cols != null)
                        CreateTableFromSql(tableName, cols);
                    return;
                }
            }

            if (trimmed.StartsWith("INSERT INTO", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(trimmed,
                    @"INSERT\s+INTO\s+\[(?<table>[^\]]+)\]\s*\((?<columns>[^)]+)\)\s*VALUES\s*\(",
                    RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var tableName = match.Groups["table"].Value;
                    var columns = match.Groups["columns"].Value;
                    var values = ExtractParenContent(trimmed, match.Index + match.Length - 1);
                    if (values != null)
                    {
                        InsertInto(tableName, columns, values);
                        return;
                    }
                }
            }

            if (trimmed.StartsWith("ALTER DATABASE", StringComparison.OrdinalIgnoreCase))
                return;

            throw new NotSupportedException($"SQL not supported in LocalFiles: {sql}");
        }

        private static string ExtractParenContent(string s, int openParenIndex)
        {
            int depth = 0;
            int start = -1;
            for (int i = openParenIndex; i < s.Length; i++)
            {
                if (s[i] == '(')
                {
                    if (depth == 0) start = i + 1;
                    depth++;
                }
                else if (s[i] == ')')
                {
                    depth--;
                    if (depth == 0)
                        return s.Substring(start, i - start);
                }
            }
            return null;
        }

        public void DropDatabase(string databaseName, string connectionString)
        {
            try
            {
                if (!string.IsNullOrEmpty(_dbPath) && Directory.Exists(_dbPath))
                    Directory.Delete(_dbPath, true);
            }
            catch { }
        }

        public string BuildConnectionString(string databaseName)
        {
            return _dbPath ?? ScrapsConfig.LocalDataPath;
        }

        public IEnumerable<string> FindDatabasesByPrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) yield break;
            var parent = Path.GetTempPath();
            foreach (var dir in Directory.GetDirectories(parent, prefix + "*"))
                yield return dir;
        }

        public void CleanupDatabases(IEnumerable<string> databaseNames)
        {
            foreach (var name in databaseNames ?? Enumerable.Empty<string>())
            {
                try
                {
                    if (Directory.Exists(name))
                        Directory.Delete(name, true);
                }
                catch { }
            }
        }

        private void CreateTableFromSql(string tableName, string columnsDef)
        {
            var schema = new LocalDatabaseSchema();
            var columns = new Dictionary<string, string>();
            var parts = columnsDef.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var colMatch = Regex.Match(part.Trim(), @"\[(?<name>[^\]]+)\]\s+(?<type>\w+)", RegexOptions.IgnoreCase);
                if (colMatch.Success)
                {
                    var colName = colMatch.Groups["name"].Value;
                    var sqlType = colMatch.Groups["type"].Value.ToLowerInvariant();
                    columns[colName] = MapSqlType(sqlType);
                }
            }
            if (!schema.TableExists(tableName))
                schema.CreateTable(tableName, columns);
        }

        private void InsertInto(string tableName, string columnsStr, string valuesStr)
        {
            var data = new LocalDatabaseData();
            var dt = data.GetTableData(tableName);
            var columns = columnsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim().Trim('[', ']'))
                .ToArray();
            var values = ParseValues(valuesStr);
            var row = dt.NewRow();
            for (int i = 0; i < columns.Length && i < values.Length; i++)
            {
                if (dt.Columns.Contains(columns[i]))
                    row[columns[i]] = ConvertVal(values[i], dt.Columns[columns[i]].DataType);
            }
            dt.Rows.Add(row);
            data.ApplyTableChanges(tableName, dt);
        }

        private static string[] ParseValues(string valuesStr)
        {
            var result = new List<string>();
            var sb = new System.Text.StringBuilder();
            bool inString = false;
            for (int i = 0; i < valuesStr.Length; i++)
            {
                char c = valuesStr[i];
                if (c == '\'')
                {
                    inString = !inString;
                    continue;
                }
                if (c == ',' && !inString)
                {
                    result.Add(sb.ToString().Trim());
                    sb.Clear();
                    continue;
                }
                sb.Append(c);
            }
            if (sb.Length > 0)
                result.Add(sb.ToString().Trim());
            return result.ToArray();
        }

        private static object ConvertVal(string value, Type targetType)
        {
            if (value.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                return DBNull.Value;
            if (targetType == typeof(int)) return int.Parse(value);
            if (targetType == typeof(string)) return value;
            if (targetType == typeof(bool)) return bool.Parse(value);
            if (targetType == typeof(DateTime)) return DateTime.Parse(value);
            if (targetType == typeof(double)) return double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            if (targetType == typeof(decimal)) return decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            return value;
        }

        private static string MapSqlType(string sqlType)
        {
            switch (sqlType)
            {
                case "int": case "bigint": case "smallint": case "tinyint": return "Int32";
                case "nvarchar": case "varchar": case "nchar": case "char": case "text": case "ntext": return "String";
                case "bit": return "Boolean";
                case "datetime": case "datetime2": case "date": case "smalldatetime": return "DateTime";
                case "float": case "real": return "Double";
                case "decimal": case "numeric": case "money": case "smallmoney": return "Decimal";
                case "uniqueidentifier": return "Guid";
                default: return "String";
            }
        }
    }
}
