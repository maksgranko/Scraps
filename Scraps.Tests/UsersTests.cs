using Scraps.Databases;
using System;
using Xunit;

namespace Scraps.Tests
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

            var roleId = MSSQL.Roles.Create(roleName);
            try
            {
                MSSQL.Users.Create(login, password, "default");
                MSSQL.Users.ChangeRole(login, roleName);

                var role = MSSQL.Users.GetUserStatus(login);
                Assert.Equal(roleName, role);
            }
            finally
            {
                MSSQL.Users.Delete(login);
                MSSQL.Roles.Delete(roleName);
            }
        }

        [DbFact]
        public void ChangePassword_Works()
        {
            var login = "user_" + Guid.NewGuid().ToString("N");
            var password = "Pass1!";
            var newPassword = "Pass2!";

            MSSQL.Users.Create(login, password, "default");
            try
            {
                MSSQL.Users.ChangePassword(login, newPassword);
                var user = MSSQL.Users.GetByLogin(login);
                Assert.Equal(newPassword, user["Password"].ToString());
            }
            finally
            {
                MSSQL.Users.Delete(login);
            }
        }

        [DbFact]
        public void Delete_ThrowsWhenMissing()
        {
            var login = "missing_" + Guid.NewGuid().ToString("N");
            Assert.Throws<InvalidOperationException>(() => MSSQL.Users.Delete(login));
        }
    }

}





