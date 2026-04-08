using Scraps.Data.DataTables;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Xunit;

namespace Scraps.Tests
{
    public class DataGridViewSearchTests
    {
        [StaFact]
        public void DataGridView_Smoke_SelectMatch()
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add(1, "Ivan");

            var match = Search.FindMatches(dt, "Ivan").FirstOrDefault();
            Assert.NotNull(match);

            var grid = new DataGridView();
            grid.AutoGenerateColumns = false;
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id" });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", DataPropertyName = "Name" });
            grid.Rows.Add(1, "Ivan");

            var col = grid.Columns
                .Cast<DataGridViewColumn>()
                .FirstOrDefault(c =>
                    string.Equals(c.DataPropertyName, match.ColumnName, System.StringComparison.Ordinal) ||
                    string.Equals(c.Name, match.ColumnName, System.StringComparison.Ordinal));
            Assert.NotNull(col);

            grid.ClearSelection();
            var cell = grid.Rows[match.RowIndex].Cells[col.Index];
            grid.CurrentCell = cell;
            cell.Selected = true;
            grid.FirstDisplayedScrollingRowIndex = match.RowIndex;

            Assert.Equal(cell, grid.CurrentCell);
        }
    }
}
