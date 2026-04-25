using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Scraps.Configs;
using Scraps.Security;

namespace Scraps.Database.Local
{
    /// <summary>
    /// Управление правами ролей в JSON-файле.
    /// </summary>
    public class LocalDatabaseRolePermissions : IDatabaseRolePermissions
    {
        private readonly LocalDatabaseData _data = new LocalDatabaseData();
        private const string TableName = "RolePermissions";

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
                        new SchemaEntry { Name = "TableName", Type = "String" },
                        new SchemaEntry { Name = "Flags", Type = "Int32" }
                    }
                };
                JsonTableSerializer.Save(path, empty);
            }
        }

        public List<RolePermissionInfo> GetAll()
        {
            EnsureTable();
            var dt = _data.GetTableData(TableName);
            return ParsePermissions(dt);
        }

        public List<RolePermissionInfo> GetByRoleName(string roleName)
        {
            var roles = new LocalDatabaseRoles();
            var roleId = roles.GetRoleIdByName(roleName);
            if (roleId == null) return new List<RolePermissionInfo>();
            return GetByRoleId(roleId.Value);
        }

        public List<RolePermissionInfo> GetByRoleId(int roleId)
        {
            EnsureTable();
            var dt = _data.GetTableData(TableName);
            var result = new List<RolePermissionInfo>();
            foreach (DataRow row in dt.Rows)
            {
                if (int.TryParse(row["RoleID"]?.ToString(), out var id) && id == roleId)
                {
                    result.Add(new RolePermissionInfo
                    {
                        RoleId = roleId,
                        TableName = row["TableName"]?.ToString(),
                        Flags = (PermissionFlags)int.Parse(row["Flags"]?.ToString() ?? "0")
                    });
                }
            }
            return result;
        }

        public void Set(string roleName, string tableName, PermissionFlags flags)
        {
            var roles = new LocalDatabaseRoles();
            var roleId = roles.GetRoleIdByName(roleName);
            if (roleId == null)
                throw new InvalidOperationException($"Роль '{roleName}' не найдена.");
            Set(roleId.Value, tableName, flags);
        }

        public void Set(int roleId, string tableName, PermissionFlags flags)
        {
            EnsureTable();
            var dt = _data.GetTableData(TableName);

            // Удаляем существующую запись
            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                if (int.TryParse(dt.Rows[i]["RoleID"]?.ToString(), out var id) && id == roleId &&
                    string.Equals(dt.Rows[i]["TableName"]?.ToString(), tableName, StringComparison.OrdinalIgnoreCase))
                {
                    dt.Rows[i].Delete();
                }
            }

            var newRow = dt.NewRow();
            newRow["RoleID"] = roleId;
            newRow["TableName"] = tableName;
            newRow["Flags"] = (int)flags;
            dt.Rows.Add(newRow);

            dt.AcceptChanges();
            _data.ApplyTableChanges(TableName, dt);
        }

        public void Delete(string roleName, string tableName)
        {
            var roles = new LocalDatabaseRoles();
            var roleId = roles.GetRoleIdByName(roleName);
            if (roleId == null) return;
            Delete(roleId.Value, tableName);
        }

        public void Delete(int roleId, string tableName)
        {
            EnsureTable();
            var dt = _data.GetTableData(TableName);

            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                if (int.TryParse(dt.Rows[i]["RoleID"]?.ToString(), out var id) && id == roleId &&
                    string.Equals(dt.Rows[i]["TableName"]?.ToString(), tableName, StringComparison.OrdinalIgnoreCase))
                {
                    dt.Rows[i].Delete();
                }
            }

            dt.AcceptChanges();
            _data.ApplyTableChanges(TableName, dt);
        }

        public void DeleteAllForRole(string roleName)
        {
            var roles = new LocalDatabaseRoles();
            var roleId = roles.GetRoleIdByName(roleName);
            if (roleId == null) return;
            DeleteAllForRole(roleId.Value);
        }

        public void DeleteAllForRole(int roleId)
        {
            EnsureTable();
            var dt = _data.GetTableData(TableName);

            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                if (int.TryParse(dt.Rows[i]["RoleID"]?.ToString(), out var id) && id == roleId)
                {
                    dt.Rows[i].Delete();
                }
            }

            dt.AcceptChanges();
            _data.ApplyTableChanges(TableName, dt);
        }

        private static List<RolePermissionInfo> ParsePermissions(DataTable dt)
        {
            var result = new List<RolePermissionInfo>();
            foreach (DataRow row in dt.Rows)
            {
                if (int.TryParse(row["RoleID"]?.ToString(), out var roleId))
                {
                    result.Add(new RolePermissionInfo
                    {
                        RoleId = roleId,
                        TableName = row["TableName"]?.ToString(),
                        Flags = (PermissionFlags)int.Parse(row["Flags"]?.ToString() ?? "0")
                    });
                }
            }
            return result;
        }
    }
}
