using Scraps.Configs;
using Scraps.Database;
using Scraps.Database.MSSQL;
using Scraps.Security;
using Scraps.Tests.Setup;
using System.Data;
using System.Linq;
using Xunit;
using Db = Scraps.Database.Current;

namespace Scraps.Tests.Database
{
    [Collection("Db")]
    public class SchemaTests
    {
        [DbFact]
        public void GetTables_ReturnsCurrentDbTables()
        {
            var tables = Db.GetTables();
            Assert.Contains("Users", tables);
            Assert.Contains("Таблица 1", tables);
        }

        [DbFact]
        public void GetTables_WithSchemaName_Works()
        {
            if (TestDatabaseConfig.Provider == DatabaseProvider.LocalFiles)
                return; // schema prefixes are MSSQL-specific

            var tables = MSSQL.GetTables(includeSchemaInName: true);
            Assert.Contains("dbo.Users", tables);
        }

        [DbFact]
        public void GetTableColumns_Works()
        {
            var cols = Db.GetTableColumns("Таблица 1");
            Assert.Contains("Id", cols);
            Assert.Contains("Name", cols);
        }

        [DbFact]
        public void GetTableSchema_Works()
        {
            var schema = Db.GetTableSchema("Таблица 1");
            Assert.NotNull(schema);
            var columnNames = schema.Rows.Cast<System.Data.DataRow>().Select(r => r["ColumnName"].ToString()).ToList();
            Assert.Contains("Id", columnNames);
            Assert.Contains("Name", columnNames);
        }

        [DbFact]
        public void IdentityAndNullable_Works()
        {
            if (TestDatabaseConfig.Provider == DatabaseProvider.LocalFiles)
                return; // LocalDatabaseSchema does not track NULL constraints

            var schema = DatabaseProviderFactory.Current.Schema;
            Assert.True(schema.IsIdentityColumn("Таблица 1", "Id"));
            Assert.False(schema.IsNullableColumn("Таблица 1", "Name"));
        }

        [DbFact]
        public void IsNullableColumn_ThrowsOnMissingColumn()
        {
            Assert.Throws<System.InvalidOperationException>(() =>
                DatabaseProviderFactory.Current.Schema.IsNullableColumn("Таблица 1", "MissingColumn"));
        }

        [DbFact]
        public void RolePermissions_DefaultRowExists()
        {
            var flags = Db.Roles.GetEffectivePermissions("any", "*");
            Assert.Equal(PermissionFlags.None, flags);
        }
    }
}
