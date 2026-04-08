using System.Collections.Generic;

namespace Scraps.Configs
{
    /// <summary>
    /// Алгоритмы хэширования паролей.
    /// </summary>
    public enum HashAlgorithm
    {
        /// <summary>SHA-256 (по умолчанию).</summary>
        SHA256,
        /// <summary>SHA-1.</summary>
        SHA1,
        /// <summary>MD5.</summary>
        MD5
    }

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
        /// Автоматически устанавливается при Initialize() в зависимости от режима.
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
        /// Хэшировать пароли при регистрации/смене/проверке.
        /// </summary>
        public static bool AuthHashPasswords = true;

        /// <summary>
        /// Алгоритм хэширования паролей (по умолчанию SHA-256).
        /// </summary>
        public static HashAlgorithm AuthHashAlgorithm = HashAlgorithm.SHA256;

        // Пример переводов (таблицы, которые гарантированно есть в БД).
        // Рекомендуется загружать одним вызовом через словарь:
        // TranslationManager.Load(new Dictionary<string, string>
        // {
        //     ["Roles"] = "Роли",
        //     [TranslationManager.ColumnKey("Roles", "RoleID")] = "Идентификатор",
        //     [TranslationManager.ColumnKey("Roles", "RoleName")] = "Название",
        //     ["Users"] = "Пользователи",
        //     [TranslationManager.ColumnKey("Users", "UserID")] = "Идентификатор",
        //     [TranslationManager.ColumnKey("Users", "Login")] = "Логин",
        //     [TranslationManager.ColumnKey("Users", "Password")] = "Пароль",
        //     [TranslationManager.ColumnKey("Users", "Role")] = "Роль"
        // }, clearBeforeLoad: false);
        //
        // Тот же вариант "в одну строку" через прямые присваивания:
        // TranslationManager.Translations["Roles"] = "Роли"; TranslationManager.Translations[TranslationManager.ColumnKey("Roles", "RoleID")] = "Идентификатор"; TranslationManager.Translations[TranslationManager.ColumnKey("Roles", "RoleName")] = "Название"; TranslationManager.Translations["Users"] = "Пользователи"; TranslationManager.Translations[TranslationManager.ColumnKey("Users", "UserID")] = "Идентификатор"; TranslationManager.Translations[TranslationManager.ColumnKey("Users", "Login")] = "Логин"; TranslationManager.Translations[TranslationManager.ColumnKey("Users", "Password")] = "Пароль"; TranslationManager.Translations[TranslationManager.ColumnKey("Users", "Role")] = "Роль";
        //
        // Также можно загрузить из CSV-файла:
        // TranslationManager.Load("translations.csv", delimiter: ';', hasHeader: true, clearBeforeLoad: false);
        //
        // И из строки (CSV-текст):
        // var csv = "key;value\nUsers;Пользователи\nUsers::Login;Логин";
        // var table = Scraps.Data.DataTables.Parser.ParseCsv(csv, delimiter: ';', hasHeader: true, trim: true);
        // var dict = Scraps.Data.DataTables.Parser.ParseNx2ToDictionary(
        //     table,
        //     key => key?.ToString(),
        //     value => value?.ToString() ?? string.Empty,
        //     keyColumnIndex: 0,
        //     valueColumnIndex: 1,
        //     skipInvalidRows: true);
        // TranslationManager.Load(dict, clearBeforeLoad: false);

        /// <summary>
        /// Явно указанный SQL Server (пропускает автопоиск).
        /// </summary>
        public static string ExplicitServerName = "";

        /// <summary>
        /// Таймаут подключения при поиске сервера (секунды).
        /// </summary>
        public static int ServerDiscoveryTimeout = 1;

        /// <summary>
        /// Использовать параллельный поиск серверов (ускоряет в 3-7 раз).
        /// </summary>
        public static bool UseParallelServerDiscovery = true;

        /// <summary>
        /// Максимальное количество параллельных подключений при поиске.
        /// </summary>
        public static int MaxParallelConnections = 10;
    }
}







