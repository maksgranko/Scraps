using Scraps.Configs;
using Scraps.Databases;
using System;
using System.Text.RegularExpressions;

namespace Scraps.Security
{
    /// <summary>
    /// Утилиты авторизации и работы с паролями.
    /// </summary>
    public static class Auth
    {
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
        /// Проверить пароль на простые требования (длина, заглавная буква, спецсимвол).
        /// </summary>
        public static bool IsPasswordValid(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;

            return Regex.IsMatch(password, @"^(?=.*[A-Z])(?=.*[!@#$%^&*]).+$");
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
        /// Простой SHA256-хэш пароля.
        /// </summary>
        public static string SimpleHash(string input)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
