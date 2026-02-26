using Scraps.Configs;
using Scraps.Databases;
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
            /// Хэшировать строку с указанным алгоритмом.
            /// </summary>
            public static string HashPassword(string input, HashAlgorithm algorithm)
            {
                using (var algo = CreateAlgorithm(algorithm))
                {
                    var bytes = algo.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
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
            UserRole = MSSQL.Users.GetUserStatus(login);
            UserData = MSSQL.Users.GetByLogin(login);
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

            var user = MSSQL.Users.GetByLogin(login);
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
        /// Зарегистрировать пользователя и выполнить вход.
        /// </summary>
        /// <exception cref="ArgumentException">Пустой логин, пароль или роль</exception>
        /// <exception cref="InvalidOperationException">Пользователь уже существует или пароль не соответствует требованиям</exception>
        public static void Register(string login, string password, string role)
        {
            RegisterUser(login, password, role);
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
            return MSSQL.Users.GetUserStatus(login);
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

            var user = MSSQL.Users.GetByLogin(login);
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
                MSSQL.Users.GetByLogin(login);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Зарегистрировать пользователя.
        /// </summary>
        /// <exception cref="ArgumentException">Пустой логин, пароль или роль</exception>
        /// <exception cref="InvalidOperationException">Пользователь уже существует или пароль не соответствует требованиям</exception>
        public static void RegisterUser(string login, string password, string role)
        {
            if (string.IsNullOrWhiteSpace(login))
                throw new ArgumentException("Логин не может быть пустым.", nameof(login));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Пароль не может быть пустым.", nameof(password));
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Роль не может быть пустой.", nameof(role));

            if (CheckIsUserExists(login))
                throw new InvalidOperationException($"Пользователь '{login}' уже существует.");

            if (!Utilities.IsPasswordValid(password))
                throw new InvalidOperationException("Пароль не соответствует требованиям (минимум 8 символов, заглавная буква, спецсимвол).");

            string storedPassword = ScrapsConfig.AuthHashPasswords ? Utilities.HashPassword(password) : password;
            MSSQL.Users.Create(login, storedPassword, role);
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
            MSSQL.Users.ChangePassword(UserLogin, storedPassword);
        }
    }
}
