using Scraps.Configs;
using Scraps.Tests.Setup;

namespace Scraps.Tests.Setup
{
    /// <summary>
    /// Центральная конфигурация тестовой базы данных.
    /// Чтобы переключиться на другой провайдер — поменяйте Provider в одном месте.
    /// </summary>
    public static class TestDatabaseConfig
    {
        /// <summary>
        /// Текущий провайдер для DB-тестов.
        /// По умолчанию MSSQL.
        /// </summary>
        public static DatabaseProvider Provider { get; set; } = DatabaseProvider.LocalFiles;
    }

    /// <summary>
    /// Фабрика создания ITestDatabaseSetup для выбранного провайдера.
    /// </summary>
    public static class TestDatabaseSetupFactory
    {
        /// <summary>Создать ITestDatabaseSetup для текущего провайдера.</summary>
        public static ITestDatabaseSetup Create()
        {
            switch (TestDatabaseConfig.Provider)
            {
                case DatabaseProvider.MSSQL:
                    return new MSSQLTestSetup();
                case DatabaseProvider.LocalFiles:
                    return new LocalTestSetup();
                default:
                    throw new System.InvalidOperationException(
                        $"Провайдер {TestDatabaseConfig.Provider} не поддерживается в тестах.");
            }
        }
    }
}
