using Scraps.Export;
using Scraps.Localization;
using System.Data;
using Xunit;

namespace Scraps.Tests
{
    [Collection("Db")]
    public class ReportDataBuilderTests
    {
        [DbFact]
        public void GetTableTranslated_UsesTranslations()
        {
            TranslationManager.TableTranslations.Clear();
            TranslationManager.ColumnTranslations.Clear();

            TranslationManager.ColumnTranslations["Таблица 1"] = new System.Collections.Generic.Dictionary<string, string>
            {
                ["Name"] = "Имя"
            };

            var dt = ReportDataBuilder.GetTableTranslated("Таблица 1");
            Assert.True(dt.Columns.Contains("Имя"));
        }

        [DbFact]
        public void GetBySql_TranslatesWhenRequested()
        {
            TranslationManager.TableTranslations.Clear();
            TranslationManager.ColumnTranslations.Clear();

            TranslationManager.ColumnTranslations["Таблица 1"] = new System.Collections.Generic.Dictionary<string, string>
            {
                ["Name"] = "Имя"
            };

            var dt = ReportDataBuilder.GetBySql("SELECT Name FROM [Таблица 1]", "Таблица 1");
            Assert.True(dt.Columns.Contains("Имя"));
        }
    }
}

