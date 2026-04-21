using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Scraps.Data.Parsers.Internal;

namespace Scraps.Data.Parsers
{
    /// <summary>
    /// CSV/DSV парсер с поддержкой кавычек, экранирования и настраиваемых разделителей.
    /// </summary>
    public static class Csv
    {
        /// <summary>
        /// Парсить CSV/DSV-текст в DataTable с поддержкой кавычек, экранирования двойными кавычками
        /// и пользовательского разделителя строк.
        /// </summary>
        public static DataTable Parse(
            string input,
            char delimiter = ',',
            string rowSeparator = null,
            bool hasHeader = true,
            bool trim = true)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var rows = ParseRows(input, delimiter, rowSeparator, trim);
            var dt = new DataTable();
            if (rows.Count == 0)
                return dt;

            int startRow = 0;
            if (hasHeader)
            {
                var header = rows[0];
                for (int i = 0; i < header.Count; i++)
                {
                    var name = string.IsNullOrWhiteSpace(header[i]) ? $"Column{i + 1}" : header[i];
                    dt.Columns.Add(DataTableParserHelpers.MakeUniqueColumnName(dt, name));
                }
                startRow = 1;
            }
            else
            {
                DataTableParserHelpers.EnsureColumns(dt, rows[0].Count);
            }

            for (int i = startRow; i < rows.Count; i++)
            {
                var rowValues = rows[i];
                if (rowValues == null || rowValues.Count == 0)
                    continue;

                bool allEmpty = true;
                for (int c = 0; c < rowValues.Count; c++)
                {
                    if (!string.IsNullOrWhiteSpace(rowValues[c]))
                    {
                        allEmpty = false;
                        break;
                    }
                }

                if (allEmpty)
                    continue;

                DataTableParserHelpers.EnsureColumns(dt, rowValues.Count);
                var row = dt.NewRow();
                for (int c = 0; c < rowValues.Count; c++)
                {
                    row[c] = rowValues[c] ?? string.Empty;
                }
                dt.Rows.Add(row);
            }

            return dt;
        }

        private static List<List<string>> ParseRows(string input, char delimiter, string rowSeparator, bool trim)
        {
            var result = new List<List<string>>();
            if (string.IsNullOrEmpty(input))
                return result;

            var row = new List<string>();
            var cell = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < input.Length && input[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                    continue;
                }

                if (!inQuotes)
                {
                    if (!string.IsNullOrEmpty(rowSeparator) && StartsWithAt(input, rowSeparator, i))
                    {
                        row.Add(PrepareCellValue(cell.ToString(), trim));
                        result.Add(row);
                        row = new List<string>();
                        cell.Clear();
                        i += rowSeparator.Length - 1;
                        continue;
                    }

                    if (string.IsNullOrEmpty(rowSeparator))
                    {
                        if (c == '\r')
                            continue;
                        if (c == '\n')
                        {
                            row.Add(PrepareCellValue(cell.ToString(), trim));
                            result.Add(row);
                            row = new List<string>();
                            cell.Clear();
                            continue;
                        }
                    }

                    if (c == delimiter)
                    {
                        row.Add(PrepareCellValue(cell.ToString(), trim));
                        cell.Clear();
                        continue;
                    }
                }

                cell.Append(c);
            }

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(PrepareCellValue(cell.ToString(), trim));
                result.Add(row);
            }

            return result;
        }

        private static string PrepareCellValue(string value, bool trim)
        {
            if (!trim)
                return value ?? string.Empty;

            return value?.Trim() ?? string.Empty;
        }

        private static bool StartsWithAt(string text, string value, int index)
        {
            if (index + value.Length > text.Length)
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                if (text[index + i] != value[i])
                    return false;
            }

            return true;
        }
    }
}


