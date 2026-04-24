using Scraps.Configs;
using Scraps.Security;
using System.Collections.Generic;
using System.Data;

namespace Scraps.Database
{
    /// <summary>
    /// Основной интерфейс для работы с базой данных.
    /// </summary>
    public interface IDatabase
    {
        /// <summary>Провайдер базы данных.</summary>
        DatabaseProvider Provider { get; }

        /// <summary>Подключение к базе данных.</summary>
        IDatabaseConnection Connection { get; }

        /// <summary>Работа со схемой.</summary>
        IDatabaseSchema Schema { get; }

        /// <summary>Работа с данными.</summary>
        IDatabaseData Data { get; }

        /// <summary>Работа с пользователями.</summary>
        IDatabaseUsers Users { get; }

        /// <summary>Работа с ролями.</summary>
        IDatabaseRoles Roles { get; }

        /// <summary>Работа с правами ролей.</summary>
        IDatabaseRolePermissions RolePermissions { get; }

        /// <summary>Редактор строк.</summary>
        IRowEditor RowEditor { get; }

        /// <summary>Реестр виртуальных таблиц.</summary>
        IVirtualTableRegistry VirtualTables { get; }

        /// <summary>Работа с внешними ключами.</summary>
        IForeignKeyProvider ForeignKeys { get; }

        /// <summary>Проверить подключение.</summary>
        bool TestConnection();

        /// <summary>Инициализировать базу данных.</summary>
        void Initialize(DatabaseGenerationOptions options);
    }
}
