using System;
using System.Data;
using Scraps.Data.Parsers.Internal;

namespace Scraps.Data.Parsers
{
    /// <summary>
    /// Простой парсер разделённых таблиц без поддержки кавычек и escaping.
    /// </summary>
    public static class DelimitedTable
    {
        /// <summary>
        /// Простой парс строк с разделителем без поддержки кавычек/экранирования.
        /// </summary>
        public static DataTable Parse(string input, char delimiter = ',', bool hasHeader = true, bool trim = true)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var dt = new DataTable();
            if (string.IsNullOrWhiteSpace(input)) return dt;

            var lines = input.Replace("\r\n", "\n").Replace("\r", "\n").Split(new[] { '\n' }, StringSplitOptions.None);
            if (lines.Length == 0) return dt;

            int startRow = 0;
            if (hasHeader)
            {
                var header = SplitLine(lines[0], delimiter, trim);
                for (int i = 0; i < header.Length; i++)
                {
                    var name = string.IsNullOrWhiteSpace(header[i]) ? $"Column{i + 1}" : header[i];
                    dt.Columns.Add(DataTableParserHelpers.MakeUniqueColumnName(dt, name));
                }
                startRow = 1;
            }
            else
            {
                var first = SplitLine(lines[0], delimiter, trim);
                DataTableParserHelpers.EnsureColumns(dt, first.Length);
                startRow = 0;
            }

            for (int i = startRow; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var values = SplitLine(lines[i], delimiter, trim);
                DataTableParserHelpers.EnsureColumns(dt, values.Length);

                var row = dt.NewRow();
                for (int c = 0; c < values.Length; c++)
                {
                    row[c] = values[c];
                }
                dt.Rows.Add(row);
            }

            return dt;
        }

        private static string[] SplitLine(string line, char delimiter, bool trim)
        {
            var parts = line.Split(new[] { delimiter }, StringSplitOptions.None);
            if (!trim) return parts;

            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i]?.Trim();
            }
            return parts;
        }
    }
}

