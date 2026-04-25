using Scraps.Configs;
using System;
using System.Data;
using System.Linq;

namespace Scraps.Database.LocalFiles
{
    /// <summary>
    /// Управление пользователями в JSON-файле.
    /// </summary>
    public class LocalDatabaseUsers : IDatabaseUsers
    {
        private readonly LocalDatabaseData _data = new LocalDatabaseData();
        private readonly string _tableName;

        /// <summary>Создать провайдер пользователей для указанной таблицы или ScrapsConfig.UsersTableName.</summary>
        public LocalDatabaseUsers(string tableName = null)
        {
            _tableName = tableName ?? ScrapsConfig.UsersTableName;
        }

        /// <summary>Получить пользователя по логину.</summary>
        public DataRow GetByLogin(string login)
        {
            var dt = _data.GetTableData(_tableName);
            var loginCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("Login", out var c) ? c : "Login";
            if (!dt.Columns.Contains(loginCol))
                throw new InvalidOperationException($"Колонка '{loginCol}' не найдена в таблице '{_tableName}'.");

            foreach (DataRow row in dt.Rows)
            {
                if (string.Equals(row[loginCol]?.ToString(), login, StringComparison.OrdinalIgnoreCase))
                    return row;
            }
            throw new InvalidOperationException($"Пользователь '{login}' не найден.");
        }

        /// <summary>Получить статус пользователя (значение колонки роли).</summary>
        public string GetUserRole(string login)
        {
            var row = GetByLogin(login);
            var roleCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("Role", out var c) ? c : "Role";
            return row[roleCol]?.ToString() ?? "";
        }

        /// <summary>Создать нового пользователя.</summary>
        public void Create(string login, string password, string role)
        {
            var dt = _data.GetTableData(_tableName);
            var loginCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("Login", out var lc) ? lc : "Login";
            var passCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("Password", out var pc) ? pc : "Password";
            var roleCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("Role", out var rc) ? rc : "Role";
            var idCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("UserID", out var ic) ? ic : "UserID";

            if (dt.Columns.Contains(loginCol))
            {
                foreach (DataRow row in dt.Rows)
                {
                    if (string.Equals(row[loginCol]?.ToString(), login, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"Пользователь '{login}' уже существует.");
                }
            }

            int nextId = 1;
            if (dt.Columns.Contains(idCol))
            {
                foreach (DataRow row in dt.Rows)
                {
                    if (int.TryParse(row[idCol]?.ToString(), out var existingId) && existingId >= nextId)
                        nextId = existingId + 1;
                }
            }

            var newRow = dt.NewRow();
            if (dt.Columns.Contains(idCol))
                newRow[idCol] = nextId;
            newRow[loginCol] = login;
            newRow[passCol] = password;
            newRow[roleCol] = role;
            dt.Rows.Add(newRow);

            _data.ApplyTableChanges(_tableName, dt);
        }

        /// <summary>Удалить пользователя по логину.</summary>
        public void Delete(string login)
        {
            var dt = _data.GetTableData(_tableName);
            var loginCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("Login", out var c) ? c : "Login";

            if (!dt.Columns.Contains(loginCol))
                throw new InvalidOperationException($"Колонка '{loginCol}' не найдена в таблице '{_tableName}'.");

            bool found = false;
            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                if (string.Equals(dt.Rows[i][loginCol]?.ToString(), login, StringComparison.OrdinalIgnoreCase))
                {
                    dt.Rows[i].Delete();
                    found = true;
                }
            }

            if (!found)
                throw new InvalidOperationException($"Пользователь '{login}' не найден.");

            dt.AcceptChanges();
            _data.ApplyTableChanges(_tableName, dt);
        }

        /// <summary>Изменить пароль пользователя.</summary>
        public void ChangePassword(string login, string newPassword)
        {
            var dt = _data.GetTableData(_tableName);
            var loginCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("Login", out var lc) ? lc : "Login";
            var passCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("Password", out var pc) ? pc : "Password";
            var found = false;

            foreach (DataRow row in dt.Rows)
            {
                if (string.Equals(row[loginCol]?.ToString(), login, StringComparison.OrdinalIgnoreCase))
                {
                    row[passCol] = newPassword;
                    found = true;
                    break;
                }
            }

            if (!found)
                throw new InvalidOperationException($"Пользователь '{login}' не найден.");

            _data.ApplyTableChanges(_tableName, dt);
        }

        /// <summary>Изменить роль пользователя.</summary>
        public void ChangeRole(string login, string newRole)
        {
            var dt = _data.GetTableData(_tableName);
            var loginCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("Login", out var lc) ? lc : "Login";
            var roleCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("Role", out var rc) ? rc : "Role";
            var found = false;

            foreach (DataRow row in dt.Rows)
            {
                if (string.Equals(row[loginCol]?.ToString(), login, StringComparison.OrdinalIgnoreCase))
                {
                    row[roleCol] = newRole;
                    found = true;
                    break;
                }
            }

            if (!found)
                throw new InvalidOperationException($"Пользователь '{login}' не найден.");

            _data.ApplyTableChanges(_tableName, dt);
        }
    }
}
