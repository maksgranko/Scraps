using System.Collections.Generic;

namespace Scraps.Databases
{
    /// <summary>
    /// Опции генерации схемы базы данных.
    /// </summary>
    public class DatabaseGenerationOptions
    {
        /// <summary>Название базы данных.</summary>
        public string DatabaseName { get; set; }
        /// <summary>Строка подключения.</summary>
        public string ConnectionString { get; set; }
        /// <summary>Название таблицы пользователей.</summary>
        public string UsersTableName { get; set; }
        /// <summary>Сопоставление логических ключей колонок с реальными именами.</summary>
        public Dictionary<string, string> UsersTableColumnsNames { get; set; }
        /// <summary>Обязательные логические ключи для таблицы пользователей.</summary>
        public string[] UsersRequiredColumnKeys { get; set; }
        /// <summary>Если true, Users.Role хранит RoleID (int). Если false, хранит RoleName (string).</summary>
        public bool UseRoleIdMapping { get; set; }
        /// <summary>Название роли по умолчанию (RoleID = 0).</summary>
        public string DefaultRoleName { get; set; }
        /// <summary>Роли для первичного заполнения.</summary>
        public string[] SeedRoles { get; set; }
        /// <summary>Создать таблицу Roles.</summary>
        public bool CreateRolesTable { get; set; }
        /// <summary>Создать таблицу RolePermissions.</summary>
        public bool CreateRolePermissionsTable { get; set; }
        /// <summary>Создать таблицу Users.</summary>
        public bool CreateUsersTable { get; set; }
    }
}
