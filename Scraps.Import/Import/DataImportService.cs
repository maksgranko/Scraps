using Scraps.Database;
using static Scraps.Database.Current;
using Scraps.Localization;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Globalization;

namespace Scraps.Import
{
    /// <summary>
    /// Импорт данных из Excel/CSV в DataTable и проверка соответствия таблице БД.
    /// </summary>
    public static class DataImportService
    {
        /// <summary>
        /// Загрузить первый лист Excel в DataTable.
        /// </summary>
        public static DataTable LoadExcelToDataTable(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Файл не найден.", filePath);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var excel = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = excel.Workbook.Worksheets.First();
                var dt = new DataTable();

                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    dt.Columns.Add(worksheet.Cells[1, col].Text);
                }

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    var newRow = dt.NewRow();
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        newRow[col - 1] = worksheet.Cells[row, col].Text;
                    }
                    dt.Rows.Add(newRow);
                }
                return dt;
            }
        }

        /// <summary>
        /// Загрузить CSV в DataTable.
        /// </summary>
        public static DataTable LoadCsvToDataTable(string filePath, char delimiter = ',')
        {
            return LoadCsvToDataTable(filePath, new[] { delimiter }, autoDetectDelimiter: false);
        }

        /// <summary>
        /// Загрузить CSV в DataTable с поддержкой нескольких разделителей.
        /// </summary>
        public static DataTable LoadCsvToDataTable(string filePath, char[] delimiters, bool autoDetectDelimiter = true)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Файл не найден.", filePath);
            if (delimiters == null || delimiters.Length == 0) throw new ArgumentNullException(nameof(delimiters));

            var dt = new DataTable();
            using (var reader = new StreamReader(filePath))
            {
                string headerLine = reader.ReadLine();
                if (headerLine == null) return dt;

                char activeDelimiter = autoDetectDelimiter ? DetectDelimiter(headerLine, delimiters) : delimiters[0];
                string[] headers = ParseCsvLine(headerLine, activeDelimiter);
                foreach (string header in headers)
                {
                    dt.Columns.Add(header);
                }

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line == null) continue;
                    string[] rows = ParseCsvLine(line, activeDelimiter);
                    dt.Rows.Add(rows);
                }
            }
            return dt;
        }

        /// <summary>
        /// Получить схему таблицы как словарь [ColumnName] = DataType.
        /// </summary>
        private static Dictionary<string, string> GetTableSchemaDict(string tableName)
        {
            var schema = GetTableSchema(tableName);
            var dict = new Dictionary<string, string>();
            foreach (DataRow row in schema.Rows)
            {
                dict[row["ColumnName"].ToString()] = row["DataType"].ToString();
            }
            return dict;
        }

        /// <summary>
        /// Проверить количество колонок относительно схемы таблицы.
        /// </summary>
        public static bool ValidateColumnCount(
            DataTable importData,
            string tableName,
            out int expectedCount,
            out int actualCount)
        {
            if (importData == null) throw new ArgumentNullException(nameof(importData));
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));

            var dbSchema = GetTableSchemaDict(tableName);
            expectedCount = dbSchema.Keys.Count;
            actualCount = importData.Columns.Count;
            return expectedCount == actualCount;
        }

        /// <summary>
        /// Проверить наличие нужных колонок (учитывая переводы, если включено).
        /// </summary>
        public static bool ValidateColumns(
            DataTable importData,
            string tableName,
            out List<string> missingColumns,
            bool allowTranslatedColumns = true)
        {
            if (importData == null) throw new ArgumentNullException(nameof(importData));
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));

            var dbSchema = GetTableSchemaDict(tableName);
            var importColumns = importData.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            missingColumns = new List<string>();

            foreach (var dbColumn in dbSchema.Keys)
            {
                bool found = importColumns.Contains(dbColumn);

                if (!found && allowTranslatedColumns)
                {
                    var translatedName = TranslationManager.TranslateColumnName(tableName, dbColumn);
                    found = importColumns.Contains(translatedName);
                }

                if (!found)
                {
                    missingColumns.Add(dbColumn);
                }
            }

            return missingColumns.Count == 0;
        }

        /// <summary>
        /// Проверить совместимость типов (упрощённая проверка по первой строке).
        /// </summary>
        public static bool ValidateTypes(
            DataTable importData,
            string tableName,
            out List<string> typeErrors,
            bool allowTranslatedColumns = true)
        {
            if (importData == null) throw new ArgumentNullException(nameof(importData));
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));

            var dbSchema = GetTableSchemaDict(tableName);
            typeErrors = new List<string>();

            foreach (DataColumn column in importData.Columns)
            {
                string columnName = column.ColumnName;
                string originalName = columnName;

                if (allowTranslatedColumns)
                {
                    originalName = TranslationManager.UntranslateColumnName(tableName, columnName);
                }

                if (dbSchema.TryGetValue(originalName, out var dbType))
                {
                    for (int i = 0; i < importData.Rows.Count; i++)
                    {
                        var raw = importData.Rows[i][column];
                        if (!IsDbTypeCompatible(raw, dbType))
                        {
                            typeErrors.Add($"{columnName}: ожидается {dbType}, строка {i + 1} содержит '{raw}'");
                        }
                    }
                }
            }

            return typeErrors.Count == 0;
        }

        /// <summary>
        /// Комплексная проверка: колонки, типы, количество (опционально).
        /// </summary>
        public static bool ValidateImport(
            DataTable importData,
            string tableName,
            out List<string> errors,
            bool allowTranslatedColumns = true,
            bool checkCount = true,
            bool checkColumns = true,
            bool checkTypes = true)
        {
            errors = new List<string>();

            if (checkCount && !ValidateColumnCount(importData, tableName, out int expected, out int actual))
            {
                errors.Add($"Несовпадение количества колонок: в таблице {expected}, в файле {actual}");
            }

            if (checkColumns && !ValidateColumns(importData, tableName, out var missing, allowTranslatedColumns))
            {
                if (missing.Count > 0)
                    errors.Add("Отсутствуют обязательные колонки: " + string.Join(", ", missing));
            }

            if (checkTypes && !ValidateTypes(importData, tableName, out var typeErrors, allowTranslatedColumns))
            {
                if (typeErrors.Count > 0)
                    errors.AddRange(typeErrors);
            }

            return errors.Count == 0;
        }

        private static bool IsDbTypeCompatible(object raw, string dbType)
        {
            if (raw == null || raw == DBNull.Value) return true;
            var value = raw.ToString();
            if (string.IsNullOrWhiteSpace(value)) return true;

            switch ((dbType ?? string.Empty).ToLowerInvariant())
            {
                case "int":
                case "smallint":
                case "tinyint":
                    return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                        || int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out _);
                case "bigint":
                    return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                        || long.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out _);
                case "decimal":
                case "numeric":
                case "money":
                case "smallmoney":
                    return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _)
                        || decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out _);
                case "float":
                case "real":
                    return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _)
                        || double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out _);
                case "bit":
                    return bool.TryParse(value, out _)
                        || value == "0"
                        || value == "1";
                case "date":
                case "datetime":
                case "datetime2":
                case "smalldatetime":
                    return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                        || DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out _);
                case "uniqueidentifier":
                    return Guid.TryParse(value, out _);
                default:
                    return true;
            }
        }

        private static char DetectDelimiter(string line, char[] delimiters)
        {
            char best = delimiters[0];
            int bestCount = -1;
            for (int i = 0; i < delimiters.Length; i++)
            {
                var count = ParseCsvLine(line, delimiters[i]).Length;
                if (count > bestCount)
                {
                    bestCount = count;
                    best = delimiters[i];
                }
            }
            return best;
        }

        private static string[] ParseCsvLine(string line, char delimiter)
        {
            if (line == null) return new string[0];
            var result = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == delimiter && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result.ToArray();
        }

        /// <summary>
        /// Импортировать данные в таблицу (с учётом переводов колонок).
        /// </summary>
        public static int ImportToTable(string tableName, DataTable importData)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (importData == null) throw new ArgumentNullException(nameof(importData));

            BulkInsert(tableName, importData);
            return importData.Rows.Count;
        }

    }
}




