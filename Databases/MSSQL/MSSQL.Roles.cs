using Scraps.Configs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Scraps.Databases
{
    public static partial class MSSQL
    {
        /// <summary>Модель роли (ID + название).</summary>
        public class RoleInfo
        {
            /// <summary>Идентификатор роли.</summary>
            public int Id { get; set; }
            /// <summary>Название роли.</summary>
            public string Name { get; set; }
        }

        /// <summary>Операции с таблицей Roles.</summary>
        public static class Roles
        {
            /// <summary>Название таблицы ролей.</summary>
            public static string RolesTableName = "Roles";
            /// <summary>Название колонки RoleID.</summary>
            public static string RoleIdColumnName = "RoleID";
            /// <summary>Название колонки RoleName.</summary>
            public static string RoleNameColumnName = "RoleName";

            /// <summary>Получить имя роли по ID.</summary>
            public static string GetRoleNameById(int roleId)
            {
                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    string query = $"SELECT {RoleNameColumnName} FROM {RolesTableName} WHERE {RoleIdColumnName} = @RoleID";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@RoleID", roleId);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return result?.ToString();
                }
            }

            /// <summary>Получить ID роли по названию.</summary>
            public static int? GetRoleIdByName(string roleName)
            {
                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    string query = $"SELECT {RoleIdColumnName} FROM {RolesTableName} WHERE {RoleNameColumnName} = @RoleName";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@RoleName", roleName);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value) return null;
                    return Convert.ToInt32(result);
                }
            }

            /// <summary>Получить список всех ролей.</summary>
            public static List<RoleInfo> GetAll()
            {
                var result = new List<RoleInfo>();
                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    string query = $"SELECT {RoleIdColumnName}, {RoleNameColumnName} FROM {RolesTableName}";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new RoleInfo
                            {
                                Id = Convert.ToInt32(reader[RoleIdColumnName]),
                                Name = reader[RoleNameColumnName].ToString()
                            });
                        }
                    }
                }
                return result;
            }
        }
    }
}
