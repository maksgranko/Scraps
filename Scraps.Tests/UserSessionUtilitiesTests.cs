using Scraps.Security;
using Xunit;

namespace Scraps.Tests
{
    public class UserSessionUtilitiesTests
    {
        [Fact]
        public void PasswordUtilities_Work()
        {
            Assert.True(UserSession.Utilities.IsPasswordValid("Abcdef1!"));
            var hash = UserSession.Utilities.HashPassword("test");
            Assert.False(string.IsNullOrWhiteSpace(hash));
        }
    }
}
