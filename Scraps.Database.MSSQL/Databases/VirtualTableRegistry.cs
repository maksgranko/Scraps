using Scraps.Security;
using System;
using System.Collections.Generic;
using System.Data;

namespace Scraps.Database.MSSQL
{
    /// <summary>
    /// Реестр виртуальных таблиц (имя -> SQL запрос).
    /// </summary>
    public static class VirtualTableRegistry
    {
        private class VirtualTableEntry
        {
            public string Name { get; set; }
            public string Sql { get; set; }
            public Dictionary<string, PermissionFlags> RolePermissions { get; set; } = new Dictionary<string, PermissionFlags>(StringComparer.OrdinalIgnoreCase);
        }

        private static readonly Dictionary<string, VirtualTableEntry> Entries = new Dictionary<string, VirtualTableEntry>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Зарегистрировать виртуальную таблицу.
        /// </summary>
        public static void Register(string name, string sql, PermissionFlags required = PermissionFlags.Read)
        {
            Register(name, sql, new Dictionary<string, PermissionFlags> { ["*"] = required });
        }

        /// <summary>
        /// Зарегистрировать виртуальную таблицу как простой SELECT из таблицы (с экранированием).
        /// </summary>
        public static void RegisterSelect(string name, string tableName, PermissionFlags required = PermissionFlags.Read)
        {
            RegisterSelect(name, tableName, columns: null, where: null, required: required);
        }

        /// <summary>
        /// Зарегистрировать виртуальную таблицу как SELECT из таблицы (с экранированием).
        /// </summary>
        public static void RegisterSelect(
            string name,
            string tableName,
            string[] columns,
            string where,
            PermissionFlags required = PermissionFlags.Read)
        {
            var sql = BuildSelectQuery(tableName, columns, where);
            Register(name, sql, required);
        }

        /// <summary>
        /// Собрать безопасный SELECT по таблице (экранирует имена).
        /// </summary>
        public static string BuildSelectQuery(string tableName, string[] columns = null, string where = null)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));

            string selectColumns;
            if (columns == null || columns.Length == 0)
            {
                selectColumns = "*";
            }
            else
            {
                var quoted = new string[columns.Length];
                for (int i = 0; i < columns.Length; i++)
                {
                    quoted[i] = MSSQL.QuoteIdentifier(columns[i]);
                }
                selectColumns = string.Join(", ", quoted);
            }

            var sql = $"SELECT {selectColumns} FROM {MSSQL.QuoteIdentifier(tableName)}";
            if (!string.IsNullOrWhiteSpace(where))
            {
                sql += " WHERE " + where;
            }
            return sql;
        }

        /// <summary>
        /// Зарегистрировать виртуальную таблицу с правилами по ролям.
        /// </summary>
        public static void Register(string name, string sql, IDictionary<string, PermissionFlags> rolePermissions)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentNullException(nameof(sql));
            if (rolePermissions == null) throw new ArgumentNullException(nameof(rolePermissions));

            var entry = new VirtualTableEntry
            {
                Name = name,
                Sql = sql,
                RolePermissions = new Dictionary<string, PermissionFlags>(rolePermissions, StringComparer.OrdinalIgnoreCase)
            };

            Entries[name] = entry;
        }

        /// <summary>
        /// Зарегистрировать несколько виртуальных таблиц.
        /// </summary>
        public static void RegisterMany(IDictionary<string, string> queries)
        {
            if (queries == null) throw new ArgumentNullException(nameof(queries));
            foreach (var kv in queries)
            {
                Register(kv.Key, kv.Value);
            }
        }

        /// <summary>
        /// Удалить виртуальную таблицу по имени.
        /// </summary>
        public static bool Remove(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return Entries.Remove(name);
        }

        /// <summary>
        /// Очистить все виртуальные таблицы.
        /// </summary>
        public static void Clear()
        {
            Entries.Clear();
        }

        /// <summary>
        /// Получить список имён виртуальных таблиц.
        /// </summary>
        public static string[] GetNames()
        {
            var result = new string[Entries.Count];
            Entries.Keys.CopyTo(result, 0);
            return result;
        }

        /// <summary>
        /// Получить SQL запроса по имени.
        /// </summary>
        public static bool TryGetQuery(string name, out string sql)
        {
            sql = null;
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (!Entries.TryGetValue(name, out var entry)) return false;
            sql = entry.Sql;
            return true;
        }

        /// <summary>
        /// Получить права по ролям виртуальной таблицы по имени.
        /// </summary>
        public static bool TryGetPermissions(string name, out Dictionary<string, PermissionFlags> permissions)
        {
            permissions = null;
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (!Entries.TryGetValue(name, out var entry)) return false;
            permissions = new Dictionary<string, PermissionFlags>(entry.RolePermissions, StringComparer.OrdinalIgnoreCase);
            return true;
        }

        /// <summary>
        /// Проверить доступ по роли и требуемым правам.
        /// </summary>
        public static bool CheckAccess(string name, string roleName, PermissionFlags required, out string error)
        {
            error = null;
            if (!Entries.TryGetValue(name, out var entry))
            {
                error = $"Виртуальная таблица '{name}' не зарегистрирована.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(roleName))
            {
                error = "Роль не может быть пустой.";
                return false;
            }

            if (entry.RolePermissions.TryGetValue(roleName, out var flags))
            {
                return (flags & required) == required;
            }

            if (entry.RolePermissions.TryGetValue("*", out var defaultFlags))
            {
                return (defaultFlags & required) == required;
            }

            error = $"Нет прав ({required}) для роли '{roleName}' на виртуальную таблицу '{name}'.";
            return false;
        }

        /// <summary>
        /// Выполнить SQL виртуальной таблицы и вернуть DataTable.
        /// </summary>
        public static DataTable GetData(string name, string roleName = null, PermissionFlags required = PermissionFlags.Read)
        {
            if (!CheckAccess(name, roleName, required, out var error))
                throw new UnauthorizedAccessException(error);

            if (!Entries.TryGetValue(name, out var entry))
                throw new KeyNotFoundException($"Виртуальная таблица '{name}' не зарегистрирована.");

            return MSSQL.GetDataTableFromSQL(entry.Sql);
        }
    }
}




