using Scraps.Data.DataTables;
using Scraps.Database;
using Scraps.Tests.Setup;
using Xunit;
using Db = Scraps.Database.Current;

namespace Scraps.Tests.Core
{
    [Collection("Db")]
    public class DbSearchTests
    {
        [DbFact]
        public void Navigator_Works()
        {
            var dt = Db.GetTableData("Таблица 1");
            var nav = Search.CreateNavigator(dt, "Ivan");
            var first = nav.First();
            Assert.NotNull(first);
            var next = nav.Next();
            Assert.NotNull(next);
        }

        [DbFact]
        public void FilterRows_ByColumn()
        {
            var dt = Db.GetTableData("Таблица 1");
            var filtered = Search.FilterRows(dt, "Name", "Ivan");
            Assert.True(filtered.Rows.Count >= 1);
        }
    }
}






