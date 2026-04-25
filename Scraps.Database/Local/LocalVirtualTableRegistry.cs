using Scraps.Security;
using System;
using System.Collections.Generic;
using System.Data;

namespace Scraps.Database.Local
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

        public void Register(string name, string sql, PermissionFlags required = PermissionFlags.Read)
        {
            Register(name, sql, new Dictionary<string, PermissionFlags> { ["*"] = required });
        }

        public void Register(string name, string sql, IDictionary<string, PermissionFlags> rolePermissions)
        {
            _entries[name] = new Entry
            {
                Name = name,
                Sql = sql,
                RolePermissions = new Dictionary<string, PermissionFlags>(rolePermissions, StringComparer.OrdinalIgnoreCase)
            };
        }

        public void Remove(string name)
        {
            _entries.Remove(name);
        }

        public void Clear()
        {
            _entries.Clear();
        }

        public List<string> GetNames()
        {
            return new List<string>(_entries.Keys);
        }

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
