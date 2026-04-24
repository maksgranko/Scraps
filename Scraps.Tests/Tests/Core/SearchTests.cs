using Scraps.Data.DataTables;
using Scraps.Databases;
using Scraps.Tests.Setup;
using Xunit;

namespace Scraps.Tests.Core
{
    [Collection("Db")]
    public class DbSearchTests
    {
        [DbFact]
        public void Navigator_Works()
        {
            var dt = MSSQL.GetTableData("Таблица 1");
            var nav = Search.CreateNavigator(dt, "Ivan");
            var first = nav.First();
            Assert.NotNull(first);
            var next = nav.Next();
            Assert.NotNull(next);
        }

        [DbFact]
        public void FilterRows_ByColumn()
        {
            var dt = MSSQL.GetTableData("Таблица 1");
            var filtered = Search.FilterRows(dt, "Name", "Ivan");
            Assert.True(filtered.Rows.Count >= 1);
        }
    }
}






