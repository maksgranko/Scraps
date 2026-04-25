using Scraps.Database;
using Scraps.Databases;
using Scraps.Tests.Setup;
using System;
using Xunit;
using Db = Scraps.Database.Database;

namespace Scraps.Tests.Database
{
    [Collection("Db")]
    public class UsersTests
    {
        [DbFact]
        public void ChangeRole_Works()
        {
            var login = "user_" + Guid.NewGuid().ToString("N");
            var password = "Pass1!";
            var roleName = "role_" + Guid.NewGuid().ToString("N");

            var roleId = Db.Roles.Create(roleName);
            try
            {
                Db.Users.Create(login, password, "default");
                Db.Users.ChangeRole(login, roleName);

                var role = Db.Users.GetUserStatus(login);
                Assert.Equal(roleName, role);
            }
            finally
            {
                Db.Users.Delete(login);
                Db.Roles.Delete(roleName);
            }
        }

        [DbFact]
        public void ChangePassword_Works()
        {
            var login = "user_" + Guid.NewGuid().ToString("N");
            var password = "Pass1!";
            var newPassword = "Pass2!";

            Db.Users.Create(login, password, "default");
            try
            {
                Db.Users.ChangePassword(login, newPassword);
                var user = Db.Users.GetByLogin(login);
                Assert.Equal(newPassword, user["Password"].ToString());
            }
            finally
            {
                Db.Users.Delete(login);
            }
        }

        [DbFact]
        public void Delete_ThrowsWhenMissing()
        {
            var login = "missing_" + Guid.NewGuid().ToString("N");
            Assert.Throws<InvalidOperationException>(() => Db.Users.Delete(login));
        }
    }

}





