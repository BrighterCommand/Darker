using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Caching.Tests;

// Test doubles: queries that implement IAmCacheable but return invalid cache keys at runtime
file record QueryWithNullCacheKey : IAmCacheable
{
    public string CacheKey => null!;
}

file record QueryWithEmptyCacheKey : IAmCacheable
{
    public string CacheKey => string.Empty;
}

file record QueryWithWhitespaceCacheKey : IAmCacheable
{
    public string CacheKey => "   ";
}

public class When_cacheable_key_is_null_or_whitespace_should_throw_configuration_exception
{
    private readonly DefaultCacheKeyGenerator _sut = new();

    [Fact]
    public void Should_throw_configuration_exception_for_null_empty_and_whitespace_cache_keys()
    {
        // Arrange
        var nullKeyQuery = new QueryWithNullCacheKey();
        var emptyKeyQuery = new QueryWithEmptyCacheKey();
        var whitespaceKeyQuery = new QueryWithWhitespaceCacheKey();

        // Act & Assert — null cache key
        Should.Throw<ConfigurationException>(() => _sut.GenerateKey(nullKeyQuery));

        // Act & Assert — empty cache key
        Should.Throw<ConfigurationException>(() => _sut.GenerateKey(emptyKeyQuery));

        // Act & Assert — whitespace cache key
        Should.Throw<ConfigurationException>(() => _sut.GenerateKey(whitespaceKeyQuery));
    }
}
