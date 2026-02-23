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
            /// Доступные алгоритмы хэширования.
            /// </summary>
            public enum HashAlgorithmKind
            {
                /// <summary>SHA-256.</summary>
                Sha256,
                /// <summary>SHA-1.</summary>
                Sha1,
                /// <summary>MD5.</summary>
                Md5
            }

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
            /// Простой SHA256-хэш строки.
            /// </summary>
            public static string SimpleHash(string input)
            {
                return SimpleHash(input, HashAlgorithmKind.Sha256);
            }

            /// <summary>
            /// Простой хэш строки с выбором алгоритма.
            /// </summary>
            public static string SimpleHash(string input, HashAlgorithmKind algorithm)
            {
                using (var algo = CreateAlgorithm(algorithm))
                {
                    var bytes = algo.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                    return Convert.ToBase64String(bytes);
                }
            }

            private static System.Security.Cryptography.HashAlgorithm CreateAlgorithm(HashAlgorithmKind algorithm)
            {
                switch (algorithm)
                {
                    case HashAlgorithmKind.Md5:
                        return System.Security.Cryptography.MD5.Create();
                    case HashAlgorithmKind.Sha1:
                        return System.Security.Cryptography.SHA1.Create();
                    case HashAlgorithmKind.Sha256:
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
        public static void Reload()
        {
            string tempLogin = UserLogin;
            Logout();
            if (!string.IsNullOrWhiteSpace(tempLogin))
            {
                LoginByName(tempLogin);
            }
        }

        /// <summary>
        /// Войти по логину (загружает роль и данные пользователя).
        /// </summary>
        public static void LoginByName(string login)
        {
            UserLogin = login;
            UserRole = GetUserStatus(login);
            UserData = MSSQL.Users.GetByLogin(login);
            UserId = UserData == null ? -1 : (int)UserData[0];
        }

        /// <summary>
        /// Войти по логину и паролю (с проверкой).
        /// </summary>
        public static bool Login(string login, string password)
        {
            if (!CheckIsUserValid(login, password))
                return false;

            LoginByName(login);
            return true;
        }

        /// <summary>
        /// Зарегистрировать пользователя и при успехе выполнить вход.
        /// </summary>
        public static bool Register(string login, string password, string role)
        {
            if (!RegisterUser(login, password, role))
                return false;

            LoginByName(login);
            return true;
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
        public static string GetUserStatus(string login)
        {
            return MSSQL.Users.GetUserStatus(login);
        }

        /// <summary>
        /// Проверить логин и пароль.
        /// </summary>
        public static bool CheckIsUserValid(string login, string password)
        {
            var user = MSSQL.Users.GetByLogin(login);
            if (user == null) return false;

            string storedValue = user[ScrapsConfig.UsersTableColumnsNames["Password"]].ToString();
            if (ScrapsConfig.AuthHashPasswords)
            {
                string inputHash = Utilities.SimpleHash(password);
                return storedValue == inputHash;
            }
            return storedValue == password;
        }

        /// <summary>
        /// Проверить, существует ли пользователь.
        /// </summary>
        public static bool CheckIsUserExists(string login)
        {
            return MSSQL.Users.GetByLogin(login) != null;
        }

        /// <summary>
        /// Зарегистрировать пользователя.
        /// </summary>
        public static bool RegisterUser(string login, string password, string role)
        {
            if (CheckIsUserExists(login))
            {
                return false;
            }

            if (!Utilities.IsPasswordValid(password))
            {
                return false;
            }

            string storedPassword = ScrapsConfig.AuthHashPasswords ? Utilities.SimpleHash(password) : password;
            return MSSQL.Users.Create(login, storedPassword, role);
        }
    }
}
