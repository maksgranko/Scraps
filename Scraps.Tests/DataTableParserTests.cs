using Scraps.Data.DataTables;
using System;
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

        [Fact]
        public void ParseNx2ToDictionary_FromText_Works()
        {
            var input = "1 Отлично\n2 Хорошо\n3 Плохо\n4 Неизвестно";
            var dict = Parser.ParseNx2ToDictionary(input);

            Assert.Equal(4, dict.Count);
            Assert.Equal("Отлично", dict[1]);
            Assert.Equal("Хорошо", dict[2]);
            Assert.Equal("Плохо", dict[3]);
            Assert.Equal("Неизвестно", dict[4]);
        }

        [Fact]
        public void ParseNx2ToDictionary_FromDataTable_Works()
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Title", typeof(string));
            dt.Rows.Add(1, "Отлично");
            dt.Rows.Add(2, "Хорошо");

            var dict = Parser.ParseNx2ToDictionary(dt);

            Assert.Equal(2, dict.Count);
            Assert.Equal("Отлично", dict[1]);
            Assert.Equal("Хорошо", dict[2]);
        }

        [Fact]
        public void ParseNx2ToDictionary_ThrowsOnDuplicateKey()
        {
            var input = "1 First\n1 Second";
            Assert.Throws<ArgumentException>(() => Parser.ParseNx2ToDictionary(input));
        }

        [Fact]
        public void ParseNx2ToDictionary_WithCustomColumnAndRowSeparators_Works()
        {
            var input = "1=>Отлично|2=>Хорошо|3=>Плохо";
            var dict = Parser.ParseNx2ToDictionary(input, "=>", "|");

            Assert.Equal(3, dict.Count);
            Assert.Equal("Отлично", dict[1]);
            Assert.Equal("Хорошо", dict[2]);
            Assert.Equal("Плохо", dict[3]);
        }

        [Fact]
        public void ParseNx1ToList_FromText_Works()
        {
            var input = "Антон\nАндрей\nВасилий";
            var list = Parser.ParseNx1ToList(input);

            Assert.Equal(3, list.Count);
            Assert.Equal("Антон", list[0]);
            Assert.Equal("Андрей", list[1]);
            Assert.Equal("Василий", list[2]);
        }

        [Fact]
        public void ParseNx1ToList_FromDataTable_Works()
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add("Антон");
            dt.Rows.Add(" ");
            dt.Rows.Add("Андрей");

            var list = Parser.ParseNx1ToList(dt, valueColumnIndex: 0, trim: true, skipEmpty: true);
            Assert.Equal(2, list.Count);
            Assert.Equal("Антон", list[0]);
            Assert.Equal("Андрей", list[1]);
        }
    }
}





