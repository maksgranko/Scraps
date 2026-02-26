using Scraps.Data.DataTable;
using Scraps.Databases;
using Xunit;

namespace Scraps.Tests
{
    [Collection("Db")]
    public class SearchTests
    {
        [Fact]
        public void Navigator_Works()
        {
            var dt = MSSQL.GetTableData("Таблица 1");
            var nav = DataTableSearch.CreateNavigator(dt, "Ivan");
            var first = nav.First();
            Assert.NotNull(first);
            var next = nav.Next();
            Assert.NotNull(next);
        }

        [Fact]
        public void FilterRows_ByColumn()
        {
            var dt = MSSQL.GetTableData("Таблица 1");
            var filtered = DataTableSearch.FilterRows(dt, "Name", "Ivan");
            Assert.True(filtered.Rows.Count >= 1);
        }
    }
}

