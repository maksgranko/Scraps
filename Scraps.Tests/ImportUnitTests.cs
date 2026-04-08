using Scraps.Import;
using Scraps.Security;
using System.IO;
using Xunit;

namespace Scraps.Tests
{
    public class ImportUnitTests
    {
        [Fact]
        public void LoadCsvToDataTable_AutoDetectDelimiter_Works()
        {
            var file = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file, "Name;Age\nIvan;20");
                var dt = DataImportService.LoadCsvToDataTable(file, new[] { ',', ';', '\t' }, autoDetectDelimiter: true);
                Assert.Equal(2, dt.Columns.Count);
                Assert.Equal("Ivan", dt.Rows[0]["Name"]);
                Assert.Equal("20", dt.Rows[0]["Age"]);
            }
            finally
            {
                if (File.Exists(file)) File.Delete(file);
            }
        }

        [Fact]
        public void ValidateImportAccess_DeniesWhenNoRights()
        {
            RoleManager.Initialize(new[]
            {
                new Role("Limited", "ImportTest", PermissionFlags.Read)
            });

            var ok = DataImportService.ValidateImportAccess("Limited", "ImportTest", PermissionFlags.Import, out var error);
            Assert.False(ok);
            Assert.False(string.IsNullOrWhiteSpace(error));
        }
    }
}
