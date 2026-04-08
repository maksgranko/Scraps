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
        public static DataTable ParseDelimited(string input, char delimiter = ',', bool hasHeader = true, bool trim = true)
        {
            return DelimitedTable.Parse(input, delimiter, hasHeader, trim);
        }

        public static DataTable ParseCsv(
            string input,
            char delimiter = ',',
            string rowSeparator = null,
            bool hasHeader = true,
            bool trim = true)
        {
            return Csv.Parse(input, delimiter, rowSeparator, hasHeader, trim);
        }

        public static Dictionary<int, string> ParseNx2ToDictionary(string input)
        {
            return Nx.ParseNx2ToDictionary(input);
        }

        public static Dictionary<int, string> ParseNx2ToDictionary(string input, string columnSeparator, string rowSeparator)
        {
            return Nx.ParseNx2ToDictionary(input, columnSeparator, rowSeparator);
        }

        public static List<string> ParseNx1ToList(string input, string rowSeparator = null, bool trim = true, bool skipEmpty = true)
        {
            return Nx.ParseNx1ToList(input, rowSeparator, trim, skipEmpty);
        }

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

        public static Dictionary<int, string> ParseNx2ToDictionary(DataTable table, int keyColumnIndex = 0, int valueColumnIndex = 1)
        {
            return Nx.ParseNx2ToDictionary(table, keyColumnIndex, valueColumnIndex);
        }

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

        public static List<string> ParseNx1ToList(DataTable table, int valueColumnIndex = 0, bool trim = true, bool skipEmpty = true)
        {
            return Nx.ParseNx1ToList(table, valueColumnIndex, trim, skipEmpty);
        }
    }
}


