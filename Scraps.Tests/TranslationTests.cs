using Scraps.Localization;
using System.Collections.Generic;
using System.Data;
using Xunit;

namespace Scraps.Tests
{
    public class TranslationTests
    {
        [Fact]
        public void TranslateTableName_Works()
        {
            TranslationManager.TableTranslations.Clear();
            TranslationManager.ColumnTranslations.Clear();

            TranslationManager.TableTranslations["Users"] = "Пользователи";
            Assert.Equal("Пользователи", TranslationManager.TranslateTableName("Users"));
            Assert.Equal("Users", TranslationManager.UntranslateTableName("Пользователи"));
        }

        [Fact]
        public void TranslateDataTable_Works()
        {
            TranslationManager.TableTranslations.Clear();
            TranslationManager.ColumnTranslations.Clear();

            TranslationManager.ColumnTranslations["Users"] = new Dictionary<string, string>
            {
                ["Login"] = "Логин"
            };

            var dt = new DataTable();
            dt.Columns.Add("Login");
            dt.Rows.Add("admin");

            TranslationManager.TranslateDataTable(dt, "Users");
            Assert.True(dt.Columns.Contains("Логин"));

            TranslationManager.UntranslateDataTable(dt, "Users");
            Assert.True(dt.Columns.Contains("Login"));
        }

        [Fact]
        public void TranslateTableList_Works()
        {
            TranslationManager.TableTranslations.Clear();
            TranslationManager.ColumnTranslations.Clear();

            TranslationManager.TableTranslations["ImportTest"] = "Импорт";
            var list = TranslationManager.TranslateTableList(new[] { "ImportTest" });
            Assert.Equal("Импорт", list[0]);
        }
    }
}
