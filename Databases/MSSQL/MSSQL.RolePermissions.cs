using Scraps.Configs;
using Scraps.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Scraps.Databases
{
    public static partial class MSSQL
    {
        /// <summary>Модель прав роли по таблице.</summary>
        public class RolePermissionInfo
        {
            /// <summary>Идентификатор роли.</summary>
            public int RoleId { get; set; }
            /// <summary>Имя таблицы.</summary>
            public string TableName { get; set; }
            /// <summary>Права (флаги).</summary>
            public TablePermission Permission { get; set; }
            /// <summary>Права (флаги).</summary>
            public PermissionFlags Flags => Permission?.Flags ?? PermissionFlags.None;
        }

        /// <summary>Операции с таблицей RolePermissions.</summary>
        public static class RolePermissions
        {
            /// <summary>Название таблицы прав ролей.</summary>
            public static string RolePermissionsTableName = "RolePermissions";

            /// <summary>Получить все правила прав.</summary>
            public static List<RolePermissionInfo> GetAll()
            {
                var result = new List<RolePermissionInfo>();
                try
                {
                    using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                    {
                        string query = $"SELECT RoleID, TableName, CanRead, CanWrite, CanDelete, CanExport, CanImport FROM {QuoteIdentifier(RolePermissionsTableName)}";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var tableName = reader["TableName"].ToString();
                                var flags = PermissionFlags.None;
                                if (Convert.ToBoolean(reader["CanRead"])) flags |= PermissionFlags.Read;
                                if (Convert.ToBoolean(reader["CanWrite"])) flags |= PermissionFlags.Write;
                                if (Convert.ToBoolean(reader["CanDelete"])) flags |= PermissionFlags.Delete;
                                if (Convert.ToBoolean(reader["CanExport"])) flags |= PermissionFlags.Export;
                                if (Convert.ToBoolean(reader["CanImport"])) flags |= PermissionFlags.Import;
                                result.Add(new RolePermissionInfo
                                {
                                    RoleId = Convert.ToInt32(reader["RoleID"]),
                                    TableName = tableName,
                                    Permission = new TablePermission
                                    {
                                        TableName = tableName,
                                        Flags = flags
                                    }
                                });
                            }
                        }
                    }
                }
                catch
                {
                    // Таблица не существует (Simple или Standard режим)
                }
                return result;
            }

            /// <summary>Получить права роли по названию роли.</summary>
            public static List<RolePermissionInfo> GetByRoleName(string roleName)
            {
                var roleId = Roles.GetRoleIdByName(roleName);
                if (roleId == null) return new List<RolePermissionInfo>();
                return GetByRoleId(roleId.Value);
            }

            /// <summary>Получить права роли по ID.</summary>
            public static List<RolePermissionInfo> GetByRoleId(int roleId)
            {
                var result = new List<RolePermissionInfo>();
                try
                {
                    using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                    {
                        var cmd = new SqlCommand(
                            $"SELECT RoleID, TableName, CanRead, CanWrite, CanDelete, CanExport, CanImport FROM {QuoteIdentifier(RolePermissionsTableName)} WHERE RoleID = @RoleID",
                            conn);
                        cmd.Parameters.AddWithValue("@RoleID", roleId);
                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var tableName = reader["TableName"].ToString();
                                var flags = PermissionFlags.None;
                                if (Convert.ToBoolean(reader["CanRead"])) flags |= PermissionFlags.Read;
                                if (Convert.ToBoolean(reader["CanWrite"])) flags |= PermissionFlags.Write;
                                if (Convert.ToBoolean(reader["CanDelete"])) flags |= PermissionFlags.Delete;
                                if (Convert.ToBoolean(reader["CanExport"])) flags |= PermissionFlags.Export;
                                if (Convert.ToBoolean(reader["CanImport"])) flags |= PermissionFlags.Import;
                                result.Add(new RolePermissionInfo
                                {
                                    RoleId = roleId,
                                    TableName = tableName,
                                    Permission = new TablePermission(tableName, flags)
                                });
                            }
                        }
                    }
                }
                catch { }
                return result;
            }

            /// <summary>Установить права роли на таблицу (создать или обновить).</summary>
            public static bool Set(string roleName, string tableName, PermissionFlags flags)
            {
                var roleId = Roles.GetRoleIdByName(roleName);
                if (roleId == null) return false;
                return Set(roleId.Value, tableName, flags);
            }

            /// <summary>Установить права роли на таблицу по ID (создать или обновить).</summary>
            public static bool Set(int roleId, string tableName, PermissionFlags flags)
            {
                if (string.IsNullOrWhiteSpace(tableName)) return false;
                try
                {
                    using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                    {
                        var cmd = new SqlCommand(
                            $"IF EXISTS (SELECT 1 FROM {QuoteIdentifier(RolePermissionsTableName)} WHERE RoleID = @RoleID AND TableName = @TableName) " +
                            $"UPDATE {QuoteIdentifier(RolePermissionsTableName)} SET CanRead = @CanRead, CanWrite = @CanWrite, CanDelete = @CanDelete, CanExport = @CanExport, CanImport = @CanImport WHERE RoleID = @RoleID AND TableName = @TableName " +
                            $"ELSE " +
                            $"INSERT INTO {QuoteIdentifier(RolePermissionsTableName)} (RoleID, TableName, CanRead, CanWrite, CanDelete, CanExport, CanImport) VALUES (@RoleID, @TableName, @CanRead, @CanWrite, @CanDelete, @CanExport, @CanImport)",
                            conn);
                        cmd.Parameters.AddWithValue("@RoleID", roleId);
                        cmd.Parameters.AddWithValue("@TableName", tableName);
                        cmd.Parameters.AddWithValue("@CanRead", (flags & PermissionFlags.Read) == PermissionFlags.Read);
                        cmd.Parameters.AddWithValue("@CanWrite", (flags & PermissionFlags.Write) == PermissionFlags.Write);
                        cmd.Parameters.AddWithValue("@CanDelete", (flags & PermissionFlags.Delete) == PermissionFlags.Delete);
                        cmd.Parameters.AddWithValue("@CanExport", (flags & PermissionFlags.Export) == PermissionFlags.Export);
                        cmd.Parameters.AddWithValue("@CanImport", (flags & PermissionFlags.Import) == PermissionFlags.Import);
                        conn.Open();
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
                catch
                {
                    return false;
                }
            }

            /// <summary>Удалить права роли на таблицу.</summary>
            public static bool Delete(string roleName, string tableName)
            {
                var roleId = Roles.GetRoleIdByName(roleName);
                if (roleId == null) return false;
                return Delete(roleId.Value, tableName);
            }

            /// <summary>Удалить права роли на таблицу по ID.</summary>
            public static bool Delete(int roleId, string tableName)
            {
                if (string.IsNullOrWhiteSpace(tableName)) return false;
                try
                {
                    using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                    {
                        var cmd = new SqlCommand(
                            $"DELETE FROM {QuoteIdentifier(RolePermissionsTableName)} WHERE RoleID = @RoleID AND TableName = @TableName",
                            conn);
                        cmd.Parameters.AddWithValue("@RoleID", roleId);
                        cmd.Parameters.AddWithValue("@TableName", tableName);
                        conn.Open();
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
                catch
                {
                    return false;
                }
            }

            /// <summary>Удалить все права роли.</summary>
            public static bool DeleteAllForRole(string roleName)
            {
                var roleId = Roles.GetRoleIdByName(roleName);
                if (roleId == null) return false;
                return DeleteAllForRole(roleId.Value);
            }

            /// <summary>Удалить все права роли по ID.</summary>
            public static bool DeleteAllForRole(int roleId)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                    {
                        var cmd = new SqlCommand(
                            $"DELETE FROM {QuoteIdentifier(RolePermissionsTableName)} WHERE RoleID = @RoleID",
                            conn);
                        cmd.Parameters.AddWithValue("@RoleID", roleId);
                        conn.Open();
                        return cmd.ExecuteNonQuery() >= 0;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
