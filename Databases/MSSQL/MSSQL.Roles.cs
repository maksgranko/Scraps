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
                    string query = $"SELECT {QuoteIdentifier(RoleNameColumnName)} FROM {QuoteIdentifier(RolesTableName)} WHERE {QuoteIdentifier(RoleIdColumnName)} = @RoleID";
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
                try
                {
                    using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                    {
                        string query = $"SELECT {QuoteIdentifier(RoleIdColumnName)} FROM {QuoteIdentifier(RolesTableName)} WHERE {QuoteIdentifier(RoleNameColumnName)} = @RoleName";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@RoleName", roleName);
                        conn.Open();
                        var result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value) return null;
                        return Convert.ToInt32(result);
                    }
                }
                catch
                {
                    return null;
                }
            }

            /// <summary>Создать новую роль. Возвращает ID созданной роли или null.</summary>
            public static int? Create(string roleName)
            {
                if (string.IsNullOrWhiteSpace(roleName)) return null;
                try
                {
                    using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                    {
                        var cmd = new SqlCommand(
                            $"IF NOT EXISTS (SELECT 1 FROM {QuoteIdentifier(RolesTableName)} WHERE {QuoteIdentifier(RoleNameColumnName)} = @RoleName) " +
                            $"BEGIN INSERT INTO {QuoteIdentifier(RolesTableName)} ({QuoteIdentifier(RoleNameColumnName)}) OUTPUT INSERTED.{QuoteIdentifier(RoleIdColumnName)} VALUES (@RoleName) END",
                            conn);
                        cmd.Parameters.AddWithValue("@RoleName", roleName);
                        conn.Open();
                        var result = cmd.ExecuteScalar();
                        return result != null && result != DBNull.Value ? Convert.ToInt32(result) : (int?)null;
                    }
                }
                catch
                {
                    return null;
                }
            }

            /// <summary>Удалить роль по названию.</summary>
            public static bool Delete(string roleName)
            {
                if (string.IsNullOrWhiteSpace(roleName)) return false;
                try
                {
                    using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                    {
                        var cmd = new SqlCommand(
                            $"DELETE FROM {QuoteIdentifier(RolesTableName)} WHERE {QuoteIdentifier(RoleNameColumnName)} = @RoleName",
                            conn);
                        cmd.Parameters.AddWithValue("@RoleName", roleName);
                        conn.Open();
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
                catch
                {
                    return false;
                }
            }

            /// <summary>Переименовать роль.</summary>
            public static bool Rename(string oldName, string newName)
            {
                if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return false;
                try
                {
                    using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                    {
                        var cmd = new SqlCommand(
                            $"UPDATE {QuoteIdentifier(RolesTableName)} SET {QuoteIdentifier(RoleNameColumnName)} = @NewName WHERE {QuoteIdentifier(RoleNameColumnName)} = @OldName",
                            conn);
                        cmd.Parameters.AddWithValue("@NewName", newName);
                        cmd.Parameters.AddWithValue("@OldName", oldName);
                        conn.Open();
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
                catch
                {
                    return false;
                }
            }

            /// <summary>Получить список всех ролей.</summary>
            public static List<RoleInfo> GetAll()
            {
                var result = new List<RoleInfo>();
                try
                {
                    using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                    {
                        string query = $"SELECT {QuoteIdentifier(RoleIdColumnName)}, {QuoteIdentifier(RoleNameColumnName)} FROM {QuoteIdentifier(RolesTableName)}";
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
                }
                catch
                {
                    // Таблица не существует (Simple режим)
                }
                return result;
            }
        }
    }
}
