using Scraps.Databases;
using Scraps.Security;
using System.Linq;
using System.Windows.Forms;
using Xunit;

namespace Scraps.Tests
{
    [Collection("Db")]
    public class ScrapsTests
    {
        [Fact]
        public void QuoteIdentifier_WrapsNamesWithSpaces()
        {
            var name = "Таблица 1";
            var quoted = MSSQL.QuoteIdentifier(name);
            Assert.Equal("[Таблица 1]", quoted);
        }

        [Fact]
        public void QuoteIdentifier_WrapsSchemaQualified()
        {
            var name = "dbo.Table";
            var quoted = MSSQL.QuoteIdentifier(name);
            Assert.Equal("[dbo].[Table]", quoted);
        }

        [Fact]
        public void ConnectionStringBuilder_AllowsDirectString()
        {
            var input = "Data Source=.;Initial Catalog=Test;Integrated Security=True;";
            var output = MSSQL.ConnectionStringBuilder(input);
            Assert.Equal(input, output);
        }

        [Fact]
        public void GetTableData_WorksWithSpaceInName()
        {
            var dt = MSSQL.GetTableData("Таблица 1");
            Assert.NotNull(dt);
            Assert.True(dt.Rows.Count > 0);
        }

        [Fact]
        public void VirtualTableRegistry_SelectWorks()
        {
            VirtualTableRegistry.RegisterSelect("Virtual_Test", "Таблица 1");
            var dt = VirtualTableRegistry.GetData("Virtual_Test", roleName: null, required: PermissionFlags.Read);
            Assert.NotNull(dt);
            Assert.True(dt.Rows.Count > 0);
        }

        [Fact]
        public void RoleManager_EffectivePermissions_Default()
        {
            RoleManager.InitializeFromDb();
            var flags = RoleManager.GetEffectivePermissions("no-role", "Таблица 1");
            Assert.Equal(PermissionFlags.None, flags);
        }

        [StaFact]
        public void DataGridView_Smoke_SelectMatch()
        {
            var dt = MSSQL.GetTableData("Таблица 1");
            var match = Scraps.Data.DataTableSearch.FindMatches(dt, "Ivan").FirstOrDefault();
            Assert.NotNull(match);

            var grid = new DataGridView();
            grid.DataSource = dt;

            var col = grid.Columns[match.ColumnName];
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
