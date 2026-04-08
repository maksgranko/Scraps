using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Scraps.Data.DataTables
{
    /// <summary>
    /// Простые методы поиска и фильтрации по DataTable.
    /// </summary>
    public static class Search
    {
        /// <summary>
        /// Навигатор по результатам поиска.
        ///
        /// Типичный сценарий:
        /// 1) Создать через <see cref="CreateNavigator(DataTable, string, bool)"/>.
        /// 2) Вызвать <see cref="MatchNavigator.First"/> или <see cref="MatchNavigator.Next(bool)"/>.
        /// 3) Использовать <see cref="DataCellMatch.RowIndex"/> и <see cref="DataCellMatch.ColumnName"/>
        ///    для подсветки в DataGridView.
        ///
        /// Для полной текстовой инструкции можно вызвать <see cref="GetMatchNavigatorHelp"/>.
        /// </summary>
        public class MatchNavigator
        {
            private readonly List<DataCellMatch> _matches;
            private int _index = -1;

            /// <summary>
            /// Создать навигатор по набору найденных совпадений.
            /// </summary>
            public MatchNavigator(List<DataCellMatch> matches)
            {
                _matches = matches ?? new List<DataCellMatch>();
            }

            /// <summary>Количество совпадений.</summary>
            public int Count => _matches.Count;

            /// <summary>Текущий индекс.</summary>
            public int Index => _index;

            /// <summary>Текущее совпадение (null, если нет).</summary>
            public DataCellMatch Current => (_index >= 0 && _index < _matches.Count) ? _matches[_index] : null;

            /// <summary>
            /// Перейти к следующему совпадению.
            /// </summary>
            /// <param name="wrap">Если true, переход с последнего совпадения возвращается к первому.</param>
            public DataCellMatch Next(bool wrap = false)
            {
                if (_matches.Count == 0) return null;
                if (_index + 1 >= _matches.Count)
                {
                    if (!wrap) return _matches[_index];
                    _index = 0;
                }
                else
                {
                    _index++;
                }
                return _matches[_index];
            }

            /// <summary>
            /// Перейти к предыдущему совпадению.
            /// </summary>
            /// <param name="wrap">Если true, переход с первого совпадения выполняется к последнему.</param>
            public DataCellMatch Prev(bool wrap = false)
            {
                if (_matches.Count == 0) return null;
                if (_index == -1 && !wrap) return null;
                if (_index - 1 < 0)
                {
                    if (!wrap) return _matches[_index];
                    _index = _matches.Count - 1;
                }
                else
                {
                    _index--;
                }
                return _matches[_index];
            }

            /// <summary>
            /// Перейти к первому совпадению.
            /// </summary>
            public DataCellMatch First()
            {
                if (_matches.Count == 0) return null;
                _index = 0;
                return _matches[_index];
            }

            /// <summary>
            /// Перейти к последнему совпадению.
            /// </summary>
            public DataCellMatch Last()
            {
                if (_matches.Count == 0) return null;
                _index = _matches.Count - 1;
                return _matches[_index];
            }
        }

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
        /// Получить подробную текстовую справку по использованию MatchNavigator
        /// (подходит для вывода в MessageBox, лог или отдельную вкладку Help в приложении).
        /// </summary>
        public static string GetMatchNavigatorHelp()
        {
            var sb = new StringBuilder();
            sb.AppendLine("MatchNavigator quick help:");
            sb.AppendLine("1. var nav = Search.CreateNavigator(dataTable, searchText);");
            sb.AppendLine("2. var match = nav.First();");
            sb.AppendLine("3. Пока match != null, подсвечивайте ячейку по match.RowIndex + match.ColumnName.");
            sb.AppendLine("4. Для перехода дальше используйте nav.Next(wrap: true).");
            sb.AppendLine();
            sb.AppendLine("DataGridView mapping example:");
            sb.AppendLine("- col = grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.DataPropertyName == match.ColumnName || c.Name == match.ColumnName);");
            sb.AppendLine("- if (col != null) grid.CurrentCell = grid.Rows[match.RowIndex].Cells[col.Index];");
            return sb.ToString();
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
        /// Создать навигатор по совпадениям (все колонки).
        /// </summary>
        public static MatchNavigator CreateNavigator(DataTable table, string searchText, bool ignoreCase = true)
        {
            return new MatchNavigator(FindMatches(table, searchText, ignoreCase));
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
        /// Создать навигатор по совпадениям (одна колонка).
        /// </summary>
        public static MatchNavigator CreateNavigator(DataTable table, string columnName, string searchText, bool ignoreCase = true)
        {
            return new MatchNavigator(FindMatches(table, columnName, searchText, ignoreCase));
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
        /// Вернуть уникальные индексы строк, где есть совпадения (все колонки).
        /// </summary>
        public static int[] GetMatchRowIndices(DataTable table, string searchText, bool ignoreCase = true)
        {
            var matches = FindMatches(table, searchText, ignoreCase);
            return matches.Select(m => m.RowIndex).Distinct().ToArray();
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

        /// <summary>
        /// Вернуть уникальные индексы строк, где есть совпадения (одна колонка).
        /// </summary>
        public static int[] GetMatchRowIndices(DataTable table, string columnName, string searchText, bool ignoreCase = true)
        {
            var matches = FindMatches(table, columnName, searchText, ignoreCase);
            return matches.Select(m => m.RowIndex).Distinct().ToArray();
        }

        private static bool Contains(string source, string value, bool ignoreCase)
        {
            if (source == null || value == null) return false;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return source.IndexOf(value, comparison) >= 0;
        }
    }
}

