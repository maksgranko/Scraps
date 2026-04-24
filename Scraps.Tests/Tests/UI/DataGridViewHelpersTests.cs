using Scraps.Data.DataTables;
using Scraps.UI.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Xunit;

namespace Scraps.Tests.UI
{
    public class DataGridViewHelpersTests
    {
        #region --- Helpers ---

        private static DataGridView CreateDgv(int columns = 1, int rows = 0)
        {
            var dgv = new DataGridView();
            dgv.AllowUserToAddRows = false;
            for (int c = 0; c < columns; c++)
                dgv.Columns.Add($"Col{c}", $"Col{c}");
            for (int r = 0; r < rows; r++)
            {
                var values = new object[columns];
                for (int c = 0; c < columns; c++) values[c] = $"R{r}C{c}";
                dgv.Rows.Add(values);
            }
            return dgv;
        }

        private static DataGridView CreateBoundDgv(DataTable dt)
        {
            var dgv = new DataGridView { DataSource = dt, AllowUserToAddRows = false };
            dgv.BindingContext = new BindingContext();
            return dgv;
        }

        #endregion

        #region --- Row Selection ---

        [StaFact]
        public void GetSelectedRowIndices_NullDgv_ReturnsEmpty()
        {
            List<int> result = ((DataGridView)null).GetSelectedRowIndices();
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [StaFact]
        public void GetSelectedRowIndices_NoSelection_ReturnsEmpty()
        {
            var dgv = CreateDgv(1, 3);
            var indices = dgv.GetSelectedRowIndices();
            Assert.Empty(indices);
        }

        [StaFact]
        public void GetSelectedRowIndices_ReturnsCorrectIndices()
        {
            var dgv = CreateDgv(1, 3);
            dgv.Rows[0].Selected = true;
            dgv.Rows[2].Selected = true;

            var indices = dgv.GetSelectedRowIndices();
            Assert.Equal(2, indices.Count);
            Assert.Contains(0, indices);
            Assert.Contains(2, indices);
            Assert.True(indices.SequenceEqual(indices.OrderBy(i => i)));
        }

        [StaFact]
        public void GetSelectedRowIndices_IgnoresNewRow()
        {
            var dgv = new DataGridView();
            dgv.AllowUserToAddRows = true;
            dgv.Columns.Add("A", "A");
            dgv.Rows.Add("1");
            dgv.Rows[0].Selected = true;
            dgv.Rows[1].Selected = true; // new row

            var indices = dgv.GetSelectedRowIndices();
            Assert.Single(indices);
            Assert.Contains(0, indices);
        }

        [StaFact]
        public void GetSelectedRows_NullDgv_ReturnsEmpty()
        {
            var result = ((DataGridView)null).GetSelectedRows();
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [StaFact]
        public void GetSelectedRows_ReturnsRowObjects()
        {
            var dgv = CreateDgv(1, 2);
            dgv.Rows[1].Selected = true;
            var rows = dgv.GetSelectedRows();
            Assert.Single(rows);
            Assert.Equal(1, rows[0].Index);
        }

        [StaFact]
        public void SelectRow_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).SelectRow(0);
        }

        [StaFact]
        public void SelectRow_InvalidIndex_DoesNotThrow()
        {
            var dgv = CreateDgv(1, 1);
            dgv.SelectRow(-1);
            dgv.SelectRow(99);
        }

        [StaFact]
        public void SelectRow_SetsCurrentCell()
        {
            var dgv = CreateDgv(1, 2);
            dgv.SelectRow(1);
            Assert.True(dgv.Rows[1].Selected);
            Assert.Equal(1, dgv.CurrentCell.RowIndex);
        }

        [StaFact]
        public void SelectRow_ClearOthers_RemovesPreviousSelection()
        {
            var dgv = CreateDgv(1, 3);
            dgv.SelectRow(0);
            dgv.SelectRow(2, clearOthers: true);
            Assert.False(dgv.Rows[0].Selected);
            Assert.True(dgv.Rows[2].Selected);
        }

        [StaFact]
        public void SelectRows_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).SelectRows(new[] { 0 });
        }

        [StaFact]
        public void SelectRows_SelectsMultiple()
        {
            var dgv = CreateDgv(1, 3);
            dgv.SelectRows(new[] { 0, 2 });
            Assert.True(dgv.Rows[0].Selected);
            Assert.False(dgv.Rows[1].Selected);
            Assert.True(dgv.Rows[2].Selected);
        }

        [StaFact]
        public void SelectRows_InvalidIndicesIgnored()
        {
            var dgv = CreateDgv(1, 2);
            dgv.SelectRows(new[] { -1, 0, 99 });
            Assert.True(dgv.Rows[0].Selected);
            Assert.False(dgv.Rows[1].Selected);
        }

        [StaFact]
        public void AddRowToSelection_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).AddRowToSelection(0);
        }

        [StaFact]
        public void AddRowToSelection_InvalidIndex_DoesNotThrow()
        {
            var dgv = CreateDgv(1, 1);
            dgv.AddRowToSelection(-1);
            dgv.AddRowToSelection(99);
        }

        [StaFact]
        public void AddRowToSelection_AddsToExisting()
        {
            var dgv = CreateDgv(1, 3);
            dgv.SelectRow(0);
            dgv.AddRowToSelection(2);
            Assert.True(dgv.Rows[0].Selected);
            Assert.True(dgv.Rows[2].Selected);
        }

        [StaFact]
        public void DeselectRow_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).DeselectRow(0);
        }

        [StaFact]
        public void DeselectRow_InvalidIndex_DoesNotThrow()
        {
            var dgv = CreateDgv(1, 1);
            dgv.DeselectRow(-1);
            dgv.DeselectRow(99);
        }

        [StaFact]
        public void DeselectRow_RemovesSelection()
        {
            var dgv = CreateDgv(1, 2);
            dgv.SelectRows(new[] { 0, 1 });
            dgv.DeselectRow(0);
            Assert.False(dgv.Rows[0].Selected);
            Assert.True(dgv.Rows[1].Selected);
        }

        [StaFact]
        public void DeleteRow_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).DeleteRow(0);
        }

        [StaFact]
        public void DeleteRow_InvalidIndex_DoesNotThrow()
        {
            var dgv = CreateDgv(1, 1);
            dgv.DeleteRow(-1);
            dgv.DeleteRow(99);
        }

        [StaFact]
        public void DeleteRow_WhenNotAllowedAndNoDataSource_DoesNothing()
        {
            var dgv = new DataGridView();
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.Columns.Add("A", "A");
            dgv.Rows.Add("1");
            dgv.DeleteRow(0);
            Assert.Single(dgv.Rows);
        }

        [StaFact]
        public void DeleteRow_RemovesRow()
        {
            var dgv = CreateDgv(1, 2);
            dgv.Rows[0].Cells[0].Value = "Ivan";
            dgv.Rows[1].Cells[0].Value = "Petr";
            dgv.DeleteRow(0);
            Assert.Single(dgv.Rows);
            Assert.Equal("Petr", dgv.Rows[0].Cells[0].Value);
        }

        [StaFact]
        public void DeleteSelectedRows_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).DeleteSelectedRows();
        }

        [StaFact]
        public void DeleteSelectedRows_DeletesInReverseOrder()
        {
            var dgv = CreateDgv(1, 3);
            dgv.SelectRows(new[] { 0, 2 });
            dgv.DeleteSelectedRows();
            Assert.Single(dgv.Rows);
            Assert.Equal("R1C0", dgv.Rows[0].Cells[0].Value);
        }

        [StaFact]
        public void DeleteSelectedRows_NoSelection_DoesNothing()
        {
            var dgv = CreateDgv(1, 2);
            dgv.DeleteSelectedRows();
            Assert.Equal(2, dgv.Rows.Count);
        }

        [StaFact]
        public void MoveRowUp_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).MoveRowUp(1);
        }

        [StaFact]
        public void MoveRowUp_SwapsRows()
        {
            var dgv = CreateDgv(2, 2);
            dgv.MoveRowUp(1);
            Assert.Equal("R1C0", dgv.Rows[0].Cells[0].Value);
            Assert.Equal("R0C0", dgv.Rows[1].Cells[0].Value);
        }

        [StaFact]
        public void MoveRowDown_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).MoveRowDown(0);
        }

        [StaFact]
        public void MoveRowDown_InvalidIndex_DoesNotThrow()
        {
            var dgv = CreateDgv(1, 2);
            dgv.MoveRowDown(-1);
            dgv.MoveRowDown(1); // last row
            dgv.MoveRowDown(99);
        }

        [StaFact]
        public void MoveRowDown_SwapsRows()
        {
            var dgv = CreateDgv(2, 2);
            dgv.MoveRowDown(0);
            Assert.Equal("R1C0", dgv.Rows[0].Cells[0].Value);
            Assert.Equal("R0C0", dgv.Rows[1].Cells[0].Value);
        }

        [StaFact]
        public void MoveRowUp_WithDataSource_SwapsDataTableRows()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Ivan");
            dt.Rows.Add("Petr");

            var dgv = CreateBoundDgv(dt);
            dgv.MoveRowUp(1);

            Assert.Equal("Petr", dt.Rows[0]["Name"]);
            Assert.Equal("Ivan", dt.Rows[1]["Name"]);
        }

        [StaFact]
        public void DuplicateRow_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).DuplicateRow(0);
        }

        [StaFact]
        public void DuplicateRow_InvalidIndex_DoesNotThrow()
        {
            var dgv = CreateDgv(1, 1);
            dgv.DuplicateRow(-1);
            dgv.DuplicateRow(99);
        }

        [StaFact]
        public void DuplicateRow_CreatesCopy()
        {
            var dgv = CreateDgv(2, 2);
            dgv.Rows[0].Cells[0].Value = "A";
            dgv.Rows[0].Cells[1].Value = "B";
            dgv.DuplicateRow(0);
            Assert.Equal(3, dgv.Rows.Count);
            Assert.Equal("A", dgv.Rows[1].Cells[0].Value);
            Assert.Equal("B", dgv.Rows[1].Cells[1].Value);
        }

        #endregion

        #region --- Column Selection ---

        [StaFact]
        public void GetSelectedColumns_NullDgv_ReturnsEmpty()
        {
            var result = ((DataGridView)null).GetSelectedColumns();
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [StaFact]
        public void GetSelectedColumnIndices_NullDgv_ReturnsEmpty()
        {
            var result = ((DataGridView)null).GetSelectedColumnIndices();
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [StaFact]
        public void SelectColumn_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).SelectColumn(0);
        }

        [StaFact]
        public void SelectColumn_InvalidIndex_DoesNotThrow()
        {
            var dgv = CreateDgv(1, 1);
            dgv.SelectColumn(-1);
            dgv.SelectColumn(99);
        }

        [StaFact]
        public void AddColumnToSelection_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).AddColumnToSelection(0);
        }

        [StaFact]
        public void AddColumnToSelection_InvalidIndex_DoesNotThrow()
        {
            var dgv = CreateDgv(1, 1);
            dgv.AddColumnToSelection(-1);
            dgv.AddColumnToSelection(99);
        }

        [StaFact]
        public void DeselectColumn_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).DeselectColumn(0);
        }

        [StaFact]
        public void DeselectColumn_InvalidIndex_DoesNotThrow()
        {
            var dgv = CreateDgv(1, 1);
            dgv.DeselectColumn(-1);
            dgv.DeselectColumn(99);
        }

        [StaFact]
        public void DeselectColumn_RemovesSelection()
        {
            var dgv = CreateDgv(3, 1);
            dgv.SelectColumn(0);
            dgv.DeselectColumn(0);
            Assert.False(dgv.Columns[0].Selected);
        }

        [StaFact]
        public void DeleteColumn_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).DeleteColumn(0);
        }

        [StaFact]
        public void DeleteColumn_InvalidIndex_DoesNotThrow()
        {
            var dgv = CreateDgv(2, 1);
            dgv.DeleteColumn(-1);
            dgv.DeleteColumn(99);
        }

        [StaFact]
        public void DeleteColumn_RemovesColumn()
        {
            var dgv = CreateDgv(3, 1);
            dgv.DeleteColumn(1);
            Assert.Equal(2, dgv.Columns.Count);
        }

        #endregion

        #region --- Cell Selection ---

        [StaFact]
        public void GetSelectedCells_NullDgv_ReturnsEmpty()
        {
            var result = ((DataGridView)null).GetSelectedCells();
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [StaFact]
        public void GetSelectedCells_ReturnsCells()
        {
            var dgv = CreateDgv(2, 2);
            dgv.Rows[0].Cells[1].Selected = true;
            var cells = dgv.GetSelectedCells();
            Assert.Single(cells);
            Assert.Equal(0, cells[0].RowIndex);
            Assert.Equal(1, cells[0].ColumnIndex);
        }

        [StaFact]
        public void GetSelectedCellIndices_NullDgv_ReturnsEmpty()
        {
            var result = ((DataGridView)null).GetSelectedCellIndices();
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [StaFact]
        public void GetSelectedCellIndices_ReturnsSortedPairs()
        {
            var dgv = CreateDgv(2, 2);
            dgv.Rows[1].Cells[0].Selected = true;
            dgv.Rows[0].Cells[1].Selected = true;
            var cells = dgv.GetSelectedCellIndices();
            Assert.Equal(new[] { (0, 1), (1, 0) }, cells);
        }

        [StaFact]
        public void SelectCell_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).SelectCell(0, 0);
        }

        [StaFact]
        public void SelectCell_InvalidIndex_DoesNotThrow()
        {
            var dgv = CreateDgv(2, 2);
            dgv.SelectCell(-1, 0);
            dgv.SelectCell(0, -1);
            dgv.SelectCell(99, 0);
            dgv.SelectCell(0, 99);
        }

        [StaFact]
        public void SelectCell_SetsCurrentCell()
        {
            var dgv = CreateDgv(2, 2);
            dgv.SelectCell(1, 1);
            Assert.True(dgv.Rows[1].Cells[1].Selected);
            Assert.Equal(1, dgv.CurrentCell.RowIndex);
            Assert.Equal(1, dgv.CurrentCell.ColumnIndex);
        }

        [StaFact]
        public void SelectCell_ClearOthers_RemovesPrevious()
        {
            var dgv = CreateDgv(2, 2);
            dgv.SelectCell(0, 0);
            dgv.SelectCell(1, 1, clearOthers: true);
            Assert.False(dgv.Rows[0].Cells[0].Selected);
            Assert.True(dgv.Rows[1].Cells[1].Selected);
        }

        [StaFact]
        public void AddCellToSelection_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).AddCellToSelection(0, 0);
        }

        [StaFact]
        public void AddCellToSelection_InvalidIndex_DoesNotThrow()
        {
            var dgv = CreateDgv(2, 2);
            dgv.AddCellToSelection(-1, 0);
            dgv.AddCellToSelection(0, -1);
            dgv.AddCellToSelection(99, 0);
            dgv.AddCellToSelection(0, 99);
        }

        [StaFact]
        public void AddCellToSelection_AddsToExisting()
        {
            var dgv = CreateDgv(2, 2);
            dgv.SelectCell(0, 0);
            dgv.AddCellToSelection(1, 1);
            var cells = dgv.GetSelectedCellIndices();
            Assert.Equal(2, cells.Count);
        }

        [StaFact]
        public void DeselectCell_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).DeselectCell(0, 0);
        }

        [StaFact]
        public void DeselectCell_InvalidIndex_DoesNotThrow()
        {
            var dgv = CreateDgv(2, 2);
            dgv.DeselectCell(-1, 0);
            dgv.DeselectCell(0, -1);
            dgv.DeselectCell(99, 0);
            dgv.DeselectCell(0, 99);
        }

        [StaFact]
        public void DeselectCell_RemovesSelection()
        {
            var dgv = CreateDgv(2, 2);
            dgv.SelectCell(0, 0);
            dgv.AddCellToSelection(1, 1);
            dgv.DeselectCell(0, 0);
            var cells = dgv.GetSelectedCellIndices();
            Assert.Single(cells);
            Assert.Contains((1, 1), cells);
        }

        [StaFact]
        public void DeselectAll_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).DeselectAll();
        }

        [StaFact]
        public void DeselectAll_ClearsEverything()
        {
            var dgv = CreateDgv(1, 2);
            dgv.SelectRows(new[] { 0, 1 });
            dgv.DeselectAll();
            Assert.Empty(dgv.GetSelectedRowIndices());
        }

        #endregion

        #region --- Save/Restore Selection ---

        [StaFact]
        public void SaveSelection_NullDgv_ReturnsNull()
        {
            Assert.Null(((DataGridView)null).SaveSelection());
        }

        [StaFact]
        public void SaveSelection_SavesCellsAndCurrent()
        {
            var dgv = CreateDgv(2, 3);
            dgv.SelectCell(0, 0, clearOthers: true);
            dgv.AddCellToSelection(1, 1);
            dgv.AddCellToSelection(2, 0);

            var saved = dgv.SaveSelection();
            Assert.NotNull(saved);
            Assert.Equal(3, saved.SelectedCells.Count);
            Assert.Equal((0, 0), saved.CurrentCell);
        }

        [StaFact]
        public void RestoreSelection_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).RestoreSelection(new DataGridViewHelpers.SelectionState());
        }

        [StaFact]
        public void RestoreSelection_NullState_DoesNotThrow()
        {
            var dgv = CreateDgv(1, 1);
            dgv.RestoreSelection(null);
        }

        [StaFact]
        public void RestoreSelection_InvalidCellsIgnored()
        {
            var dgv = CreateDgv(2, 2);
            var state = new DataGridViewHelpers.SelectionState
            {
                SelectedCells = new List<(int Row, int Column)> { (0, 0), (99, 99), (-1, 0) },
                CurrentCell = (99, 99)
            };
            dgv.RestoreSelection(state);
            Assert.Single(dgv.GetSelectedCellIndices());
            Assert.Contains((0, 0), dgv.GetSelectedCellIndices());
            // CurrentCell должен остаться null/не измениться, так что не проверяем
        }

        [StaFact]
        public void RestoreSelection_RestoresCellsAndCurrent()
        {
            var dgv = CreateDgv(2, 3);
            var state = new DataGridViewHelpers.SelectionState
            {
                SelectedCells = new List<(int Row, int Column)> { (0, 0), (1, 1), (2, 0) },
                CurrentCell = (1, 1)
            };
            dgv.RestoreSelection(state);
            var cells = dgv.GetSelectedCellIndices();
            Assert.Equal(3, cells.Count);
            Assert.Contains((0, 0), cells);
            Assert.Contains((1, 1), cells);
            Assert.Contains((2, 0), cells);
            Assert.Equal(1, dgv.CurrentCell.RowIndex);
            Assert.Equal(1, dgv.CurrentCell.ColumnIndex);
        }

        [StaFact]
        public void SaveAndRestoreSelection_Works()
        {
            var dgv = CreateDgv(2, 3);
            dgv.SelectCell(0, 0, clearOthers: true);
            dgv.AddCellToSelection(1, 1);
            dgv.AddCellToSelection(2, 0);

            var saved = dgv.SaveSelection();
            Assert.NotNull(saved);
            Assert.Equal(3, saved.SelectedCells.Count);

            dgv.DeselectAll();
            Assert.Empty(dgv.GetSelectedCellIndices());

            dgv.RestoreSelection(saved);
            var restored = dgv.GetSelectedCellIndices();
            Assert.Equal(3, restored.Count);
            Assert.Contains((0, 0), restored);
            Assert.Contains((1, 1), restored);
            Assert.Contains((2, 0), restored);
        }

        [StaFact]
        public void SetCellValue_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).SetCellValue(0, 0, "x");
        }

        [StaFact]
        public void SetCellValue_InvalidIndex_DoesNotThrow()
        {
            var dgv = CreateDgv(1, 1);
            dgv.SetCellValue(-1, 0, "x");
            dgv.SetCellValue(0, -1, "x");
            dgv.SetCellValue(99, 0, "x");
            dgv.SetCellValue(0, 99, "x");
        }

        [StaFact]
        public void SetCellValue_SetsValue()
        {
            var dgv = CreateDgv(1, 1);
            dgv.SetCellValue(0, 0, "Petr");
            Assert.Equal("Petr", dgv.Rows[0].Cells[0].Value);
        }

        [StaFact]
        public void GetCellValue_NullDgv_ReturnsNull()
        {
            Assert.Null(((DataGridView)null).GetCellValue(0, 0));
        }

        [StaFact]
        public void GetCellValue_InvalidIndex_ReturnsNull()
        {
            var dgv = CreateDgv(1, 1);
            Assert.Null(dgv.GetCellValue(-1, 0));
            Assert.Null(dgv.GetCellValue(0, -1));
            Assert.Null(dgv.GetCellValue(99, 0));
            Assert.Null(dgv.GetCellValue(0, 99));
        }

        [StaFact]
        public void GetCellValue_ReturnsValue()
        {
            var dgv = CreateDgv(1, 1);
            Assert.Equal("R0C0", dgv.GetCellValue(0, 0));
        }

        #endregion

        #region --- Filtering ---

        [StaFact]
        public void ApplyFilter_String_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).ApplyFilter("test");
        }

        [StaFact]
        public void ApplyFilter_String_NoDataSource_DoesNotThrow()
        {
            var dgv = CreateDgv(1, 2);
            dgv.ApplyFilter("test");
        }

        [StaFact]
        public void ApplyFilter_String_Empty_ClearsFilter()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Ivan");
            var dgv = CreateBoundDgv(dt);
            dgv.ApplyFilter("Ivan");
            dgv.ApplyFilter("");
            Assert.Single(dt.DefaultView);
        }

        [StaFact]
        public void ApplyFilter_String_LooksLikeRowFilter_AppliesDirectly()
        {
            var dt = new DataTable();
            dt.Columns.Add("Age", typeof(int));
            dt.Rows.Add(20);
            dt.Rows.Add(30);
            var dgv = CreateBoundDgv(dt);
            dgv.ApplyFilter("[Age] > 25");
            var filtered = dgv.GetFilteredRows();
            Assert.Single(filtered.Rows);
            Assert.Equal(30, filtered.Rows[0]["Age"]);
        }

        [StaFact]
        public void ApplyFilter_String_TextSearch()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Ivan");
            dt.Rows.Add("Petr");
            dt.Rows.Add("Sidor");
            var dgv = CreateBoundDgv(dt);
            dgv.ApplyFilter("et");
            var filtered = dgv.GetFilteredRows();
            Assert.Single(filtered.Rows);
            Assert.Equal("Petr", filtered.Rows[0]["Name"]);
        }

        [StaFact]
        public void ApplyFilter_Column_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).ApplyFilter("Name", "test");
        }

        [StaFact]
        public void ApplyFilter_Column_EmptyColumnName_DoesNotThrow()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            var dgv = CreateBoundDgv(dt);
            dgv.ApplyFilter("", "test");
        }

        [StaFact]
        public void ApplyFilter_Column_FiltersByColumn()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Ivan");
            dt.Rows.Add("Petr");
            var dgv = CreateBoundDgv(dt);
            dgv.ApplyFilter("Name", "et");
            var filtered = dgv.GetFilteredRows();
            Assert.Single(filtered.Rows);
            Assert.Equal("Petr", filtered.Rows[0]["Name"]);
        }

        [StaFact]
        public void ApplyFilter_Column_EmptyValue_ClearsFilter()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Ivan");
            var dgv = CreateBoundDgv(dt);
            dgv.ApplyFilter("Name", "xyz");
            dgv.ApplyFilter("Name", "");
            Assert.Single(dt.DefaultView);
        }

        [StaFact]
        public void ClearFilter_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).ClearFilter();
        }

        [StaFact]
        public void ClearFilter_RestoresAllRows()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Ivan");
            dt.Rows.Add("Petr");
            var dgv = CreateBoundDgv(dt);
            dgv.ApplyFilter("Name", "xyz");
            dgv.ClearFilter();
            Assert.Equal(2, dt.DefaultView.Count);
        }

        [StaFact]
        public void GetFilteredRows_NullDgv_ReturnsNull()
        {
            Assert.Null(((DataGridView)null).GetFilteredRows());
        }

        [StaFact]
        public void GetFilteredRows_NoDataSource_ReturnsNull()
        {
            var dgv = CreateDgv(1, 1);
            Assert.Null(dgv.GetFilteredRows());
        }

        [StaFact]
        public void GetFilteredRows_ReturnsFilteredTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Ivan");
            dt.Rows.Add("Petr");
            var dgv = CreateBoundDgv(dt);
            dgv.ApplyFilter("Name", "et");
            var filtered = dgv.GetFilteredRows();
            Assert.NotNull(filtered);
            Assert.Single(filtered.Rows);
        }

        #endregion

        #region --- Search & Highlight ---

        [StaFact]
        public void CreateMatchNavigator_NullDgv_ReturnsNull()
        {
            Assert.Null(((DataGridView)null).CreateMatchNavigator("test"));
        }

        [StaFact]
        public void CreateMatchNavigator_NoDataSource_ReturnsNull()
        {
            var dgv = CreateDgv(1, 1);
            Assert.Null(dgv.CreateMatchNavigator("test"));
        }

        [StaFact]
        public void CreateMatchNavigator_WithDataSource_ReturnsNavigator()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Ivan");
            dt.Rows.Add("Petr");
            var dgv = CreateBoundDgv(dt);
            var navigator = dgv.CreateMatchNavigator("Petr");
            Assert.NotNull(navigator);
            Assert.Equal(1, navigator.Count);
        }

        [StaFact]
        public void CreateMatchNavigator_WithDataView_ReturnsNavigator()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Ivan");
            dt.Rows.Add("Petr");
            var dgv = new DataGridView { DataSource = dt.DefaultView, AllowUserToAddRows = false };
            var navigator = dgv.CreateMatchNavigator("Petr");
            Assert.NotNull(navigator);
            Assert.Equal(1, navigator.Count);
        }

        [StaFact]
        public void CreateMatchNavigator_NoMatches_ReturnsEmptyNavigator()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Ivan");
            var dgv = CreateBoundDgv(dt);
            var navigator = dgv.CreateMatchNavigator("xyz");
            Assert.NotNull(navigator);
            Assert.Equal(0, navigator.Count);
        }

        [StaFact]
        public void FindNext_NullNavigator_ReturnsFalse()
        {
            var dgv = CreateDgv(1, 1);
            Assert.False(dgv.FindNext(null));
        }

        [StaFact]
        public void FindNext_MovesToMatch()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Ivan");
            dt.Rows.Add("Petr");
            var dgv = CreateBoundDgv(dt);
            var nav = dgv.CreateMatchNavigator("Petr");
            Assert.True(dgv.FindNext(nav));
            Assert.NotNull(dgv.CurrentCell);
            Assert.Equal(1, dgv.CurrentCell.RowIndex);
        }

        [StaFact]
        public void FindPrevious_NullNavigator_ReturnsFalse()
        {
            var dgv = CreateDgv(1, 1);
            Assert.False(dgv.FindPrevious(null));
        }

        [StaFact]
        public void FindPrevious_MovesToMatch()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Ivan");
            dt.Rows.Add("Petr");
            var dgv = CreateBoundDgv(dt);
            var nav = dgv.CreateMatchNavigator("Petr");
            Assert.True(dgv.FindPrevious(nav));
            Assert.Equal(1, dgv.CurrentCell.RowIndex);
        }

        [StaFact]
        public void HighlightSearchResults_NullDgv_ReturnsEmpty()
        {
            var result = ((DataGridView)null).HighlightSearchResults("test");
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [StaFact]
        public void HighlightSearchResults_EmptySearch_ReturnsEmpty()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            var dgv = CreateBoundDgv(dt);
            var result = dgv.HighlightSearchResults("");
            Assert.Empty(result);
        }

        [StaFact]
        public void HighlightSearchResults_NoDataSource_ReturnsEmpty()
        {
            var dgv = CreateDgv(1, 1);
            var result = dgv.HighlightSearchResults("test");
            Assert.Empty(result);
        }

        [StaFact]
        public void HighlightSearchResults_FindsAndColors()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Ivan");
            dt.Rows.Add("Petr");

            var dgv = CreateBoundDgv(dt);

            var matches = dgv.HighlightSearchResults("et");
            Assert.Single(matches);
            Assert.Equal((1, 0), matches[0]);
            Assert.Equal(Color.Yellow, dgv.Rows[1].Cells[0].Style.BackColor);

            dgv.ClearSearchHighlight();
            Assert.Equal(Color.Empty, dgv.Rows[1].Cells[0].Style.BackColor);
        }

        [StaFact]
        public void HighlightSearchResults_CustomColor()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Petr");
            var dgv = CreateBoundDgv(dt);
            dgv.HighlightSearchResults("et", highlightColor: Color.Red);
            Assert.Equal(Color.Red, dgv.Rows[0].Cells[0].Style.BackColor);
        }

        [StaFact]
        public void HighlightSearchResults_NoMatches_ReturnsEmpty()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Ivan");
            var dgv = CreateBoundDgv(dt);
            var matches = dgv.HighlightSearchResults("xyz");
            Assert.Empty(matches);
        }

        [StaFact]
        public void HighlightSearchResults_CaseSensitive()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("PETR");
            dt.Rows.Add("Petr");
            var dgv = CreateBoundDgv(dt);
            var matches = dgv.HighlightSearchResults("et", caseSensitive: true);
            Assert.Single(matches);
            Assert.Equal((1, 0), matches[0]);
        }

        [StaFact]
        public void ClearSearchHighlight_NullDgv_DoesNotThrow()
        {
            ((DataGridView)null).ClearSearchHighlight();
        }

        [StaFact]
        public void ClearSearchHighlight_ClearsAllCells()
        {
            var dt = new DataTable();
            dt.Columns.Add("A", typeof(string));
            dt.Columns.Add("B", typeof(string));
            dt.Rows.Add("1", "2");
            var dgv = CreateBoundDgv(dt);
            dgv.Rows[0].Cells[0].Style.BackColor = Color.Red;
            dgv.Rows[0].Cells[1].Style.BackColor = Color.Blue;
            dgv.ClearSearchHighlight();
            Assert.Equal(Color.Empty, dgv.Rows[0].Cells[0].Style.BackColor);
            Assert.Equal(Color.Empty, dgv.Rows[0].Cells[1].Style.BackColor);
        }

        #endregion
    }
}
