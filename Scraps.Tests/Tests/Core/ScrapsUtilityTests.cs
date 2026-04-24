using Scraps.Databases;
using Xunit;

namespace Scraps.Tests.Core
{
    public class ScrapsUtilityTests
    {
        [Fact]
        public void QuoteIdentifier_WrapsNamesWithSpaces()
        {
            var name = "Таблица 1";
            var quoted = MSSQL.QuoteIdentifier(name);
            Assert.Equal("[Таблица 1]", quoted);
        }

        [Fact]
        public void QuoteIdentifier_WrapsSchemaQualified()
        {
            var name = "dbo.Table";
            var quoted = MSSQL.QuoteIdentifier(name);
            Assert.Equal("[dbo].[Table]", quoted);
        }

        [Fact]
        public void QuoteIdentifier_EscapesClosingBracket()
        {
            var name = "na]me";
            var quoted = MSSQL.QuoteIdentifier(name);
            Assert.Equal("[na]]me]", quoted);
        }

        [Fact]
        public void ConnectionStringBuilder_AllowsDirectString()
        {
            var input = "Data Source=.;Initial Catalog=Test;Integrated Security=True;";
            var output = MSSQL.ConnectionStringBuilder(input);
            Assert.Equal(input, output);
        }
    }
}
