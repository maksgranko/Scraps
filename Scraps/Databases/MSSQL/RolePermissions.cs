using Scraps.Configs;
using Scraps.Security;
using System;
using System.Collections.Generic;
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
            /// <exception cref="InvalidOperationException">Таблица RolePermissions не существует</exception>
            public static List<RolePermissionInfo> GetAll()
            {
                var result = new List<RolePermissionInfo>();
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
                return result;
            }

            /// <summary>Получить права роли по названию роли.</summary>
            /// <exception cref="InvalidOperationException">Роль не найдена</exception>
            public static List<RolePermissionInfo> GetByRoleName(string roleName)
            {
                var roleId = Roles.GetRoleIdByName(roleName);
                if (roleId == null)
                    throw new InvalidOperationException($"Роль '{roleName}' не найдена.");
                return GetByRoleId(roleId.Value);
            }

            /// <summary>Получить права роли по ID.</summary>
            public static List<RolePermissionInfo> GetByRoleId(int roleId)
            {
                var result = new List<RolePermissionInfo>();
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
                return result;
            }

            /// <summary>Установить права роли на таблицу (создать или обновить).</summary>
            /// <exception cref="ArgumentException">Пустое название таблицы</exception>
            /// <exception cref="InvalidOperationException">Роль не найдена</exception>
            public static void Set(string roleName, string tableName, PermissionFlags flags)
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));

                var roleId = Roles.GetRoleIdByName(roleName);
                if (roleId == null)
                    throw new InvalidOperationException($"Роль '{roleName}' не найдена.");

                Set(roleId.Value, tableName, flags);
            }

            /// <summary>Установить права роли на таблицу по ID (создать или обновить).</summary>
            /// <exception cref="ArgumentException">Пустое название таблицы</exception>
            public static void Set(int roleId, string tableName, PermissionFlags flags)
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));

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
                    cmd.ExecuteNonQuery();
                }
            }

            /// <summary>Удалить права роли на таблицу.</summary>
            /// <exception cref="ArgumentException">Пустое название таблицы</exception>
            /// <exception cref="InvalidOperationException">Роль или права не найдены</exception>
            public static void Delete(string roleName, string tableName)
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));

                var roleId = Roles.GetRoleIdByName(roleName);
                if (roleId == null)
                    throw new InvalidOperationException($"Роль '{roleName}' не найдена.");

                Delete(roleId.Value, tableName);
            }

            /// <summary>Удалить права роли на таблицу по ID.</summary>
            /// <exception cref="ArgumentException">Пустое название таблицы</exception>
            /// <exception cref="InvalidOperationException">Права не найдены</exception>
            public static void Delete(int roleId, string tableName)
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    throw new ArgumentException("Название таблицы не может быть пустым.", nameof(tableName));

                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    var cmd = new SqlCommand(
                        $"DELETE FROM {QuoteIdentifier(RolePermissionsTableName)} WHERE RoleID = @RoleID AND TableName = @TableName",
                        conn);
                    cmd.Parameters.AddWithValue("@RoleID", roleId);
                    cmd.Parameters.AddWithValue("@TableName", tableName);
                    conn.Open();
                    var affected = cmd.ExecuteNonQuery();

                    if (affected == 0)
                        throw new InvalidOperationException($"Права роли {roleId} на таблицу '{tableName}' не найдены.");
                }
            }

            /// <summary>Удалить все права роли.</summary>
            /// <exception cref="InvalidOperationException">Роль не найдена</exception>
            public static void DeleteAllForRole(string roleName)
            {
                var roleId = Roles.GetRoleIdByName(roleName);
                if (roleId == null)
                    throw new InvalidOperationException($"Роль '{roleName}' не найдена.");
                DeleteAllForRole(roleId.Value);
            }

            /// <summary>Удалить все права роли по ID.</summary>
            public static void DeleteAllForRole(int roleId)
            {
                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    var cmd = new SqlCommand(
                        $"DELETE FROM {QuoteIdentifier(RolePermissionsTableName)} WHERE RoleID = @RoleID",
                        conn);
                    cmd.Parameters.AddWithValue("@RoleID", roleId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}




