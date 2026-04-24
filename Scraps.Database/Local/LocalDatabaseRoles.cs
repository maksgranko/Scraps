using Scraps.Configs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Scraps.Database.Local
{
    /// <summary>
    /// Управление ролями в JSON-файле.
    /// </summary>
    internal class LocalDatabaseRoles : IDatabaseRoles
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
                    Schema = new Dictionary<string, string>
                    {
                        ["RoleID"] = "Int32",
                        ["RoleName"] = "String"
                    }
                };
                JsonTableSerializer.Save(path, empty);
            }
        }

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
    }
}
