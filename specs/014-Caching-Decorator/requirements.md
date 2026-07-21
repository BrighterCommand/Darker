# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: [#291 — Add caching decorator with HybridCache and FusionCache support](https://github.com/BrighterCommand/Darker/issues/291)

## Problem Statement

As a **developer using Darker**, I would like **to cache query results declaratively via an attribute on my query handler**, so that **repeated queries with the same inputs can be served from a cache instead of re-executing the handler (and its downstream I/O), improving latency and reducing load — without hand-writing cache-check/populate logic in every handler**.

Caching is one of the most common cross-cutting concerns for queries. Because queries are read-only and deterministic for the same input, they are inherently cacheable, and Darker's decorator pipeline is a natural place to apply caching transparently. Darker currently has no first-class caching stage in the pipeline.

## Proposed Solution

Provide an opt-in caching decorator that a developer applies to a query handler. When a query flows through the pipeline, the decorator computes a cache key for the query and asks the cache for the result. On a cache **hit**, the cached result is returned immediately and the rest of the pipeline (including the handler) is **not** invoked. On a cache **miss**, the decorator invokes the rest of the pipeline (`next`), stores the returned result in the cache under the computed key, and returns it.

From the developer's perspective:

- Annotate a handler's execute method with a `[CacheableQuery]` attribute to enable caching for that query, with configurable options such as expiry.
- Control the cache key for a query either by implementing an interface on the query (e.g. `IAmCacheable` exposing a `CacheKey`) or by relying on a default key strategy derived from the query's properties.
- Register a cache implementation in DI. Darker builds on Microsoft's `HybridCache` abstraction (in-memory + optional distributed backing), so either Microsoft's `HybridCache` or an alternative that implements it (e.g. FusionCache) can be plugged in.
- Observe cache behaviour (hits/misses) through telemetry.

Because the decorator short-circuits the pipeline on a hit, **its position in the pipeline (step ordering) is significant**: any decorator ordered "inside" the cache decorator will not run on a cache hit. This must be documented clearly.

## Requirements

### Functional Requirements

- **FR1** — Provide the caching decorator attribute in **two variants**, mirroring Darker's existing decorator convention (e.g. `ValidateQueryAttribute` / `ValidateQueryAttributeAsync`): a synchronous **`CacheableQueryAttribute`** (naming the sync decorator) and an asynchronous **`CacheableQueryAttributeAsync`** (naming the async decorator). Both derive from Darker's `QueryHandlerAttribute`, whose constructor requires a mandatory **`int step`** (there is no parameterless base) — so each variant's constructor takes `step` **first** (positional, per the existing `[QueryLogging(1)]` / `[ValidateQuery(step)]` convention) followed by the caching-specific arguments (FR2). This `step` is what places the decorator in the pipeline order the feature depends on. The well-known Bag-key constant (FR9) is a single shared public constant — `CacheableQueryAttribute.CacheTag` — referenced by both variants and both decorators.
- **FR2** — Each attribute carries the cache **expiry/time-to-live** as a **required** constructor argument, in addition to the mandatory `step` (FR1). The full signature is `CacheableQueryAttribute(int step, int expirationSeconds)` (and likewise for the async variant). Because attribute arguments must be compile-time constants (a `TimeSpan` cannot be an attribute argument), expiry is expressed as an integer count of **seconds** — e.g. `[CacheableQuery(step: 1, expirationSeconds: 300)]` — which the decorator maps to **`HybridCacheEntryOptions.Expiration`** (the overall expiration, spanning the L2/distributed tier). `LocalCacheExpiration` (the L1/in-memory tier) is left to HybridCache's default, which caps it at `Expiration`; the decorator does not set it explicitly in v1. There is **no silent default** — omitting expiry is a compile error, so every cached query has an explicit lifetime. **`expirationSeconds` must be positive**: a non-positive value fails fast with a configuration exception at pipeline build (mirroring Darker's fail-fast stance), rather than caching with an undefined lifetime. Tag-based eviction is **not** an attribute option (see FR9).
- **FR3** — Provide a `CacheableQueryDecorator` (and async variant) that: computes the cache key; on a hit returns the cached result **without** invoking `next`; on a miss invokes `next`, stores the result, and returns it — following the `GetOrCreateAsync` pattern.
- **FR4** — Cache key generation is **pluggable**:
  - A query may implement **`IAmCacheable`** to supply its own key. The interface lives in the caching package (`namespace Paramore.Darker.Caching`) with the exact shape `public interface IAmCacheable { string CacheKey { get; } }` (getter-only, non-nullable `string`). If a query's `CacheKey` returns `null` or an empty/whitespace string **at runtime** (the nullable annotation is compile-time only), the decorator **fails fast** with a configuration exception rather than caching under an empty/colliding key.
  - Queries that do **not** implement `IAmCacheable` fall back to a **default key strategy**: the query type's full name (`Type.FullName`) plus a **deterministic, culture-invariant JSON serialization of the query's public readable properties**, combined into a single string. Serialization must be stable across runs — property ordering fixed (e.g. by name, ordinal), `CultureInfo.InvariantCulture` for value formatting, and `null` properties emitted explicitly so present-vs-absent is distinguishable. Nested objects and collections are serialized structurally (element order as given).
- **FR5** — The cache key must account for the query **type** and all query inputs that affect the result, so that distinct queries (or distinct inputs) do not collide. *Worked example:* for `record GetUser(int UserId) : IQuery<UserDto>` with `UserId = 42`, the default key is `"MyApp.Queries.GetUser|{\"UserId\":42}"` (type full-name, a separator, then the invariant JSON body); `UserId = 43` yields a different body and therefore a different key. A query implementing `IAmCacheable` with `CacheKey => $"GetUser-{UserId}"` uses `"GetUser-42"` instead.
- **FR6** — Build on Microsoft's `HybridCache` as the caching abstraction so that both Microsoft's implementation and alternatives that implement `HybridCache` (notably **FusionCache**) are supported without Darker-specific provider code per implementation.
- **FR7** — The caching abstraction/decorator lives in its **own single package** (`Paramore.Darker.Caching`) depending on `Microsoft.Extensions.Caching.Hybrid`, separate from the Darker core, and is enabled via a DI registration extension consistent with Darker's other optional decorators. FusionCache is selected by the consumer registering it as the `HybridCache` implementation — no Darker-specific FusionCache package.
- **FR8** — Provide **both sync and async** pathways.
  - The **async** decorator's `ExecuteAsync` (which returns `Task<TResult>` per Darker's decorator contract) awaits `HybridCache.GetOrCreateAsync`, whose factory invokes `next`; it returns the awaited result.
  - The **sync** decorator's `Execute` calls `HybridCache.GetOrCreateAsync` (whose factory invokes `next` synchronously wrapped in a completed `ValueTask<TResult>`) and inspects the **`ValueTask<TResult>` returned by `GetOrCreateAsync`**: if `IsCompletedSuccessfully` it returns `.Result` synchronously (the common in-memory-hit fast path); otherwise it materializes with `.AsTask().GetAwaiter().GetResult()`. The `ValueTask<TResult>` is consumed exactly once by this sequence.
- **FR9** — Support **externally-driven tag-based eviction** without Darker exposing an invalidation API: the decorator reads a **well-known `IQueryContext.Bag` key — the public constant `CacheableQueryAttribute.CacheTag` with literal value `"Paramore.Darker.Caching.Tag"`** — to obtain a cache **tag**. `HybridCache` tags are `IEnumerable<string>`; the well-known Bag value is a single `string`, which the decorator **wraps as a one-element tag set** and passes to `GetOrCreateAsync`. Other application code can then evict entries via the underlying cache's `RemoveByTagAsync` using the same tag. When the key is **absent, or its value is not a non-empty `string`**, the entry is stored **untagged** (no throw — consistent with the best-effort stance of FR14). Darker itself provides no explicit invalidation call in v1.
- **FR10** — Emit Darker's **own** OpenTelemetry cache **metrics** for hit and miss (counter instruments — a distinct signal from span attributes), via the existing Observability support. Emission is controlled by its **own opt-out toggle** (independent of `InstrumentationOptions`, which gates *span attribute groups* rather than metric emission) so it can be disabled to avoid double-reporting when the underlying cache (HybridCache/FusionCache) already emits equivalent metrics.
- **FR11** — **Null result handling:** a `null` result is cached like any other value (negative caching), consistent with the `GetOrCreateAsync` mechanism (FR3/FR8), which stores whatever the factory returns. On a subsequent call with the same key within the expiry window, the cached `null` is returned as a **hit** and `next` is **not** re-run; the entry re-populates only after expiry (FR2) or external tag eviction (FR9). The decorator does **not** special-case `null`. (Rationale: caching "not found" for the configured TTL is standard best practice and avoids fighting the cache abstraction.)
- **FR12** — **Missing cache registration (fail fast):** if a query is marked `[CacheableQuery]` but no `HybridCache` is registered in DI, the decorator throws a configuration exception rather than silently bypassing the cache — mirroring the validation decorator's fail-fast behaviour.
- **FR13** — **Serialization failure:** if the chosen `HybridCache` cannot serialize the result type, the resulting exception surfaces to the caller (it is not swallowed and the result is not silently returned uncached without signal). This is a configuration/serializer concern of the chosen cache, surfaced honestly.
- **FR14** — **Tag unsupported by implementation:** supplying a tag via the well-known Bag key is **best-effort**. If the configured `HybridCache` implementation does not support tag-based eviction, the entry is still cached (tagging is a no-op for eviction); this does not fail the query.

### Non-functional Requirements

- **Correctness / short-circuit semantics** — On a cache hit the handler and any inner decorators must not execute; on a miss the pipeline runs exactly once and the result is cached. The ordering-sensitivity of the decorator must be documented.
- **Async-first with sync fast-path** — The async path is primary. The sync path is an optimization: **when** `GetOrCreateAsync`'s `ValueTask<TResult>` has already completed (the expected case for an in-memory L1 hit, though not contractually guaranteed by HybridCache), it returns the result synchronously; **otherwise** it blocks via `.AsTask().GetAwaiter().GetResult()`. Correctness never depends on synchronous completion — the blocking fallback always applies — so the sync-over-async cost (and its deadlock risk) is paid only when the `ValueTask` has not already completed. Both the fast-path and the blocking fallback must be covered by tests.
- **Extensibility** — Swapping the underlying cache (Microsoft HybridCache ↔ FusionCache) must be a DI/configuration choice requiring no changes to Darker code. Cache key strategy must be replaceable without changing the decorator.
- **Low overhead when not opted in** — Handlers that do not carry `[CacheableQuery]` incur no caching cost.
- **Consistency** — Naming, packaging, and registration ergonomics should follow Darker's existing decorator conventions (e.g. validation, logging, policies).
- **Targeting** — The new caching package targets **net8.0 and net9.0 only** — deliberately **not** `netstandard2.0` (which the Darker core and other decorator packages do target), because `Microsoft.Extensions.Caching.Hybrid` requires net8.0+. This is acceptable because caching is a separate, opt-in package: consumers on `netstandard2.0` who want caching must supply their own bespoke solution. Must build and test on net8.0 and net9.0.

### Constraints and Assumptions

- Origin: [V5 discussion #273](https://github.com/BrighterCommand/Darker/discussions/273); tracked by issue #291.
- Prior art / references:
  - Microsoft [`HybridCache`](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/hybrid) and the [HybridCache proposal](https://github.com/dotnet/aspnetcore/issues/54647).
  - [`IMemoryCache`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.imemorycache) / [`IDistributedCache`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.distributed.idistributedcache).
  - [FusionCache HybridCache support](https://github.com/ZiggyCreatures/FusionCache/blob/main/docs/MicrosoftHybridCache.md).
- Central Package Management (CPM) via `Directory.Packages.props` governs package versions. The `Microsoft.Extensions.Caching.Hybrid` package is **not yet referenced anywhere in the repo** — adding it (with a pinned version) to `Directory.Packages.props` is a tracked assumption/prerequisite of this feature, not an existing fact.
- Cache implementation is resolved from Microsoft.Extensions.DependencyInjection, consistent with Darker's DI integration.
- Assumption: caching is opt-in per handler (via attribute), not global.
- Assumption: cached results should be serializable by the chosen cache (HybridCache handles serialization of the result type).

### Out of Scope

- Automatic/global caching of all queries without opt-in.
- Providing Darker's own distributed cache backend or serializer — these come from the chosen `HybridCache` implementation and its configured backing store.
- Streaming-query (`IStreamQuery` / `IAsyncEnumerable`) caching.
- A Darker-provided invalidation/eviction API. v1 supports **expiry (TTL)** for cache lifetime plus **externally-driven tag eviction** (FR9); Darker exposes no `Remove`/`RemoveByTag` call of its own.
- A Darker-specific FusionCache package — FusionCache is consumed via its `HybridCache` implementation and DI registration.

## Acceptance Criteria

How we'll know this is working correctly (below, `[CacheableQuery]` is shorthand for "the applicable variant" — the async variant `[CacheableQueryAsync]` for async handlers, the sync `[CacheableQuery]` for sync handlers; the core hit/miss criteria are exercised on **both**, and at minimum the async variant since Darker handlers are async-first):

- A query handler annotated with the caching attribute and backed by a registered `HybridCache`:
  - on first execution runs the handler and returns its result, populating the cache;
  - on a subsequent execution with the same key returns the cached result **without** re-running the handler (verifiable via a handler-execution recorder / call count);
  - after expiry, re-runs the handler.
- A query implementing `IAmCacheable` uses its `CacheKey`; a query that does not falls back to the default key strategy. The default strategy is deterministic (same query ⇒ same key across runs) and distinct inputs produce distinct keys — asserted against the worked example in FR5 (`GetUser(42)` ⇒ `"MyApp.Queries.GetUser|{\"UserId\":42}"`, `GetUser(43)` ⇒ a different key).
- Both attribute variants exist — `CacheableQueryAttribute` (sync) and `CacheableQueryAttributeAsync` (async) — with the signature `(int step, int expirationSeconds)`; each maps to its respective sync/async caching decorator; the shared `CacheableQueryAttribute.CacheTag` constant is the single well-known Bag key used by both.
- The `step` argument orders the caching decorator in the pipeline: a test with the cache decorator wrapping an inner decorator proves the inner decorator is skipped on a hit, and the cache decorator's position is honoured relative to other decorators (consistent with `[QueryLogging(1)]`-style ordering).
- Expiry is a required attribute argument in seconds, mapped to `HybridCacheEntryOptions.Expiration`; after the configured expiry elapses, the handler re-runs (a test with a short expiry proves re-execution). A non-positive `expirationSeconds` fails fast with a configuration exception at pipeline build.
- A query whose `IAmCacheable.CacheKey` returns `null`/empty/whitespace at runtime fails fast with a configuration exception (does not cache under an empty key).
- The caching decorator ships as the package **`Paramore.Darker.Caching`** (net8.0/net9.0), depends on `Microsoft.Extensions.Caching.Hybrid`, and is enabled by a named DI registration extension (mirroring the validation `Use*()` / `AddDarker().Add…()` convention). A test/registration proves opt-in via that extension.
- The underlying cache can be switched between Microsoft `HybridCache` and FusionCache purely via DI registration, with no change to handler or decorator code.
- Cache hits and misses are observable as OpenTelemetry **metrics** (hit/miss counters), and that emission can be turned **off** via its own opt-out toggle (independent of `InstrumentationOptions`).
- Decorator ordering behaviour (inner decorators skipped on hit) is demonstrated by a test and documented.
- Both sync and async paths are covered, including the sync fast-path (`ValueTask.IsCompletedSuccessfully` ⇒ synchronous return) and the blocking fallback.
- A tag placed in `IQueryContext.Bag` under `CacheableQueryAttribute.CacheTag` (`"Paramore.Darker.Caching.Tag"`) is applied to the cache entry, and eviction via the underlying cache's `RemoveByTagAsync` with that tag removes the entry; absent the key, the entry is stored untagged.
- **Error/boundary behaviour is tested:** a `null` handler result **is cached** and a subsequent call within the expiry window returns the cached `null` as a hit without re-running the handler (FR11); a `[CacheableQuery]` handler with no `HybridCache` registered throws a fail-fast configuration exception (FR12); a serialization failure surfaces to the caller (FR13); a non-`string`/absent Bag value under the tag key stores the entry untagged, and tagging against an implementation without tag support still caches the entry and does not fail the query (FR9, FR14).
- Builds and tests pass on net8.0 and net9.0 via `Darker.Filter.slnf`, following the TDD workflow.

## Resolved Decisions

Confirmed with the product owner; these now inform the design:

1. **Package layout** — **A single caching package** (e.g. `Paramore.Darker.Caching`) depending on `Microsoft.Extensions.Caching.Hybrid`. Both Microsoft `HybridCache` and FusionCache are supported because both implement `HybridCache`; the consumer chooses via DI. No per-provider packages. *(→ FR6, FR7)*
2. **Sync pathway** — **Sync uses the async cache path with an immediate-completion fast return.** The sync `Execute` calls `HybridCache.GetOrCreateAsync` and inspects the **`ValueTask<TResult>` it returns**: on `IsCompletedSuccessfully` (the in-memory-hit case) it returns `.Result` synchronously; otherwise it falls back to `.AsTask().GetAwaiter().GetResult()`. The `ValueTask` is consumed exactly once. The `Task<TResult>` return type of the async decorator's `ExecuteAsync` is irrelevant to this fast-path decision — the mechanism is entirely about the `ValueTask` from `GetOrCreateAsync`. *(→ FR8)*
3. **Invalidation** — **Expiry (TTL) only for lifetime**, supplied as a **required** attribute argument in seconds (no silent default; `TimeSpan` can't be an attribute argument). Additionally, **a cache tag may be supplied via the well-known Bag key `CacheableQueryAttribute.CacheTag` = `"Paramore.Darker.Caching.Tag"`** (value: a single `string`); the decorator applies it to the stored entry so other application code can evict using the underlying cache's `RemoveByTagAsync` with the same tag. Darker ships no invalidation API of its own. *(→ FR2, FR9)*
4. **Telemetry** — **Darker emits its own hit/miss OTel metrics (counters)** — a distinct signal type from span attributes — governed by its **own opt-out toggle**, **not** by `InstrumentationOptions` (which gates span attribute groups, a different concern). The toggle exists so emission can be disabled to avoid double-reporting when the underlying cache (HybridCache/FusionCache) already emits equivalent metrics. *(→ FR10)*
5. **Targeting** — The `Paramore.Darker.Caching` package targets **net8.0/net9.0 only** (not `netstandard2.0`), because `Microsoft.Extensions.Caching.Hybrid` requires net8.0+. Caching is a separate opt-in package, so `netstandard2.0` consumers who want caching supply their own solution. `Microsoft.Extensions.Caching.Hybrid` must be added to `Directory.Packages.props`. *(→ FR7, Targeting NFR)*
6. **Error/boundary behaviour** — `null` results **are cached** like any value (negative caching, honouring `GetOrCreateAsync`; no special-casing, FR11); a missing `HybridCache` registration fails fast (FR12); a **non-positive `expirationSeconds`** and a **null/empty runtime `IAmCacheable.CacheKey`** both fail fast with a configuration exception (FR2, FR4); serialization failures surface to the caller (FR13); a non-string/absent tag value stores the entry untagged and tags against a non-tag-supporting implementation are best-effort and never fail the query (FR9, FR14).
7. **Attribute constructor** — Both variants derive from `QueryHandlerAttribute(int step)` (step mandatory, no parameterless base), so the signature is `(int step, int expirationSeconds)`, step-first per the existing `[QueryLogging(1)]` convention. The `step` provides the pipeline ordering the feature's short-circuit semantics depend on. *(→ FR1, FR2)*

## Additional Context

Caching complements Darker's existing cross-cutting decorators (logging, retry/fallback policies, validation, telemetry). Because a cache hit short-circuits the pipeline, correct step-ordering guidance is a first-class part of this feature's documentation.
