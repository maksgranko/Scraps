using Scraps.Databases;
using Scraps.Export;
using System;
using System.Data;
using System.IO;
using Xunit;

namespace Scraps.Tests
{
    [Collection("Db")]
    public class ExportTests
    {
        [Fact]
        public void ExportToExcel_WritesFile()
        {
            var dt = MSSQL.GetTableData("Таблица 1");
            var path = Path.Combine(Path.GetTempPath(), "scraps_test.xlsx");
            if (File.Exists(path)) File.Delete(path);

            ReportExporter.ExportToExcel(dt, path);
            Assert.True(File.Exists(path));

            File.Delete(path);
        }

        [Fact]
        public void ExportToPdf_WritesFile()
        {
            var dt = MSSQL.GetTableData("Таблица 1");
            var path = Path.Combine(Path.GetTempPath(), "scraps_test.pdf");
            if (File.Exists(path)) File.Delete(path);

            ReportExporter.ExportToPdf(dt, path, "Тест");
            Assert.True(File.Exists(path));

            File.Delete(path);
        }
    }
}
