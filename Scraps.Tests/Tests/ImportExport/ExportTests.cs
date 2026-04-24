using Scraps.Databases;
using Scraps.Export;
using Scraps.Tests.Setup;
using System;
using System.Data;
using System.IO;
using Xunit;

namespace Scraps.Tests.ImportExport
{
    [Collection("Db")]
    public class ExportTests
    {
        [DbFact]
        public void ExportToExcel_WritesFile()
        {
            var dt = MSSQL.GetTableData("Таблица 1");
            var path = Path.Combine(Path.GetTempPath(), "scraps_test_" + Guid.NewGuid().ToString("N") + ".xlsx");
            if (File.Exists(path)) File.Delete(path);

            ReportExporter.ExportToExcel(dt, path);
            Assert.True(File.Exists(path));

            File.Delete(path);
        }

        [DbFact]
        public void ExportToPdf_WritesFile()
        {
            if (!File.Exists("c:\\windows\\fonts\\arial.ttf"))
                throw new FileNotFoundException("Для PDF-теста нужен шрифт Arial.", "c:\\windows\\fonts\\arial.ttf");

            var dt = MSSQL.GetTableData("Таблица 1");
            var path = Path.Combine(Path.GetTempPath(), "scraps_test_" + Guid.NewGuid().ToString("N") + ".pdf");
            if (File.Exists(path)) File.Delete(path);

            ReportExporter.ExportToPdf(dt, path, "Тест");
            Assert.True(File.Exists(path));

            File.Delete(path);
        }
    }
}


