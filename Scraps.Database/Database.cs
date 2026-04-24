using System;
using System.Collections.Generic;
using System.Data;

namespace Scraps.Database
{
    /// <summary>
    /// Статический фасад для работы с базой данных.
    /// Автоматически выбирает провайдер из ScrapsConfig.DatabaseProvider.
    /// </summary>
    public static class Database
    {
        private static IDatabase Current => DatabaseProviderFactory.Current;

        #region Connection

        /// <summary>Проверить подключение.</summary>
        public static bool TestConnection() => Current.TestConnection();

        /// <summary>Выполнить SQL без возврата данных.</summary>
        public static void ExecuteNonQuery(string sql, params object[] parameters)
            => Current.Connection.ExecuteNonQuery(sql, parameters);

        /// <summary>Выполнить SQL и вернуть скаляр.</summary>
        public static object ExecuteScalar(string sql, params object[] parameters)
            => Current.Connection.ExecuteScalar(sql, parameters);

        /// <summary>Выполнить SQL и вернуть DataTable.</summary>
        public static DataTable GetDataTable(string sql, params object[] parameters)
            => Current.Connection.GetDataTable(sql, parameters);

        #endregion

        #region Schema

        /// <summary>Получить список таблиц.</summary>
        public static List<string> GetTables(bool includeViews = false, bool includeSystem = false)
        {
            if (Current.Schema == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseSchema.");
            return Current.Schema.GetTables(includeViews, includeSystem);
        }

        /// <summary>Получить колонки таблицы.</summary>
        public static List<string> GetTableColumns(string tableName)
        {
            if (Current.Schema == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseSchema.");
            return Current.Schema.GetTableColumns(tableName);
        }

        /// <summary>Получить схему таблицы.</summary>
        public static DataTable GetTableSchema(string tableName)
        {
            if (Current.Schema == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseSchema.");
            return Current.Schema.GetTableSchema(tableName);
        }

        #endregion

        #region Data

        /// <summary>Получить данные таблицы.</summary>
        public static DataTable GetTableData(string tableName, params string[] columns)
        {
            if (Current.Data == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseData.");
            return Current.Data.GetTableData(tableName, columns);
        }

        /// <summary>Получить данные с разворачиванием FK.</summary>
        public static DataTable GetTableDataExpanded(string tableName, IEnumerable<ForeignKeyJoin> foreignKeys, params string[] baseColumns)
        {
            if (Current.Data == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseData.");
            return Current.Data.GetTableDataExpanded(tableName, foreignKeys, baseColumns);
        }

        /// <summary>Найти записи по колонке.</summary>
        public static DataTable FindByColumn(string tableName, string columnName, object value, bool exactMatch = true)
        {
            if (Current.Data == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseData.");
            return Current.Data.FindByColumn(tableName, columnName, value, exactMatch);
        }

        /// <summary>Применить изменения.</summary>
        public static void ApplyTableChanges(string tableName, DataTable changes)
        {
            if (Current.Data == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseData.");
            Current.Data.ApplyTableChanges(tableName, changes);
        }

        /// <summary>Массовая вставка.</summary>
        public static void BulkInsert(string tableName, DataTable data)
        {
            if (Current.Data == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseData.");
            Current.Data.BulkInsert(tableName, data);
        }

        #endregion

        #region Users

        /// <summary>Получить пользователя по логину.</summary>
        public static DataRow GetUserByLogin(string login)
        {
            if (Current.Users == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseUsers.");
            return Current.Users.GetByLogin(login);
        }

        /// <summary>Создать пользователя.</summary>
        public static void CreateUser(string login, string password, string role)
        {
            if (Current.Users == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseUsers.");
            Current.Users.Create(login, password, role);
        }

        /// <summary>Удалить пользователя.</summary>
        public static void DeleteUser(string login)
        {
            if (Current.Users == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseUsers.");
            Current.Users.Delete(login);
        }

        /// <summary>Изменить пароль.</summary>
        public static void ChangeUserPassword(string login, string newPassword)
        {
            if (Current.Users == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseUsers.");
            Current.Users.ChangePassword(login, newPassword);
        }

        #endregion

        #region Roles

        /// <summary>Получить ID роли по имени.</summary>
        public static int? GetRoleIdByName(string roleName)
        {
            if (Current.Roles == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseRoles.");
            return Current.Roles.GetRoleIdByName(roleName);
        }

        /// <summary>Создать роль.</summary>
        public static int CreateRole(string roleName)
        {
            if (Current.Roles == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseRoles.");
            return Current.Roles.Create(roleName);
        }

        /// <summary>Удалить роль.</summary>
        public static void DeleteRole(string roleName)
        {
            if (Current.Roles == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseRoles.");
            Current.Roles.Delete(roleName);
        }

        #endregion

        #region Row Editor

        /// <summary>Добавить строку.</summary>
        public static AddEditResult AddRow(string tableName, Dictionary<string, object> values, bool strictFk = true, params ChildInsert[] children)
        {
            if (Current.RowEditor == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IRowEditor.");
            return Current.RowEditor.AddRow(tableName, values, strictFk, children);
        }

        /// <summary>Обновить строку.</summary>
        public static AddEditResult UpdateRow(string tableName, string idColumn, object idValue, Dictionary<string, object> values, bool strictFk = true)
        {
            if (Current.RowEditor == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IRowEditor.");
            return Current.RowEditor.UpdateRow(tableName, idColumn, idValue, values, strictFk);
        }

        #endregion

        #region Foreign Keys

        /// <summary>Получить внешние ключи таблицы.</summary>
        public static List<ForeignKeyInfo> GetForeignKeys(string tableName)
        {
            if (Current.ForeignKeys == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IForeignKeyProvider.");
            return Current.ForeignKeys.GetForeignKeys(tableName);
        }

        /// <summary>Получить справочник для FK.</summary>
        public static DataTable GetForeignKeyLookup(string tableName, string fkColumn)
        {
            if (Current.ForeignKeys == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IForeignKeyProvider.");
            return Current.ForeignKeys.GetForeignKeyLookup(tableName, fkColumn);
        }

        /// <summary>Определить колонку отображения.</summary>
        public static string ResolveDisplayColumn(string tableName, string idColumn = "ID")
        {
            if (Current.ForeignKeys == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IForeignKeyProvider.");
            return Current.ForeignKeys.ResolveDisplayColumn(tableName, idColumn);
        }

        #endregion

        #region Virtual Tables

        /// <summary>Зарегистрировать виртуальную таблицу.</summary>
        public static void RegisterVirtualTable(string name, string sql, Scraps.Security.PermissionFlags required = Scraps.Security.PermissionFlags.Read)
        {
            if (Current.VirtualTables == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IVirtualTableRegistry.");
            Current.VirtualTables.Register(name, sql, required);
        }

        /// <summary>Получить данные виртуальной таблицы.</summary>
        public static DataTable GetVirtualTableData(string name, string roleName = null, Scraps.Security.PermissionFlags required = Scraps.Security.PermissionFlags.Read)
        {
            if (Current.VirtualTables == null)
                throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IVirtualTableRegistry.");
            return Current.VirtualTables.GetData(name, roleName, required);
        }

        #endregion

        #region Initialization

        /// <summary>Инициализировать базу данных.</summary>
        public static void Initialize(DatabaseGenerationOptions options)
            => Current.Initialize(options);

        #endregion
    }
}
