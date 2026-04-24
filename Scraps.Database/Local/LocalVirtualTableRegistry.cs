using Scraps.Security;
using System;
using System.Collections.Generic;
using System.Data;

namespace Scraps.Database.LocalFiles
{
    /// <summary>
    /// Реестр виртуальных таблиц (in-memory, как в MSSQL).
    /// Данные читаются через LocalDatabaseData.
    /// </summary>
    public class LocalVirtualTableRegistry : IVirtualTableRegistry
    {
        private readonly LocalDatabaseData _data = new LocalDatabaseData();

        private class Entry
        {
            public string Name { get; set; }
            public string Sql { get; set; }
            public Dictionary<string, PermissionFlags> RolePermissions { get; set; } = new Dictionary<string, PermissionFlags>(StringComparer.OrdinalIgnoreCase);
        }

        private static readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Зарегистрировать виртуальную таблицу с единым требуемым правом для всех ролей.</summary>
        public void Register(string name, string sql, PermissionFlags required = PermissionFlags.Read)
        {
            Register(name, sql, new Dictionary<string, PermissionFlags> { ["*"] = required });
        }

        /// <summary>Зарегистрировать виртуальную таблицу с правами по ролям.</summary>
        public void Register(string name, string sql, IDictionary<string, PermissionFlags> rolePermissions)
        {
            _entries[name] = new Entry
            {
                Name = name,
                Sql = sql,
                RolePermissions = new Dictionary<string, PermissionFlags>(rolePermissions, StringComparer.OrdinalIgnoreCase)
            };
        }

        /// <summary>Удалить виртуальную таблицу из реестра.</summary>
        public void Remove(string name)
        {
            _entries.Remove(name);
        }

        /// <summary>Очистить реестр виртуальных таблиц.</summary>
        public void Clear()
        {
            _entries.Clear();
        }

        /// <summary>Получить имена всех зарегистрированных виртуальных таблиц.</summary>
        public List<string> GetNames()
        {
            return new List<string>(_entries.Keys);
        }

        /// <summary>Попытаться получить SQL и права доступа для виртуальной таблицы.</summary>
        public bool TryGetQuery(string name, out string sql, out IDictionary<string, PermissionFlags> permissions)
        {
            if (_entries.TryGetValue(name, out var entry))
            {
                sql = entry.Sql;
                permissions = entry.RolePermissions;
                return true;
            }
            sql = null;
            permissions = null;
            return false;
        }

        /// <summary>Получить данные виртуальной таблицы с проверкой прав доступа.</summary>
        public DataTable GetData(string name, string roleName = null, PermissionFlags required = PermissionFlags.Read)
        {
            if (!_entries.TryGetValue(name, out var entry))
                throw new InvalidOperationException($"Виртуальная таблица '{name}' не найдена.");

            // Проверка прав
            if (!string.IsNullOrWhiteSpace(roleName))
            {
                var effective = PermissionFlags.None;
                if (entry.RolePermissions.TryGetValue(roleName, out var rolePerm))
                    effective = rolePerm;
                else if (entry.RolePermissions.TryGetValue("*", out var wildcardPerm))
                    effective = wildcardPerm;

                if ((effective & required) != required)
                    throw new UnauthorizedAccessException($"Недостаточно прав для виртуальной таблицы '{name}'.");
            }

            // В файловом режиме SQL не выполняется — возвращаем пустую таблицу
            // (в будущем можно добавить простой SQL-парсер)
            return new DataTable(name);
        }
    }
}
