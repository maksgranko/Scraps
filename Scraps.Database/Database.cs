using System;
using System.Collections.Generic;
using System.Data;

namespace Scraps.Database
{
    /// <summary>
    /// Статический фасад для работы с базой данных.
    /// Автоматически выбирает провайдер из ScrapsConfig.DatabaseProvider.
    /// </summary>
    public static class Current
    {
        private static IDatabase Active => DatabaseProviderFactory.Current;

        /// <summary>Подключение к базе данных.</summary>
        public static IDatabaseConnection Connection => Active.Connection ?? throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseConnection.");
        /// <summary>Схема базы данных (таблицы, колонки).</summary>
        public static IDatabaseSchema Schema => Active.Schema ?? throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseSchema.");
        /// <summary>Данные таблиц (CRUD-операции).</summary>
        public static IDatabaseData Data => Active.Data ?? throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseData.");
        /// <summary>Управление пользователями.</summary>
        public static IDatabaseUsers Users => Active.Users ?? throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseUsers.");
        /// <summary>Управление ролями.</summary>
        public static IDatabaseRoles Roles => Active.Roles ?? throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseRoles.");
        /// <summary>Управление правами ролей.</summary>
        public static IDatabaseRolePermissions RolePermissions => Active.RolePermissions ?? throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseRolePermissions.");
        /// <summary>Редактор строк (добавление/обновление).</summary>
        public static IRowEditor RowEditor => Active.RowEditor ?? throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IRowEditor.");
        /// <summary>Провайдер внешних ключей.</summary>
        public static IForeignKeyProvider ForeignKeys => Active.ForeignKeys ?? throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IForeignKeyProvider.");
        /// <summary>Реестр виртуальных таблиц.</summary>
        public static IVirtualTableRegistry VirtualTables => Active.VirtualTables ?? throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IVirtualTableRegistry.");

        public static IDatabaseConnection Connection => Current.Connection ?? throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseConnection.");
        public static IDatabaseSchema Schema => Current.Schema ?? throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseSchema.");
        public static IDatabaseData Data => Current.Data ?? throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseData.");
        public static IDatabaseUsers Users => Current.Users ?? throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseUsers.");
        public static IDatabaseRoles Roles => Current.Roles ?? throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseRoles.");
        public static IDatabaseRolePermissions RolePermissions => Current.RolePermissions ?? throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IDatabaseRolePermissions.");
        public static IRowEditor RowEditor => Current.RowEditor ?? throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IRowEditor.");
        public static IForeignKeyProvider ForeignKeys => Current.ForeignKeys ?? throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IForeignKeyProvider.");
        public static IVirtualTableRegistry VirtualTables => Current.VirtualTables ?? throw new InvalidOperationException($"Провайдер '{Current.Provider}' не реализует IVirtualTableRegistry.");

        #region Connection

        /// <summary>Проверить подключение.</summary>
        public static bool TestConnection() => Active.TestConnection();

        /// <summary>Выполнить SQL без возврата данных.</summary>
        public static void ExecuteNonQuery(string sql, params object[] parameters)
            => Active.Connection.ExecuteNonQuery(sql, parameters);

        /// <summary>Выполнить SQL и вернуть скаляр.</summary>
        public static object ExecuteScalar(string sql, params object[] parameters)
            => Active.Connection.ExecuteScalar(sql, parameters);

        /// <summary>Выполнить SQL и вернуть DataTable.</summary>
        public static DataTable GetDataTable(string sql, params object[] parameters)
            => Active.Connection.GetDataTable(sql, parameters);

        #endregion

        #region Schema

        /// <summary>Получить список таблиц.</summary>
        public static List<string> GetTables(bool includeSystem = false)
        {
            if (Active.Schema == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseSchema.");
            return Active.Schema.GetTables(includeSystem);
        }

        /// <summary>Получить колонки таблицы.</summary>
        public static List<string> GetTableColumns(string tableName)
        {
            if (Active.Schema == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseSchema.");
            return Active.Schema.GetTableColumns(tableName);
        }

        /// <summary>Получить схему таблицы.</summary>
        public static DataTable GetTableSchema(string tableName)
        {
            if (Active.Schema == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseSchema.");
            return Active.Schema.GetTableSchema(tableName);
        }

        #endregion

        #region Data

        /// <summary>Получить данные таблицы.</summary>
        public static DataTable GetTableData(string tableName, params string[] columns)
        {
            if (Active.Data == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseData.");
            return Active.Data.GetTableData(tableName, columns);
        }

        /// <summary>Получить данные с разворачиванием FK.</summary>
        public static DataTable GetTableDataExpanded(string tableName, IEnumerable<ForeignKeyJoin> foreignKeys, params string[] baseColumns)
        {
            if (Active.Data == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseData.");
            return Active.Data.GetTableDataExpanded(tableName, foreignKeys, baseColumns);
        }

        /// <summary>Найти записи по колонке.</summary>
        public static DataTable FindByColumn(string tableName, string columnName, object value, SqlFilterOperator op = SqlFilterOperator.Eq)
        {
            if (Active.Data == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseData.");
            return Active.Data.FindByColumn(tableName, columnName, value, op);
        }

        /// <summary>Применить изменения.</summary>
        public static void ApplyTableChanges(string tableName, DataTable changes)
        {
            if (Active.Data == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseData.");
            Active.Data.ApplyTableChanges(tableName, changes);
        }

        /// <summary>Массовая вставка.</summary>
        public static void BulkInsert(string tableName, DataTable data)
        {
            if (Active.Data == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseData.");
            Active.Data.BulkInsert(tableName, data);
        }

        #endregion

        #region Users

        /// <summary>Получить пользователя по логину.</summary>
        public static DataRow GetUserByLogin(string login)
        {
            if (Active.Users == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseUsers.");
            return Active.Users.GetByLogin(login);
        }

        /// <summary>Создать пользователя.</summary>
        public static void CreateUser(string login, string password, string role)
        {
            if (Active.Users == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseUsers.");
            Active.Users.Create(login, password, role);
        }

        /// <summary>Удалить пользователя.</summary>
        public static void DeleteUser(string login)
        {
            if (Active.Users == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseUsers.");
            Active.Users.Delete(login);
        }

        /// <summary>Изменить пароль.</summary>
        public static void ChangeUserPassword(string login, string newPassword)
        {
            if (Active.Users == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseUsers.");
            Active.Users.ChangePassword(login, newPassword);
        }

        #endregion

        #region Roles

        /// <summary>Получить ID роли по имени.</summary>
        public static int? GetRoleIdByName(string roleName)
        {
            if (Active.Roles == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseRoles.");
            return Active.Roles.GetRoleIdByName(roleName);
        }

        /// <summary>Создать роль.</summary>
        public static int CreateRole(string roleName)
        {
            if (Active.Roles == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseRoles.");
            return Active.Roles.Create(roleName);
        }

        /// <summary>Удалить роль.</summary>
        public static void DeleteRole(string roleName)
        {
            if (Active.Roles == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IDatabaseRoles.");
            Active.Roles.Delete(roleName);
        }

        #endregion

        #region Row Editor

        /// <summary>Добавить строку.</summary>
        public static AddEditResult AddRow(string tableName, Dictionary<string, object> values, bool strictFk = true, params ChildInsert[] children)
        {
            if (Active.RowEditor == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IRowEditor.");
            return Active.RowEditor.AddRow(tableName, values, strictFk, children);
        }

        /// <summary>Обновить строку.</summary>
        public static AddEditResult UpdateRow(string tableName, string idColumn, object idValue, Dictionary<string, object> values, bool strictFk = true)
        {
            if (Active.RowEditor == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IRowEditor.");
            return Active.RowEditor.UpdateRow(tableName, idColumn, idValue, values, strictFk);
        }

        #endregion

        #region Foreign Keys

        /// <summary>Получить внешние ключи таблицы.</summary>
        public static List<ForeignKeyInfo> GetForeignKeys(string tableName)
        {
            if (Active.ForeignKeys == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IForeignKeyProvider.");
            return Active.ForeignKeys.GetForeignKeys(tableName);
        }

        /// <summary>Получить справочник для FK.</summary>
        public static DataTable GetForeignKeyLookup(string tableName, string fkColumn)
        {
            if (Active.ForeignKeys == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IForeignKeyProvider.");
            return Active.ForeignKeys.GetForeignKeyLookup(tableName, fkColumn);
        }

        /// <summary>Определить колонку отображения.</summary>
        public static string ResolveDisplayColumn(string tableName, string idColumn = "ID")
        {
            if (Active.ForeignKeys == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IForeignKeyProvider.");
            return Active.ForeignKeys.ResolveDisplayColumn(tableName, idColumn);
        }

        #endregion

        #region Virtual Tables

        /// <summary>Зарегистрировать виртуальную таблицу.</summary>
        public static void RegisterVirtualTable(string name, string sql, Scraps.Security.PermissionFlags required = Scraps.Security.PermissionFlags.Read)
        {
            if (Active.VirtualTables == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IVirtualTableRegistry.");
            Active.VirtualTables.Register(name, sql, required);
        }

        /// <summary>Получить данные виртуальной таблицы.</summary>
        public static DataTable GetVirtualTableData(string name, string roleName = null, Scraps.Security.PermissionFlags required = Scraps.Security.PermissionFlags.Read)
        {
            if (Active.VirtualTables == null)
                throw new InvalidOperationException($"Провайдер '{Active.Provider}' не реализует IVirtualTableRegistry.");
            return Active.VirtualTables.GetData(name, roleName, required);
        }

        #endregion

        #region Initialization

        /// <summary>Инициализировать базу данных.</summary>
        public static void Initialize(DatabaseGenerationOptions options)
            => Active.Initialize(options);

        #endregion
    }
}
