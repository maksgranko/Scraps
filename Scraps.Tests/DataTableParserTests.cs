using Scraps.Data.DataTables;
using System.Data;
using Xunit;

namespace Scraps.Tests
{
    public class ParserTests
    {
        [Fact]
        public void ParseDelimited_WithHeader_Works()
        {
            var input = "Name,Age\nIvan,20";
            var dt = Parser.ParseDelimited(input);

            Assert.Equal(2, dt.Columns.Count);
            Assert.Equal("Name", dt.Columns[0].ColumnName);
            Assert.Equal("Age", dt.Columns[1].ColumnName);
            Assert.Equal(1, dt.Rows.Count);
            Assert.Equal("Ivan", dt.Rows[0][0]);
            Assert.Equal("20", dt.Rows[0][1]);
        }

        [Fact]
        public void ParseDelimited_WithoutHeader_Works()
        {
            var input = "Ivan,20\nPetr,30";
            var dt = Parser.ParseDelimited(input, hasHeader: false);

            Assert.Equal(2, dt.Columns.Count);
            Assert.Equal("Column1", dt.Columns[0].ColumnName);
            Assert.Equal("Column2", dt.Columns[1].ColumnName);
            Assert.Equal(2, dt.Rows.Count);
        }

        [Fact]
        public void ParseDelimited_ExpandsColumns_WhenNeeded()
        {
            var input = "A,B\n1,2,3";
            var dt = Parser.ParseDelimited(input);

            Assert.Equal(3, dt.Columns.Count);
            Assert.Equal("3", dt.Rows[0][2]);
        }

        [Fact]
        public void ParseDelimited_TrimsValues()
        {
            var input = "Name,Age\n Ivan , 20 ";
            var dt = Parser.ParseDelimited(input, trim: true);

            Assert.Equal("Ivan", dt.Rows[0][0]);
            Assert.Equal("20", dt.Rows[0][1]);
        }
    }
}





