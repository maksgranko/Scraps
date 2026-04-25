using Scraps.Configs;
using Scraps.Database;
using Scraps.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Scraps.Database.MSSQL
{
    /// <summary>
    /// Реализация IDatabase для Microsoft SQL Server.
    /// Оборачивает legacy static API MSSQL.* через адаптеры.
    /// </summary>
    public class MSSQLDatabase : DatabaseBase
    {
        /// <summary>Провайдер базы данных.</summary>
        public override DatabaseProvider Provider => DatabaseProvider.MSSQL;

        /// <summary>Создать экземпляр MSSQLDatabase.</summary>
        public MSSQLDatabase()
        {
            Connection = new MSSQLConnectionAdapter();
            Schema = new MSSQLSchemaAdapter();
            Data = new MSSQLDataAdapter();
            Users = new MSSQLUsersAdapter();
            Roles = new MSSQLRolesAdapter();
            RolePermissions = new MSSQLRolePermissionsAdapter();
            RowEditor = new MSSQLRowEditorAdapter();
            VirtualTables = new MSSQLVirtualTableRegistryAdapter();
            ForeignKeys = new MSSQLForeignKeyProviderAdapter();
        }
    }

    internal class MSSQLConnectionAdapter : IDatabaseConnection
    {
        public string ConnectionString => ConnectionStringBuilder();

        public string ConnectionStringBuilder(string value = null)
            => MSSQL.ConnectionStringBuilder(value);

        public void ExecuteNonQuery(string sql, params object[] parameters)
        {
            using (var conn = new System.Data.SqlClient.SqlConnection(ConnectionString))
            using (var cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
            {
                AddParameters(cmd, parameters);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public object ExecuteScalar(string sql, params object[] parameters)
        {
            using (var conn = new System.Data.SqlClient.SqlConnection(ConnectionString))
            using (var cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
            {
                AddParameters(cmd, parameters);
                conn.Open();
                return cmd.ExecuteScalar();
            }
        }

        public DataTable GetDataTable(string sql, params object[] parameters)
        {
            var dt = new DataTable();
            using (var conn = new System.Data.SqlClient.SqlConnection(ConnectionString))
            {
                var cmd = new System.Data.SqlClient.SqlCommand(sql, conn);
                AddParameters(cmd, parameters);
                new System.Data.SqlClient.SqlDataAdapter(cmd).Fill(dt);
            }
            return dt;
        }

        public bool TestConnection()
            => MSSQL.CheckConnection();

        private static void AddParameters(System.Data.SqlClient.SqlCommand cmd, object[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return;
            for (int i = 0; i < parameters.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@p{i}", parameters[i] ?? System.DBNull.Value);
            }
        }
    }

    internal class MSSQLSchemaAdapter : IDatabaseSchema
    {
        public List<string> GetTables(bool includeSystem = false)
            => MSSQL.GetTables(includeSystemTables: includeSystem, includeSchemaInName: false).ToList();

        public List<string> GetTableColumns(string tableName)
            => MSSQL.GetTableColumns(tableName).ToList();

        public DataTable GetTableSchema(string tableName)
        {
            var dict = MSSQL.GetTableSchema(tableName);
            var table = new DataTable();
            table.Columns.Add("ColumnName", typeof(string));
            table.Columns.Add("DataType", typeof(string));
            foreach (var kv in dict)
            {
                var row = table.NewRow();
                row["ColumnName"] = kv.Key;
                row["DataType"] = kv.Value;
                table.Rows.Add(row);
            }
            return table;
        }

        public bool IsIdentityColumn(string tableName, string columnName)
            => MSSQL.IsIdentityColumn(tableName, columnName);

        public bool IsNullableColumn(string tableName, string columnName)
            => MSSQL.IsNullableColumn(tableName, columnName);
    }

    internal class MSSQLDataAdapter : IDatabaseData
    {
        public DataTable GetTableData(string tableName, params string[] columns)
            => MSSQL.GetTableData(tableName, columns);

        public DataTable GetTableData(string tableName, string connectionString, params string[] columns)
            => MSSQL.GetTableData(tableName, connectionString, columns);

        public DataTable GetTableDataExpanded(string tableName, IEnumerable<ForeignKeyJoin> foreignKeys, params string[] baseColumns)
        {
            var legacyJoins = foreignKeys?.Select(fk => new MSSQL.ForeignKeyJoin
            {
                BaseColumn = fk.BaseColumn,
                ReferenceTable = fk.ReferenceTable,
                ReferenceColumn = fk.ReferenceColumn,
                ReferenceColumns = fk.ReferenceColumns,
                AliasPrefix = fk.AliasPrefix
            });
            return MSSQL.GetTableData(tableName, legacyJoins, baseColumns);
        }

        public DataTable FindByColumn(string tableName, string columnName, object value, SqlFilterOperator op = SqlFilterOperator.Eq)
            => MSSQL.FindByColumn(tableName, columnName, value, op);

        public void ApplyTableChanges(string tableName, DataTable changes)
            => MSSQL.ApplyTableChanges(tableName, changes);

        public void BulkInsert(string tableName, DataTable data)
            => MSSQL.BulkInsert(tableName, data);

        public Dictionary<string, string> GetNx2Dictionary(string tableName, string keyColumn, string valueColumn)
        {
            var result = MSSQL.GetNx2Dictionary(tableName, keyColumn, valueColumn);
            return result.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
        }

        public List<string> GetNx1List(string tableName, string columnName, bool distinct = true, bool sort = true)
            => MSSQL.GetNx1List(tableName, columnName);
    }

    internal class MSSQLUsersAdapter : IDatabaseUsers
    {
        public DataRow GetByLogin(string login)
            => MSSQL.Users.GetByLogin(login);

        public string GetUserRole(string login)
            => MSSQL.Users.GetUserRole(login);

        public void Create(string login, string password, string role)
            => MSSQL.Users.Create(login, password, role);

        public void Delete(string login)
            => MSSQL.Users.Delete(login);

        public void ChangePassword(string login, string newPassword)
            => MSSQL.Users.ChangePassword(login, newPassword);

        public void ChangeRole(string login, string newRole)
            => MSSQL.Users.ChangeRole(login, newRole);
    }

    internal class MSSQLRolesAdapter : IDatabaseRoles
    {
        public string GetRoleNameById(int roleId)
            => MSSQL.Roles.GetRoleNameById(roleId);

        public int? GetRoleIdByName(string roleName)
            => MSSQL.Roles.GetRoleIdByName(roleName);

        public int Create(string roleName)
            => MSSQL.Roles.Create(roleName);

        public void Delete(string roleName)
            => MSSQL.Roles.Delete(roleName);

        public void Rename(string oldName, string newName)
            => MSSQL.Roles.Rename(oldName, newName);

        public List<RoleInfo> GetAll()
        {
            return MSSQL.Roles.GetAll();
        }

        public bool CheckAccess(string roleName, string tableName, PermissionFlags required)
        {
            var flags = GetEffectivePermissions(roleName, tableName);
            return (flags & required) == required;
        }

        public PermissionFlags GetEffectivePermissions(string roleName, string tableName)
        {
            var roleId = MSSQL.Roles.GetRoleIdByName(roleName);
            if (roleId.HasValue)
            {
                var rolePermissions = MSSQL.RolePermissions.GetByRoleId(roleId.Value);
                var roleFlags = ResolveFlags(rolePermissions, tableName);
                if (roleFlags != PermissionFlags.None)
                    return roleFlags;
            }

            var defaults = MSSQL.RolePermissions.GetByRoleId(0);
            return ResolveFlags(defaults, tableName);
        }

        private static PermissionFlags ResolveFlags(List<RolePermissionInfo> permissions, string tableName)
        {
            var explicitPermission = permissions.FirstOrDefault(p =>
                string.Equals(p.TableName, tableName, StringComparison.OrdinalIgnoreCase));
            if (explicitPermission != null)
                return explicitPermission.Flags;

            var wildcardPermission = permissions.FirstOrDefault(p =>
                string.Equals(p.TableName, TablePermission.AnyTable, StringComparison.OrdinalIgnoreCase));
            if (wildcardPermission != null)
                return wildcardPermission.Flags;

            return PermissionFlags.None;
        }
    }

    internal class MSSQLRolePermissionsAdapter : IDatabaseRolePermissions
    {
        public List<RolePermissionInfo> GetAll()
            => MSSQL.RolePermissions.GetAll();

        public List<RolePermissionInfo> GetByRoleName(string roleName)
            => MSSQL.RolePermissions.GetByRoleName(roleName);

        public List<RolePermissionInfo> GetByRoleId(int roleId)
            => MSSQL.RolePermissions.GetByRoleId(roleId);

        public void Set(string roleName, string tableName, PermissionFlags flags)
            => MSSQL.RolePermissions.Set(roleName, tableName, flags);

        public void Set(int roleId, string tableName, PermissionFlags flags)
            => MSSQL.RolePermissions.Set(roleId, tableName, flags);

        public void Delete(string roleName, string tableName)
            => MSSQL.RolePermissions.Delete(roleName, tableName);

        public void Delete(int roleId, string tableName)
            => MSSQL.RolePermissions.Delete(roleId, tableName);

        public void DeleteAllForRole(string roleName)
            => MSSQL.RolePermissions.DeleteAllForRole(roleName);

        public void DeleteAllForRole(int roleId)
            => MSSQL.RolePermissions.DeleteAllForRole(roleId);
    }

    internal class MSSQLRowEditorAdapter : IRowEditor
    {
        public AddEditResult AddRow(string tableName, Dictionary<string, object> values, bool strictFk = true, params ChildInsert[] children)
            => Scraps.Database.MSSQL.Utilities.TableRows.RowEditor.AddRow(tableName, values, strictFk, children);

        public AddEditResult UpdateRow(string tableName, string idColumn, object idValue, Dictionary<string, object> values, bool strictFk = true)
            => Scraps.Database.MSSQL.Utilities.TableRows.RowEditor.UpdateRow(tableName, idColumn, idValue, values, strictFk);
    }

    internal class MSSQLVirtualTableRegistryAdapter : IVirtualTableRegistry
    {
        public void Register(string name, string sql, PermissionFlags required = PermissionFlags.Read)
            => VirtualTableRegistry.Register(name, sql, required);

        public void Register(string name, string sql, IDictionary<string, PermissionFlags> rolePermissions)
            => VirtualTableRegistry.Register(name, sql, rolePermissions);

        public void Remove(string name)
            => VirtualTableRegistry.Remove(name);

        public void Clear()
            => VirtualTableRegistry.Clear();

        public List<string> GetNames()
            => VirtualTableRegistry.GetNames().ToList();

        public bool TryGetQuery(string name, out string sql, out IDictionary<string, PermissionFlags> permissions)
        {
            if (VirtualTableRegistry.TryGetQuery(name, out sql) &&
                VirtualTableRegistry.TryGetPermissions(name, out var perms))
            {
                permissions = perms;
                return true;
            }
            permissions = null;
            return false;
        }

        public DataTable GetData(string name, string roleName = null, PermissionFlags required = PermissionFlags.Read)
            => VirtualTableRegistry.GetData(name, roleName, required);
    }

    internal class MSSQLForeignKeyProviderAdapter : IForeignKeyProvider
    {
        public List<ForeignKeyInfo> GetForeignKeys(string tableName)
            => MSSQL.GetForeignKeys(tableName);

        public DataTable GetForeignKeyLookup(string tableName, string fkColumn)
            => MSSQL.GetForeignKeyLookup(tableName, fkColumn);

        public List<LookupItem> GetForeignKeyLookupItems(string tableName, string fkColumn)
            => MSSQL.GetForeignKeyLookupItems(tableName, fkColumn);

        public string ResolveDisplayColumn(string tableName, string idColumn = "ID")
            => MSSQL.ResolveDisplayColumn(tableName, idColumn);

        public TableEditMetadata GetTableEditMetadata(string tableName)
            => MSSQL.GetTableEditMetadata(tableName);
    }
}
