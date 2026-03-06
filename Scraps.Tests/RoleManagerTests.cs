using Scraps.Security;
using System;
using Xunit;

namespace Scraps.Tests
{
    [Collection("Db")]
    public class RoleManagerTests
    {
        [DbFact]
        public void CreateRole_SetPermission_CheckAccess()
        {
            RoleManager.InitializeFromDb();
            var roleName = "role_" + Guid.NewGuid().ToString("N");

            RoleManager.CreateRole(roleName);
            try
            {
                RoleManager.SetPermission(roleName, "Таблица 1", PermissionFlags.Read | PermissionFlags.Export);
                RoleManager.RefreshCache();

                Assert.True(RoleManager.CheckAccess(roleName, "Таблица 1", PermissionFlags.Read));
                Assert.True(RoleManager.CheckAccess(roleName, "Таблица 1", PermissionFlags.Export));
                Assert.False(RoleManager.CheckAccess(roleName, "Таблица 1", PermissionFlags.Delete));
            }
            finally
            {
                RoleManager.DeleteRole(roleName);
            }
        }

        [DbFact]
        public void RenameRole_Works()
        {
            RoleManager.InitializeFromDb();
            var roleName = "role_" + Guid.NewGuid().ToString("N");
            var newName = roleName + "_renamed";

            RoleManager.CreateRole(roleName);
            try
            {
                RoleManager.RenameRole(roleName, newName);
                Assert.True(RoleManager.RoleExists(newName));
                Assert.False(RoleManager.RoleExists(roleName));
            }
            finally
            {
                if (RoleManager.RoleExists(newName))
                    RoleManager.DeleteRole(newName);
            }
        }

        [DbFact]
        public void DeleteRole_Works()
        {
            RoleManager.InitializeFromDb();
            var roleName = "role_" + Guid.NewGuid().ToString("N");

            RoleManager.CreateRole(roleName);
            Assert.True(RoleManager.RoleExists(roleName));

            RoleManager.DeleteRole(roleName);
            Assert.False(RoleManager.RoleExists(roleName));
        }
    }
}

