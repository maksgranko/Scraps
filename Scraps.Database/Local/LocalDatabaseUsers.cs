using Scraps.Configs;
using System;
using System.Data;
using System.Linq;

namespace Scraps.Database.Local
{
    /// <summary>
    /// Управление пользователями в JSON-файле.
    /// </summary>
    public class LocalDatabaseUsers : IDatabaseUsers
    {
        private readonly LocalDatabaseData _data = new LocalDatabaseData();
        private readonly string _tableName;

        public LocalDatabaseUsers(string tableName = null)
        {
            _tableName = tableName ?? ScrapsConfig.UsersTableName;
        }

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

        public string GetUserStatus(string login)
        {
            var row = GetByLogin(login);
            var roleCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("Role", out var c) ? c : "Role";
            return row[roleCol]?.ToString() ?? "";
        }

        public void Create(string login, string password, string role)
        {
            var dt = _data.GetTableData(_tableName);
            var loginCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("Login", out var lc) ? lc : "Login";
            var passCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("Password", out var pc) ? pc : "Password";
            var roleCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("Role", out var rc) ? rc : "Role";
            var idCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("UserID", out var ic) ? ic : "UserID";

            // Генерируем ID
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

        public void ChangePassword(string login, string newPassword)
        {
            var dt = _data.GetTableData(_tableName);
            var loginCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("Login", out var lc) ? lc : "Login";
            var passCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("Password", out var pc) ? pc : "Password";

            foreach (DataRow row in dt.Rows)
            {
                if (string.Equals(row[loginCol]?.ToString(), login, StringComparison.OrdinalIgnoreCase))
                {
                    row[passCol] = newPassword;
                }
            }

            _data.ApplyTableChanges(_tableName, dt);
        }

        public void ChangeRole(string login, string newRole)
        {
            var dt = _data.GetTableData(_tableName);
            var loginCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("Login", out var lc) ? lc : "Login";
            var roleCol = ScrapsConfig.UsersTableColumnsNames.TryGetValue("Role", out var rc) ? rc : "Role";

            foreach (DataRow row in dt.Rows)
            {
                if (string.Equals(row[loginCol]?.ToString(), login, StringComparison.OrdinalIgnoreCase))
                {
                    row[roleCol] = newRole;
                }
            }

            _data.ApplyTableChanges(_tableName, dt);
        }
    }
}
