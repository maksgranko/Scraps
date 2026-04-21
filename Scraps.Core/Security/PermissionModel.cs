using System;
using System.Collections.Generic;
using System.Linq;

namespace Scraps.Security
{
    /// <summary>
    /// Набор прав доступа. Можно комбинировать флаги.
    /// </summary>
    [Flags]
    public enum PermissionFlags
    {
        /// <summary>
        /// Нет прав.
        /// </summary>
        None = 0,
        /// <summary>
        /// Чтение.
        /// </summary>
        Read = 1,
        /// <summary>
        /// Запись/редактирование.
        /// </summary>
        Write = 2,
        /// <summary>
        /// Удаление.
        /// </summary>
        Delete = 4,
        /// <summary>
        /// Экспорт.
        /// </summary>
        Export = 8,
        /// <summary>
        /// Импорт.
        /// </summary>
        Import = 16,
        /// <summary>
        /// Удобный набор: Read + Write.
        /// </summary>
        ReadWrite = Read | Write,
        /// <summary>
        /// Все базовые права.
        /// </summary>
        All = Read | Write | Delete | Export | Import
    }

    /// <summary>
    /// Права на конкретную таблицу.
    /// </summary>
    public class TablePermission
    {
        /// <summary>
        /// Специальное имя таблицы для wildcard-правила на все таблицы.
        /// </summary>
        public const string AnyTable = "*";

        /// <summary>
        /// Имя таблицы (или "*" для глобального правила).
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Флаги прав.
        /// </summary>
        public PermissionFlags Flags { get; set; }

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public TablePermission() { }

        /// <summary>
        /// Конструктор с таблицей и флагами.
        /// </summary>
        public TablePermission(string tableName, PermissionFlags flags)
        {
            TableName = tableName;
            Flags = flags;
        }

        /// <summary>
        /// Удобный конструктор прав из булевых значений.
        /// </summary>
        public static TablePermission FromBooleans(string tableName, bool canRead, bool canWrite, bool canDelete, bool canExport, bool canImport)
        {
            var flags = PermissionFlags.None;
            if (canRead) flags |= PermissionFlags.Read;
            if (canWrite) flags |= PermissionFlags.Write;
            if (canDelete) flags |= PermissionFlags.Delete;
            if (canExport) flags |= PermissionFlags.Export;
            if (canImport) flags |= PermissionFlags.Import;
            return new TablePermission(tableName, flags);
        }

        /// <summary>
        /// Удобный конструктор wildcard-правила на все таблицы.
        /// </summary>
        public static TablePermission Any(PermissionFlags flags)
        {
            return new TablePermission(AnyTable, flags);
        }

        /// <summary>
        /// Проверить, является ли имя таблицы wildcard-значением.
        /// </summary>
        public static bool IsWildcardTableName(string tableName)
        {
            return string.Equals(tableName, AnyTable, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Роль и её права.
    /// </summary>
    public class Role
    {
        /// <summary>
        /// Название роли.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Права по таблицам.
        /// </summary>
        public List<TablePermission> TablePermissions { get; set; } = new List<TablePermission>();

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public Role() { }

        /// <summary>
        /// Конструктор с названием.
        /// </summary>
        public Role(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Конструктор с названием и правами на одну таблицу.
        /// </summary>
        public Role(string name, string tableName, PermissionFlags flags)
        {
            Name = name;
            TablePermissions.Add(new TablePermission(tableName, flags));
        }

        /// <summary>
        /// Конструктор с названием и правами на несколько таблиц.
        /// </summary>
        public Role(string name, params (string tableName, PermissionFlags flags)[] permissions)
        {
            Name = name;
            foreach (var (tableName, flags) in permissions)
            {
                TablePermissions.Add(new TablePermission(tableName, flags));
            }
        }

        /// <summary>
        /// Добавить права на таблицу.
        /// </summary>
        public Role WithPermission(string tableName, PermissionFlags flags)
        {
            TablePermissions.Add(new TablePermission(tableName, flags));
            return this;
        }

        /// <summary>
        /// Проверить, есть ли у роли нужные права на таблицу.
        /// </summary>
        public bool HasPermission(string tableName, PermissionFlags required)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return false;

            var explicitPermission = TablePermissions.FirstOrDefault(p =>
                string.Equals(p.TableName, tableName, StringComparison.OrdinalIgnoreCase));

            if (explicitPermission != null)
                return (explicitPermission.Flags & required) == required;

            var wildcardPermission = TablePermissions.FirstOrDefault(p =>
                TablePermission.IsWildcardTableName(p.TableName));

            if (wildcardPermission != null)
                return (wildcardPermission.Flags & required) == required;

            return false;
        }
    }
}
