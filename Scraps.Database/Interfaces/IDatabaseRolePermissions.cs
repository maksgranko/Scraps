using Scraps.Security;
using System.Collections.Generic;

namespace Scraps.Database
{
    /// <summary>
    /// Интерфейс для работы с правами ролей.
    /// </summary>
    public interface IDatabaseRolePermissions
    {
        /// <summary>Получить все права.</summary>
        List<RolePermissionInfo> GetAll();

        /// <summary>Получить права роли по имени.</summary>
        List<RolePermissionInfo> GetByRoleName(string roleName);

        /// <summary>Получить права роли по ID.</summary>
        List<RolePermissionInfo> GetByRoleId(int roleId);

        /// <summary>Установить права роли на таблицу.</summary>
        void Set(string roleName, string tableName, PermissionFlags flags);

        /// <summary>Установить права роли на таблицу по ID.</summary>
        void Set(int roleId, string tableName, PermissionFlags flags);

        /// <summary>Удалить права роли на таблицу.</summary>
        void Delete(string roleName, string tableName);

        /// <summary>Удалить права роли на таблицу по ID.</summary>
        void Delete(int roleId, string tableName);

        /// <summary>Удалить все права роли.</summary>
        void DeleteAllForRole(string roleName);

        /// <summary>Удалить все права роли по ID.</summary>
        void DeleteAllForRole(int roleId);
    }
}
