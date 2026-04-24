using Scraps.Database.MSSQL.Utilities;
using Xunit;
using Scraps.Tests.Setup;

namespace Scraps.Tests.Database
{
    [Collection("Db")]
    public class TableCatalogDbTests
    {
        [DbFact]
        public void InitializeTables_RemoveOnAutodetect_Works()
        {
            var tables = TableCatalog.InitializeTables(
                autodetect: true,
                manualTables: null,
                removeOnAutodetect: new[] { "Users" });

            Assert.DoesNotContain("Users", tables);
        }
    }
}
