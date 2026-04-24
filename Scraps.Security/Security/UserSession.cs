using Scraps.Configs;
using Scraps.Database;
using System;
using System.Data;
using System.Linq;

namespace Scraps.Security
{
    /// <summary>
    /// Сессионные данные пользователя (логин, роль, данные строки).
    /// </summary>
    public static class UserSession
    {
        /// <summary>
        /// Параметры проверки регистрации пользователя.
        /// </summary>
        public sealed class RegistrationValidationOptions
        {
            /// <summary>Проверять, что логин не пустой.</summary>
            public bool RequireLogin { get; set; } = true;
            /// <summary>Проверять, что пароль не пустой.</summary>
            public bool RequirePassword { get; set; } = true;
            /// <summary>Проверять, что роль не пустая.</summary>
            public bool RequireRole { get; set; } = true;
            /// <summary>Проверять, что пользователь с логином еще не существует.</summary>
            public bool CheckUserDoesNotExist { get; set; } = true;
            /// <summary>Проверять сложность пароля.</summary>
            public bool ValidatePasswordStrength { get; set; } = true;
            /// <summary>Минимальная длина пароля.</summary>
            public int MinLength { get; set; } = 8;
            /// <summary>Требовать заглавную букву.</summary>
            public bool RequireUpper { get; set; } = true;
            /// <summary>Требовать спецсимвол.</summary>
            public bool RequireSpecial { get; set; } = true;
            /// <summary>Набор спецсимволов.</summary>
            public string SpecialChars { get; set; } = "!@#$%^&*";
        }

        /// <summary>
        /// Вспомогательные утилиты для авторизации/паролей.
        /// </summary>
        public static class Utilities
        {
            /// <summary>
            /// Проверить пароль на требования (длина, заглавная буква, спецсимвол).
            /// </summary>
            public static bool IsPasswordValid(string password)
            {
                return IsPasswordValid(password, minLength: 8, requireUpper: true, requireSpecial: true, specialChars: "!@#$%^&*");
            }

            /// <summary>
            /// Проверить пароль на требования (длина, заглавная буква, спецсимвол).
            /// </summary>
            public static bool IsPasswordValid(
                string password,
                int minLength,
                bool requireUpper,
                bool requireSpecial,
                string specialChars)
            {
                if (string.IsNullOrWhiteSpace(password))
                    return false;

                if (minLength < 1) minLength = 1;
                if (password.Length < minLength)
                    return false;

                if (requireUpper && !password.Any(char.IsUpper))
                    return false;

                if (requireSpecial)
                {
                    if (string.IsNullOrEmpty(specialChars))
                        return false;

                    bool hasSpecial = password.Any(c => specialChars.IndexOf(c) >= 0);
                    if (!hasSpecial)
                        return false;
                }

                return true;
            }

            /// <summary>
            /// Хэшировать строку с использованием алгоритма из ScrapsConfig.AuthHashAlgorithm.
            /// </summary>
            public static string HashPassword(string input)
            {
                return HashPassword(input, ScrapsConfig.AuthHashAlgorithm);
            }

            /// <summary>
            /// Хэшировать строку с указанным алгоритмом и солью.
            /// </summary>
            public static string HashPassword(string input, HashAlgorithm algorithm)
            {
                var salt = GetSalt();
                using (var algo = CreateAlgorithm(algorithm))
                {
                    var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
                    var saltBytes = System.Text.Encoding.UTF8.GetBytes(salt);
                    var combined = new byte[inputBytes.Length + saltBytes.Length];
                    System.Buffer.BlockCopy(inputBytes, 0, combined, 0, inputBytes.Length);
                    System.Buffer.BlockCopy(saltBytes, 0, combined, inputBytes.Length, saltBytes.Length);
                    var bytes = algo.ComputeHash(combined);
                    return Convert.ToBase64String(bytes);
                }
            }

            private static string GetSalt()
            {
                if (!string.IsNullOrWhiteSpace(ScrapsConfig.PasswordSalt))
                    return ScrapsConfig.PasswordSalt;

                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(ScrapsConfig.DatabaseName + "Scraps"));
                    return Convert.ToBase64String(bytes);
                }
            }

            private static System.Security.Cryptography.HashAlgorithm CreateAlgorithm(HashAlgorithm algorithm)
            {
                switch (algorithm)
                {
                    case HashAlgorithm.MD5:
                        return System.Security.Cryptography.MD5.Create();
                    case HashAlgorithm.SHA1:
                        return System.Security.Cryptography.SHA1.Create();
                    case HashAlgorithm.SHA256:
                    default:
                        return System.Security.Cryptography.SHA256.Create();
                }
            }
        }

        /// <summary>Идентификатор пользователя.</summary>
        public static int UserId { get; private set; } = -1;
        /// <summary>Логин пользователя.</summary>
        public static string UserLogin { get; private set; } = "";
        /// <summary>Роль пользователя.</summary>
        public static string UserRole { get; private set; } = "";
        /// <summary>Строка пользователя из таблицы Users.</summary>
        public static DataRow UserData { get; private set; }

        /// <summary>
        /// Обновить сессию по текущему логину.
        /// </summary>
        /// <exception cref="InvalidOperationException">Пользователь не авторизован</exception>
        public static void Reload()
        {
            if (string.IsNullOrWhiteSpace(UserLogin))
                throw new InvalidOperationException("Пользователь не авторизован.");

            string tempLogin = UserLogin;
            Logout();
            LoginByName(tempLogin);
        }

        /// <summary>
        /// Войти по логину (загружает роль и данные пользователя).
        /// </summary>
        /// <exception cref="ArgumentException">Пустой логин</exception>
        /// <exception cref="InvalidOperationException">Пользователь не найден</exception>
        public static void LoginByName(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                throw new ArgumentException("Логин не может быть пустым.", nameof(login));

            UserLogin = login;
            UserRole = DatabaseProviderFactory.Current.Users.GetUserStatus(login);
            UserData = DatabaseProviderFactory.Current.Users.GetByLogin(login);
            if (ScrapsConfig.UsersTableColumnsNames != null &&
                ScrapsConfig.UsersTableColumnsNames.TryGetValue("UserID", out var idColumn))
            {
                UserId = Convert.ToInt32(UserData[idColumn]);
            }
            else
            {
                UserId = Convert.ToInt32(UserData[0]);
            }
        }

        /// <summary>
        /// Войти по логину и паролю.
        /// </summary>
        /// <exception cref="ArgumentException">Пустой логин или пароль</exception>
        /// <exception cref="InvalidOperationException">Пользователь не найден или неверный пароль</exception>
        public static void Login(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login))
                throw new ArgumentException("Логин не может быть пустым.", nameof(login));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Пароль не может быть пустым.", nameof(password));

            var user = DatabaseProviderFactory.Current.Users.GetByLogin(login);
            string storedValue = user[ScrapsConfig.UsersTableColumnsNames["Password"]].ToString();

            bool valid;
            if (ScrapsConfig.AuthHashPasswords)
            {
                string inputHash = Utilities.HashPassword(password);
                valid = storedValue == inputHash;
            }
            else
            {
                valid = storedValue == password;
            }

            if (!valid)
                throw new InvalidOperationException("Неверный пароль.");

            LoginByName(login);
        }

        /// <summary>
        /// Зарегистрировать пользователя.
        /// По умолчанию после регистрации выполняется вход.
        /// </summary>
        /// <param name="login">Логин пользователя.</param>
        /// <param name="password">Пароль пользователя.</param>
        /// <param name="role">Роль пользователя.</param>
        /// <param name="loginAfterRegistration">Если true, выполнить вход сразу после регистрации.</param>
        /// <exception cref="ArgumentException">Пустой логин, пароль или роль</exception>
        /// <exception cref="InvalidOperationException">Пользователь уже существует</exception>
        public static void Register(string login, string password, string role, bool loginAfterRegistration = true)
        {
            if (string.IsNullOrWhiteSpace(login))
                throw new ArgumentException("Логин не может быть пустым.", nameof(login));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Пароль не может быть пустым.", nameof(password));
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Роль не может быть пустой.", nameof(role));

            string storedPassword = ScrapsConfig.AuthHashPasswords ? Utilities.HashPassword(password) : password;
            DatabaseProviderFactory.Current.Users.Create(login, storedPassword, role);

            if (loginAfterRegistration)
                LoginByName(login);
        }

        /// <summary>
        /// Сбросить сессию.
        /// </summary>
        public static void Logout()
        {
            UserId = -1;
            UserLogin = "";
            UserRole = "";
            UserData = null;
        }

        /// <summary>
        /// Получить отображаемое имя роли пользователя.
        /// </summary>
        /// <exception cref="ArgumentException">Пустой логин</exception>
        /// <exception cref="InvalidOperationException">Пользователь не найден</exception>
        public static string GetUserStatus(string login)
        {
            return DatabaseProviderFactory.Current.Users.GetUserStatus(login);
        }

        /// <summary>
        /// Проверить логин и пароль.
        /// </summary>
        /// <exception cref="ArgumentException">Пустой логин или пароль</exception>
        /// <exception cref="InvalidOperationException">Пользователь не найден или неверный пароль</exception>
        public static bool CheckIsUserValid(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login))
                throw new ArgumentException("Логин не может быть пустым.", nameof(login));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Пароль не может быть пустым.", nameof(password));

            var user = DatabaseProviderFactory.Current.Users.GetByLogin(login);
            string storedValue = user[ScrapsConfig.UsersTableColumnsNames["Password"]].ToString();

            if (ScrapsConfig.AuthHashPasswords)
            {
                string inputHash = Utilities.HashPassword(password);
                return storedValue == inputHash;
            }
            return storedValue == password;
        }

        /// <summary>
        /// Проверить, существует ли пользователь.
        /// </summary>
        /// <exception cref="ArgumentException">Пустой логин</exception>
        public static bool CheckIsUserExists(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                throw new ArgumentException("Логин не может быть пустым.", nameof(login));

            try
            {
                DatabaseProviderFactory.Current.Users.GetByLogin(login);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Проверить регистрацию по настраиваемым правилам (без записи в БД).
        /// </summary>
        public static bool ValidateRegistration(
            string login,
            string password,
            string role,
            out string[] errors,
            RegistrationValidationOptions options = null)
        {
            options = options ?? new RegistrationValidationOptions();
            var list = new System.Collections.Generic.List<string>();

            if (options.RequireLogin && string.IsNullOrWhiteSpace(login))
                list.Add("Логин не может быть пустым.");
            if (options.RequirePassword && string.IsNullOrWhiteSpace(password))
                list.Add("Пароль не может быть пустым.");
            if (options.RequireRole && string.IsNullOrWhiteSpace(role))
                list.Add("Роль не может быть пустой.");

            if (options.CheckUserDoesNotExist && !string.IsNullOrWhiteSpace(login) && CheckIsUserExists(login))
                list.Add($"Пользователь '{login}' уже существует.");

            if (options.ValidatePasswordStrength && !string.IsNullOrWhiteSpace(password))
            {
                bool valid = Utilities.IsPasswordValid(
                    password,
                    options.MinLength,
                    options.RequireUpper,
                    options.RequireSpecial,
                    options.SpecialChars);
                if (!valid)
                    list.Add("Пароль не соответствует требованиям.");
            }

            errors = list.ToArray();
            return errors.Length == 0;
        }

        /// <summary>
        /// Изменить пароль текущего пользователя.
        /// </summary>
        /// <exception cref="InvalidOperationException">Пользователь не авторизован</exception>
        /// <exception cref="ArgumentException">Пароль не соответствует требованиям</exception>
        public static void ChangePassword(string newPassword)
        {
            if (string.IsNullOrWhiteSpace(UserLogin))
                throw new InvalidOperationException("Пользователь не авторизован.");

            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException("Пароль не может быть пустым.", nameof(newPassword));

            if (!Utilities.IsPasswordValid(newPassword))
                throw new ArgumentException("Пароль не соответствует требованиям (минимум 8 символов, заглавная буква, спецсимвол).", nameof(newPassword));

            string storedPassword = ScrapsConfig.AuthHashPasswords ? Utilities.HashPassword(newPassword) : newPassword;
            DatabaseProviderFactory.Current.Users.ChangePassword(UserLogin, storedPassword);
        }
    }
}




