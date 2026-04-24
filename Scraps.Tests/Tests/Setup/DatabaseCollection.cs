using Xunit;

namespace Scraps.Tests.Setup
{
    [CollectionDefinition("Db")]
    public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
    {
    }
}




