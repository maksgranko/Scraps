using System.Collections.Generic;

namespace Scraps.Configs
{
    /// <summary>
    /// Глобальные настройки библиотеки.
    /// </summary>
    public static class ScrapsConfig
    {
        /// <summary>
        /// Название базы данных.
        /// </summary>
        public static string DatabaseName = "";

        /// <summary>
        /// Строка подключения к БД.
        /// </summary>
        public static string ConnectionString = "";

        /// <summary>
        /// Название таблицы пользователей.
        /// </summary>
        public static string UsersTableName = "Users";

        /// <summary>
        /// Сопоставление логических ключей колонок с реальными именами.
        /// </summary>
        public static Dictionary<string, string> UsersTableColumnsNames = new Dictionary<string, string>
        {
            { "UserID", "UserID" },
            { "Login", "Login" },
            { "Password", "Password" },
            { "Role", "Role" }
        };

        /// <summary>
        /// Обязательные логические ключи колонок в таблице пользователей.
        /// </summary>
        public static string[] UsersRequiredColumnKeys = new[] { "UserID", "Login", "Password", "Role" };

        /// <summary>
        /// Если true, Users.Role хранит RoleID (int). Если false, хранит строку RoleName.
        /// </summary>
        public static bool UseRoleIdMapping = true;

        /// <summary>
        /// Название роли по умолчанию (RoleID = 0).
        /// </summary>
        public static string DefaultRoleName = "default";

        /// <summary>
        /// Роли, которые будут созданы при генерации схемы, если их нет.
        /// </summary>
        public static string[] SeedRoles = new string[] { };

        /// <summary>
        /// Хэшировать пароли при регистрации/проверке.
        /// </summary>
        public static bool AuthHashPasswords = true;

        // Пример переводов (таблицы, которые гарантированно есть в БД).
        // Рекомендуется задавать в месте инициализации приложения:
        // TranslationManager.TableTranslations["Roles"] = "Роли";
        // TranslationManager.ColumnTranslations["Roles"] = new Dictionary<string, string>
        // {
        //     ["RoleID"] = "Идентификатор",
        //     ["RoleName"] = "Название"
        // };
        // TranslationManager.TableTranslations["Users"] = "Пользователи";
        // TranslationManager.ColumnTranslations["Users"] = new Dictionary<string, string>
        // {
        //     ["UserID"] = "Идентификатор",
        //     ["Login"] = "Логин",
        //     ["Password"] = "Пароль",
        //     ["Role"] = "Роль"
        // };
    }
}
