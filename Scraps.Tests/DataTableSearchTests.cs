using Scraps.Data.DataTable;
using System.Data;
using Xunit;

namespace Scraps.Tests
{
    public class DataTableSearchTests
    {
        [Fact]
        public void Navigator_Prev_FromStart_ReturnsNull()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name");
            dt.Rows.Add("Ivan");

            var nav = DataTableSearch.CreateNavigator(dt, "Ivan");
            var prev = nav.Prev();

            Assert.Null(prev);
        }

        [Fact]
        public void Navigator_Prev_WrapsToLast()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name");
            dt.Rows.Add("Ivan");
            dt.Rows.Add("Ivan");

            var nav = DataTableSearch.CreateNavigator(dt, "Ivan");
            var prev = nav.Prev(wrap: true);

            Assert.NotNull(prev);
            Assert.Equal(1, prev.RowIndex);
        }
    }
}

