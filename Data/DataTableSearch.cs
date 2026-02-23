using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Scraps.Data
{
    /// <summary>
    /// Простые методы поиска и фильтрации по DataTable.
    /// </summary>
    public static class DataTableSearch
    {
        /// <summary>
        /// Результат поиска по одной ячейке.
        /// </summary>
        public class DataCellMatch
        {
            /// <summary>Индекс строки.</summary>
            public int RowIndex { get; set; }
            /// <summary>Имя колонки.</summary>
            public string ColumnName { get; set; }
            /// <summary>Значение ячейки.</summary>
            public object Value { get; set; }
        }

        /// <summary>
        /// Найти совпадения по всем колонкам.
        /// </summary>
        public static List<DataCellMatch> FindMatches(DataTable table, string searchText, bool ignoreCase = true)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (string.IsNullOrWhiteSpace(searchText)) return new List<DataCellMatch>();

            var results = new List<DataCellMatch>();
            for (int r = 0; r < table.Rows.Count; r++)
            {
                var row = table.Rows[r];
                foreach (DataColumn col in table.Columns)
                {
                    var value = row[col];
                    if (value == null || value == DBNull.Value) continue;

                    if (Contains(value.ToString(), searchText, ignoreCase))
                    {
                        results.Add(new DataCellMatch
                        {
                            RowIndex = r,
                            ColumnName = col.ColumnName,
                            Value = value
                        });
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Найти совпадения по одной колонке.
        /// </summary>
        public static List<DataCellMatch> FindMatches(DataTable table, string columnName, string searchText, bool ignoreCase = true)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (string.IsNullOrWhiteSpace(columnName)) throw new ArgumentNullException(nameof(columnName));
            if (string.IsNullOrWhiteSpace(searchText)) return new List<DataCellMatch>();

            if (!table.Columns.Contains(columnName))
                throw new ArgumentException($"Колонка '{columnName}' не найдена.", nameof(columnName));

            var results = new List<DataCellMatch>();
            for (int r = 0; r < table.Rows.Count; r++)
            {
                var value = table.Rows[r][columnName];
                if (value == null || value == DBNull.Value) continue;

                if (Contains(value.ToString(), searchText, ignoreCase))
                {
                    results.Add(new DataCellMatch
                    {
                        RowIndex = r,
                        ColumnName = columnName,
                        Value = value
                    });
                }
            }
            return results;
        }

        /// <summary>
        /// Отфильтровать строки по всем колонкам.
        /// </summary>
        public static DataTable FilterRows(DataTable table, string searchText, bool ignoreCase = true)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (string.IsNullOrWhiteSpace(searchText)) return table.Copy();

            var result = table.Clone();
            foreach (DataRow row in table.Rows)
            {
                bool match = false;
                foreach (DataColumn col in table.Columns)
                {
                    var value = row[col];
                    if (value == null || value == DBNull.Value) continue;

                    if (Contains(value.ToString(), searchText, ignoreCase))
                    {
                        match = true;
                        break;
                    }
                }
                if (match) result.ImportRow(row);
            }
            return result;
        }

        /// <summary>
        /// Отфильтровать строки по одной колонке.
        /// </summary>
        public static DataTable FilterRows(DataTable table, string columnName, string searchText, bool ignoreCase = true)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (string.IsNullOrWhiteSpace(columnName)) throw new ArgumentNullException(nameof(columnName));

            if (!table.Columns.Contains(columnName))
                throw new ArgumentException($"Колонка '{columnName}' не найдена.", nameof(columnName));

            if (string.IsNullOrWhiteSpace(searchText)) return table.Copy();

            var result = table.Clone();
            foreach (DataRow row in table.Rows)
            {
                var value = row[columnName];
                if (value == null || value == DBNull.Value) continue;

                if (Contains(value.ToString(), searchText, ignoreCase))
                {
                    result.ImportRow(row);
                }
            }
            return result;
        }

        private static bool Contains(string source, string value, bool ignoreCase)
        {
            if (source == null || value == null) return false;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return source.IndexOf(value, comparison) >= 0;
        }
    }
}
