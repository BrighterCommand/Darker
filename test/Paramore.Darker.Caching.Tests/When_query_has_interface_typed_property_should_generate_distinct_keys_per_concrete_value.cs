using Shouldly;
using Xunit;

namespace Paramore.Darker.Caching.Tests;

// Test doubles: a marker interface whose implementations carry the distinguishing data.
// The interface itself exposes no readable members, so serializing a property against the
// DECLARED interface type would drop that data.
file interface IFilter;

file record TextFilter(string Text) : IFilter;

// Test double: a query whose only property is declared as the interface type. Two instances
// differ solely in the concrete filter's data (the search text).
file record SearchQuery(IFilter Filter) : IQuery<string>;

public class InterfaceTypedPropertyKeyTests
{
    private readonly DefaultCacheKeyGenerator defaultCacheKeyGenerator = new();

    [Fact]
    public void When_query_has_interface_typed_property_should_generate_distinct_keys_per_concrete_value()
    {
        //Arrange
        // Same query type, same concrete filter type — only the filter's data differs.
        var searchCats = new SearchQuery(new TextFilter("cats"));
        var searchDogs = new SearchQuery(new TextFilter("dogs"));

        //Act
        var keyForCats = defaultCacheKeyGenerator.GenerateKey(searchCats);
        var keyForDogs = defaultCacheKeyGenerator.GenerateKey(searchDogs);

        //Assert
        // The distinguishing data lives on the concrete TextFilter, not on IFilter, so two
        // queries that must produce different results MUST NOT collide onto the same cache key.
        keyForDogs.ShouldNotBe(keyForCats);
    }
}
