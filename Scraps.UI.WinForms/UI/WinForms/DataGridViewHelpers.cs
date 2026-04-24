using Scraps.Data.DataTables;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Scraps.UI.WinForms
{
    /// <summary>
    /// Хелперы для работы с DataGridView: выделение, навигация, поиск, фильтрация.
    /// </summary>
    public static class DataGridViewHelpers
    {
        #region --- Row Selection ---

        /// <summary>Получить индексы выделенных строк.</summary>
        public static List<int> GetSelectedRowIndices(this DataGridView dgv)
        {
            var result = new List<int>();
            if (dgv == null) return result;

            foreach (DataGridViewRow row in dgv.SelectedRows)
            {
                if (!row.IsNewRow)
                    result.Add(row.Index);
            }
            return result.OrderBy(i => i).ToList();
        }

        /// <summary>Получить выделенные строки.</summary>
        public static List<DataGridViewRow> GetSelectedRows(this DataGridView dgv)
        {
            var result = new List<DataGridViewRow>();
            if (dgv == null) return result;
            foreach (DataGridViewRow row in dgv.SelectedRows)
            {
                if (!row.IsNewRow)
                    result.Add(row);
            }
            return result;
        }

        /// <summary>Выделить строку по индексу (по умолчанию добавляет к выделению).</summary>
        public static void SelectRow(this DataGridView dgv, int rowIndex, bool clearOthers = false)
        {
            if (dgv == null || rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
            if (clearOthers) dgv.ClearSelection();
            dgv.Rows[rowIndex].Selected = true;
            dgv.CurrentCell = dgv.Rows[rowIndex].Cells[0];
        }

        /// <summary>Выделить несколько строк (сбрасывает предыдущее выделение).</summary>
        public static void SelectRows(this DataGridView dgv, IEnumerable<int> rowIndices)
        {
            if (dgv == null) return;
            dgv.ClearSelection();
            foreach (var idx in rowIndices)
            {
                if (idx >= 0 && idx < dgv.Rows.Count)
                    dgv.Rows[idx].Selected = true;
            }
        }

        /// <summary>Добавить строку к текущему выделению.</summary>
        public static void AddRowToSelection(this DataGridView dgv, int rowIndex)
        {
            if (dgv == null || rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
            dgv.Rows[rowIndex].Selected = true;
        }

        /// <summary>Снять выделение со строки.</summary>
        public static void DeselectRow(this DataGridView dgv, int rowIndex)
        {
            if (dgv == null || rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
            dgv.Rows[rowIndex].Selected = false;
        }

        /// <summary>Удалить строку по индексу.</summary>
        public static void DeleteRow(this DataGridView dgv, int rowIndex)
        {
            if (dgv == null || rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
            if (!dgv.AllowUserToDeleteRows && dgv.DataSource == null) return;
            dgv.Rows.RemoveAt(rowIndex);
        }

        /// <summary>Удалить выделенные строки.</summary>
        public static void DeleteSelectedRows(this DataGridView dgv)
        {
            if (dgv == null) return;
            var indices = dgv.GetSelectedRowIndices().OrderByDescending(i => i);
            foreach (var idx in indices)
                dgv.DeleteRow(idx);
        }

        /// <summary>Переместить строку вверх.</summary>
        public static void MoveRowUp(this DataGridView dgv, int rowIndex)
        {
            if (dgv == null || rowIndex <= 0) return;
            SwapRows(dgv, rowIndex, rowIndex - 1);
        }

        /// <summary>Переместить строку вниз.</summary>
        public static void MoveRowDown(this DataGridView dgv, int rowIndex)
        {
            if (dgv == null || rowIndex < 0 || rowIndex >= dgv.Rows.Count - 1) return;
            SwapRows(dgv, rowIndex, rowIndex + 1);
        }

        /// <summary>Дублировать строку.</summary>
        public static void DuplicateRow(this DataGridView dgv, int rowIndex)
        {
            if (dgv == null || rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
            var newRow = dgv.Rows[rowIndex].Clone() as DataGridViewRow;
            if (newRow == null) return;
            for (int i = 0; i < dgv.Rows[rowIndex].Cells.Count; i++)
                newRow.Cells[i].Value = dgv.Rows[rowIndex].Cells[i].Value;
            dgv.Rows.Insert(rowIndex + 1, newRow);
        }

        private static void SwapRows(DataGridView dgv, int idx1, int idx2)
        {
            if (dgv.DataSource != null)
            {
                var dt = dgv.DataSource is DataView dv ? dv.Table : dgv.DataSource as DataTable;
                if (dt != null)
                {
                    var row = dt.NewRow();
                    row.ItemArray = dt.Rows[idx1].ItemArray;
                    dt.Rows.RemoveAt(idx1);
                    dt.Rows.InsertAt(row, idx2);
                }
            }
            else
            {
                var row1 = dgv.Rows[idx1];
                var row2 = dgv.Rows[idx2];
                for (int i = 0; i < row1.Cells.Count; i++)
                {
                    var temp = row1.Cells[i].Value;
                    row1.Cells[i].Value = row2.Cells[i].Value;
                    row2.Cells[i].Value = temp;
                }
            }
        }

        #endregion

        #region --- Column Selection ---

        /// <summary>Получить выделенные столбцы.</summary>
        public static List<DataGridViewColumn> GetSelectedColumns(this DataGridView dgv)
        {
            var result = new List<DataGridViewColumn>();
            if (dgv == null) return result;
            foreach (DataGridViewColumn col in dgv.SelectedColumns)
                result.Add(col);
            return result;
        }

        /// <summary>Получить индексы выделенных столбцов.</summary>
        public static List<int> GetSelectedColumnIndices(this DataGridView dgv)
        {
            var result = new List<int>();
            if (dgv == null) return result;

            foreach (DataGridViewColumn col in dgv.SelectedColumns)
                result.Add(col.Index);
            return result.OrderBy(i => i).ToList();
        }

        /// <summary>Выделить столбец по индексу (по умолчанию добавляет к выделению).</summary>
        public static void SelectColumn(this DataGridView dgv, int columnIndex, bool clearOthers = false)
        {
            if (dgv == null || columnIndex < 0 || columnIndex >= dgv.Columns.Count) return;
            if (clearOthers) dgv.ClearSelection();
            dgv.Columns[columnIndex].Selected = true;
        }

        /// <summary>Добавить столбец к текущему выделению.</summary>
        public static void AddColumnToSelection(this DataGridView dgv, int columnIndex)
        {
            if (dgv == null || columnIndex < 0 || columnIndex >= dgv.Columns.Count) return;
            dgv.Columns[columnIndex].Selected = true;
        }

        /// <summary>Снять выделение со столбца.</summary>
        public static void DeselectColumn(this DataGridView dgv, int columnIndex)
        {
            if (dgv == null || columnIndex < 0 || columnIndex >= dgv.Columns.Count) return;
            dgv.Columns[columnIndex].Selected = false;
        }

        /// <summary>Удалить столбец по индексу.</summary>
        public static void DeleteColumn(this DataGridView dgv, int columnIndex)
        {
            if (dgv == null || columnIndex < 0 || columnIndex >= dgv.Columns.Count) return;
            dgv.Columns.RemoveAt(columnIndex);
        }

        #endregion

        #region --- Cell Selection ---

        /// <summary>Получить выделенные ячейки.</summary>
        public static List<DataGridViewCell> GetSelectedCells(this DataGridView dgv)
        {
            var result = new List<DataGridViewCell>();
            if (dgv == null) return result;
            foreach (DataGridViewCell cell in dgv.SelectedCells)
                result.Add(cell);
            return result;
        }

        /// <summary>Получить индексы выделенных ячеек (Row, Column).</summary>
        public static List<(int Row, int Column)> GetSelectedCellIndices(this DataGridView dgv)
        {
            var result = new List<(int Row, int Column)>();
            if (dgv == null) return result;

            foreach (DataGridViewCell cell in dgv.SelectedCells)
                result.Add((cell.RowIndex, cell.ColumnIndex));
            return result.OrderBy(c => c.Row).ThenBy(c => c.Column).ToList();
        }

        /// <summary>Выделить ячейку по индексу (по умолчанию добавляет к выделению).</summary>
        public static void SelectCell(this DataGridView dgv, int rowIndex, int columnIndex, bool clearOthers = false)
        {
            if (dgv == null || rowIndex < 0 || rowIndex >= dgv.Rows.Count || columnIndex < 0 || columnIndex >= dgv.Columns.Count) return;
            if (clearOthers) dgv.ClearSelection();
            dgv.Rows[rowIndex].Cells[columnIndex].Selected = true;
            dgv.CurrentCell = dgv.Rows[rowIndex].Cells[columnIndex];
        }

        /// <summary>Добавить ячейку к текущему выделению.</summary>
        public static void AddCellToSelection(this DataGridView dgv, int rowIndex, int columnIndex)
        {
            if (dgv == null || rowIndex < 0 || rowIndex >= dgv.Rows.Count || columnIndex < 0 || columnIndex >= dgv.Columns.Count) return;
            dgv.Rows[rowIndex].Cells[columnIndex].Selected = true;
        }

        /// <summary>Снять выделение с ячейки.</summary>
        public static void DeselectCell(this DataGridView dgv, int rowIndex, int columnIndex)
        {
            if (dgv == null || rowIndex < 0 || rowIndex >= dgv.Rows.Count || columnIndex < 0 || columnIndex >= dgv.Columns.Count) return;
            dgv.Rows[rowIndex].Cells[columnIndex].Selected = false;
        }

        /// <summary>Очистить всё выделение.</summary>
        public static void DeselectAll(this DataGridView dgv)
        {
            dgv?.ClearSelection();
        }

        #endregion

        #region --- Save/Restore Selection ---

        /// <summary>
        /// Сохранить текущее выделение (выделенные ячейки + текущая ячейка).
        /// </summary>
        public static SelectionState SaveSelection(this DataGridView dgv)
        {
            if (dgv == null) return null;

            var state = new SelectionState
            {
                SelectedCells = dgv.GetSelectedCellIndices(),
                CurrentCell = dgv.CurrentCell != null
                    ? (dgv.CurrentCell.RowIndex, dgv.CurrentCell.ColumnIndex)
                    : ((int, int)?)null
            };
            return state;
        }

        /// <summary>
        /// Восстановить сохранённое выделение.
        /// </summary>
        public static void RestoreSelection(this DataGridView dgv, SelectionState state)
        {
            if (dgv == null || state == null) return;

            dgv.ClearSelection();

            // Восстанавливаем текущую ячейку СНАЧАЛА
            // (иначе установка CurrentCell сбросит выделение)
            if (state.CurrentCell.HasValue)
            {
                var (curRow, curCol) = state.CurrentCell.Value;
                if (curRow >= 0 && curRow < dgv.Rows.Count && curCol >= 0 && curCol < dgv.Columns.Count)
                    dgv.CurrentCell = dgv.Rows[curRow].Cells[curCol];
            }

            // Восстанавливаем выделенные ячейки
            foreach (var (row, col) in state.SelectedCells)
            {
                if (row >= 0 && row < dgv.Rows.Count && col >= 0 && col < dgv.Columns.Count)
                    dgv.Rows[row].Cells[col].Selected = true;
            }
        }

        /// <summary>
        /// Состояние выделения DataGridView.
        /// </summary>
        public class SelectionState
        {
            /// <summary>Список выделенных ячеек (Row, Column).</summary>
            public List<(int Row, int Column)> SelectedCells { get; set; } = new List<(int Row, int Column)>();

            /// <summary>Текущая ячейка (Row, Column) или null.</summary>
            public (int Row, int Column)? CurrentCell { get; set; }
        }

        /// <summary>Установить значение ячейки.</summary>
        public static void SetCellValue(this DataGridView dgv, int rowIndex, int columnIndex, object value)
        {
            if (dgv == null || rowIndex < 0 || rowIndex >= dgv.Rows.Count || columnIndex < 0 || columnIndex >= dgv.Columns.Count) return;
            dgv.Rows[rowIndex].Cells[columnIndex].Value = value;
        }

        /// <summary>Получить значение ячейки.</summary>
        public static object GetCellValue(this DataGridView dgv, int rowIndex, int columnIndex)
        {
            if (dgv == null || rowIndex < 0 || rowIndex >= dgv.Rows.Count || columnIndex < 0 || columnIndex >= dgv.Columns.Count) return null;
            return dgv.Rows[rowIndex].Cells[columnIndex].Value;
        }

        #endregion

        #region --- Filtering ---

        /// <summary>
        /// Применить фильтр.
        /// Если <paramref name="filterExpression"/> похож на RowFilter (содержит [, ], =, AND, OR, LIKE, &gt;, &lt;) — применяется как RowFilter.
        /// Иначе — ищет по всем текстовым колонкам (LIKE '%value%') через OR.
        /// </summary>
        public static void ApplyFilter(this DataGridView dgv, string filterExpression)
        {
            if (dgv?.DataSource == null) return;
            var dv = dgv.DataSource is DataView view ? view : (dgv.DataSource as DataTable)?.DefaultView;
            if (dv == null) return;

            if (string.IsNullOrWhiteSpace(filterExpression))
            {
                dv.RowFilter = "";
                return;
            }

            // Если похоже на RowFilter — применяем напрямую
            if (LooksLikeRowFilter(filterExpression))
            {
                dv.RowFilter = filterExpression;
                return;
            }

            // Иначе — ищем по всем текстовым колонкам
            var safeValue = filterExpression.Replace("'", "''");
            var dt = dv.Table;
            var conditions = new List<string>();

            foreach (DataColumn col in dt.Columns)
            {
                // Только строковые колонки
                if (col.DataType == typeof(string))
                {
                    conditions.Add($"[{col.ColumnName}] LIKE '%{safeValue}%'");
                }
            }

            if (conditions.Count == 0)
            {
                dv.RowFilter = "";
                return;
            }

            dv.RowFilter = string.Join(" OR ", conditions);
        }

        /// <summary>Применить фильтр по конкретной колонке (LIKE).</summary>
        public static void ApplyFilter(this DataGridView dgv, string columnName, string filterValue)
        {
            if (dgv?.DataSource == null || string.IsNullOrWhiteSpace(columnName)) return;
            var safeValue = filterValue?.Replace("'", "''") ?? "";
            var filter = string.IsNullOrWhiteSpace(safeValue) ? "" : $"[{columnName}] LIKE '%{safeValue}%'";
            
            var dv = dgv.DataSource is DataView view ? view : (dgv.DataSource as DataTable)?.DefaultView;
            if (dv != null) dv.RowFilter = filter;
        }

        /// <summary>Определить, похожа ли строка на RowFilter-выражение.</summary>
        private static bool LooksLikeRowFilter(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var upper = value.ToUpperInvariant();
            return value.Contains("[") || value.Contains("]")
                || upper.Contains(" AND ") || upper.Contains(" OR ")
                || upper.Contains("LIKE") || upper.Contains("IS ")
                || upper.Contains("NULL") || upper.Contains("NOT ")
                || value.Contains("=") || value.Contains(">") || value.Contains("<")
                || value.Contains("%");
        }

        /// <summary>Сбросить фильтр.</summary>
        public static void ClearFilter(this DataGridView dgv)
        {
            dgv?.ApplyFilter("");
        }

        /// <summary>Получить отфильтрованные строки как DataTable.</summary>
        public static DataTable GetFilteredRows(this DataGridView dgv)
        {
            if (dgv?.DataSource == null) return null;
            var dt = dgv.DataSource is DataView dv ? dv.Table : dgv.DataSource as DataTable;
            if (dt == null) return null;

            var filtered = dt.DefaultView.ToTable();
            return filtered;
        }

        #endregion

        #region --- Search & Highlight ---

        /// <summary>
        /// Создать навигатор по совпадениям для DataGridView.
        /// </summary>
        public static Search.MatchNavigator CreateMatchNavigator(this DataGridView dgv, string searchText, bool caseSensitive = false)
        {
            if (dgv?.DataSource == null) return null;
            var dt = dgv.DataSource as DataTable;
            if (dt == null)
            {
                var dv = dgv.DataSource as DataView;
                dt = dv?.Table;
            }
            if (dt == null) return null;

            var matches = Search.FindMatches(dt, searchText, !caseSensitive);
            return new Search.MatchNavigator(matches);
        }

        /// <summary>Перейти к следующему совпадению.</summary>
        public static bool FindNext(this DataGridView dgv, Search.MatchNavigator navigator)
        {
            if (navigator == null || dgv == null) return false;
            var pos = navigator.Next();
            if (pos == null) return false;
            var col = dgv.Columns[pos.ColumnName];
            if (col == null) return false;
            dgv.SelectCell(pos.RowIndex, col.Index);
            return true;
        }

        /// <summary>Перейти к предыдущему совпадению.</summary>
        public static bool FindPrevious(this DataGridView dgv, Search.MatchNavigator navigator)
        {
            if (navigator == null || dgv == null) return false;
            var pos = navigator.Prev(wrap: true);
            if (pos == null) return false;
            var col = dgv.Columns[pos.ColumnName];
            if (col == null) return false;
            dgv.SelectCell(pos.RowIndex, col.Index);
            return true;
        }

        /// <summary>Найти все совпадения и выделить их специальным стилем.</summary>
        public static List<(int Row, int Column)> HighlightSearchResults(this DataGridView dgv, string searchText, bool caseSensitive = false, Color? highlightColor = null)
        {
            var result = new List<(int Row, int Column)>();
            if (dgv == null || string.IsNullOrWhiteSpace(searchText)) return result;

            var color = highlightColor ?? Color.Yellow;
            var nav = dgv.CreateMatchNavigator(searchText, caseSensitive);
            if (nav == null) return result;

            for (int i = 0; i < nav.Count; i++)
            {
                var match = i == 0 ? nav.First() : nav.Next();
                if (match == null) continue;
                var col = dgv.Columns[match.ColumnName];
                if (col == null) continue;
                var cell = dgv.Rows[match.RowIndex].Cells[col.Index];
                cell.Style.BackColor = color;
                result.Add((match.RowIndex, col.Index));
            }
            return result;
        }

        /// <summary>Снять цветовое выделение поиска со всех ячеек.</summary>
        public static void ClearSearchHighlight(this DataGridView dgv)
        {
            if (dgv == null) return;
            foreach (DataGridViewRow row in dgv.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                    cell.Style.BackColor = Color.Empty;
            }
        }

        #endregion
    }
}
