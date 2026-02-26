using Scraps.Configs;
using System.Collections.Generic;

namespace Scraps.Databases.Utilities
{
    /// <summary>
    /// Режим генерации схемы базы данных.
    /// </summary>
    public enum DatabaseGenerationMode
    {
        /// <summary>
        /// Только создать базу данных, без таблиц.
        /// </summary>
        None,
        /// <summary>
        /// Минимум: только таблица Users (Role как строка).
        /// </summary>
        Simple,
        /// <summary>
        /// Стандарт: Users + Roles (Role как внешний ключ).
        /// </summary>
        Standard,
        /// <summary>
        /// Полный: Users + Roles + RolePermissions (система прав).
        /// </summary>
        Full
    }

    /// <summary>
    /// Опции генерации схемы базы данных.
    /// </summary>
    public class DatabaseGenerationOptions
    {
        /// <summary>Режим генерации (по умолчанию Full).</summary>
        public DatabaseGenerationMode Mode { get; set; } = DatabaseGenerationMode.Full;

        /// <summary>Название базы данных (по умолчанию из ScrapsConfig).</summary>
        public string DatabaseName { get; set; } = ScrapsConfig.DatabaseName;

        /// <summary>Строка подключения (по умолчанию из ScrapsConfig).</summary>
        public string ConnectionString { get; set; } = ScrapsConfig.ConnectionString;

        /// <summary>Название таблицы пользователей (по умолчанию "Users").</summary>
        public string UsersTableName { get; set; } = ScrapsConfig.UsersTableName;

        /// <summary>Сопоставление логических ключей колонок с реальными именами.</summary>
        public Dictionary<string, string> UsersTableColumnsNames { get; set; } = ScrapsConfig.UsersTableColumnsNames;

        /// <summary>Название роли по умолчанию (RoleID = 0 или строка).</summary>
        public string DefaultRoleName { get; set; } = ScrapsConfig.DefaultRoleName;

        /// <summary>Роли для первичного заполнения (только для Standard/Full).</summary>
        public string[] SeedRoles { get; set; } = ScrapsConfig.SeedRoles;

        /// <summary>
        /// Создать опции с значениями по умолчанию из ScrapsConfig.
        /// </summary>
        public static DatabaseGenerationOptions Default() => new DatabaseGenerationOptions();

        /// <summary>
        /// Создать опции для указанной базы данных.
        /// </summary>
        public static DatabaseGenerationOptions ForDatabase(string databaseName, DatabaseGenerationMode mode = DatabaseGenerationMode.Full) 
            => new DatabaseGenerationOptions { DatabaseName = databaseName, Mode = mode };

        /// <summary>
        /// Только база данных, без таблиц.
        /// </summary>
        public static DatabaseGenerationOptions None(string databaseName = null)
            => new DatabaseGenerationOptions { DatabaseName = databaseName, Mode = DatabaseGenerationMode.None };

        /// <summary>
        /// Простой режим: только Users (Role как строка).
        /// </summary>
        public static DatabaseGenerationOptions Simple(string databaseName = null) 
            => new DatabaseGenerationOptions { DatabaseName = databaseName, Mode = DatabaseGenerationMode.Simple };

        /// <summary>
        /// Стандартный режим: Users + Roles.
        /// </summary>
        public static DatabaseGenerationOptions Standard(string databaseName = null) 
            => new DatabaseGenerationOptions { DatabaseName = databaseName, Mode = DatabaseGenerationMode.Standard };

        /// <summary>
        /// Полный режим: Users + Roles + RolePermissions.
        /// </summary>
        public static DatabaseGenerationOptions Full(string databaseName = null) 
            => new DatabaseGenerationOptions { DatabaseName = databaseName, Mode = DatabaseGenerationMode.Full };
    }
}
