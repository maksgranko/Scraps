using Scraps.Database;
using Scraps.Security;
using System;
using Scraps.Tests.Setup;
using Xunit;
using Db = Scraps.Database.Current;

namespace Scraps.Tests.Database
{
    [Collection("Db")]
    public class RolesApiTests
    {
        [DbFact]
        public void CreateRole_SetPermission_CheckAccess()
        {
            var roleName = "role_" + Guid.NewGuid().ToString("N");
            var roleId = Db.Roles.Create(roleName);
            try
            {
                Db.RolePermissions.Set(roleId, "Таблица 1", PermissionFlags.Read | PermissionFlags.Export);

                Assert.True(Db.Roles.CheckAccess(roleName, "Таблица 1", PermissionFlags.Read));
                Assert.True(Db.Roles.CheckAccess(roleName, "Таблица 1", PermissionFlags.Export));
                Assert.False(Db.Roles.CheckAccess(roleName, "Таблица 1", PermissionFlags.Delete));
            }
            finally
            {
                Db.RolePermissions.DeleteAllForRole(roleId);
                Db.Roles.Delete(roleName);
            }
        }

        [DbFact]
        public void RenameRole_Works()
        {
            var roleName = "role_" + Guid.NewGuid().ToString("N");
            var newName = roleName + "_renamed";

            Db.Roles.Create(roleName);
            try
            {
                Db.Roles.Rename(roleName, newName);
                Assert.NotNull(Db.Roles.GetRoleIdByName(newName));
                Assert.Null(Db.Roles.GetRoleIdByName(roleName));
            }
            finally
            {
                if (Db.Roles.GetRoleIdByName(newName) != null)
                    Db.Roles.Delete(newName);
            }
        }

        [DbFact]
        public void DeleteRole_Works()
        {
            var roleName = "role_" + Guid.NewGuid().ToString("N");

            Db.Roles.Create(roleName);
            Assert.NotNull(Db.Roles.GetRoleIdByName(roleName));

            Db.Roles.Delete(roleName);
            Assert.Null(Db.Roles.GetRoleIdByName(roleName));
        }
    }
}
