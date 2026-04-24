using System.Collections.Generic;
using Scraps.Security;

namespace Scraps.Database
{
    /// <summary>
    /// Интерфейс для работы с ролями.
    /// </summary>
    public interface IDatabaseRoles
    {
        /// <summary>Получить имя роли по ID.</summary>
        string GetRoleNameById(int roleId);

        /// <summary>Получить ID роли по имени.</summary>
        int? GetRoleIdByName(string roleName);

        /// <summary>Создать роль.</summary>
        int Create(string roleName);

        /// <summary>Удалить роль.</summary>
        void Delete(string roleName);

        /// <summary>Переименовать роль.</summary>
        void Rename(string oldName, string newName);

        /// <summary>Получить все роли.</summary>
        List<RoleInfo> GetAll();

        /// <summary>Проверить доступ роли к таблице.</summary>
        bool CheckAccess(string roleName, string tableName, PermissionFlags required);

        /// <summary>Получить эффективные права роли на таблицу.</summary>
        PermissionFlags GetEffectivePermissions(string roleName, string tableName);
    }
}
