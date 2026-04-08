using Scraps.Databases.Utilities;
using Xunit;

namespace Scraps.Tests
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
