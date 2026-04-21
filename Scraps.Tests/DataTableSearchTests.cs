using Scraps.Data.DataTables;
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

            var nav = Search.CreateNavigator(dt, "Ivan");
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

            var nav = Search.CreateNavigator(dt, "Ivan");
            var prev = nav.Prev(wrap: true);

            Assert.NotNull(prev);
            Assert.Equal(1, prev.RowIndex);
        }

        [Fact]
        public void Search_FullApi_Smoke_Works()
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add(1, "Ivan");
            dt.Rows.Add(2, "Petr");
            dt.Rows.Add(3, "Ivan");

            var allMatches = Search.FindMatches(dt, "Ivan");
            Assert.Equal(2, allMatches.Count);

            var columnMatches = Search.FindMatches(dt, "Name", "Ivan");
            Assert.Equal(2, columnMatches.Count);

            var filteredAll = Search.FilterRows(dt, "Ivan");
            Assert.Equal(2, filteredAll.Rows.Count);

            var filteredColumn = Search.FilterRows(dt, "Name", "Ivan");
            Assert.Equal(2, filteredColumn.Rows.Count);

            var rowIndicesAll = Search.GetMatchRowIndices(dt, "Ivan");
            Assert.Equal(2, rowIndicesAll.Length);

            var rowIndicesColumn = Search.GetMatchRowIndices(dt, "Name", "Ivan");
            Assert.Equal(2, rowIndicesColumn.Length);

            var navByColumn = Search.CreateNavigator(dt, "Name", "Ivan");
            Assert.Equal(2, navByColumn.Count);
            Assert.Equal(-1, navByColumn.Index);
            Assert.Null(navByColumn.Current);
            var last = navByColumn.Last();
            Assert.NotNull(last);
            Assert.Equal(2, last.RowIndex);
            Assert.NotNull(navByColumn.Current);
        }
    }
}





