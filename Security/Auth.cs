using Scraps.Configs;
using Scraps.Databases;
using System;
using System.Linq;

namespace Scraps.Security
{
    /// <summary>
    /// Утилиты авторизации и работы с паролями.
    /// </summary>
    public static class Auth
    {
        /// <summary>
        /// Доступные простые алгоритмы хэширования.
        /// </summary>
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
                string inputHash = SimpleHash(password);
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
        /// Зарегистрировать пользователя.
        /// </summary>
        public static bool RegisterUser(string login, string password, string role)
        {
            if (CheckIsUserExists(login))
            {
                return false;
            }

            if (!IsPasswordValid(password))
            {
                return false;
            }

            string storedPassword = ScrapsConfig.AuthHashPasswords ? SimpleHash(password) : password;
            return MSSQL.Users.Create(login, storedPassword, role);
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
}
