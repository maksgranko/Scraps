using Xunit;

namespace Scraps.Tests
{
    [CollectionDefinition("DbGeneration")]
    public class DatabaseGenerationCollection : ICollectionFixture<DatabaseGenerationCleanupFixture>
    {
    }

    /// <summary>
    /// Cleanup fixture для DatabaseGenerationTests:
    /// - на старте убирает старые тестовые БД с префиксом Scraps_Test_
    /// - в конце удаляет БД, созданные в текущем прогоне.
    /// </summary>
    public sealed class DatabaseGenerationCleanupFixture : System.IDisposable
    {
        public DatabaseGenerationCleanupFixture()
        {
            DatabaseGenerationTests.CleanupOrphanedTestDatabases();
        }

        public void Dispose()
        {
            DatabaseGenerationTests.CleanupCurrentRunDatabases();
        }
    }
}
