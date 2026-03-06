using Scraps.Databases;
using Scraps.Security;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Xunit;

namespace Scraps.Tests
{
    [Collection("Db")]
    public class ScrapsTests
    {
        [DbFact]
        public void QuoteIdentifier_WrapsNamesWithSpaces()
        {
            var name = "Таблица 1";
            var quoted = MSSQL.QuoteIdentifier(name);
            Assert.Equal("[Таблица 1]", quoted);
        }

        [DbFact]
        public void QuoteIdentifier_WrapsSchemaQualified()
        {
            var name = "dbo.Table";
            var quoted = MSSQL.QuoteIdentifier(name);
            Assert.Equal("[dbo].[Table]", quoted);
        }

        [DbFact]
        public void QuoteIdentifier_EscapesClosingBracket()
        {
            var name = "na]me";
            var quoted = MSSQL.QuoteIdentifier(name);
            Assert.Equal("[na]]me]", quoted);
        }

        [DbFact]
        public void ConnectionStringBuilder_AllowsDirectString()
        {
            var input = "Data Source=.;Initial Catalog=Test;Integrated Security=True;";
            var output = MSSQL.ConnectionStringBuilder(input);
            Assert.Equal(input, output);
        }

        [DbFact]
        public void GetTableData_WorksWithSpaceInName()
        {
            var dt = MSSQL.GetTableData("Таблица 1");
            Assert.NotNull(dt);
            Assert.True(dt.Rows.Count > 0);
        }

        [DbFact]
        public void VirtualTableRegistry_SelectWorks()
        {
            VirtualTableRegistry.Clear();
            VirtualTableRegistry.RegisterSelect("Virtual_Test", "Таблица 1");
            var dt = VirtualTableRegistry.GetData("Virtual_Test", roleName: "default", required: PermissionFlags.Read);
            Assert.NotNull(dt);
            Assert.True(dt.Rows.Count > 0);
        }

        [DbFact]
        public void RoleManager_EffectivePermissions_Default()
        {
            RoleManager.InitializeFromDb();
            var flags = RoleManager.GetEffectivePermissions("no-role", "Таблица 1");
            Assert.Equal(PermissionFlags.None, flags);
        }

        [DbStaFact]
        public void DataGridView_Smoke_SelectMatch()
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add(1, "Ivan");

            var match = Scraps.Data.DataTables.Search.FindMatches(dt, "Ivan").FirstOrDefault();
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
