using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Scraps.Localization
{
    /// <summary>
    /// Глобальный модуль переводов для названий таблиц и колонок.
    /// </summary>
    public static class TranslationManager
    {
        /// <summary>
        /// Переводы колонок: TableName -> (ColumnName -> Translation).
        /// </summary>
        public static Dictionary<string, Dictionary<string, string>> ColumnTranslations { get; } =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Переводы таблиц: TableName -> Translation.
        /// </summary>
        public static Dictionary<string, string> TableTranslations { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Перевести название таблицы.
        /// </summary>
        public static string TranslateTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) return tableName;
            return TableTranslations.TryGetValue(tableName, out var translation) ? translation : tableName;
        }

        /// <summary>
        /// Перевести список названий таблиц.
        /// </summary>
        public static string[] TranslateTableList(string[] tableNames)
        {
            if (tableNames == null) return Array.Empty<string>();
            var result = new string[tableNames.Length];
            for (int i = 0; i < tableNames.Length; i++)
            {
                result[i] = TranslateTableName(tableNames[i]);
            }
            return result;
        }

        /// <summary>
        /// Вернуть оригинальное название таблицы по переводу.
        /// </summary>
        public static string UntranslateTableName(string translated)
        {
            if (string.IsNullOrWhiteSpace(translated)) return translated;
            var match = TableTranslations.FirstOrDefault(x =>
                string.Equals(x.Value, translated, StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrEmpty(match.Key) ? translated : match.Key;
        }

        /// <summary>
        /// Вернуть оригинальные названия таблиц по списку переводов.
        /// </summary>
        public static string[] UntranslateTableList(string[] translatedTableNames)
        {
            if (translatedTableNames == null) return Array.Empty<string>();
            var result = new string[translatedTableNames.Length];
            for (int i = 0; i < translatedTableNames.Length; i++)
            {
                result[i] = UntranslateTableName(translatedTableNames[i]);
            }
            return result;
        }

        /// <summary>
        /// Перевести имя колонки.
        /// </summary>
        public static string TranslateColumnName(string tableName, string columnName)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName)) return columnName;
            if (ColumnTranslations.TryGetValue(tableName, out var map) && map.TryGetValue(columnName, out var translated))
                return translated;
            return columnName;
        }

        /// <summary>
        /// Вернуть оригинальное имя колонки по переводу.
        /// </summary>
        public static string UntranslateColumnName(string tableName, string translatedColumn)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(translatedColumn)) return translatedColumn;
            if (ColumnTranslations.TryGetValue(tableName, out var map))
            {
                var match = map.FirstOrDefault(x =>
                    string.Equals(x.Value, translatedColumn, StringComparison.OrdinalIgnoreCase));
                return string.IsNullOrEmpty(match.Key) ? translatedColumn : match.Key;
            }
            return translatedColumn;
        }

        /// <summary>
        /// Перевести названия колонок в DataTable (in-place).
        /// </summary>
        public static DataTable TranslateDataTable(DataTable dt, string tableName)
        {
            if (dt == null) throw new ArgumentNullException(nameof(dt));
            if (string.IsNullOrWhiteSpace(tableName)) return dt;

            if (!ColumnTranslations.TryGetValue(tableName, out var translations))
                return dt;

            foreach (DataColumn column in dt.Columns)
            {
                if (column == null || string.IsNullOrEmpty(column.ColumnName))
                    continue;

                if (translations.TryGetValue(column.ColumnName, out var translatedName))
                {
                    column.ColumnName = translatedName;
                }
            }

            return dt;
        }

        /// <summary>
        /// Вернуть оригинальные названия колонок в DataTable (in-place).
        /// </summary>
        public static DataTable UntranslateDataTable(DataTable dt, string tableName)
        {
            if (dt == null) throw new ArgumentNullException(nameof(dt));
            if (string.IsNullOrWhiteSpace(tableName)) return dt;

            if (!ColumnTranslations.TryGetValue(tableName, out var translations))
                return dt;

            var reverse = translations.ToDictionary(x => x.Value, x => x.Key, StringComparer.OrdinalIgnoreCase);
            foreach (DataColumn column in dt.Columns)
            {
                if (column == null || string.IsNullOrEmpty(column.ColumnName))
                    continue;

                if (reverse.TryGetValue(column.ColumnName, out var originalName))
                {
                    column.ColumnName = originalName;
                }
            }

            return dt;
        }
    }
}




