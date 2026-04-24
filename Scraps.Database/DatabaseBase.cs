using Scraps.Configs;

namespace Scraps.Database
{
    /// <summary>
    /// Базовый класс для всех провайдеров баз данных.
    /// </summary>
    public abstract class DatabaseBase : IDatabase
    {
        /// <summary>Провайдер базы данных.</summary>
        public abstract DatabaseProvider Provider { get; }

        /// <summary>Подключение к базе данных.</summary>
        public virtual IDatabaseConnection Connection { get; set; }

        /// <summary>Работа со схемой.</summary>
        public virtual IDatabaseSchema Schema { get; set; }

        /// <summary>Работа с данными.</summary>
        public virtual IDatabaseData Data { get; set; }

        /// <summary>Работа с пользователями.</summary>
        public virtual IDatabaseUsers Users { get; set; }

        /// <summary>Работа с ролями.</summary>
        public virtual IDatabaseRoles Roles { get; set; }

        /// <summary>Работа с правами ролей.</summary>
        public virtual IDatabaseRolePermissions RolePermissions { get; set; }

        /// <summary>Редактор строк.</summary>
        public virtual IRowEditor RowEditor { get; set; }

        /// <summary>Реестр виртуальных таблиц.</summary>
        public virtual IVirtualTableRegistry VirtualTables { get; set; }

        /// <summary>Работа с внешними ключами.</summary>
        public virtual IForeignKeyProvider ForeignKeys { get; set; }

        /// <summary>Проверить подключение.</summary>
        public virtual bool TestConnection() => Connection?.TestConnection() ?? false;

        /// <summary>Инициализировать базу данных.</summary>
        public virtual void Initialize(DatabaseGenerationOptions options) { }

        /// <summary>Инициализировать базу данных (упрощённая перегрузка).</summary>
        public virtual void Initialize(DatabaseGenerationMode mode) => Initialize(new DatabaseGenerationOptions { Mode = mode });
    }
}
