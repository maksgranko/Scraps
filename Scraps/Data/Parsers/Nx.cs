using System;
using System.Collections.Generic;
using System.Data;

namespace Scraps.Data.Parsers
{
    /// <summary>
    /// Парсер Nx-форматов (Nx1/Nx2) из текста и <see cref="DataTable"/>.
    /// </summary>
    public static class Nx
    {
        /// <summary>
        /// Парсить таблицу формата Nx2 из текста в словарь.
        /// По умолчанию ключ/значение парсятся как int/string и разделяются первым пробельным блоком.
        /// </summary>
        public static Dictionary<int, string> ParseNx2ToDictionary(string input)
        {
            return ParseNx2ToDictionary(
                input,
                s => int.Parse(s),
                s => s,
                delimiter: null,
                trim: true,
                skipInvalidLines: false);
        }

        /// <summary>
        /// Парсить таблицу формата Nx2 из текста в словарь int/string c явными разделителями.
        /// </summary>
        public static Dictionary<int, string> ParseNx2ToDictionary(string input, string columnSeparator, string rowSeparator)
        {
            return ParseNx2ToDictionary(
                input,
                s => int.Parse(s),
                s => s,
                columnSeparator,
                rowSeparator,
                trim: true,
                skipInvalidLines: false);
        }

        /// <summary>
        /// Парсить моно-таблицу формата Nx1 (одна колонка на строку) в список строк.
        /// </summary>
        public static List<string> ParseNx1ToList(string input, string rowSeparator = null, bool trim = true, bool skipEmpty = true)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var rows = SplitRows(input, rowSeparator);
            var result = new List<string>(rows.Length);

            for (int i = 0; i < rows.Length; i++)
            {
                var value = rows[i];
                if (trim) value = value?.Trim();
                if (skipEmpty && string.IsNullOrWhiteSpace(value))
                    continue;
                result.Add(value ?? string.Empty);
            }

            return result;
        }

        /// <summary>
        /// Парсить таблицу формата Nx2 из текста в словарь c пользовательскими преобразователями.
        /// </summary>
        public static Dictionary<TKey, TValue> ParseNx2ToDictionary<TKey, TValue>(
            string input,
            Func<string, TKey> keyParser,
            Func<string, TValue> valueParser,
            char? delimiter = null,
            bool trim = true,
            bool skipInvalidLines = false)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (keyParser == null) throw new ArgumentNullException(nameof(keyParser));
            if (valueParser == null) throw new ArgumentNullException(nameof(valueParser));

            var dict = new Dictionary<TKey, TValue>();
            var lines = SplitLines(input);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (!TrySplitNx2Line(line, delimiter, out var keyRaw, out var valueRaw, trim))
                {
                    if (skipInvalidLines)
                        continue;
                    throw new FormatException($"Некорректная строка Nx2 на позиции {i + 1}: '{line}'");
                }

                try
                {
                    var key = keyParser(keyRaw);
                    var value = valueParser(valueRaw);

                    if (dict.ContainsKey(key))
                    {
                        if (skipInvalidLines)
                            continue;
                        throw new ArgumentException($"Обнаружен дублирующийся ключ '{key}' в строке {i + 1}.");
                    }

                    dict.Add(key, value);
                }
                catch when (skipInvalidLines)
                {
                    // ignore invalid line
                }
            }

            return dict;
        }

        /// <summary>
        /// Парсить таблицу формата Nx2 из текста в словарь c пользовательскими преобразователями
        /// и явными разделителями колонок/строк.
        /// </summary>
        public static Dictionary<TKey, TValue> ParseNx2ToDictionary<TKey, TValue>(
            string input,
            Func<string, TKey> keyParser,
            Func<string, TValue> valueParser,
            string columnSeparator,
            string rowSeparator,
            bool trim = true,
            bool skipInvalidLines = false)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (keyParser == null) throw new ArgumentNullException(nameof(keyParser));
            if (valueParser == null) throw new ArgumentNullException(nameof(valueParser));

            var dict = new Dictionary<TKey, TValue>();
            var lines = SplitRows(input, rowSeparator);
            bool useWhitespaceSeparator = string.IsNullOrEmpty(columnSeparator);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                bool splitOk = useWhitespaceSeparator
                    ? TrySplitNx2Line(line, delimiter: null, out var keyRaw, out var valueRaw, trim)
                    : TrySplitNx2Line(line, columnSeparator, out keyRaw, out valueRaw, trim);

                if (!splitOk)
                {
                    if (skipInvalidLines)
                        continue;
                    throw new FormatException($"Некорректная строка Nx2 на позиции {i + 1}: '{line}'");
                }

                try
                {
                    var key = keyParser(keyRaw);
                    var value = valueParser(valueRaw);

                    if (dict.ContainsKey(key))
                    {
                        if (skipInvalidLines)
                            continue;
                        throw new ArgumentException($"Обнаружен дублирующийся ключ '{key}' в строке {i + 1}.");
                    }

                    dict.Add(key, value);
                }
                catch when (skipInvalidLines)
                {
                    // ignore invalid line
                }
            }

            return dict;
        }

        /// <summary>
        /// Преобразовать DataTable формата Nx2 в словарь int/string.
        /// </summary>
        public static Dictionary<int, string> ParseNx2ToDictionary(DataTable table, int keyColumnIndex = 0, int valueColumnIndex = 1)
        {
            return ParseNx2ToDictionary(
                table,
                obj => Convert.ToInt32(obj),
                obj => obj?.ToString() ?? string.Empty,
                keyColumnIndex,
                valueColumnIndex,
                skipInvalidRows: false);
        }

        /// <summary>
        /// Преобразовать DataTable формата Nx2 в словарь c пользовательскими преобразователями.
        /// </summary>
        public static Dictionary<TKey, TValue> ParseNx2ToDictionary<TKey, TValue>(
            DataTable table,
            Func<object, TKey> keyParser,
            Func<object, TValue> valueParser,
            int keyColumnIndex = 0,
            int valueColumnIndex = 1,
            bool skipInvalidRows = false)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (keyParser == null) throw new ArgumentNullException(nameof(keyParser));
            if (valueParser == null) throw new ArgumentNullException(nameof(valueParser));
            if (table.Columns.Count < 2)
                throw new ArgumentException("Для Nx2-парсинга таблица должна содержать минимум 2 колонки.", nameof(table));
            if (keyColumnIndex < 0 || keyColumnIndex >= table.Columns.Count)
                throw new ArgumentOutOfRangeException(nameof(keyColumnIndex));
            if (valueColumnIndex < 0 || valueColumnIndex >= table.Columns.Count)
                throw new ArgumentOutOfRangeException(nameof(valueColumnIndex));

            var dict = new Dictionary<TKey, TValue>();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
                try
                {
                    var key = keyParser(row[keyColumnIndex]);
                    var value = valueParser(row[valueColumnIndex]);

                    if (dict.ContainsKey(key))
                    {
                        if (skipInvalidRows)
                            continue;
                        throw new ArgumentException($"Обнаружен дублирующийся ключ '{key}' в строке {i + 1}.");
                    }

                    dict.Add(key, value);
                }
                catch when (skipInvalidRows)
                {
                    // ignore invalid row
                }
            }

            return dict;
        }

        /// <summary>
        /// Преобразовать DataTable формата Nx1 (одна колонка) в список строк.
        /// </summary>
        public static List<string> ParseNx1ToList(DataTable table, int valueColumnIndex = 0, bool trim = true, bool skipEmpty = true)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (table.Columns.Count < 1)
                throw new ArgumentException("Для Nx1-парсинга таблица должна содержать минимум 1 колонку.", nameof(table));
            if (valueColumnIndex < 0 || valueColumnIndex >= table.Columns.Count)
                throw new ArgumentOutOfRangeException(nameof(valueColumnIndex));

            var result = new List<string>(table.Rows.Count);
            for (int i = 0; i < table.Rows.Count; i++)
            {
                var raw = table.Rows[i][valueColumnIndex];
                var value = raw == null || raw == DBNull.Value ? string.Empty : raw.ToString();
                if (trim) value = value.Trim();
                if (skipEmpty && string.IsNullOrWhiteSpace(value))
                    continue;
                result.Add(value);
            }

            return result;
        }

        private static string[] SplitLines(string input)
        {
            return input.Replace("\r\n", "\n").Replace("\r", "\n").Split(new[] { '\n' }, StringSplitOptions.None);
        }

        private static string[] SplitRows(string input, string rowSeparator)
        {
            if (string.IsNullOrEmpty(rowSeparator))
                return SplitLines(input);

            return input.Split(new[] { rowSeparator }, StringSplitOptions.None);
        }

        private static bool TrySplitNx2Line(string line, char? delimiter, out string keyRaw, out string valueRaw, bool trim)
        {
            keyRaw = null;
            valueRaw = null;

            if (string.IsNullOrWhiteSpace(line))
                return false;

            int separatorIndex;
            if (delimiter.HasValue)
            {
                separatorIndex = line.IndexOf(delimiter.Value);
                if (separatorIndex < 0)
                    return false;
            }
            else
            {
                separatorIndex = -1;
                for (int i = 0; i < line.Length; i++)
                {
                    if (char.IsWhiteSpace(line[i]))
                    {
                        separatorIndex = i;
                        break;
                    }
                }

                if (separatorIndex < 0)
                    return false;
            }

            keyRaw = line.Substring(0, separatorIndex);
            valueRaw = line.Substring(separatorIndex + 1);

            if (!delimiter.HasValue)
                valueRaw = valueRaw.TrimStart();

            if (trim)
            {
                keyRaw = keyRaw.Trim();
                valueRaw = valueRaw.Trim();
            }

            return !string.IsNullOrWhiteSpace(keyRaw);
        }

        private static bool TrySplitNx2Line(string line, string separator, out string keyRaw, out string valueRaw, bool trim)
        {
            keyRaw = null;
            valueRaw = null;

            if (string.IsNullOrWhiteSpace(line))
                return false;
            if (string.IsNullOrEmpty(separator))
                return TrySplitNx2Line(line, delimiter: null, out keyRaw, out valueRaw, trim);

            int separatorIndex = line.IndexOf(separator, StringComparison.Ordinal);
            if (separatorIndex < 0)
                return false;

            keyRaw = line.Substring(0, separatorIndex);
            valueRaw = line.Substring(separatorIndex + separator.Length);

            if (trim)
            {
                keyRaw = keyRaw.Trim();
                valueRaw = valueRaw.Trim();
            }

            return !string.IsNullOrWhiteSpace(keyRaw);
        }
    }
}


