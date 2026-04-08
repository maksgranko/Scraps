using System;
using System.Collections.Generic;
using System.Data;
using Scraps.Data.Parsers;

namespace Scraps.Data.DataTables
{
    /// <summary>
    /// Фасад совместимости для парсеров из Scraps.Data.Parsers.
    /// </summary>
    public static class Parser
    {
        /// <summary>
        /// Парсить разделённый текст (DSV) в <see cref="DataTable"/> простым парсером без кавычек.
        /// </summary>
        public static DataTable ParseDelimited(string input, char delimiter = ',', bool hasHeader = true, bool trim = true)
        {
            return DelimitedTable.Parse(input, delimiter, hasHeader, trim);
        }

        /// <summary>
        /// Парсить CSV/DSV-текст в <see cref="DataTable"/> с поддержкой кавычек и пользовательского разделителя строк.
        /// </summary>
        public static DataTable ParseCsv(
            string input,
            char delimiter = ',',
            string rowSeparator = null,
            bool hasHeader = true,
            bool trim = true)
        {
            return Csv.Parse(input, delimiter, rowSeparator, hasHeader, trim);
        }

        /// <summary>
        /// Парсить Nx2-текст в словарь <c>int -&gt; string</c>.
        /// </summary>
        public static Dictionary<int, string> ParseNx2ToDictionary(string input)
        {
            return Nx.ParseNx2ToDictionary(input);
        }

        /// <summary>
        /// Парсить Nx2-текст в словарь <c>int -&gt; string</c> с явными разделителями колонок и строк.
        /// </summary>
        public static Dictionary<int, string> ParseNx2ToDictionary(string input, string columnSeparator, string rowSeparator)
        {
            return Nx.ParseNx2ToDictionary(input, columnSeparator, rowSeparator);
        }

        /// <summary>
        /// Парсить Nx1-текст (одна колонка на строку) в список строк.
        /// </summary>
        public static List<string> ParseNx1ToList(string input, string rowSeparator = null, bool trim = true, bool skipEmpty = true)
        {
            return Nx.ParseNx1ToList(input, rowSeparator, trim, skipEmpty);
        }

        /// <summary>
        /// Парсить Nx2-текст в словарь пользовательских типов с функциями преобразования ключа и значения.
        /// </summary>
        public static Dictionary<TKey, TValue> ParseNx2ToDictionary<TKey, TValue>(
            string input,
            Func<string, TKey> keyParser,
            Func<string, TValue> valueParser,
            char? delimiter = null,
            bool trim = true,
            bool skipInvalidLines = false)
        {
            return Nx.ParseNx2ToDictionary(input, keyParser, valueParser, delimiter, trim, skipInvalidLines);
        }

        /// <summary>
        /// Парсить Nx2-текст в словарь пользовательских типов с явными разделителями колонок и строк.
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
            return Nx.ParseNx2ToDictionary(
                input,
                keyParser,
                valueParser,
                columnSeparator,
                rowSeparator,
                trim,
                skipInvalidLines);
        }

        /// <summary>
        /// Преобразовать <see cref="DataTable"/> формата Nx2 в словарь <c>int -&gt; string</c>.
        /// </summary>
        public static Dictionary<int, string> ParseNx2ToDictionary(DataTable table, int keyColumnIndex = 0, int valueColumnIndex = 1)
        {
            return Nx.ParseNx2ToDictionary(table, keyColumnIndex, valueColumnIndex);
        }

        /// <summary>
        /// Преобразовать <see cref="DataTable"/> формата Nx2 в словарь пользовательских типов.
        /// </summary>
        public static Dictionary<TKey, TValue> ParseNx2ToDictionary<TKey, TValue>(
            DataTable table,
            Func<object, TKey> keyParser,
            Func<object, TValue> valueParser,
            int keyColumnIndex = 0,
            int valueColumnIndex = 1,
            bool skipInvalidRows = false)
        {
            return Nx.ParseNx2ToDictionary(
                table,
                keyParser,
                valueParser,
                keyColumnIndex,
                valueColumnIndex,
                skipInvalidRows);
        }

        /// <summary>
        /// Преобразовать <see cref="DataTable"/> формата Nx1 в список строк.
        /// </summary>
        public static List<string> ParseNx1ToList(DataTable table, int valueColumnIndex = 0, bool trim = true, bool skipEmpty = true)
        {
            return Nx.ParseNx1ToList(table, valueColumnIndex, trim, skipEmpty);
        }
    }
}


