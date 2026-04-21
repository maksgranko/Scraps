using Scraps.Configs;
using Scraps.Databases;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scraps.Security
{
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
            foreach (var role in roles ?? Enumerable.Empty<Role>())
            {
                Roles[role.Name] = role;
            }
        }

        /// <summary>
        /// Инициализация из списка ролей + role-defaults (wildcard) для каждой роли.
        /// </summary>
        public static void Initialize(IEnumerable<Role> roles, IDictionary<string, PermissionFlags> roleDefaults)
        {
            Initialize(roles);

            if (roleDefaults == null)
                return;

            foreach (var kv in roleDefaults)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                    continue;

                if (!Roles.TryGetValue(kv.Key, out var role))
                {
                    role = new Role(kv.Key);
                    Roles[kv.Key] = role;
                }

                var wildcard = role.TablePermissions.FirstOrDefault(p =>
                    TablePermission.IsWildcardTableName(p.TableName));

                if (wildcard != null)
                    wildcard.Flags = kv.Value;
                else
                    role.TablePermissions.Add(TablePermission.Any(kv.Value));
            }
        }

        /// <summary>
        /// Инициализация ролей и прав из БД (Roles + RolePermissions).
        /// Работает только в режимах Standard и Full.
        /// </summary>
        public static void InitializeFromDb()
        {
            if (!ScrapsConfig.UseRoleIdMapping)
            {
                // Simple режим — роли как строки, без системы прав
                Roles.Clear();
                DefaultPermissions.Clear();
                return;
            }

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
            if (TryGetRolePermission(roleName, tableName, out var roleFlags))
                return (roleFlags & required) == required;

            return CheckDefaultAccess(tableName, required);
        }

        /// <summary>
        /// Создать роль в БД и добавить в кэш.
        /// </summary>
        /// <exception cref="ArgumentException">Пустое название роли</exception>
        /// <exception cref="InvalidOperationException">Роль уже существует или система ролей отключена</exception>
        public static int CreateRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException("Название роли не может быть пустым.", nameof(roleName));

            if (!ScrapsConfig.UseRoleIdMapping)
                throw new InvalidOperationException("Система ролей отключена (UseRoleIdMapping = false).");

            var id = MSSQL.Roles.Create(roleName);
            Roles[roleName] = new Role(roleName);
            return id;
        }

        /// <summary>
        /// Создать роль с правами в БД.
        /// </summary>
        /// <exception cref="ArgumentException">Пустое название роли</exception>
        /// <exception cref="InvalidOperationException">Роль уже существует или система ролей отключена</exception>
        public static int CreateRole(string roleName, params (string tableName, PermissionFlags flags)[] permissions)
        {
            var id = CreateRole(roleName);
            var role = Roles[roleName];

            foreach (var (tableName, flags) in permissions)
            {
                MSSQL.RolePermissions.Set(id, tableName, flags);
                role.TablePermissions.Add(new TablePermission(tableName, flags));
            }
            return id;
        }

        /// <summary>
        /// Проверить существование роли.
        /// </summary>
        /// <exception cref="ArgumentException">Пустое название роли</exception>
        public static bool RoleExists(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException("Название роли не может быть пустым.", nameof(roleName));

            if (!ScrapsConfig.UseRoleIdMapping) return false;
            return MSSQL.Roles.GetRoleIdByName(roleName) != null;
        }

        /// <summary>
        /// Добавить роль в менеджер (только кэш, без записи в БД).
        /// </summary>
        public static void AddRole(Role role)
        {
            Roles[role.Name] = role;
        }

        /// <summary>
        /// Удалить роль из БД и кэша.
        /// </summary>
        /// <exception cref="ArgumentException">Пустое название роли</exception>
        /// <exception cref="InvalidOperationException">Роль не найдена или система ролей отключена</exception>
        public static void DeleteRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException("Название роли не может быть пустым.", nameof(roleName));

            if (!ScrapsConfig.UseRoleIdMapping)
                throw new InvalidOperationException("Система ролей отключена (UseRoleIdMapping = false).");

            var roleId = MSSQL.Roles.GetRoleIdByName(roleName);
            if (roleId == null)
                throw new InvalidOperationException($"Роль '{roleName}' не найдена.");

            // Удаляем права роли
            MSSQL.RolePermissions.DeleteAllForRole(roleId.Value);

            // Удаляем роль
            MSSQL.Roles.Delete(roleName);
            Roles.Remove(roleName);
        }

        /// <summary>
        /// Переименовать роль в БД и кэше.
        /// </summary>
        /// <exception cref="ArgumentException">Пустое название роли</exception>
        /// <exception cref="InvalidOperationException">Роль не найдена, новое имя занято или система ролей отключена</exception>
        public static void RenameRole(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName))
                throw new ArgumentException("Старое название роли не может быть пустым.", nameof(oldName));
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Новое название роли не может быть пустым.", nameof(newName));

            if (!ScrapsConfig.UseRoleIdMapping)
                throw new InvalidOperationException("Система ролей отключена (UseRoleIdMapping = false).");

            MSSQL.Roles.Rename(oldName, newName);

            if (Roles.TryGetValue(oldName, out var role))
            {
                Roles.Remove(oldName);
                role.Name = newName;
                Roles[newName] = role;
            }
        }

        /// <summary>
        /// Установить права роли на таблицу (БД + кэш).
        /// </summary>
        /// <exception cref="ArgumentException">Пустое название роли или таблицы</exception>
        /// <exception cref="InvalidOperationException">Роль не найдена или система ролей отключена</exception>
        public static void SetPermission(string roleName, string tableName, PermissionFlags flags)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException("Название роли не может быть пустым.", nameof(roleName));
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));

            if (!ScrapsConfig.UseRoleIdMapping)
                throw new InvalidOperationException("Система ролей отключена (UseRoleIdMapping = false).");

            MSSQL.RolePermissions.Set(roleName, tableName, flags);

            if (Roles.TryGetValue(roleName, out var role))
            {
                var existing = role.TablePermissions.FirstOrDefault(p =>
                    p.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                    existing.Flags = flags;
                else
                    role.TablePermissions.Add(new TablePermission(tableName, flags));
            }
        }

        /// <summary>
        /// Удалить права роли на таблицу (БД + кэш).
        /// </summary>
        /// <exception cref="ArgumentException">Пустое название роли или таблицы</exception>
        /// <exception cref="InvalidOperationException">Роль или права не найдены или система ролей отключена</exception>
        public static void RemovePermission(string roleName, string tableName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException("Название роли не может быть пустым.", nameof(roleName));
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));

            if (!ScrapsConfig.UseRoleIdMapping)
                throw new InvalidOperationException("Система ролей отключена (UseRoleIdMapping = false).");

            MSSQL.RolePermissions.Delete(roleName, tableName);

            if (Roles.TryGetValue(roleName, out var role))
            {
                role.TablePermissions.RemoveAll(p =>
                    p.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Обновить кэш из БД.
        /// </summary>
        public static void RefreshCache()
        {
            InitializeFromDb();
        }

        /// <summary>
        /// Получить роль по имени.
        /// </summary>
        public static Role GetRole(string roleName)
        {
            return Roles.TryGetValue(roleName, out var role) ? role : null;
        }

        /// <summary>
        /// Получить эффективные права роли на таблицу (с учётом default).
        /// </summary>
        public static PermissionFlags GetEffectivePermissions(string roleName, string tableName)
        {
            if (TryGetRolePermission(roleName, tableName, out var roleFlags))
                return roleFlags;

            if (DefaultPermissions.TryGetValue(tableName, out var perm))
                return perm;

            if (DefaultPermissions.TryGetValue(TablePermission.AnyTable, out var permGlobal))
                return permGlobal;

            return PermissionFlags.None;
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

            if (DefaultPermissions.TryGetValue(TablePermission.AnyTable, out var permGlobal))
                return (permGlobal & required) == required;

            return false;
        }

        private static bool TryGetRolePermission(string roleName, string tableName, out PermissionFlags flags)
        {
            flags = PermissionFlags.None;
            if (string.IsNullOrWhiteSpace(roleName) || string.IsNullOrWhiteSpace(tableName))
                return false;

            if (!Roles.TryGetValue(roleName, out var role))
                return false;

            var explicitPermission = role.TablePermissions.FirstOrDefault(p =>
                string.Equals(p.TableName, tableName, StringComparison.OrdinalIgnoreCase));
            if (explicitPermission != null)
            {
                flags = explicitPermission.Flags;
                return true;
            }

            var wildcardPermission = role.TablePermissions.FirstOrDefault(p =>
                TablePermission.IsWildcardTableName(p.TableName));
            if (wildcardPermission != null)
            {
                flags = wildcardPermission.Flags;
                return true;
            }

            return false;
        }
    }
}
