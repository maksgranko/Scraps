using Scraps.Configs;
using Scraps.Databases;
using Scraps.Security;
using Xunit;

namespace Scraps.Tests
{
    [Collection("Db")]
    public class UserSessionTests
    {
        [Fact]
        public void RegisterAndLogin_Works()
        {
            var login = "user_" + System.Guid.NewGuid().ToString("N");
            var password = "TestPass1!";
            var role = "default";

            UserSession.RegisterUser(login, password, role);
            var registered = UserSession.CheckIsUserExists(login);
            Assert.True(registered);

            UserSession.Login(login, password);
            Assert.False(string.IsNullOrWhiteSpace(UserSession.UserLogin));
            Assert.Equal(login, UserSession.UserLogin);
            Assert.NotEqual(-1, UserSession.UserId);

            UserSession.Logout();
        }

        [Fact]
        public void PasswordUtilities_Work()
        {
            Assert.True(UserSession.Utilities.IsPasswordValid("Abcdef1!"));
            var hash = UserSession.Utilities.HashPassword("test");
            Assert.False(string.IsNullOrWhiteSpace(hash));
        }
        [Fact]
        public void RegisterUser_InvalidPassword_Throws()
        {
            var login = "user_" + System.Guid.NewGuid().ToString("N");
            var badPassword = "short";
            var role = "default";

            Assert.Throws<System.InvalidOperationException>(() =>
                UserSession.RegisterUser(login, badPassword, role));
        }

        [Fact]
        public void CheckIsUserExists_ReturnsFalse_WhenMissing()
        {
            var login = "missing_" + System.Guid.NewGuid().ToString("N");
            Assert.False(UserSession.CheckIsUserExists(login));
        }
        [Fact]
        public void AuthHashing_RespectsConfig()
        {
            var login = "user_" + System.Guid.NewGuid().ToString("N");
            var password = "TestPass1!";
            var role = "default";

            var prev = ScrapsConfig.AuthHashPasswords;
            ScrapsConfig.AuthHashPasswords = true;
            try
            {
                UserSession.RegisterUser(login, password, role);
                var registered = UserSession.CheckIsUserExists(login);
                Assert.True(registered);

                var ok = UserSession.CheckIsUserValid(login, password);
                Assert.True(ok);
            }
            finally
            {
                ScrapsConfig.AuthHashPasswords = prev;
                UserSession.Logout();
            }
        }

        [Fact]
        public void ChangePassword_UpdatesStoredValue()
        {
            var login = "user_" + System.Guid.NewGuid().ToString("N");
            var password = "TestPass1!";
            var newPassword = "NewPass1!";
            var role = "default";

            UserSession.RegisterUser(login, password, role);
            try
            {
                UserSession.Login(login, password);
                UserSession.ChangePassword(newPassword);
                UserSession.Logout();

                Assert.True(UserSession.CheckIsUserValid(login, newPassword));
            }
            finally
            {
                MSSQL.Users.Delete(login);
                UserSession.Logout();
            }
        }
    }
}

