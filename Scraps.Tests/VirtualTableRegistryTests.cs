using Scraps.Databases;
using Scraps.Security;
using Xunit;

namespace Scraps.Tests
{
    [Collection("Db")]
    public class VirtualTableRegistryTests
    {
        [Fact]
        public void RolePermissions_Applied()
        {
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
    }
}
