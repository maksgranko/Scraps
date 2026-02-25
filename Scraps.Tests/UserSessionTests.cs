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
            var ok = UserSession.UserLogin!=null;
            Assert.True(ok);
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
        public void AuthHashing_RespectsConfig()
        {
            var login = "user_" + System.Guid.NewGuid().ToString("N");
            var password = "TestPass1!";
            var role = "default";

            ScrapsConfig.AuthHashPasswords = true;
            UserSession.RegisterUser(login, password, role);
            var registered = UserSession.CheckIsUserExists(login);
            Assert.True(registered);

            var ok = UserSession.CheckIsUserValid(login, password);
            Assert.True(ok);
        }
    }
}
