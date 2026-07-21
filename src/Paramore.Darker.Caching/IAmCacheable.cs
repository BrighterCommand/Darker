namespace Paramore.Darker.Caching;

/// <summary>
/// A query may implement this interface to supply its own cache key, bypassing the default
/// reflection-based key strategy. A runtime <c>null</c>, empty, or whitespace-only value
/// from <see cref="CacheKey"/> fails fast with a configuration exception.
/// </summary>
public interface IAmCacheable
{
    /// <summary>The caller-supplied cache key for this query instance.</summary>
    string CacheKey { get; }
}
