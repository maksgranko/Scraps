using Scraps.Configs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Scraps.Databases
{
    public static partial class MSSQL
    {
        /// <summary>Операции с таблицей Users.</summary>
        public static class Users
        {
            /// <summary>Название таблицы пользователей.</summary>
            public static string UsersTableName => ScrapsConfig.UsersTableName;
            /// <summary>Сопоставление логических ключей колонок с реальными именами.</summary>
            public static Dictionary<string, string> UsersTableColumnsNames => ScrapsConfig.UsersTableColumnsNames;

            /// <summary>Получить пользователя по логину.</summary>
            public static DataRow GetByLogin(string login)
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    string query = $"SELECT * FROM {UsersTableName} WHERE {UsersTableColumnsNames["Login"]} = @Login";
                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    da.SelectCommand.Parameters.AddWithValue("@Login", login);
                    try
                    {
                        da.Fill(dt);
                    }
                    catch
                    {
                        return null;
                    }
                }
                return dt.Rows.Count > 0 ? dt.Rows[0] : null;
            }

            /// <summary>Получить название роли пользователя.</summary>
            public static string GetUserStatus(string login)
            {
                var user = GetByLogin(login);
                var roleObj = user?[UsersTableColumnsNames["Role"]];
                if (roleObj == null || roleObj == DBNull.Value) return null;

                if (!ScrapsConfig.UseRoleIdMapping)
                    return roleObj.ToString();

                int roleId = Convert.ToInt32(roleObj);
                return Roles.GetRoleNameById(roleId);
            }

            /// <summary>Создать пользователя.</summary>
            public static bool Create(string login, string password, string role)
            {
                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    string query = $"INSERT INTO {UsersTableName}" +
                                   $"({UsersTableColumnsNames["Login"]}, " +
                                   $"{UsersTableColumnsNames["Password"]}, " +
                                   $"{UsersTableColumnsNames["Role"]}) " +
                                   $"VALUES (@Login, @Password, @Role)";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Login", login);
                    cmd.Parameters.AddWithValue("@Password", password);

                    if (ScrapsConfig.UseRoleIdMapping)
                    {
                        var roleId = Roles.GetRoleIdByName(role);
                        if (roleId == null) throw new Exception("Role not found: " + role);
                        cmd.Parameters.AddWithValue("@Role", roleId.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@Role", role);
                    }

                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
