using System;
using Scraps.Database.MSSQL;
using Scraps.Database.MSSQL.Utilities;
using Xunit;

namespace Scraps.Tests.Database
{
    public class TableCatalogUnitTests
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
        public void InitializeTables_WithRegistryNames_Works()
        {
            VirtualTableRegistry.Clear();
            VirtualTableRegistry.RegisterSelect("Virtual_Reg", "Таблица 1");
            var tables = TableCatalog.InitializeTables(autodetect: false, manualTables: new string[0], virtualTables: VirtualTableRegistry.GetNames());
            Assert.Contains("Virtual_Reg", tables);
        }

        [Fact]
        public void InitializeTables_RemoveOnStart_Works()
        {
            var tables = TableCatalog.InitializeTables(
                autodetect: false,
                manualTables: new[] { "A", "B" },
                removeOnStart: new[] { "B" });

            Assert.Contains("A", tables);
            Assert.DoesNotContain("B", tables);
        }

        [Fact]
        public void VirtualTableRegistry_BuildSelectQuery_UnsafeWhere_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                VirtualTableRegistry.BuildSelectQuery("Users", where: "1=1; DROP TABLE Users"));
        }
    }
}
