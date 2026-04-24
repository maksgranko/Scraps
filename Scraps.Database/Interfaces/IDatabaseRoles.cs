using System.Collections.Generic;

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
    }
}
