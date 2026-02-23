using Scraps.Databases;
using Scraps.Localization;
using Scraps.Security;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

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
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Файл не найден.", filePath);

            var dt = new DataTable();
            using (var reader = new StreamReader(filePath))
            {
                string headerLine = reader.ReadLine();
                if (headerLine == null) return dt;

                string[] headers = headerLine.Split(delimiter);
                foreach (string header in headers)
                {
                    dt.Columns.Add(header);
                }

                while (!reader.EndOfStream)
                {
                    string[] rows = reader.ReadLine().Split(delimiter);
                    dt.Rows.Add(rows);
                }
            }
            return dt;
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

            var dbSchema = MSSQL.GetTableSchema(tableName);
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

            var dbSchema = MSSQL.GetTableSchema(tableName);
            var importColumns = importData.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            missingColumns = new List<string>();

            foreach (var dbColumn in dbSchema.Keys)
            {
                bool found = importColumns.Contains(dbColumn);

                if (!found && allowTranslatedColumns &&
                    TranslationManager.ColumnTranslations.TryGetValue(tableName, out var translations) &&
                    translations.TryGetValue(dbColumn, out var translatedName))
                {
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

            var dbSchema = MSSQL.GetTableSchema(tableName);
            typeErrors = new List<string>();

            foreach (DataColumn column in importData.Columns)
            {
                string columnName = column.ColumnName;
                string originalName = columnName;

                if (allowTranslatedColumns &&
                    TranslationManager.ColumnTranslations.TryGetValue(tableName, out var translations))
                {
                    var reverse = translations.ToDictionary(x => x.Value, x => x.Key, StringComparer.OrdinalIgnoreCase);
                    if (reverse.TryGetValue(columnName, out var origName))
                    {
                        originalName = origName;
                    }
                }

                if (dbSchema.TryGetValue(originalName, out var dbType))
                {
                    if ((dbType == "int" || dbType == "decimal") &&
                        importData.Rows.Count > 0 &&
                        !double.TryParse(importData.Rows[0][column].ToString(), out _))
                    {
                        typeErrors.Add($"{columnName}: ожидается {dbType}, получено string");
                    }
                }
            }

            return typeErrors.Count == 0;
        }

        /// <summary>
        /// Проверить доступ на импорт данных по роли.
        /// </summary>
        public static bool ValidateImportAccess(string roleName, string tableName, PermissionFlags required, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(roleName)) return true;

            if (!RoleManager.CheckAccess(roleName, tableName, required))
            {
                error = $"Нет прав на импорт ({required}) для роли '{roleName}' в таблицу '{tableName}'.";
                return false;
            }
            return true;
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

        /// <summary>
        /// Импортировать данные в таблицу (с учётом переводов колонок).
        /// </summary>
        public static int ImportToTable(string tableName, DataTable importData)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (importData == null) throw new ArgumentNullException(nameof(importData));

            return MSSQL.BulkInsert(tableName, importData);
        }

        /// <summary>
        /// Безопасный импорт с проверками и правами доступа.
        /// </summary>
        public static int ImportToTableSafe(
            string tableName,
            DataTable importData,
            string roleName = null,
            PermissionFlags required = PermissionFlags.Import | PermissionFlags.Write,
            bool allowTranslatedColumns = true)
        {
            if (!ValidateImportAccess(roleName, tableName, required, out var accessError))
                throw new UnauthorizedAccessException(accessError);

            if (!ValidateImport(importData, tableName, out var errors, allowTranslatedColumns))
                throw new Exception("Ошибка импорта: " + string.Join("; ", errors));

            return ImportToTable(tableName, importData);
        }
    }
}
