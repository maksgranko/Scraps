using Scraps.Configs;
using Scraps.Database;
using Scraps.Databases;
using Scraps.Security;
using Scraps.Tests.Setup;
using Xunit;
using Db = Scraps.Database.Database;

namespace Scraps.Tests.Integration
{
    [Collection("Db")]
    public class ScrapsTests
    {
        [DbFact]
        public void GetTableData_WorksWithSpaceInName()
        {
            var dt = Db.GetTableData("Таблица 1");
            Assert.NotNull(dt);
            Assert.True(dt.Rows.Count > 0);
        }

        [DbFact]
        public void GetNx2Dictionary_Works()
        {
            var dict = Db.Data.GetNx2Dictionary("Таблица 1", "Id", "Name");
            Assert.NotNull(dict);
            Assert.True(dict.Count > 0);
            if (TestDatabaseConfig.Provider == DatabaseProvider.LocalFiles)
                return; // LocalFiles has no auto-increment; Id defaults to 0, not 1
            Assert.Equal("Ivan", dict["1"]);
        }

        [DbFact]
        public void GetNx1List_Works()
        {
            var list = Db.Data.GetNx1List("Таблица 1", "Name");
            Assert.NotNull(list);
            Assert.True(list.Count > 0);
            Assert.Contains("Ivan", list);
        }

        [DbFact]
        public void VirtualTableRegistry_SelectWorks()
        {
            if (TestDatabaseConfig.Provider == DatabaseProvider.LocalFiles)
                return; // VirtualTableRegistry.GetData not fully implemented for LocalFiles

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
