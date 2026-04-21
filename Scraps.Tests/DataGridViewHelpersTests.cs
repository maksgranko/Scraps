using Scraps.UI.WinForms;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Xunit;

namespace Scraps.Tests
{
    public class DataGridViewHelpersTests
    {
        [StaFact]
        public void GetSelectedRowIndices_ReturnsCorrectIndices()
        {
            var dgv = new DataGridView();
            dgv.Columns.Add("Name", "Name");
            dgv.Rows.Add("Ivan");
            dgv.Rows.Add("Petr");
            dgv.Rows.Add("Sidor");

            dgv.Rows[0].Selected = true;
            dgv.Rows[2].Selected = true;

            var indices = dgv.GetSelectedRowIndices();
            Assert.Equal(2, indices.Count);
            Assert.Contains(0, indices);
            Assert.Contains(2, indices);
        }

        [StaFact]
        public void SelectRow_SetsCurrentCell()
        {
            var dgv = new DataGridView();
            dgv.Columns.Add("Name", "Name");
            dgv.Rows.Add("Ivan");
            dgv.Rows.Add("Petr");

            dgv.SelectRow(1);

            Assert.True(dgv.Rows[1].Selected);
            Assert.Equal(1, dgv.CurrentCell.RowIndex);
        }

        [StaFact]
        public void GetSelectedCellIndices_ReturnsRowColumnPairs()
        {
            var dgv = new DataGridView();
            dgv.Columns.Add("A", "A");
            dgv.Columns.Add("B", "B");
            dgv.Rows.Add("1", "2");
            dgv.Rows.Add("3", "4");

            dgv.Rows[0].Cells[1].Selected = true;
            dgv.Rows[1].Cells[0].Selected = true;

            var cells = dgv.GetSelectedCellIndices();
            Assert.Equal(2, cells.Count);
            Assert.Contains((0, 1), cells);
            Assert.Contains((1, 0), cells);
        }

        [StaFact]
        public void SelectCell_SetsSelection()
        {
            var dgv = new DataGridView();
            dgv.Columns.Add("A", "A");
            dgv.Rows.Add("1");

            dgv.SelectCell(0, 0);

            Assert.True(dgv.Rows[0].Cells[0].Selected);
            Assert.Equal(0, dgv.CurrentCell.ColumnIndex);
        }

        [StaFact]
        public void CreateMatchNavigator_WithDataSource_ReturnsNavigator()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Ivan");
            dt.Rows.Add("Petr");
            dt.Rows.Add("Sidor");

            var dgv = new DataGridView { DataSource = dt };

            var navigator = dgv.CreateMatchNavigator("Petr");
            Assert.NotNull(navigator);
        }

        [StaFact]
        public void AddAndRemoveCellFromSelection_Works()
        {
            var dgv = new DataGridView();
            dgv.Columns.Add("A", "A");
            dgv.Columns.Add("B", "B");
            dgv.Rows.Add("1", "2");
            dgv.Rows.Add("3", "4");

            dgv.SelectCell(0, 0);
            dgv.AddCellToSelection(1, 1);

            var cells = dgv.GetSelectedCellIndices();
            Assert.Equal(2, cells.Count);

            dgv.DeselectCell(0, 0);
            cells = dgv.GetSelectedCellIndices();
            Assert.Single(cells);
            Assert.Contains((1, 1), cells);
        }

        [StaFact]
        public void DeselectAll_ClearsEverything()
        {
            var dgv = new DataGridView();
            dgv.Columns.Add("A", "A");
            dgv.Rows.Add("1");
            dgv.Rows.Add("2");

            dgv.SelectRows(new[] { 0, 1 });
            dgv.DeselectAll();

            Assert.Empty(dgv.GetSelectedRowIndices());
        }

        [StaFact]
        public void SetAndGetCellValue_Works()
        {
            var dgv = new DataGridView();
            dgv.Columns.Add("Name", "Name");
            dgv.Rows.Add("Ivan");

            dgv.SetCellValue(0, 0, "Petr");
            var value = dgv.GetCellValue(0, 0);

            Assert.Equal("Petr", value);
        }

        [StaFact]
        public void ApplyFilter_FiltersRows()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Ivan");
            dt.Rows.Add("Petr");
            dt.Rows.Add("Sidor");

            var dgv = new DataGridView { DataSource = dt };
            dgv.ApplyFilter("Name", "et");

            var filtered = dgv.GetFilteredRows();
            Assert.Single(filtered.Rows);
            Assert.Equal("Petr", filtered.Rows[0]["Name"]);
        }

        [StaFact]
        public void ClearFilter_RestoresAllRows()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Ivan");
            dt.Rows.Add("Petr");

            var dgv = new DataGridView { DataSource = dt };
            dgv.ApplyFilter("Name", "xyz");
            dgv.ClearFilter();

            Assert.Equal(2, dt.DefaultView.Count);
        }

        [StaFact]
        public void DeleteRow_RemovesRow()
        {
            var dgv = new DataGridView();
            dgv.Columns.Add("Name", "Name");
            dgv.Rows.Add("Ivan");
            dgv.Rows.Add("Petr");

            dgv.DeleteRow(0);

            Assert.Single(dgv.Rows);
        }

        [StaFact]
        public void MoveRowUp_SwapsRows()
        {
            var dgv = new DataGridView();
            dgv.Columns.Add("Name", "Name");
            dgv.Rows.Add("Ivan");
            dgv.Rows.Add("Petr");

            dgv.MoveRowDown(0);

            Assert.Equal("Petr", dgv.Rows[0].Cells[0].Value);
            Assert.Equal("Ivan", dgv.Rows[1].Cells[0].Value);
        }

        [StaFact]
        public void HighlightSearchResults_FindsAndColors()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Ivan");
            dt.Rows.Add("Petr");

            var dgv = new DataGridView { DataSource = dt };
            dgv.Columns.Add("Name", "Name");
            dgv.Columns[0].DataPropertyName = "Name";

            var matches = dgv.HighlightSearchResults("et");
            Assert.Single(matches);

            dgv.ClearSearchHighlight();
            Assert.Equal(System.Drawing.Color.Empty, dgv.Rows[1].Cells[0].Style.BackColor);
        }

        [StaFact]
        public void SaveAndRestoreSelection_Works()
        {
            var dgv = new DataGridView();
            dgv.Columns.Add("A", "A");
            dgv.Columns.Add("B", "B");
            dgv.Rows.Add("1", "2");
            dgv.Rows.Add("3", "4");
            dgv.Rows.Add("5", "6");

            // Выделяем несколько ячеек
            dgv.SelectCell(0, 0, clearOthers: true);
            dgv.AddCellToSelection(1, 1);
            dgv.AddCellToSelection(2, 0);

            // Сохраняем
            var saved = dgv.SaveSelection();
            Assert.NotNull(saved);
            Assert.Equal(3, saved.SelectedCells.Count);

            // Сбрасываем
            dgv.DeselectAll();
            Assert.Empty(dgv.GetSelectedCellIndices());

            // Восстанавливаем
            dgv.RestoreSelection(saved);
            var restored = dgv.GetSelectedCellIndices();
            Assert.Equal(3, restored.Count);
            Assert.Contains((0, 0), restored);
            Assert.Contains((1, 1), restored);
            Assert.Contains((2, 0), restored);
        }
    }
}
