using Shouldly;
using Xunit;

namespace Paramore.Darker.Caching.Tests;

// Test double: a query that implements IAmCacheable and supplies its own cache key
file record GetCacheableUser(int UserId) : IAmCacheable
{
    public string CacheKey => $"GetUser-{UserId}";
}

public class When_query_is_cacheable_should_use_its_cache_key
{
    private readonly DefaultCacheKeyGenerator defaultCacheKeyGenerator = new();

    [Fact]
    public void When_query_implements_IAmCacheable_should_return_its_CacheKey_verbatim()
    {
        //Arrange
        var query = new GetCacheableUser(42);

        //Act
        var key = defaultCacheKeyGenerator.GenerateKey(query);

        //Assert

        // The key should be exactly the value returned by IAmCacheable.CacheKey
        key.ShouldBe("GetUser-42");

        // The key must NOT contain the type FullName (proving the default strategy was bypassed)
        key.ShouldNotContain(typeof(GetCacheableUser).FullName!);

        // The key must NOT contain the pipe separator used by the default strategy
        key.ShouldNotContain("|");
    }
}
