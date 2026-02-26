using Scraps.Databases;
using Scraps.Import;
using Scraps.Localization;
using Scraps.Security;
using System.Collections.Generic;
using System.Data;
using Xunit;

namespace Scraps.Tests
{
    [Collection("Db")]
    public class ImportTests
    {
        [Fact]
        public void ValidateColumns_WithTranslations()
        {
            TranslationManager.ColumnTranslations.Clear();
            TranslationManager.TableTranslations.Clear();

            TranslationManager.ColumnTranslations["ImportTest"] = new Dictionary<string, string>
            {
                ["Name"] = "Имя"
            };

            var dt = new DataTable();
            dt.Columns.Add("Имя");
            dt.Rows.Add("Ivan");

            var ok = DataImportService.ValidateColumns(dt, "ImportTest", out var missing, allowTranslatedColumns: true);
            Assert.True(ok);
            Assert.Empty(missing);
        }

        [Fact]
        public void ValidateColumnCount_Works()
        {
            TranslationManager.ColumnTranslations.Clear();
            TranslationManager.TableTranslations.Clear();

            var dt = new DataTable();
            dt.Columns.Add("Name");
            var ok = DataImportService.ValidateColumnCount(dt, "ImportTest", out int expected, out int actual);
            Assert.True(ok);
            Assert.Equal(1, expected);
            Assert.Equal(1, actual);
        }

        [Fact]
        public void ValidateTypes_DetectsMismatch()
        {
            TranslationManager.ColumnTranslations.Clear();
            TranslationManager.TableTranslations.Clear();

            var dt = new DataTable();
            dt.Columns.Add("Id");
            dt.Columns.Add("Name");
            dt.Rows.Add("abc", "Ivan");

            var ok = DataImportService.ValidateTypes(dt, "Таблица 1", out var errors, allowTranslatedColumns: true);
            Assert.False(ok);
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void ImportToTable_Works()
        {
            TranslationManager.ColumnTranslations.Clear();
            TranslationManager.TableTranslations.Clear();

            var dt = new DataTable();
            dt.Columns.Add("Name");
            dt.Rows.Add("Petr");

            var count = DataImportService.ImportToTable("ImportTest", dt);
            Assert.Equal(1, count);

            var data = MSSQL.GetTableData("ImportTest");
            bool found = false;
            foreach (DataRow row in data.Rows)
            {
                if (row["Name"].ToString() == "Petr")
                {
                    found = true;
                    break;
                }
            }
            Assert.True(found);
        }
        [Fact]
        public void ValidateColumns_MissingColumns_ReturnsErrors()
        {
            TranslationManager.ColumnTranslations.Clear();
            TranslationManager.TableTranslations.Clear();

            var dt = new DataTable();
            dt.Columns.Add("Other");
            dt.Rows.Add("x");

            var ok = DataImportService.ValidateColumns(dt, "ImportTest", out var missing, allowTranslatedColumns: true);
            Assert.False(ok);
            Assert.NotEmpty(missing);
            Assert.Contains("Name", missing);
        }

        [Fact]
        public void BulkInsert_UsesTranslatedColumns()
        {
            TranslationManager.ColumnTranslations.Clear();
            TranslationManager.TableTranslations.Clear();

            TranslationManager.ColumnTranslations["ImportTest"] = new Dictionary<string, string>
            {
                ["Name"] = "Имя"
            };

            var dt = new DataTable();
            dt.Columns.Add("Имя");
            dt.Rows.Add("Alex");

            var count = MSSQL.BulkInsert("ImportTest", dt);
            Assert.Equal(1, count);

            var data = MSSQL.GetTableData("ImportTest");
            bool found = false;
            foreach (DataRow row in data.Rows)
            {
                if (row["Name"].ToString() == "Alex")
                {
                    found = true;
                    break;
                }
            }
            Assert.True(found);
        }
        [Fact]
        public void ValidateImportAccess_DeniesWhenNoRights()\r
        {
            RoleManager.Initialize(new[]
            {
                new Role("Limited", "ImportTest", PermissionFlags.Read)
            });

            var ok = DataImportService.ValidateImportAccess("Limited", "ImportTest", PermissionFlags.Import, out var error);
            Assert.False(ok);
            Assert.False(string.IsNullOrWhiteSpace(error));
        }
    }
}


