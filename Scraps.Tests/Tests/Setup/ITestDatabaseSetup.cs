using Scraps.Configs;
using Scraps.Database;
using Scraps.Databases;
using Scraps.Databases.Utilities;
using System;
using System.Data.SqlClient;

namespace Scraps.Tests.Setup
{
    /// <summary>
    /// Абстракция над созданием/удалением тестовой БД.
    /// Позволяет легко переключаться между MSSQL, LocalFiles, MySQL и т.д.
    /// </summary>
    public interface ITestDatabaseSetup
    {
        /// <summary>Имя провайдера для сообщений.</summary>
        string ProviderName { get; }

        /// <summary>Доступен ли провайдер в текущем окружении.</summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Создать тестовую БД, настроить ScrapsConfig.ConnectionString
        /// и вернуть имя базы данных.
        /// </summary>
        string CreateDatabase();

        /// <summary>Удалить тестовую БД.</summary>
        void DropDatabase(string databaseName, string connectionString);

        /// <summary>Инициализировать схему (таблицы Users, Roles и т.д.).</summary>
        void Initialize(DatabaseGenerationMode mode);

        /// <summary>Выполнить SQL без возврата результата.</summary>
        void ExecuteNonQuery(string sql);

        /// <summary>Построить строку подключения для указанной БД.</summary>
        string BuildConnectionString(string databaseName);

        /// <summary>Найти БД по префиксу (для cleanup).</summary>
        System.Collections.Generic.IEnumerable<string> FindDatabasesByPrefix(string prefix);

        /// <summary>Массовое удаление БД (для cleanup).</summary>
        void CleanupDatabases(System.Collections.Generic.IEnumerable<string> databaseNames);
    }
}
