using Scraps.Databases;
using Scraps.Security;
using Scraps.Tests.Setup;
using System.Data;
using Xunit;

namespace Scraps.Tests.Database
{
    [Collection("Db")]
    public class SchemaTests
    {
        [DbFact]
        public void GetTables_ReturnsCurrentDbTables()
        {
            var tables = MSSQL.GetTables();
            Assert.Contains("Users", tables);
            Assert.Contains("Таблица 1", tables);
        }

        [DbFact]
        public void GetTables_WithSchemaName_Works()
        {
            var tables = MSSQL.GetTables(includeSchemaInName: true);
            Assert.Contains("dbo.Users", tables);
        }

        [DbFact]
        public void GetTableColumns_Works()
        {
            var cols = MSSQL.GetTableColumns("Таблица 1");
            Assert.Contains("Id", cols);
            Assert.Contains("Name", cols);
        }

        [DbFact]
        public void GetTableSchema_Works()
        {
            var schema = MSSQL.GetTableSchema("Таблица 1");
            Assert.True(schema.ContainsKey("Id"));
            Assert.True(schema.ContainsKey("Name"));
        }

        [DbFact]
        public void IdentityAndNullable_Works()
        {
            Assert.True(MSSQL.IsIdentityColumn("Таблица 1", "Id"));
            Assert.False(MSSQL.IsNullableColumn("Таблица 1", "Name"));
        }

        [DbFact]
        public void IsNullableColumn_ThrowsOnMissingColumn()
        {
            Assert.Throws<System.InvalidOperationException>(() =>
                MSSQL.IsNullableColumn("Таблица 1", "MissingColumn"));
        }

        [DbFact]
        public void RolePermissions_DefaultRowExists()
        {
            RoleManager.InitializeFromDb();
            var flags = RoleManager.GetEffectivePermissions("any", "*");
            Assert.Equal(PermissionFlags.None, flags);
        }
    }
}

