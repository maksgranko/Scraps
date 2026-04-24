using Scraps.Configs;
using Scraps.Security;
using Xunit;

namespace Scraps.Tests.Database
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

        [Fact]
        public void HashPassword_WithExplicitSalt_IsDeterministic()
        {
            var prevSalt = ScrapsConfig.PasswordSalt;
            var prevDbName = ScrapsConfig.DatabaseName;
            try
            {
                ScrapsConfig.PasswordSalt = "MySalt123";
                ScrapsConfig.DatabaseName = "TestDb";

                var hash1 = UserSession.Utilities.HashPassword("password");
                var hash2 = UserSession.Utilities.HashPassword("password");

                Assert.Equal(hash1, hash2);
            }
            finally
            {
                ScrapsConfig.PasswordSalt = prevSalt;
                ScrapsConfig.DatabaseName = prevDbName;
            }
        }

        [Fact]
        public void HashPassword_DifferentSalts_DifferentHashes()
        {
            var prevSalt = ScrapsConfig.PasswordSalt;
            var prevDbName = ScrapsConfig.DatabaseName;
            try
            {
                ScrapsConfig.DatabaseName = "TestDb";

                ScrapsConfig.PasswordSalt = "SaltA";
                var hashA = UserSession.Utilities.HashPassword("password");

                ScrapsConfig.PasswordSalt = "SaltB";
                var hashB = UserSession.Utilities.HashPassword("password");

                Assert.NotEqual(hashA, hashB);
            }
            finally
            {
                ScrapsConfig.PasswordSalt = prevSalt;
                ScrapsConfig.DatabaseName = prevDbName;
            }
        }

        [Fact]
        public void HashPassword_FallbackSalt_FromDatabaseName()
        {
            var prevSalt = ScrapsConfig.PasswordSalt;
            var prevDbName = ScrapsConfig.DatabaseName;
            try
            {
                ScrapsConfig.PasswordSalt = "";
                ScrapsConfig.DatabaseName = "DbOne";
                var hash1 = UserSession.Utilities.HashPassword("password");

                ScrapsConfig.DatabaseName = "DbTwo";
                var hash2 = UserSession.Utilities.HashPassword("password");

                Assert.NotEqual(hash1, hash2);
            }
            finally
            {
                ScrapsConfig.PasswordSalt = prevSalt;
                ScrapsConfig.DatabaseName = prevDbName;
            }
        }
    }
}
