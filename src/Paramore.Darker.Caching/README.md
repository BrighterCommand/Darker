# Paramore.Darker.Caching

Query result caching for [Darker](https://github.com/BrighterCommand/Darker), built on Microsoft's
[`HybridCache`](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/hybrid) abstraction.

Annotate a handler's `ExecuteAsync` method with `[CacheableQueryAsync]` to have Darker check the
cache before running the handler. On a **hit** the cached result is returned immediately; on a
**miss** the handler runs, its result is stored, and subsequent calls are served from the cache for
the configured lifetime. Both Microsoft's `HybridCache` and
[FusionCache](https://github.com/ZiggyCreatures/FusionCache/blob/main/docs/MicrosoftHybridCache.md)
(via its `HybridCache` implementation) are supported — the backing cache is chosen entirely by your
DI registration.

## Quick start

```csharp
// 1. Register Darker with caching support
services.AddDarker()
        .AddHandlersFromAssemblies(typeof(MyHandler).Assembly)
        .AddCaching();          // registers the async (and sync) caching decorators

// 2. Register a HybridCache implementation (Microsoft's or FusionCache)
services.AddHybridCache();      // or: services.AddFusionCache().AsHybridCache()

// 3. Annotate the handler
public class GetProductQueryHandler : QueryHandlerAsync<GetProductQuery, ProductDto>
{
    [CacheableQueryAsync(step: 1, expirationSeconds: 300)]
    public override Task<ProductDto> ExecuteAsync(GetProductQuery query,
        CancellationToken ct = default)
        => /* ... your query logic ... */
}
```

### Cache key

By default the cache key is `"{query.GetType().FullName}|{invariant-JSON-of-public-properties}"`.
To supply your own key, implement `IAmCacheable` on the query:

```csharp
public record GetProductQuery(int ProductId) : IQuery<ProductDto>, IAmCacheable
{
    public string CacheKey => $"product-{ProductId}";
}
```

A `null`, empty, or whitespace `CacheKey` at runtime fails fast with a `ConfigurationException`.

### Tag-based eviction

Place a tag in `IQueryContext.Bag` under `CacheableQueryAttribute.CacheTag`
(`"Paramore.Darker.Caching.Tag"`) before executing the query. The decorator passes it to
`HybridCache` so you can later evict the entry via the cache's own `RemoveByTagAsync`:

```csharp
context.Bag[CacheableQueryAttribute.CacheTag] = "product-catalogue";
var result = await queryProcessor.ExecuteAsync(query, context);
// elsewhere:
await hybridCache.RemoveByTagAsync("product-catalogue");
```

## Ordering matters — cache hits short-circuit the pipeline

Darker executes decorators in **descending step order**: the decorator with the highest `step` runs
first (outermost) and the decorator with the lowest `step` runs last (innermost, just before the
handler). On a cache **hit** the caching decorator returns immediately without calling `next`. Every
decorator ordered **inside** the cache decorator — every decorator whose `step` is **lower** than
the `[CacheableQueryAsync]` attribute's `step` — is therefore **skipped entirely on a hit**.

This is a deliberate consequence of how `HybridCache.GetOrCreateAsync` short-circuits: its factory
(which is "the rest of the pipeline") only runs on a miss. It is also a footgun if you are not
aware of it.

### Choosing a `step`

| Goal | How to order |
|------|--------------|
| Logging / retry / fallback runs on **every** request (hit or miss) | Give those decorators a **higher** `step` than `[CacheableQueryAsync]` so they are **outside** it |
| Logging / retry / fallback runs **only on a miss** (only when the handler actually executes) | Give those decorators a **lower** `step` so they are **inside** the cache decorator and skipped on a hit |

### Example — cache outer, logging inner (logging skipped on a hit)

```csharp
[CacheableQueryAsync(step: 2, expirationSeconds: 300)]  // outer — runs first; hit returns here
[QueryLoggingAsync(step: 1)]                             // inner — only runs on a miss
public override Task<ProductDto> ExecuteAsync(GetProductQuery query,
    CancellationToken ct = default) { ... }
```

When the cache has an entry for the key, execution returns after step 2; step 1 never fires.

### Example — logging outer, cache inner (logging always runs)

```csharp
[QueryLoggingAsync(step: 2)]                             // outer — always runs (both hit and miss)
[CacheableQueryAsync(step: 1, expirationSeconds: 300)]  // inner — hit short-circuits here
public override Task<ProductDto> ExecuteAsync(GetProductQuery query,
    CancellationToken ct = default) { ... }
```

Logging records every query invocation. The cache decorator short-circuits before the handler, but
logging has already run.

## Expiry

`expirationSeconds` is a **required** compile-time-constant argument (no silent default). A
non-positive value fails fast at pipeline build with a `ConfigurationException`. The value maps to
`HybridCacheEntryOptions.Expiration`; the L1 in-memory expiration is left to HybridCache's default
(capped at `Expiration`).

## Targeting

`Paramore.Darker.Caching` targets **net8.0** and **net9.0** only, because
`Microsoft.Extensions.Caching.Hybrid` requires .NET 8+. Consumers on `netstandard2.0` who need
caching must supply their own solution.

---

MIT License — Copyright &copy; 2026 Ian Cooper
