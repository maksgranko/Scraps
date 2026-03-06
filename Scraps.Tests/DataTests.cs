using Scraps.Databases;
using System.Data;
using Xunit;

namespace Scraps.Tests
{
    [Collection("Db")]
    public class DataTests
    {
        [DbFact]
        public void FindByColumn_Works()
        {
            var dt = MSSQL.FindByColumn("Таблица 1", "Name", "Ivan", useLike: true);
            Assert.True(dt.Rows.Count >= 1);
        }

        [DbFact]
        public void ApplyTableChanges_InsertsRow()
        {
            var dt = MSSQL.GetTableData("Таблица 1");
            var newRow = dt.NewRow();
            newRow["Name"] = "NewName";
            dt.Rows.Add(newRow);

            var affected = MSSQL.ApplyTableChanges("Таблица 1", dt);
            Assert.True(affected >= 1);
        }

        [DbFact]
        public void FindByColumn_NullValue_Works()
        {
            const string table = "FindNullTest";
            try
            {
                MSSQL.ExecuteNonQuery(
                    "IF OBJECT_ID(N'[FindNullTest]','U') IS NULL " +
                    "CREATE TABLE [FindNullTest] ([Id] int IDENTITY(1,1) PRIMARY KEY, [Name] nvarchar(50) NULL);");
                MSSQL.ExecuteNonQuery("INSERT INTO [FindNullTest]([Name]) VALUES (NULL);");

                var dt = MSSQL.FindByColumn(table, "Name", null);
                Assert.True(dt.Rows.Count >= 1);
            }
            finally
            {
                MSSQL.ExecuteNonQuery("IF OBJECT_ID(N'[FindNullTest]','U') IS NOT NULL DROP TABLE [FindNullTest];");
            }
        }
    }
}

