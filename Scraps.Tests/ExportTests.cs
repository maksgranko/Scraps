using Scraps.Databases;
using Scraps.Export;
using System;
using System.Data;
using System.IO;
using Xunit;
using Xunit.Sdk;

namespace Scraps.Tests
{
    [Collection("Db")]
    public class ExportTests
    {
        [Fact]
        public void ExportToExcel_WritesFile()
        {
            var dt = MSSQL.GetTableData("Таблица 1");
            var path = Path.Combine(Path.GetTempPath(), "scraps_test_" + Guid.NewGuid().ToString("N") + ".xlsx");
            if (File.Exists(path)) File.Delete(path);

            ReportExporter.ExportToExcel(dt, path);
            Assert.True(File.Exists(path));

            File.Delete(path);
        }

        [Fact]
        public void ExportToPdf_WritesFile()
        {
            if (!File.Exists("c:\\windows\\fonts\\arial.ttf"))
                throw new SkipException("Шрифт Arial не найден. Пропускаем PDF-тест.");

            var dt = MSSQL.GetTableData("Таблица 1");
            var path = Path.Combine(Path.GetTempPath(), "scraps_test_" + Guid.NewGuid().ToString("N") + ".pdf");
            if (File.Exists(path)) File.Delete(path);

            ReportExporter.ExportToPdf(dt, path, "Тест");
            Assert.True(File.Exists(path));

            File.Delete(path);
        }
    }
}
