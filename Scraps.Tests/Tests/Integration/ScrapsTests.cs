using Scraps.Databases;
using Scraps.Security;
using Scraps.Tests.Setup;
using Xunit;

namespace Scraps.Tests.Integration
{
    [Collection("Db")]
    public class ScrapsTests
    {
        [DbFact]
        public void GetTableData_WorksWithSpaceInName()
        {
            var dt = MSSQL.GetTableData("Таблица 1");
            Assert.NotNull(dt);
            Assert.True(dt.Rows.Count > 0);
        }

        [DbFact]
        public void GetNx2Dictionary_Works()
        {
            var dict = MSSQL.GetNx2Dictionary("Таблица 1", "Id", "Name");
            Assert.NotNull(dict);
            Assert.True(dict.Count > 0);
            Assert.Equal("Ivan", dict[1]);
        }

        [DbFact]
        public void GetNx1List_Works()
        {
            var list = MSSQL.GetNx1List("Таблица 1", "Name");
            Assert.NotNull(list);
            Assert.True(list.Count > 0);
            Assert.Contains("Ivan", list);
        }

        [DbFact]
        public void VirtualTableRegistry_SelectWorks()
        {
            VirtualTableRegistry.Clear();
            VirtualTableRegistry.RegisterSelect("Virtual_Test", "Таблица 1");
            var dt = VirtualTableRegistry.GetData("Virtual_Test", roleName: "default", required: PermissionFlags.Read);
            Assert.NotNull(dt);
            Assert.True(dt.Rows.Count > 0);
        }

        [DbFact]
        public void RoleManager_EffectivePermissions_Default()
        {
            RoleManager.InitializeFromDb();
            var flags = RoleManager.GetEffectivePermissions("no-role", "Таблица 1");
            Assert.Equal(PermissionFlags.None, flags);
        }
    }
}
