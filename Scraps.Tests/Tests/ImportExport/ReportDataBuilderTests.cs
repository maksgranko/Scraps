using Scraps.Configs;
using Scraps.Export;
using Scraps.Localization;
using Scraps.Tests.Setup;
using System.Data;
using Xunit;

namespace Scraps.Tests.ImportExport
{
    [Collection("Db")]
    public class ReportDataBuilderTests
    {
        [DbFact]
        public void GetBySql_UsesTranslations()
        {
            if (TestDatabaseConfig.Provider == DatabaseProvider.LocalFiles)
                return; // SQL not supported in LocalFiles

            TranslationManager.Translations.Clear();

            TranslationManager.Translations[TranslationManager.ColumnKey("Таблица 1", "Name")] = "Имя";

            var dt = ReportDataBuilder.GetBySql("SELECT Name FROM [Таблица 1]", "Таблица 1");
            Assert.True(dt.Columns.Contains("Имя"));
        }

        [DbFact]
        public void GetBySql_TranslatesWhenRequested()
        {
            if (TestDatabaseConfig.Provider == DatabaseProvider.LocalFiles)
                return; // SQL not supported in LocalFiles

            TranslationManager.Translations.Clear();

            TranslationManager.Translations[TranslationManager.ColumnKey("Таблица 1", "Name")] = "Имя";

            var dt = ReportDataBuilder.GetBySql("SELECT Name FROM [Таблица 1]", "Таблица 1");
            Assert.True(dt.Columns.Contains("Имя"));
        }
    }
}




