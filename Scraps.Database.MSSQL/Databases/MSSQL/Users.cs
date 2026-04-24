using Scraps.Configs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Scraps.Database.MSSQL
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
            /// <exception cref="ArgumentException">Пустой логин</exception>
            /// <exception cref="InvalidOperationException">Пользователь не найден</exception>
            public static DataRow GetByLogin(string login)
            {
                if (string.IsNullOrWhiteSpace(login))
                    throw new ArgumentException("Логин не может быть пустым.", nameof(login));

                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    string query = $"SELECT * FROM {QuoteIdentifier(UsersTableName)} WHERE {QuoteIdentifier(UsersTableColumnsNames["Login"])} = @Login";
                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    da.SelectCommand.Parameters.AddWithValue("@Login", login);
                    da.Fill(dt);
                }

                if (dt.Rows.Count == 0)
                    throw new InvalidOperationException($"Пользователь '{login}' не найден.");

                return dt.Rows[0];
            }

            /// <summary>Получить название роли пользователя.</summary>
            /// <exception cref="ArgumentException">Пустой логин</exception>
            /// <exception cref="InvalidOperationException">Пользователь не найден</exception>
            public static string GetUserRole(string login)
            {
                var user = GetByLogin(login);
                var roleObj = user[UsersTableColumnsNames["Role"]];
                if (roleObj == null || roleObj == DBNull.Value)
                    throw new InvalidOperationException($"У пользователя '{login}' не указана роль.");

                if (!ScrapsConfig.UseRoleIdMapping)
                    return roleObj.ToString();

                int roleId = Convert.ToInt32(roleObj);
                return Roles.GetRoleNameById(roleId);
            }

            /// <summary>Создать пользователя.</summary>
            /// <exception cref="ArgumentException">Пустой логин, пароль или роль</exception>
            /// <exception cref="InvalidOperationException">Роль не найдена или пользователь уже существует</exception>
            public static void Create(string login, string password, string role)
            {
                if (string.IsNullOrWhiteSpace(login))
                    throw new ArgumentException("Логин не может быть пустым.", nameof(login));
                if (string.IsNullOrWhiteSpace(password))
                    throw new ArgumentException("Пароль не может быть пустым.", nameof(password));
                if (string.IsNullOrWhiteSpace(role))
                    throw new ArgumentException("Роль не может быть пустой.", nameof(role));

                // Проверяем существование пользователя без зависимости от текста исключения.
                var exists = false;
                try
                {
                    GetByLogin(login);
                    exists = true;
                }
                catch (InvalidOperationException)
                {
                    exists = false;
                }
                if (exists)
                    throw new InvalidOperationException($"Пользователь '{login}' уже существует.");

                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    string query = $"INSERT INTO {QuoteIdentifier(UsersTableName)}" +
                                   $"({QuoteIdentifier(UsersTableColumnsNames["Login"])}, " +
                                   $"{QuoteIdentifier(UsersTableColumnsNames["Password"])}, " +
                                   $"{QuoteIdentifier(UsersTableColumnsNames["Role"])}) " +
                                   $"VALUES (@Login, @Password, @Role)";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Login", login);
                    cmd.Parameters.AddWithValue("@Password", password);

                    if (ScrapsConfig.UseRoleIdMapping)
                    {
                        var roleId = Roles.GetRoleIdByName(role);
                        if (roleId == null)
                            throw new InvalidOperationException($"Роль '{role}' не найдена.");
                        cmd.Parameters.AddWithValue("@Role", roleId.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@Role", role);
                    }

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            /// <summary>Удалить пользователя.</summary>
            /// <exception cref="ArgumentException">Пустой логин</exception>
            /// <exception cref="InvalidOperationException">Пользователь не найден</exception>
            public static void Delete(string login)
            {
                if (string.IsNullOrWhiteSpace(login))
                    throw new ArgumentException("Логин не может быть пустым.", nameof(login));

                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    var cmd = new SqlCommand(
                        $"DELETE FROM {QuoteIdentifier(UsersTableName)} WHERE {QuoteIdentifier(UsersTableColumnsNames["Login"])} = @Login",
                        conn);
                    cmd.Parameters.AddWithValue("@Login", login);
                    conn.Open();
                    var affected = cmd.ExecuteNonQuery();

                    if (affected == 0)
                        throw new InvalidOperationException($"Пользователь '{login}' не найден.");
                }
            }

            /// <summary>Изменить пароль пользователя.</summary>
            /// <exception cref="ArgumentException">Пустой логин или пароль</exception>
            /// <exception cref="InvalidOperationException">Пользователь не найден</exception>
            public static void ChangePassword(string login, string newPassword)
            {
                if (string.IsNullOrWhiteSpace(login))
                    throw new ArgumentException("Логин не может быть пустым.", nameof(login));
                if (string.IsNullOrWhiteSpace(newPassword))
                    throw new ArgumentException("Пароль не может быть пустым.", nameof(newPassword));

                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    var cmd = new SqlCommand(
                        $"UPDATE {QuoteIdentifier(UsersTableName)} SET {QuoteIdentifier(UsersTableColumnsNames["Password"])} = @Password WHERE {QuoteIdentifier(UsersTableColumnsNames["Login"])} = @Login",
                        conn);
                    cmd.Parameters.AddWithValue("@Login", login);
                    cmd.Parameters.AddWithValue("@Password", newPassword);
                    conn.Open();
                    var affected = cmd.ExecuteNonQuery();

                    if (affected == 0)
                        throw new InvalidOperationException($"Пользователь '{login}' не найден.");
                }
            }

            /// <summary>Изменить роль пользователя.</summary>
            /// <exception cref="ArgumentException">Пустой логин или роль</exception>
            /// <exception cref="InvalidOperationException">Пользователь или роль не найдены</exception>
            public static void ChangeRole(string login, string newRole)
            {
                if (string.IsNullOrWhiteSpace(login))
                    throw new ArgumentException("Логин не может быть пустым.", nameof(login));
                if (string.IsNullOrWhiteSpace(newRole))
                    throw new ArgumentException("Роль не может быть пустой.", nameof(newRole));

                using (SqlConnection conn = new SqlConnection(ScrapsConfig.ConnectionString))
                {
                    SqlCommand cmd;
                    if (ScrapsConfig.UseRoleIdMapping)
                    {
                        var roleId = Roles.GetRoleIdByName(newRole);
                        if (roleId == null)
                            throw new InvalidOperationException($"Роль '{newRole}' не найдена.");
                        cmd = new SqlCommand(
                            $"UPDATE {QuoteIdentifier(UsersTableName)} SET {QuoteIdentifier(UsersTableColumnsNames["Role"])} = @Role WHERE {QuoteIdentifier(UsersTableColumnsNames["Login"])} = @Login",
                            conn);
                        cmd.Parameters.AddWithValue("@Role", roleId.Value);
                    }
                    else
                    {
                        cmd = new SqlCommand(
                            $"UPDATE {QuoteIdentifier(UsersTableName)} SET {QuoteIdentifier(UsersTableColumnsNames["Role"])} = @Role WHERE {QuoteIdentifier(UsersTableColumnsNames["Login"])} = @Login",
                            conn);
                        cmd.Parameters.AddWithValue("@Role", newRole);
                    }
                    cmd.Parameters.AddWithValue("@Login", login);
                    conn.Open();
                    var affected = cmd.ExecuteNonQuery();

                    if (affected == 0)
                        throw new InvalidOperationException($"Пользователь '{login}' не найден.");
                }
            }
        }
    }
}




