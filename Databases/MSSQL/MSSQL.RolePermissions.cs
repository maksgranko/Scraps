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
        }
    }
}
