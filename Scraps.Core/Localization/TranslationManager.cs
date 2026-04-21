using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Scraps.Data.DataTables;

namespace Scraps.Localization
{
    /// <summary>
    /// Глобальный модуль переводов на базе одного словаря key -> value.
    /// Формат ключей не навязывается: структура задаётся пользователем.
    /// </summary>
    public static class TranslationManager
    {
        private static readonly object Sync = new object();
        private const string ColumnSeparator = "::";

        /// <summary>
        /// Общие переводы: key -> translated value.
        /// </summary>
        public static Dictionary<string, string> Translations { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Построить ключ перевода колонки в общем словаре.
        /// </summary>
        public static string ColumnKey(string tableName, string columnName)
        {
            return string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName)
                ? string.Empty
                : tableName.Trim() + ColumnSeparator + columnName.Trim();
        }

        /// <summary>
        /// Пакетная загрузка переводов. Может дополнять существующие значения или полностью очищать перед загрузкой.
        /// </summary>
        public static void Load(IDictionary<string, string> translations, bool clearBeforeLoad = false)
        {
            lock (Sync)
            {
                if (clearBeforeLoad)
                    Translations.Clear();

                if (translations == null)
                    return;

                foreach (var kv in translations)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key))
                        continue;

                    Translations[kv.Key] = kv.Value;
                }
            }
        }

        /// <summary>
        /// Загрузить переводы из CSV-файла (2 колонки: key,value).
        /// </summary>
        public static void Load(
            string filePath,
            char delimiter = ';',
            bool hasHeader = true,
            bool clearBeforeLoad = false)
        {
            Load(
                filePath,
                delimiter: delimiter,
                rowSeparator: null,
                hasHeader: hasHeader,
                clearBeforeLoad: clearBeforeLoad);
        }

        /// <summary>
        /// Загрузить переводы из текстового файла в формате CSV.
        /// </summary>
        public static void Load(
            string filePath,
            char delimiter,
            string rowSeparator,
            bool hasHeader,
            bool clearBeforeLoad = false)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл переводов не найден.", filePath);

            var text = File.ReadAllText(filePath);
            var table = Parser.ParseCsv(
                text,
                delimiter: delimiter,
                rowSeparator: rowSeparator,
                hasHeader: hasHeader,
                trim: true);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (table.Columns.Count < 2)
            {
                Load(dict, clearBeforeLoad);
                return;
            }

            for (int i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
                if (row == null)
                    continue;

                var key = row[0]?.ToString();
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                var value = row[1]?.ToString() ?? string.Empty;
                dict[key] = value;
            }

            Load(dict, clearBeforeLoad);
        }

        /// <summary>
        /// Атомарная полная замена словаря переводов.
        /// </summary>
        public static void Replace(IDictionary<string, string> translations)
        {
            Load(translations, clearBeforeLoad: true);
        }

        /// <summary>
        /// Перевести строку по ключу из словаря Translations.
        /// </summary>
        public static string Translate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            lock (Sync)
            {
                return Translations.TryGetValue(value, out var translation) ? translation : value;
            }
        }

        /// <summary>
        /// Перевести массив строк.
        /// </summary>
        public static string[] Translate(string[] values)
        {
            if (values == null) return Array.Empty<string>();
            var result = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = Translate(values[i]);
            }
            return result;
        }

        /// <summary>
        /// Перевести список строк.
        /// </summary>
        public static List<string> Translate(IEnumerable<string> values)
        {
            if (values == null) return new List<string>();
            var result = new List<string>();
            foreach (var value in values)
            {
                result.Add(Translate(value));
            }
            return result;
        }

        /// <summary>
        /// Перевести названия колонок в DataTable (in-place).
        /// </summary>
        public static DataTable Translate(DataTable dt, string tableName)
        {
            if (dt == null) throw new ArgumentNullException(nameof(dt));
            if (string.IsNullOrWhiteSpace(tableName)) return dt;

            foreach (DataColumn column in dt.Columns)
            {
                if (column == null || string.IsNullOrEmpty(column.ColumnName))
                    continue;

                column.ColumnName = TranslateColumnName(tableName, column.ColumnName);
            }

            return dt;
        }

        /// <summary>
        /// Вернуть исходный ключ по переведенной строке.
        /// </summary>
        public static string Untranslate(string translated)
        {
            if (string.IsNullOrWhiteSpace(translated)) return translated;

            lock (Sync)
            {
                var match = Translations.FirstOrDefault(x =>
                    string.Equals(x.Value, translated, StringComparison.OrdinalIgnoreCase));
                return string.IsNullOrEmpty(match.Key) ? translated : match.Key;
            }
        }

        /// <summary>
        /// Вернуть исходные значения для массива переводов.
        /// </summary>
        public static string[] Untranslate(string[] translatedValues)
        {
            if (translatedValues == null) return Array.Empty<string>();
            var result = new string[translatedValues.Length];
            for (int i = 0; i < translatedValues.Length; i++)
            {
                result[i] = Untranslate(translatedValues[i]);
            }
            return result;
        }

        /// <summary>
        /// Вернуть исходные значения для списка переводов.
        /// </summary>
        public static List<string> Untranslate(IEnumerable<string> translatedValues)
        {
            if (translatedValues == null) return new List<string>();
            var result = new List<string>();
            foreach (var value in translatedValues)
            {
                result.Add(Untranslate(value));
            }
            return result;
        }

        /// <summary>
        /// Вернуть оригинальные названия колонок в DataTable (in-place).
        /// </summary>
        public static DataTable Untranslate(DataTable dt, string tableName)
        {
            if (dt == null) throw new ArgumentNullException(nameof(dt));
            if (string.IsNullOrWhiteSpace(tableName)) return dt;

            foreach (DataColumn column in dt.Columns)
            {
                if (column == null || string.IsNullOrEmpty(column.ColumnName))
                    continue;

                column.ColumnName = UntranslateColumnName(tableName, column.ColumnName);
            }

            return dt;
        }

        /// <summary>
        /// Перевести имя колонки.
        /// </summary>
        public static string TranslateColumnName(string tableName, string columnName)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName))
                return columnName;

            var key = ColumnKey(tableName, columnName);
            lock (Sync)
            {
                return Translations.TryGetValue(key, out var translated) ? translated : columnName;
            }
        }

        /// <summary>
        /// Вернуть оригинальное имя колонки по переводу.
        /// </summary>
        public static string UntranslateColumnName(string tableName, string translatedColumn)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(translatedColumn))
                return translatedColumn;

            var prefix = tableName.Trim() + ColumnSeparator;
            lock (Sync)
            {
                foreach (var kv in Translations)
                {
                    if (!kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!string.Equals(kv.Value, translatedColumn, StringComparison.OrdinalIgnoreCase))
                        continue;

                    return kv.Key.Substring(prefix.Length);
                }
            }

            return translatedColumn;
        }

    }
}
