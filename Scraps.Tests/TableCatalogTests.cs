using Scraps.Databases;
using Scraps.Databases.Utilities;
using Xunit;

namespace Scraps.Tests
{
    [Collection("Db")]
    public class TableCatalogTests
    {
        [DbFact]
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

        [DbFact]
        public void InitializeTablesWithRegistry_Works()
        {
            VirtualTableRegistry.Clear();
            VirtualTableRegistry.RegisterSelect("Virtual_Reg", "Таблица 1");
            var tables = TableCatalog.InitializeTablesWithRegistry(autodetect: false, manualTables: new string[0]);
            Assert.Contains("Virtual_Reg", tables);
        }

        [DbFact]
        public void InitializeTables_RemoveOnStart_Works()
        {
            var tables = TableCatalog.InitializeTables(
                autodetect: false,
                manualTables: new[] { "A", "B" },
                removeOnStart: new[] { "B" });

            Assert.Contains("A", tables);
            Assert.DoesNotContain("B", tables);
        }

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





