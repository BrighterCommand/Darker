using Shouldly;
using Xunit;

namespace Paramore.Darker.Caching.Tests;

// Test double: a query that exposes an indexer. Reflection returns the indexer among the
// query's public readable properties, but an indexer cannot be read without an index argument.
file sealed class IndexedQuery : IQuery<string>
{
    public int PageSize { get; init; }

    // The indexer — GetProperties() reports this as a readable "Item" property with parameters.
    public string this[int index] => index.ToString();
}

public class IndexerPropertyKeyTests
{
    private readonly DefaultCacheKeyGenerator defaultCacheKeyGenerator = new();

    [Fact]
    public void When_query_has_indexer_property_should_generate_key_without_throwing()
    {
        //Arrange
        var query = new IndexedQuery { PageSize = 25 };

        //Act
        var key = defaultCacheKeyGenerator.GenerateKey(query);

        //Assert
        // The indexer is skipped (it has no readable scalar value); the ordinary PageSize
        // property still contributes to the key.
        key.ShouldContain("\"PageSize\":25");
    }
}
