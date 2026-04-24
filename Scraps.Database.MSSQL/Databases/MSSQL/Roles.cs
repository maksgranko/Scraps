using Scraps.Configs;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

using RoleInfo = Scraps.Database.RoleInfo;

namespace Scraps.Database.MSSQL
{
    public static partial class MSSQL
    {
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
            /// <exception cref="InvalidOperationException">Роль не найдена</exception>
            public static string GetRoleNameById(int roleId)
            {
                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    string query = $"SELECT {QuoteIdentifier(RoleNameColumnName)} FROM {QuoteIdentifier(RolesTableName)} WHERE {QuoteIdentifier(RoleIdColumnName)} = @RoleID";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@RoleID", roleId);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    
                    if (result == null || result == DBNull.Value)
                        throw new InvalidOperationException($"Роль с ID {roleId} не найдена.");
                    
                    return result.ToString();
                }
            }

            /// <summary>Получить ID роли по названию. Возвращает null если роль не найдена.</summary>
            public static int? GetRoleIdByName(string roleName)
            {
                if (string.IsNullOrWhiteSpace(roleName))
                    throw new ArgumentException("Название роли не может быть пустым.", nameof(roleName));

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

            /// <summary>Создать новую роль.</summary>
            /// <returns>ID созданной роли</returns>
            /// <exception cref="ArgumentException">Пустое название роли</exception>
            /// <exception cref="InvalidOperationException">Роль уже существует</exception>
            public static int Create(string roleName)
            {
                if (string.IsNullOrWhiteSpace(roleName))
                    throw new ArgumentException("Название роли не может быть пустым.", nameof(roleName));

                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    // Проверяем существование
                    var checkCmd = new SqlCommand(
                        $"SELECT {QuoteIdentifier(RoleIdColumnName)} FROM {QuoteIdentifier(RolesTableName)} WHERE {QuoteIdentifier(RoleNameColumnName)} = @RoleName",
                        conn);
                    checkCmd.Parameters.AddWithValue("@RoleName", roleName);
                    conn.Open();
                    var existing = checkCmd.ExecuteScalar();
                    if (existing != null && existing != DBNull.Value)
                        throw new InvalidOperationException($"Роль '{roleName}' уже существует.");

                    // Создаём
                    var insertCmd = new SqlCommand(
                        $"INSERT INTO {QuoteIdentifier(RolesTableName)} ({QuoteIdentifier(RoleNameColumnName)}) OUTPUT INSERTED.{QuoteIdentifier(RoleIdColumnName)} VALUES (@RoleName)",
                        conn);
                    insertCmd.Parameters.AddWithValue("@RoleName", roleName);
                    var result = insertCmd.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                        throw new Exception($"Не удалось создать роль '{roleName}'.");

                    return Convert.ToInt32(result);
                }
            }

            /// <summary>Удалить роль по названию.</summary>
            /// <exception cref="ArgumentException">Пустое название роли</exception>
            /// <exception cref="InvalidOperationException">Роль не найдена</exception>
            public static void Delete(string roleName)
            {
                if (string.IsNullOrWhiteSpace(roleName))
                    throw new ArgumentException("Название роли не может быть пустым.", nameof(roleName));

                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    var cmd = new SqlCommand(
                        $"DELETE FROM {QuoteIdentifier(RolesTableName)} WHERE {QuoteIdentifier(RoleNameColumnName)} = @RoleName",
                        conn);
                    cmd.Parameters.AddWithValue("@RoleName", roleName);
                    conn.Open();
                    var affected = cmd.ExecuteNonQuery();

                    if (affected == 0)
                        throw new InvalidOperationException($"Роль '{roleName}' не найдена.");
                }
            }

            /// <summary>Переименовать роль.</summary>
            /// <exception cref="ArgumentException">Пустое название роли</exception>
            /// <exception cref="InvalidOperationException">Роль не найдена или новое имя занято</exception>
            public static void Rename(string oldName, string newName)
            {
                if (string.IsNullOrWhiteSpace(oldName))
                    throw new ArgumentException("Старое название роли не может быть пустым.", nameof(oldName));
                if (string.IsNullOrWhiteSpace(newName))
                    throw new ArgumentException("Новое название роли не может быть пустым.", nameof(newName));

                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    conn.Open();

                    // Проверяем существование старой роли
                    var checkOld = new SqlCommand(
                        $"SELECT 1 FROM {QuoteIdentifier(RolesTableName)} WHERE {QuoteIdentifier(RoleNameColumnName)} = @OldName",
                        conn);
                    checkOld.Parameters.AddWithValue("@OldName", oldName);
                    if (checkOld.ExecuteScalar() == null)
                        throw new InvalidOperationException($"Роль '{oldName}' не найдена.");

                    // Проверяем что новое имя не занято
                    var checkNew = new SqlCommand(
                        $"SELECT 1 FROM {QuoteIdentifier(RolesTableName)} WHERE {QuoteIdentifier(RoleNameColumnName)} = @NewName",
                        conn);
                    checkNew.Parameters.AddWithValue("@NewName", newName);
                    if (checkNew.ExecuteScalar() != null)
                        throw new InvalidOperationException($"Роль с именем '{newName}' уже существует.");

                    // Переименовываем
                    var cmd = new SqlCommand(
                        $"UPDATE {QuoteIdentifier(RolesTableName)} SET {QuoteIdentifier(RoleNameColumnName)} = @NewName WHERE {QuoteIdentifier(RoleNameColumnName)} = @OldName",
                        conn);
                    cmd.Parameters.AddWithValue("@NewName", newName);
                    cmd.Parameters.AddWithValue("@OldName", oldName);
                    cmd.ExecuteNonQuery();
                }
            }

            /// <summary>Получить список всех ролей.</summary>
            /// <exception cref="InvalidOperationException">Таблица Roles не существует (Simple режим)</exception>
            public static List<RoleInfo> GetAll()
            {
                var result = new List<RoleInfo>();
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
                return result;
            }
        }
    }
}




