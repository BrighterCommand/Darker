namespace Paramore.Darker.Caching;

/// <summary>
/// Computes a cache key for a query. Operates on the runtime object — never on a generic
/// type parameter — so that the correct concrete type name is always used.
/// </summary>
public interface ICacheKeyGenerator
{
    /// <summary>Generates a cache key for the given query instance.</summary>
    /// <param name="query">The runtime query object (never <c>typeof(TQuery)</c>).</param>
    /// <returns>A non-null, non-empty string that uniquely identifies the query state.</returns>
    string GenerateKey(object query);
}
