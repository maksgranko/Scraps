using Scraps.Security;
using System.Collections.Generic;
using System.Data;

namespace Scraps.Database
{
    /// <summary>
    /// Интерфейс реестра виртуальных таблиц.
    /// </summary>
    public interface IVirtualTableRegistry
    {
        /// <summary>Зарегистрировать виртуальную таблицу.</summary>
        void Register(string name, string sql, PermissionFlags required = PermissionFlags.Read);

        /// <summary>Зарегистрировать виртуальную таблицу с правами по ролям.</summary>
        void Register(string name, string sql, IDictionary<string, PermissionFlags> rolePermissions);

        /// <summary>Удалить регистрацию.</summary>
        void Remove(string name);

        /// <summary>Очистить все регистрации.</summary>
        void Clear();

        /// <summary>Получить список зарегистрированных таблиц.</summary>
        List<string> GetNames();

        /// <summary>Попробовать получить SQL запрос.</summary>
        bool TryGetQuery(string name, out string sql, out IDictionary<string, PermissionFlags> permissions);

        /// <summary>Получить данные виртуальной таблицы.</summary>
        DataTable GetData(string name, string roleName = null, PermissionFlags required = PermissionFlags.Read);
    }
}
