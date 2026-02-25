using Xunit;

namespace Scraps.Tests
{
    [CollectionDefinition("Db")]
    public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
    {
    }
}
