using Scraps.Databases;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scraps.Security
{
    /// <summary>
    /// Набор прав доступа. Можно комбинировать флаги.
    /// </summary>
    [Flags]
    public enum PermissionFlags
    {
        /// <summary>
        /// Нет прав.
        /// </summary>
        None = 0,
        /// <summary>
        /// Чтение.
        /// </summary>
        Read = 1,
        /// <summary>
        /// Запись/редактирование.
        /// </summary>
        Write = 2,
        /// <summary>
        /// Удаление.
        /// </summary>
        Delete = 4,
        /// <summary>
        /// Экспорт.
        /// </summary>
        Export = 8,
        /// <summary>
        /// Импорт.
        /// </summary>
        Import = 16
    }

    /// <summary>
    /// Права на конкретную таблицу.
    /// </summary>
    public class TablePermission
    {
        /// <summary>
        /// Имя таблицы (или "*" для глобального правила).
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Флаги прав.
        /// </summary>
        public PermissionFlags Flags { get; set; }

        /// <summary>
        /// Удобный конструктор прав из булевых значений.
        /// </summary>
        public static TablePermission FromBooleans(string tableName, bool canRead, bool canWrite, bool canDelete, bool canExport, bool canImport)
        {
            var flags = PermissionFlags.None;
            if (canRead) flags |= PermissionFlags.Read;
            if (canWrite) flags |= PermissionFlags.Write;
            if (canDelete) flags |= PermissionFlags.Delete;
            if (canExport) flags |= PermissionFlags.Export;
            if (canImport) flags |= PermissionFlags.Import;
            return new TablePermission { TableName = tableName, Flags = flags };
        }
    }

    /// <summary>
    /// Роль и её права.
    /// </summary>
    public class Role
    {
        /// <summary>
        /// Название роли.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Права по таблицам.
        /// </summary>
        public List<TablePermission> TablePermissions { get; set; } = new List<TablePermission>();

        /// <summary>
        /// Проверить, есть ли у роли нужные права на таблицу.
        /// </summary>
        public bool HasPermission(string tableName, PermissionFlags required)
        {
            var permission = TablePermissions.FirstOrDefault(p =>
                p.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));

            if (permission == null) return false;

            return (permission.Flags & required) == required;
        }
    }

    /// <summary>
    /// Менеджер ролей и прав доступа.
    /// </summary>
    public static class RoleManager
    {
        private static readonly Dictionary<string, Role> Roles = new Dictionary<string, Role>();
        private static readonly Dictionary<string, PermissionFlags> DefaultPermissions = new Dictionary<string, PermissionFlags>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Инициализация из заранее подготовленного списка ролей.
        /// </summary>
        public static void Initialize(IEnumerable<Role> roles)
        {
            Roles.Clear();
            DefaultPermissions.Clear();
            foreach (var role in roles)
            {
                Roles[role.Name] = role;
            }
        }

        /// <summary>
        /// Инициализация ролей и прав из БД (Roles + RolePermissions).
        /// </summary>
        public static void InitializeFromDb()
        {
            Roles.Clear();
            DefaultPermissions.Clear();

            var roles = MSSQL.Roles.GetAll();
            var permissions = MSSQL.RolePermissions.GetAll();

            foreach (var r in roles)
            {
                Roles[r.Name] = new Role
                {
                    Name = r.Name
                };
            }

            foreach (var p in permissions)
            {
                if (p.RoleId == 0)
                {
                    DefaultPermissions[p.TableName] = p.Flags;
                    continue;
                }

                var role = roles.Find(x => x.Id == p.RoleId);
                if (role == null) continue;

                if (!Roles.TryGetValue(role.Name, out var r))
                {
                    r = new Role { Name = role.Name };
                    Roles[role.Name] = r;
                }
                r.TablePermissions.Add(new TablePermission
                {
                    TableName = p.TableName,
                    Flags = p.Flags
                });
            }
        }

        /// <summary>
        /// Проверка доступа к таблице по роли.
        /// </summary>
        public static bool CheckAccess(string roleName, string tableName, PermissionFlags required)
        {
            if (!Roles.TryGetValue(roleName, out var role))
                return CheckDefaultAccess(tableName, required);

            if (role.HasPermission(tableName, required))
                return true;

            return CheckDefaultAccess(tableName, required);
        }

        /// <summary>
        /// Добавить роль в менеджер.
        /// </summary>
        public static void AddRole(Role role)
        {
            Roles[role.Name] = role;
        }

        /// <summary>
        /// Получить роль по имени.
        /// </summary>
        public static Role GetRole(string roleName)
        {
            return Roles.TryGetValue(roleName, out var role) ? role : null;
        }

        /// <summary>
        /// Удобное строковое представление флагов прав (для отладки/логов).
        /// </summary>
        public static string PrintPermissions(PermissionFlags flags)
        {
            if (flags == PermissionFlags.None) return "None";

            var parts = new List<string>();
            if ((flags & PermissionFlags.Read) == PermissionFlags.Read) parts.Add("Read");
            if ((flags & PermissionFlags.Write) == PermissionFlags.Write) parts.Add("Write");
            if ((flags & PermissionFlags.Delete) == PermissionFlags.Delete) parts.Add("Delete");
            if ((flags & PermissionFlags.Export) == PermissionFlags.Export) parts.Add("Export");
            if ((flags & PermissionFlags.Import) == PermissionFlags.Import) parts.Add("Import");
            return string.Join(" | ", parts);
        }

        /// <summary>
        /// Получить отладочное описание прав роли по таблицам.
        /// </summary>
        public static string PrintRolePermissions(string roleName)
        {
            if (!Roles.TryGetValue(roleName, out var role))
                return $"Role '{roleName}' not found.";

            var lines = new List<string>();
            foreach (var p in role.TablePermissions)
            {
                lines.Add($"{p.TableName}: {PrintPermissions(p.Flags)}");
            }

            if (DefaultPermissions.Count > 0)
            {
                lines.Add("Default:");
                foreach (var kv in DefaultPermissions)
                {
                    lines.Add($"{kv.Key}: {PrintPermissions(kv.Value)}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Отладочное описание всех ролей и их прав.
        /// </summary>
        public static string PrintAllRoles()
        {
            if (Roles.Count == 0) return "No roles loaded.";

            var lines = new List<string>();
            foreach (var role in Roles.Values)
            {
                lines.Add($"Role: {role.Name}");
                foreach (var p in role.TablePermissions)
                {
                    lines.Add($"{p.TableName}: {PrintPermissions(p.Flags)}");
                }
            }

            if (DefaultPermissions.Count > 0)
            {
                lines.Add("Default:");
                foreach (var kv in DefaultPermissions)
                {
                    lines.Add($"{kv.Key}: {PrintPermissions(kv.Value)}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static bool CheckDefaultAccess(string tableName, PermissionFlags required)
        {
            if (DefaultPermissions.TryGetValue(tableName, out var perm))
                return (perm & required) == required;

            if (DefaultPermissions.TryGetValue("*", out var permGlobal))
                return (permGlobal & required) == required;

            return false;
        }
    }
}
