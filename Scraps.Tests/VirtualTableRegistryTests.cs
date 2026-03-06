using Scraps.Databases;
using Scraps.Security;
using Xunit;

namespace Scraps.Tests
{
    [Collection("Db")]
    public class VirtualTableRegistryTests
    {
        [DbFact]
        public void RolePermissions_Applied()
        {
            VirtualTableRegistry.Clear();
            VirtualTableRegistry.Register(
                name: "Virtual_Role",
                sql: "SELECT Name FROM [Таблица 1]",
                rolePermissions: new System.Collections.Generic.Dictionary<string, PermissionFlags>
                {
                    ["Admin"] = PermissionFlags.Read,
                    ["*"] = PermissionFlags.None
                });

            Assert.True(VirtualTableRegistry.CheckAccess("Virtual_Role", "Admin", PermissionFlags.Read, out _));
            Assert.False(VirtualTableRegistry.CheckAccess("Virtual_Role", "User", PermissionFlags.Read, out _));
        }

        [DbFact]
        public void MissingVirtualTable_ReturnsError()
        {
            VirtualTableRegistry.Clear();
            var ok = VirtualTableRegistry.CheckAccess("Missing", "Admin", PermissionFlags.Read, out var error);

            Assert.False(ok);
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        [DbFact]
        public void BuildSelectQuery_QuotesColumnsAndTable()
        {
            var sql = VirtualTableRegistry.BuildSelectQuery("dbo.Table 1", new[] { "Col 1", "Col]2" }, "Id > 1");
            Assert.Contains("[dbo].[Table 1]", sql);
            Assert.Contains("[Col 1]", sql);
            Assert.Contains("[Col]]2]", sql);
            Assert.Contains("WHERE Id > 1", sql);
        }

        [DbFact]
        public void EmptyRole_Denied()
        {
            VirtualTableRegistry.Clear();
            VirtualTableRegistry.Register("Virtual_EmptyRole", "SELECT 1", PermissionFlags.Read);

            var ok = VirtualTableRegistry.CheckAccess("Virtual_EmptyRole", "", PermissionFlags.Read, out var error);
            Assert.False(ok);
            Assert.False(string.IsNullOrWhiteSpace(error));
        }
    }
}

