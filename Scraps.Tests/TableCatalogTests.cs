using Scraps.Databases;
using Xunit;

namespace Scraps.Tests
{
    [Collection("Db")]
    public class TableCatalogTests
    {
        [Fact]
        public void InitializeTables_WithVirtuals()
        {
            VirtualTableRegistry.Clear();
            var tables = TableCatalog.InitializeTables(
                autodetect: false,
                manualTables: new[] { "Users" },
                virtualTables: new[] { "Virtual_One" });

            Assert.Contains("Users", tables);
            Assert.Contains("Virtual_One", tables);
        }

        [Fact]
        public void InitializeTablesWithRegistry_Works()
        {
            VirtualTableRegistry.Clear();
            VirtualTableRegistry.RegisterSelect("Virtual_Reg", "Таблица 1");
            var tables = TableCatalog.InitializeTablesWithRegistry(autodetect: false, manualTables: new string[0]);
            Assert.Contains("Virtual_Reg", tables);
        }
    }
}
