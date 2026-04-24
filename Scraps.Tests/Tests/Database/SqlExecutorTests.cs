using Scraps.Configs;
using Scraps.Database.LocalFiles;
using Scraps.Database.LocalFiles.Sql;
using System;
using System.Data;
using System.IO;
using System.Linq;
using Xunit;

namespace Scraps.Tests.Database
{
    public class SqlExecutorTests : IDisposable
    {
        private readonly string _testPath;

        public SqlExecutorTests()
        {
            _testPath = Path.Combine(Path.GetTempPath(), "Scraps_SqlTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testPath);
            ScrapsConfig.LocalDataPath = _testPath;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testPath))
                    Directory.Delete(_testPath, true);
            }
            catch { }
        }

        [Fact]
        public void CreateTable_And_Select()
        {
            SqlExecutor.ExecuteNonQuery("CREATE TABLE [Test] ([Id] int, [Name] nvarchar(50))");

            var dt = SqlExecutor.ExecuteQuery("SELECT * FROM [Test]");
            Assert.NotNull(dt);
            Assert.Equal(2, dt.Columns.Count);
            Assert.Equal("Id", dt.Columns[0].ColumnName);
            Assert.Equal("Name", dt.Columns[1].ColumnName);
        }

        [Fact]
        public void Insert_And_Select()
        {
            SqlExecutor.ExecuteNonQuery("CREATE TABLE [Users] ([Id] int, [Name] nvarchar(50))");
            SqlExecutor.ExecuteNonQuery("INSERT INTO [Users] ([Id], [Name]) VALUES (1, 'Ivan')");

            var dt = SqlExecutor.ExecuteQuery("SELECT * FROM [Users] WHERE [Id] = 1");
            Assert.Single(dt.Rows);
            Assert.Equal("Ivan", dt.Rows[0]["Name"]);
        }

        [Fact]
        public void Update_And_Select()
        {
            SqlExecutor.ExecuteNonQuery("CREATE TABLE [Users] ([Id] int, [Name] nvarchar(50))");
            SqlExecutor.ExecuteNonQuery("INSERT INTO [Users] ([Id], [Name]) VALUES (1, 'Ivan')");
            SqlExecutor.ExecuteNonQuery("UPDATE [Users] SET [Name] = 'Petr' WHERE [Id] = 1");

            var dt = SqlExecutor.ExecuteQuery("SELECT * FROM [Users] WHERE [Id] = 1");
            Assert.Equal("Petr", dt.Rows[0]["Name"]);
        }

        [Fact]
        public void Delete_And_Select()
        {
            SqlExecutor.ExecuteNonQuery("CREATE TABLE [Users] ([Id] int, [Name] nvarchar(50))");
            SqlExecutor.ExecuteNonQuery("INSERT INTO [Users] ([Id], [Name]) VALUES (1, 'Ivan')");
            SqlExecutor.ExecuteNonQuery("DELETE FROM [Users] WHERE [Id] = 1");

            var dt = SqlExecutor.ExecuteQuery("SELECT * FROM [Users]");
            Assert.Empty(dt.Rows);
        }

        [Fact]
        public void IfObjectId_ConditionalDrop()
        {
            SqlExecutor.ExecuteNonQuery("CREATE TABLE [Temp] ([Id] int)");
            Assert.True(File.Exists(Path.Combine(_testPath, "Temp.json")));

            SqlExecutor.ExecuteNonQuery("IF OBJECT_ID(N'[Temp]','U') IS NOT NULL DROP TABLE [Temp]");
            Assert.False(File.Exists(Path.Combine(_testPath, "Temp.json")));
        }

        [Fact]
        public void IfObjectId_CreateTable_WithIdentity()
        {
            // Exact SQL from DatabaseFixture
            SqlExecutor.ExecuteNonQuery(
                "IF OBJECT_ID(N'[Таблица 1]','U') IS NULL " +
                "CREATE TABLE [Таблица 1] ([Id] int IDENTITY(1,1) PRIMARY KEY, [Name] nvarchar(50) NOT NULL);");

            Assert.True(File.Exists(Path.Combine(_testPath, "Таблица 1.json")));

            var dt = SqlExecutor.ExecuteQuery("SELECT * FROM [Таблица 1]");
            Assert.Equal(2, dt.Columns.Count);
        }

        [Fact]
        public void Select_Count()
        {
            SqlExecutor.ExecuteNonQuery("CREATE TABLE [Items] ([Id] int, [Name] nvarchar(50))");
            SqlExecutor.ExecuteNonQuery("INSERT INTO [Items] ([Id], [Name]) VALUES (1, 'A')");
            SqlExecutor.ExecuteNonQuery("INSERT INTO [Items] ([Id], [Name]) VALUES (2, 'B')");

            var count = SqlExecutor.ExecuteScalar("SELECT COUNT(*) FROM [Items]");
            Assert.Equal(2, count);
        }

        [Fact]
        public void Where_Like()
        {
            SqlExecutor.ExecuteNonQuery("CREATE TABLE [Users] ([Id] int, [Name] nvarchar(50))");
            SqlExecutor.ExecuteNonQuery("INSERT INTO [Users] ([Id], [Name]) VALUES (1, 'Ivan')");
            SqlExecutor.ExecuteNonQuery("INSERT INTO [Users] ([Id], [Name]) VALUES (2, 'Petr')");

            var dt = SqlExecutor.ExecuteQuery("SELECT * FROM [Users] WHERE [Name] LIKE '%van%'");
            Assert.Single(dt.Rows);
            Assert.Equal("Ivan", dt.Rows[0]["Name"]);
        }

        [Fact]
        public void Identity_AutoIncrement()
        {
            SqlExecutor.ExecuteNonQuery("CREATE TABLE [Auto] ([Id] int IDENTITY(1,1), [Name] nvarchar(50))");
            SqlExecutor.ExecuteNonQuery("INSERT INTO [Auto] ([Name]) VALUES ('First')");
            SqlExecutor.ExecuteNonQuery("INSERT INTO [Auto] ([Name]) VALUES ('Second')");

            var dt = SqlExecutor.ExecuteQuery("SELECT * FROM [Auto] ORDER BY [Id]");
            Assert.Equal(2, dt.Rows.Count);
            Assert.Equal(1, dt.Rows[0]["Id"]);
            Assert.Equal(2, dt.Rows[1]["Id"]);
        }
    }
}
