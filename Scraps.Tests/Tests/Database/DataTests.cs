using Scraps.Database;
using Scraps.Database.MSSQL;
using Scraps.Tests.Setup;
using System.Data;
using Xunit;
using Db = Scraps.Database.Current;

namespace Scraps.Tests.Database
{
    [Collection("Db")]
    public class DataTests
    {
        [DbFact]
        public void FindByColumn_Works()
        {
            var dt = Db.FindByColumn("Таблица 1", "Name", "Ivan", op: SqlFilterOperator.Like);
            Assert.True(dt.Rows.Count >= 1);
        }

        [DbFact]
        public void ApplyTableChanges_InsertsRow()
        {
            var dt = Db.GetTableData("Таблица 1");
            var newRow = dt.NewRow();
            newRow["Name"] = "NewName";
            dt.Rows.Add(newRow);

            Db.ApplyTableChanges("Таблица 1", dt);
            var updated = Db.GetTableData("Таблица 1");
            Assert.True(updated.Rows.Count >= 1);
        }

        [DbFact]
        public void FindByColumn_NullValue_Works()
        {
            if (TestDatabaseConfig.Provider == Scraps.Configs.DatabaseProvider.LocalFiles)
                return; // raw SQL not supported in LocalFiles

            const string table = "FindNullTest";
            try
            {
                MSSQL.ExecuteNonQuery(
                    "IF OBJECT_ID(N'[FindNullTest]','U') IS NULL " +
                    "CREATE TABLE [FindNullTest] ([Id] int IDENTITY(1,1) PRIMARY KEY, [Name] nvarchar(50) NULL);");
                MSSQL.ExecuteNonQuery("INSERT INTO [FindNullTest]([Name]) VALUES (NULL);");

                var dt = Db.FindByColumn(table, "Name", null, SqlFilterOperator.IsNull);
                Assert.True(dt.Rows.Count >= 1);
            }
            finally
            {
                MSSQL.ExecuteNonQuery("IF OBJECT_ID(N'[FindNullTest]','U') IS NOT NULL DROP TABLE [FindNullTest];");
            }
        }
    }
}
