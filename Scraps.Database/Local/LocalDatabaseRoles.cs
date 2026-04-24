using Scraps.Configs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Scraps.Security;

namespace Scraps.Database.LocalFiles
{
    /// <summary>
    /// Управление ролями в JSON-файле.
    /// </summary>
    public class LocalDatabaseRoles : IDatabaseRoles
    {
        private readonly LocalDatabaseData _data = new LocalDatabaseData();
        private const string TableName = "Roles";

        private void EnsureTable()
        {
            var path = System.IO.Path.Combine(ScrapsConfig.LocalDataPath, TableName + ".json");
            if (!System.IO.File.Exists(path))
            {
                var empty = new JsonTable
                {
                    Schema = new System.Collections.Generic.List<SchemaEntry>
                    {
                        new SchemaEntry { Name = "RoleID", Type = "Int32" },
                        new SchemaEntry { Name = "RoleName", Type = "String" }
                    }
                };
                JsonTableSerializer.Save(path, empty);
            }
        }

        /// <summary>Получить имя роли по ID.</summary>
        public string GetRoleNameById(int roleId)
        {
            EnsureTable();
            var dt = _data.GetTableData(TableName);
            foreach (DataRow row in dt.Rows)
            {
                if (int.TryParse(row["RoleID"]?.ToString(), out var id) && id == roleId)
                    return row["RoleName"]?.ToString();
            }
            return null;
        }

        /// <summary>Получить ID роли по имени.</summary>
        public int? GetRoleIdByName(string roleName)
        {
            EnsureTable();
            var dt = _data.GetTableData(TableName);
            foreach (DataRow row in dt.Rows)
            {
                if (string.Equals(row["RoleName"]?.ToString(), roleName, StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(row["RoleID"]?.ToString(), out var id))
                        return id;
                }
            }
            return null;
        }

        /// <summary>Создать роль и вернуть её ID.</summary>
        public int Create(string roleName)
        {
            EnsureTable();
            var dt = _data.GetTableData(TableName);

            if (GetRoleIdByName(roleName) != null)
                throw new InvalidOperationException($"Роль '{roleName}' уже существует.");

            int nextId = 1;
            foreach (DataRow row in dt.Rows)
            {
                if (int.TryParse(row["RoleID"]?.ToString(), out var existingId) && existingId >= nextId)
                    nextId = existingId + 1;
            }

            var newRow = dt.NewRow();
            newRow["RoleID"] = nextId;
            newRow["RoleName"] = roleName;
            dt.Rows.Add(newRow);

            _data.ApplyTableChanges(TableName, dt);
            return nextId;
        }

        /// <summary>Удалить роль по имени.</summary>
        public void Delete(string roleName)
        {
            EnsureTable();
            var dt = _data.GetTableData(TableName);

            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                if (string.Equals(dt.Rows[i]["RoleName"]?.ToString(), roleName, StringComparison.OrdinalIgnoreCase))
                {
                    dt.Rows[i].Delete();
                }
            }

            dt.AcceptChanges();
            _data.ApplyTableChanges(TableName, dt);
        }

        /// <summary>Переименовать роль.</summary>
        public void Rename(string oldName, string newName)
        {
            EnsureTable();
            var dt = _data.GetTableData(TableName);

            foreach (DataRow row in dt.Rows)
            {
                if (string.Equals(row["RoleName"]?.ToString(), oldName, StringComparison.OrdinalIgnoreCase))
                {
                    row["RoleName"] = newName;
                }
            }

            _data.ApplyTableChanges(TableName, dt);
        }

        /// <summary>Получить список всех ролей.</summary>
        public List<RoleInfo> GetAll()
        {
            EnsureTable();
            var dt = _data.GetTableData(TableName);
            var list = new List<RoleInfo>();
            foreach (DataRow row in dt.Rows)
            {
                if (int.TryParse(row["RoleID"]?.ToString(), out var id))
                {
                    list.Add(new RoleInfo { Id = id, Name = row["RoleName"]?.ToString() });
                }
            }
            return list;
        }

        /// <summary>Проверить доступ роли к таблице.</summary>
        public bool CheckAccess(string roleName, string tableName, PermissionFlags required)
        {
            var flags = GetEffectivePermissions(roleName, tableName);
            return (flags & required) == required;
        }

        /// <summary>Получить эффективные права роли на таблицу.</summary>
        public PermissionFlags GetEffectivePermissions(string roleName, string tableName)
        {
            var permissions = new LocalDatabaseRolePermissions();
            var all = permissions.GetAll();

            var roleId = GetRoleIdByName(roleName);
            if (roleId.HasValue)
            {
                var roleFlags = ResolveRoleFlags(all, roleId.Value, tableName);
                if (roleFlags != PermissionFlags.None)
                    return roleFlags;
            }

            return ResolveRoleFlags(all, 0, tableName);
        }

        private static PermissionFlags ResolveRoleFlags(List<RolePermissionInfo> permissions, int roleId, string tableName)
        {
            var explicitPermission = permissions.FirstOrDefault(p =>
                p.RoleId == roleId &&
                string.Equals(p.TableName, tableName, StringComparison.OrdinalIgnoreCase));
            if (explicitPermission != null)
                return explicitPermission.Flags;

            var wildcardPermission = permissions.FirstOrDefault(p =>
                p.RoleId == roleId &&
                string.Equals(p.TableName, TablePermission.AnyTable, StringComparison.OrdinalIgnoreCase));
            if (wildcardPermission != null)
                return wildcardPermission.Flags;

            return PermissionFlags.None;
        }
    }
}
