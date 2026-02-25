using Scraps.Databases;
using System.Data;
using Xunit;

namespace Scraps.Tests
{
    [Collection("Db")]
    public class DataTests
    {
        [Fact]
        public void FindByColumn_Works()
        {
            var dt = MSSQL.FindByColumn("Таблица 1", "Name", "Ivan", useLike: true);
            Assert.True(dt.Rows.Count >= 1);
        }

        [Fact]
        public void ApplyTableChanges_InsertsRow()
        {
            var dt = MSSQL.GetTableData("Таблица 1");
            var newRow = dt.NewRow();
            newRow["Name"] = "NewName";
            dt.Rows.Add(newRow);

            var affected = MSSQL.ApplyTableChanges("Таблица 1", dt);
            Assert.True(affected >= 1);
        }
    }
}
