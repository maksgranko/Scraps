using Scraps.Configs;
using Scraps.Database;
using Scraps.Database.MSSQL;
using Scraps.Security;
using Scraps.Tests.Setup;
using System;
using Xunit;
using Db = Scraps.Database.Current;

namespace Scraps.Tests.Database
{
    [Collection("Db")]
    public class UserSessionTests
    {
        [DbFact]
        public void RegisterAndLogin_Works()
        {
            var login = "user_" + Guid.NewGuid().ToString("N");
            var password = "TestPass1!";
            var role = "default";

            UserSession.Register(login, password, role, loginAfterRegistration: false);
            var registered = UserSession.CheckIsUserExists(login);
            Assert.True(registered);

            UserSession.Login(login, password);
            Assert.False(string.IsNullOrWhiteSpace(UserSession.UserLogin));
            Assert.Equal(login, UserSession.UserLogin);
            Assert.NotEqual(-1, UserSession.UserId);

            UserSession.Logout();
        }

        [DbFact]
        public void Register_WeakPassword_WorksWithoutPolicyChecks()
        {
            var login = "user_" + Guid.NewGuid().ToString("N");
            var badPassword = "short";
            var role = "default";

            UserSession.Register(login, badPassword, role, loginAfterRegistration: false);
            try
            {
                Assert.True(UserSession.CheckIsUserExists(login));
                Assert.True(UserSession.CheckIsUserValid(login, badPassword));
            }
            finally
            {
                Db.Users.Delete(login);
                UserSession.Logout();
            }
        }

        [DbFact]
        public void ValidateRegistration_CanBeCustomized()
        {
            var login = "user_" + Guid.NewGuid().ToString("N");
            var weakPassword = "short";

            var defaultValid = UserSession.ValidateRegistration(
                login,
                weakPassword,
                "default",
                out var defaultErrors);
            Assert.False(defaultValid);
            Assert.NotEmpty(defaultErrors);

            var relaxedValid = UserSession.ValidateRegistration(
                login,
                weakPassword,
                "default",
                out var relaxedErrors,
                new UserSession.RegistrationValidationOptions
                {
                    ValidatePasswordStrength = false,
                    CheckUserDoesNotExist = false
                });
            Assert.True(relaxedValid);
            Assert.Empty(relaxedErrors);
        }

        [DbFact]
        public void CheckIsUserExists_ReturnsFalse_WhenMissing()
        {
            var login = "missing_" + Guid.NewGuid().ToString("N");
            Assert.False(UserSession.CheckIsUserExists(login));
        }

        [DbFact]
        public void CheckIsUserExists_EmptyLogin_Throws()
        {
            Assert.Throws<ArgumentException>(() => UserSession.CheckIsUserExists(" "));
        }

        [DbFact]
        public void Register_LoginAfterRegistration_FlagControlsSession()
        {
            var loginAuto = "auto_" + Guid.NewGuid().ToString("N");
            var loginManual = "manual_" + Guid.NewGuid().ToString("N");
            const string password = "TestPass1!";
            const string role = "default";

            try
            {
                UserSession.Register(loginAuto, password, role, loginAfterRegistration: true);
                Assert.Equal(loginAuto, UserSession.UserLogin);

                UserSession.Logout();
                UserSession.Register(loginManual, password, role, loginAfterRegistration: false);
                Assert.True(string.IsNullOrWhiteSpace(UserSession.UserLogin));
            }
            finally
            {
                UserSession.Logout();
                if (UserSession.CheckIsUserExists(loginAuto)) Db.Users.Delete(loginAuto);
                if (UserSession.CheckIsUserExists(loginManual)) Db.Users.Delete(loginManual);
            }
        }

        [DbFact]
        public void Register_DuplicateLogin_Throws()
        {
            var login = "dup_" + Guid.NewGuid().ToString("N");
            const string password = "TestPass1!";
            const string role = "default";

            UserSession.Register(login, password, role, loginAfterRegistration: false);
            try
            {
                Assert.Throws<InvalidOperationException>(() =>
                    UserSession.Register(login, password, role, loginAfterRegistration: false));
            }
            finally
            {
                Db.Users.Delete(login);
                UserSession.Logout();
            }
        }

        [DbFact]
        public void Register_EmptyArguments_Throw()
        {
            Assert.Throws<ArgumentException>(() => UserSession.Register("", "pass", "default", false));
            Assert.Throws<ArgumentException>(() => UserSession.Register("login", "", "default", false));
            Assert.Throws<ArgumentException>(() => UserSession.Register("login", "pass", "", false));
        }

        [DbFact]
        public void AuthHashing_RespectsConfig()
        {
            var login = "user_" + Guid.NewGuid().ToString("N");
            var password = "TestPass1!";
            var role = "default";

            var prev = ScrapsConfig.AuthHashPasswords;
            ScrapsConfig.AuthHashPasswords = true;
            try
            {
                UserSession.Register(login, password, role, loginAfterRegistration: false);
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

        [DbFact]
        public void ChangePassword_UpdatesStoredValue()
        {
            var login = "user_" + Guid.NewGuid().ToString("N");
            var password = "TestPass1!";
            var newPassword = "NewPass1!";
            var role = "default";

            UserSession.Register(login, password, role, loginAfterRegistration: false);
            try
            {
                UserSession.Login(login, password);
                UserSession.ChangePassword(newPassword);
                UserSession.Logout();

                Assert.True(UserSession.CheckIsUserValid(login, newPassword));
            }
            finally
            {
                Db.Users.Delete(login);
                UserSession.Logout();
            }
        }

        [DbFact]
        public void ValidateRegistration_ExistingUser_CheckCanBeDisabled()
        {
            var login = "exists_" + Guid.NewGuid().ToString("N");
            const string password = "TestPass1!";
            const string role = "default";

            UserSession.Register(login, password, role, loginAfterRegistration: false);
            try
            {
                var strict = UserSession.ValidateRegistration(
                    login,
                    password,
                    role,
                    out var strictErrors,
                    new UserSession.RegistrationValidationOptions
                    {
                        CheckUserDoesNotExist = true,
                        ValidatePasswordStrength = false
                    });
                Assert.False(strict);
                Assert.NotEmpty(strictErrors);

                var relaxed = UserSession.ValidateRegistration(
                    login,
                    password,
                    role,
                    out var relaxedErrors,
                    new UserSession.RegistrationValidationOptions
                    {
                        CheckUserDoesNotExist = false,
                        ValidatePasswordStrength = false
                    });
                Assert.True(relaxed);
                Assert.Empty(relaxedErrors);
            }
            finally
            {
                Db.Users.Delete(login);
            }
        }

        [Theory]
        [InlineData("", "TestPass1!", "default", true, true, true, false)]
        [InlineData("user", "", "default", true, true, true, false)]
        [InlineData("user", "TestPass1!", "", true, true, true, false)]
        [InlineData("", "TestPass1!", "default", false, true, true, true)]
        [InlineData("user", "", "default", true, false, true, true)]
        [InlineData("user", "TestPass1!", "", true, true, false, true)]
        public void ValidateRegistration_RequiredFieldFlags_Work(
            string login,
            string password,
            string role,
            bool requireLogin,
            bool requirePassword,
            bool requireRole,
            bool expectedValid)
        {
            var valid = UserSession.ValidateRegistration(
                login,
                password,
                role,
                out _,
                new UserSession.RegistrationValidationOptions
                {
                    RequireLogin = requireLogin,
                    RequirePassword = requirePassword,
                    RequireRole = requireRole,
                    CheckUserDoesNotExist = false,
                    ValidatePasswordStrength = false
                });

            Assert.Equal(expectedValid, valid);
        }

        [Theory]
        [InlineData("short", 8, true, true, "!@#$%^&*", false)]
        [InlineData("lowercase!", 8, true, true, "!@#$%^&*", false)]
        [InlineData("NoSpecial123", 8, true, true, "!@#$%^&*", false)]
        [InlineData("ValidPass1!", 8, true, true, "!@#$%^&*", true)]
        [InlineData("ValidPass1!", 20, true, true, "!@#$%^&*", false)]
        [InlineData("noupperok!", 8, false, true, "!@#$%^&*", true)]
        [InlineData("UpperNoAllowedSpecial!", 8, true, true, "#$", false)]
        public void ValidateRegistration_PasswordPolicyOptions_Work(
            string password,
            int minLength,
            bool requireUpper,
            bool requireSpecial,
            string specialChars,
            bool expectedValid)
        {
            var valid = UserSession.ValidateRegistration(
                "login",
                password,
                "default",
                out _,
                new UserSession.RegistrationValidationOptions
                {
                    CheckUserDoesNotExist = false,
                    ValidatePasswordStrength = true,
                    MinLength = minLength,
                    RequireUpper = requireUpper,
                    RequireSpecial = requireSpecial,
                    SpecialChars = specialChars
                });

            Assert.Equal(expectedValid, valid);
        }
    }
}
