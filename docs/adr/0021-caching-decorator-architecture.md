# 21. Caching Decorator Architecture

Date: 2026-07-20

## Status

Accepted

## Context

Darker has no first-class caching stage in its pipeline. Caching is one of the most common
cross-cutting concerns for queries: because queries are read-only and deterministic for the same
input, they are inherently cacheable, and Darker's decorator pipeline is the natural place to apply
caching transparently. Today a developer who wants caching must hand-write cache-check/populate logic
inside every handler, mixing an infrastructural concern into query logic and repeating it everywhere.

**Parent Requirement**: [specs/014-Caching-Decorator/requirements.md](../../specs/014-Caching-Decorator/requirements.md)

**Scope**: This ADR decides the **architecture of query caching** as a single cohesive decision:
the caching attribute (sync + async), the caching decorator (sync + async) and how it plugs into
Darker's existing decorator pipeline with short-circuit-on-hit semantics, the choice of Microsoft's
`HybridCache` as the pluggable caching abstraction, the pluggable cache-key strategy, the well-known
`IQueryContext.Bag` tag seam for externally-driven eviction, the packaging/targeting split, the DI
registration extension, and cache hit/miss metrics **derived from traces by reusing Darker's existing
metrics-from-traces subsystem (ADR 0018)** under an independent opt-out toggle. It resolves the seven
decisions recorded in the requirements (single package on `HybridCache`; sync fast-path over the async
cache; TTL-only lifetime plus Bag-key tag eviction; Darker-owned hit/miss OTel metrics via the
existing observability support with an independent toggle; net8.0/net9.0 targeting; the
negative-caching / fail-fast boundary rules; and the step-first `(int step, int expirationSeconds)`
attribute constructor).

### Forces at play

- **Darker uses decorators, not a handler chain** — the caching stage is a
  `[QueryHandlerAttribute]` on the handler's execute method that returns a decorator *type* via
  `GetDecoratorType()`; `PipelineBuilder` resolves that type from the `IQueryHandlerDecoratorFactory`
  and wraps the handler. This is the same seam validation, logging, and the policies use.
- **`HybridCache` is *already* the pluggable abstraction (unlike validation)** — the validation
  feature needed an abstract template-method decorator with provider subclasses (FluentValidation,
  DataAnnotations) because there is no single BCL validation abstraction. Caching is different:
  Microsoft's `HybridCache` (`Microsoft.Extensions.Caching.Hybrid`) is *itself* the provider-agnostic
  seam, and FusionCache ships a `HybridCache` implementation. So the caching decorator is **concrete,
  not abstract** — it depends only on the `HybridCache` type, and the consumer chooses the backing
  implementation purely by which `HybridCache` they register in DI. No Darker-specific per-provider
  package or subclass is required. *(→ FR6, FR7, Resolved Decision 1)*
- **Short-circuit / ordering is significant** — on a cache hit the handler and every inner
  decorator must **not** run. That is exactly what `HybridCache.GetOrCreateAsync` gives us: its
  factory (which invokes `next`) runs only on a miss. Because a hit skips everything "inside" the
  cache decorator, the decorator's **`Step`** — the position it occupies in the pipeline — is
  first-class, and its ordering guidance must be documented. `Step` is the mandatory base-class
  constructor argument, so the attribute is step-first. *(→ FR1, FR3, NFR correctness)*
- **Decorators are closed over `IQuery<TResult>`, not the concrete query** — `PipelineBuilder`
  closes each decorator's open generic over `typeof(IQuery<TResult>)`
  (`src/Paramore.Darker/PipelineBuilder.cs:253` sync, `:404` async), so a decorator's `TQuery` type
  parameter **is `IQuery<TResult>` at runtime, never the concrete query type** — the same trap the
  0020 validation ADR's amendment caught. Both cache-key paths must therefore reflect on the
  **runtime object**: `query is IAmCacheable` and `query.GetType().FullName`, never on `typeof(TQuery)`.
- **Attribute state flows through `GetAttributeParams()` and is applied at pipeline build** —
  unlike validation (which carries no per-attribute state), the caching attribute carries
  `expirationSeconds`. `PipelineBuilder` calls `decorator.InitializeFromAttributeParams(attribute.GetAttributeParams())`
  while **building** the pipeline (`PipelineBuilder.cs:263`). That is the correct, honest place to
  validate `expirationSeconds > 0` and fail fast — the exception surfaces at pipeline build, before
  any handler runs. *(→ FR2)*
- **Fail fast (FR12)** — a query marked `[CacheableQuery]` with no `HybridCache` registered is a
  configuration error, not a silent cache-bypass. Darker already has
  `Paramore.Darker.Exceptions.ConfigurationException` (used by the policy and validation decorators
  for exactly this "you asked for X but didn't configure it" case); the same non-positive-expiry and
  null/empty-`CacheKey` failures reuse it.
- **Darker already has a metrics-from-traces subsystem — reuse it (FR10)** — Darker's observability
  is not tracing-only. `Paramore.Darker.Extensions.Diagnostics` already implements ADR 0018's
  metrics-from-traces pattern: a `DarkerMetricsFromTracesProcessor` (`BaseProcessor<Activity>`) fires
  on each span end, filters to the `paramore.darker` source, and dispatches by `ActivityKind` to
  per-concern meters — `QueryMeter` for `Internal` (query) spans, `DbMeter` for `Client` (DB) spans.
  Each meter is built on `IMeterFactory.Create(DarkerSemanticConventions.MeterName)` (meter name
  `"paramore.darker"`, a constant that lives in **core**), records measurements filtered to a
  low-cardinality allowed-tag set, and exposes `Enabled` for cheap short-circuiting; `AddDarkerInstrumentation()`
  on a `MeterProviderBuilder` registers the meters and subscribes the meter name. FR10's cache hit/miss
  **counters** are therefore *not* a new metrics primitive — they are a new instrument added to this
  existing subsystem. The cache decorator records the outcome as a **span attribute** (the same way
  every Darker metric's source data reaches the meters — via the span), and a new cache meter derives
  the **counter** at span end. The counter is a genuinely distinct signal from the span attribute
  (FR10's wording), and its emission is gated by its **own opt-out toggle** on
  `AddDarkerInstrumentation`, independent of `InstrumentationOptions` (which gates span-attribute
  *groups* on the tracing side, a different concern). *(→ FR10, Resolved Decision 4)*
- **Async-first with a sync fast-path** — Darker exposes paired sync/async decorator interfaces
  (`IQueryHandlerDecorator<TQuery,TResult>.Execute` returning `TResult`;
  `IQueryHandlerDecoratorAsync<TQuery,TResult>.ExecuteAsync` returning `Task<TResult>`). The async
  path is primary. The sync path calls `GetOrCreateAsync` and inspects the returned
  `ValueTask<TResult>`: returns synchronously on `IsCompletedSuccessfully` (the in-memory-hit case),
  otherwise blocks via `.AsTask().GetAwaiter().GetResult()`. Correctness never depends on synchronous
  completion. *(→ FR8, Resolved Decision 2)*
- **Targeting (NFR)** — `Microsoft.Extensions.Caching.Hybrid` requires net8.0+, so the caching
  package targets **net8.0/net9.0 only**, deliberately not the `netstandard2.0` the Darker core
  targets. Acceptable because caching is a separate, opt-in package. *(→ FR7, Resolved Decision 5)*

### Why this is one decision, not a bolt-on

The crux is a single control-flow seam: **`GetOrCreateAsync` *is* the short-circuit**. The factory it
invokes on a miss is precisely "the rest of the pipeline (`next`)", and the value it returns on a hit
is precisely "skip everything inside me". Everything else in this feature hangs off that one seam —
the key that indexes it (the pluggable strategy), the expiry and tag that qualify the stored entry,
the hit/miss signal derived from whether the factory ran, and the sync fast-path over the same
`ValueTask`. Splitting these into separate ADRs would obscure that they are facets of one
`GetOrCreateAsync` call.

## Decision

Adopt a **concrete caching decorator over Microsoft's `HybridCache` abstraction**, plugged into
Darker's existing decorator pipeline via a step-ordered attribute, that uses `GetOrCreateAsync` to
short-circuit the pipeline on a hit. Cache-key computation is delegated to a **replaceable
`ICacheKeyGenerator` role**; expiry is a required attribute argument; a tag may be supplied through a
well-known `IQueryContext.Bag` key for externally-driven eviction. Hit/miss is recorded by the
decorator as a **cache-outcome span attribute**, and Darker's existing metrics-from-traces subsystem
(ADR 0018) **derives a hit/miss counter** from it via a new cache meter, under an opt-out toggle
independent of `InstrumentationOptions`. The backing cache implementation (Microsoft `HybridCache`
↔ FusionCache) is chosen entirely by DI registration with no change to Darker code.

### Architecture Overview

```
[CacheableQueryAsync(step: 1, expirationSeconds: 300)]  ─ attribute on handler's ExecuteAsync
        │  GetDecoratorType() → typeof(CacheableQueryDecoratorAsync<,>)   (CONCRETE type)
        │  GetAttributeParams() → new object[] { 300 }   ──► InitializeFromAttributeParams (build-time,
        ▼                                                     validates expiry > 0, maps to Expiration)
PipelineBuilder ── resolves decorator from IQueryHandlerDecoratorFactory (closed over IQuery<TResult>)
        ▼
 ┌──────────────────────────────────────────────────────────────────────────────────┐
 │ CacheableQueryDecoratorAsync<TQuery,TResult>   (Paramore.Darker.Caching)           │
 │   ExecuteAsync(query, next, fallback, ct):                                          │
 │     var cache = serviceProvider.GetService<HybridCache>()                           │
 │                 ?? throw new ConfigurationException(...)          ◄── FR12 fail-fast │
 │     var key   = keyGenerator.GenerateKey(query)   ─────────────►  ICacheKeyGenerator │
 │     var tags  = ReadTag(Context.Bag)              ─── Bag["Paramore.Darker.Caching.Tag"]
 │     var ran   = false;                                                              │
 │     var result = await cache.GetOrCreateAsync(key,                                  │
 │                    factory: (s,c) => { ran = true; return new(next(s.q, c)); },     │
 │                    options: { Expiration = expiry }, tags, ct);   ── factory runs on MISS only
 │     Context.Span?.SetTag("paramore.darker.cache.outcome", ran ? "miss" : "hit");    │
 │     return result;   // on HIT, next (and every inner decorator) never ran          │  (only touches
 └──────────────────────────────────────────────────────────────────────────────────┘   core Activity —
        │ GenerateKey                                    │ cache-outcome span attribute   NO OTel dep)
        ▼                                                ▼
 ICacheKeyGenerator ── default DefaultCacheKeyGenerator  query span ends
   query is IAmCacheable ? query.CacheKey (fail-fast if       │
     null/empty/whitespace)                                   ▼   (Paramore.Darker.Extensions.Diagnostics,
   : query.GetType().FullName + "|" + <invariant JSON>   DarkerMetricsFromTracesProcessor.OnEnd            ADR 0018)
                                                              │  ActivityKind.Internal → cache meter
                                                              ▼
                                            IAmADarkerCacheMeter (CacheMeter)
                                              IMeterFactory.Create("paramore.darker")
                                              Counter<long> "paramore.darker.cache.requests"
                                              Add(1, {query.type, cache.outcome})   ◄── opt-out toggle on
                                                                                        AddDarkerInstrumentation
```

The cache check runs **before** `next`, so on a hit the handler and any inner decorators never run.
Placement relative to other decorators is controlled by the attribute's `Step`, exactly like every
other Darker decorator — and here that placement is semantically load-bearing, because everything
ordered "inside" the cache decorator is skipped on a hit.

### Key Components

**Package — `Paramore.Darker.Caching` (net8.0/net9.0), depends on `Microsoft.Extensions.Caching.Hybrid`
and the Darker core (but **not** on OpenTelemetry / `System.Diagnostics.Metrics`):**

- **`IAmCacheable`** — a *knowing* role a query may implement to supply its own key. Exact shape,
  in `namespace Paramore.Darker.Caching`:
  ```csharp
  public interface IAmCacheable { string CacheKey { get; } }
  ```
  Getter-only, non-nullable `string`. A runtime `null`/empty/whitespace value **fails fast** with
  `ConfigurationException` (the nullable annotation is compile-time only). *(→ FR4)*

- **`CacheableQueryAttribute` / `CacheableQueryAttributeAsync`** — *interfacer/structurer* attributes
  deriving from `QueryHandlerAttribute` / `QueryHandlerAttributeAsync` (mandatory `step`, no
  parameterless base). Signature `(int step, int expirationSeconds)`, step-first per the
  `[QueryLogging(1)]` / `[ValidateQuery(step)]` convention. `GetDecoratorType()` names the **concrete**
  decorator open generic; `GetAttributeParams()` returns `new object[] { expirationSeconds }`. The
  single shared well-known Bag-key constant lives here:
  ```csharp
  public const string CacheTag = "Paramore.Darker.Caching.Tag";
  ```
  referenced by both variants and both decorators. *(→ FR1, FR2, FR9)*

- **`CacheableQueryDecorator<TQuery,TResult>` / `CacheableQueryDecoratorAsync<TQuery,TResult>`** —
  concrete *coordinators* implementing Darker's decorator interfaces. They own the
  get-or-create/short-circuit control flow, read expiry in `InitializeFromAttributeParams`
  (validating `> 0`, mapping to `HybridCacheEntryOptions.Expiration`), read the optional tag from
  `Context.Bag`, resolve `HybridCache` (fail-fast when absent), delegate key computation to
  `ICacheKeyGenerator`, and record the hit/miss outcome onto the query span
  (`Context.Span?.SetTag(DarkerSemanticConventions.CacheOutcome, …)`) — using only the core
  `Activity` type, so the package takes no OTel/metrics dependency. They do **not** know how keys are
  formed, how the cache serialises, or how the outcome becomes a counter — those are delegated. The
  async decorator awaits `GetOrCreateAsync`; the sync decorator inspects its returned
  `ValueTask<TResult>` for the fast-path/blocking-fallback (consumed exactly once). *(→ FR3, FR8, FR10)*

- **`CacheOutcome { Hit, Miss }`** — a small *information holder* enum the decorator uses internally
  to name its factory-ran determination (`ran ? Miss : Hit`) rather than passing a bare `bool`, then
  maps to the low-cardinality span-attribute value (`"hit"`/`"miss"`). Reveals intent; avoids
  primitive obsession on a two-state flag.

- **`ICacheKeyGenerator`** (default **`DefaultCacheKeyGenerator`**) — a *deciding/knowing* role,
  injected into the decorator and **replaceable via DI without changing the decorator** (NFR
  extensibility). Contract: `string GenerateKey(object query)` operating on the **runtime object**.
  The default: if `query is IAmCacheable c` → return `c.CacheKey` (fail-fast on null/empty/whitespace);
  otherwise return `query.GetType().FullName + "|" + <deterministic invariant JSON of public readable
  properties>`. The JSON body is **stable across runs**: properties ordered by name (ordinal),
  `CultureInfo.InvariantCulture` formatting, `null` properties emitted explicitly, nested
  objects/collections serialised structurally in element order. Worked example (FR5):
  `GetUser(42)` ⇒ `"MyApp.Queries.GetUser|{\"UserId\":42}"`; `GetUser(43)` ⇒ a distinct body.
  *(→ FR4, FR5)*

- **`AddCaching(...)`** — an `IDarkerHandlerBuilder` *structurer* extension (mirroring
  `AddJsonQueryLogging()` / `AddDefaultPolicies()` and the validation `Use*` ergonomics). It registers
  the two concrete decorator open generics with Darker's decorator registry and registers
  `DefaultCacheKeyGenerator` (overridable via an options callback for a custom `ICacheKeyGenerator`).
  It does **not** register a `HybridCache` — the consumer registers Microsoft's or FusionCache's
  implementation separately, which is exactly how the backing cache is switched with no Darker code
  change. It also does **not** wire metrics: cache-metric emission is part of the observability
  pipeline (`AddDarkerInstrumentation`, below), not the handler-caching registration. *(→ FR7)*

**Core — `Paramore.Darker` (`DarkerSemanticConventions`) additions (where `MeterName`,
`QueryDurationMetricName`, and the allowed-tag sets already live):**

- `CacheOutcome = "paramore.darker.cache.outcome"` — the span-attribute / counter-dimension key
  (value `"hit"`/`"miss"`).
- `CacheRequestsMetricName = "paramore.darker.cache.requests"` — the counter instrument name.
- `CacheRequestsAllowedTags = { QueryType, CacheOutcome }` — the low-cardinality tags permitted on the
  counter (mirrors `QueryDurationAllowedTags`; high-cardinality keys like `QueryId` excluded).

**Package — `Paramore.Darker.Extensions.Diagnostics` additions (the ADR 0018 metrics subsystem):**

- **`IAmADarkerCacheMeter`** (default **`CacheMeter`**) — a *doing* role modelled exactly on
  `IAmADarkerQueryMeter`/`QueryMeter`: `CacheMeter(IMeterFactory, MeterProvider)` creates a
  `Counter<long>` via `meterFactory.Create(DarkerSemanticConventions.MeterName)` named
  `CacheRequestsMetricName`; `RecordCacheOperation(Activity activity)` reads the `CacheOutcome` tag
  off the span and — only when present — `Add(1, …)` with the allowed tags filtered from the span
  plus the service attributes; `Enabled` exposes the counter's listener state for short-circuiting.
- **`DarkerMetricsFromTracesProcessor`** — extended so its `ActivityKind.Internal` (query span) branch
  also calls `cacheMeter.RecordCacheOperation(activity)` (a no-op when the span carries no cache
  outcome), and its cheap short-circuit guard includes `cacheMeter.Enabled`.
- **`AddDarkerInstrumentation`** — extended to `TryAddSingleton<IAmADarkerCacheMeter, CacheMeter>()`
  alongside the query and DB meters, and to accept the **opt-out toggle** (e.g.
  `AddDarkerInstrumentation(bool emitCacheMetrics = true)`): when disabled it registers a no-op
  `IAmADarkerCacheMeter` (`Enabled == false`) so the cache counter is never recorded — the FR10 lever
  for avoiding double-reporting when the underlying cache already emits equivalent metrics. This
  toggle is entirely separate from `InstrumentationOptions`.

### Technology Choices

- **`Microsoft.Extensions.Caching.Hybrid` / `HybridCache`** — the single caching abstraction; both
  Microsoft's implementation and FusionCache's `HybridCache` plug in via DI. Version pinned in
  `Directory.Packages.props` (CPM) — the package is **not yet referenced anywhere in the repo**, so
  adding it is a tracked prerequisite of this feature.
- **`HybridCache.GetOrCreateAsync`** — provides the atomic get-or-populate and *is* the pipeline
  short-circuit; its `HybridCacheEntryOptions.Expiration` receives the mapped TTL, and its
  `IEnumerable<string>` `tags` parameter receives the wrapped Bag tag. `LocalCacheExpiration` (L1) is
  left to HybridCache's default (capped at `Expiration`) — not set in v1.
- **Reuse the ADR 0018 metrics-from-traces subsystem** — `IMeterFactory`, meter name
  `DarkerSemanticConventions.MeterName` (`"paramore.darker"`), the `DarkerMetricsFromTracesProcessor`,
  and `AddDarkerInstrumentation`. The cache counter is a new `Counter<long>` created through the same
  `IMeterFactory.Create(MeterName)` the query/DB histograms use, following the `QueryMeter`/`DbMeter`
  template. No new `Meter`, no new meter name, no metrics abstraction invented in the caching package.
- **Reuse `Paramore.Darker.Exceptions.ConfigurationException`** for all three fail-fast cases (missing
  `HybridCache`, non-positive `expirationSeconds`, null/empty runtime `CacheKey`) — consistent with
  the policy and validation decorators; no new exception type.
- **Primitive types at the interop boundary are a conscious, justified exception** to the
  avoid-primitive-obsession principle, not an oversight: `int expirationSeconds` is dictated by C#
  attribute arguments being compile-time constants (a `TimeSpan` cannot be one); `string CacheKey`,
  `string CacheTag`, and `string` cache keys/`IEnumerable<string>` tags are the exact shapes
  `HybridCache.GetOrCreateAsync` / `RemoveByTagAsync` consume; the `"hit"`/`"miss"` string is the
  low-cardinality span-attribute/metric-dimension value the metrics-from-traces subsystem already
  works in. The design guidance explicitly permits primitives "where we need to serialize, or for
  interoperability." Domain-meaningful state that is *not* an interop boundary uses expressive types
  (the `CacheOutcome` enum; `IAmCacheable` as a role rather than a bare string on the query).

### Implementation Approach

1. Add `Microsoft.Extensions.Caching.Hybrid` (pinned) to `Directory.Packages.props`.
2. Add project `Paramore.Darker.Caching` targeting `net8.0;net9.0` with `IAmCacheable`, the two
   attributes (with the shared `CacheTag` constant), the `CacheOutcome` enum, the two concrete
   decorators, `ICacheKeyGenerator` + `DefaultCacheKeyGenerator`, an options type (custom key
   generator), and `AddCaching(...)`. This package references the Darker core and
   `Microsoft.Extensions.Caching.Hybrid` only — **no OpenTelemetry / metrics dependency**.
3. The decorator resolves `HybridCache` from `IServiceProvider` and throws `ConfigurationException`
   when absent (FR12). It maps `expirationSeconds` → `HybridCacheEntryOptions.Expiration` in
   `InitializeFromAttributeParams`, throwing `ConfigurationException` on a non-positive value (fail
   fast at pipeline build).
4. **Hit vs miss is derived from whether the factory ran.** A local `bool` is set inside the factory
   (which runs only on a miss); after `GetOrCreateAsync` returns, `true` ⇒ miss, `false` ⇒ hit. The
   decorator records this as `CacheOutcome` and writes it to `Context.Span` (when non-null) as the
   `paramore.darker.cache.outcome` attribute. Pass the query as `GetOrCreateAsync` state to avoid
   per-call closure allocation. *(Metrics-accuracy caveat under stampede protection: see Risks.)*
5. `null` results are **not** special-cased — `GetOrCreateAsync` stores whatever the factory returns,
   so a `null` is cached (negative caching) and returned as a hit within the expiry window (FR11).
   Serialization failures from the chosen cache surface to the caller unswallowed (FR13).
6. The tag is read from `Context.Bag[CacheableQueryAttribute.CacheTag]`; a non-empty `string` is
   wrapped as a one-element tag set and passed to `GetOrCreateAsync`; absent or non-string ⇒ stored
   untagged, no throw (FR9). Tagging against an implementation without tag support still caches and
   never fails the query (FR14, best-effort).
7. In **core** `DarkerSemanticConventions`, add `CacheOutcome`, `CacheRequestsMetricName`, and
   `CacheRequestsAllowedTags`. In **`Paramore.Darker.Extensions.Diagnostics`**, add
   `IAmADarkerCacheMeter` + `CacheMeter` (modelled on `QueryMeter`), dispatch to it from the
   `ActivityKind.Internal` branch of `DarkerMetricsFromTracesProcessor` (extending the `Enabled`
   short-circuit), and register it (plus the opt-out toggle) in `AddDarkerInstrumentation`.
8. **TDD tests** (net8.0 + net9.0 via `Darker.Filter.slnf`) cover, at minimum on the async variant and
   the core cases on both: hit skips the handler (call-count recorder) and inner decorators; miss runs
   `next` once and populates; re-run after a short expiry; `IAmCacheable` key vs default-strategy key
   with the FR5 worked example (determinism + distinct-inputs); `step` ordering / inner-decorator-skip;
   non-positive `expirationSeconds` ⇒ `ConfigurationException` at build; null/empty runtime `CacheKey`
   ⇒ `ConfigurationException`; missing `HybridCache` ⇒ `ConfigurationException`; cached `null` returns
   as a hit; serialization failure surfaces; tag applied ⇒ `RemoveByTagAsync` evicts, absent ⇒
   untagged, tag on non-supporting impl still caches; **the decorator writes the cache-outcome span
   attribute, `CacheMeter` derives a hit/miss counter from it, and the `AddDarkerInstrumentation`
   toggle disables that counter**; sync fast-path (`IsCompletedSuccessfully`) **and** blocking
   fallback; switching the backing `HybridCache` (Microsoft ↔ FusionCache) purely via DI; opt-in
   proven through `AddCaching`.
9. **End-to-end pipeline tests are mandatory.** Because `TQuery` is `IQuery<TResult>` at runtime, a
   decorator instantiated directly (or resolved over the concrete query type) exercises a resolution
   path the pipeline never uses. Each core behaviour MUST have at least one test that drives a
   `[CacheableQueryAsync]` handler through a **real `QueryProcessor`** with a registered `HybridCache`.

## Consequences

### Positive

- **No provider subclassing** — because `HybridCache` is already the pluggable seam, caching needs no
  abstract template-method decorator or per-provider package; the decorator is concrete and simple.
  Switching Microsoft ↔ FusionCache is a pure DI choice.
- **`GetOrCreateAsync` gives correct short-circuit for free** — hit-skips-inner-decorators and
  run-exactly-once-on-miss (including stampede protection) are the cache abstraction's own guarantees.
  Returned *results* are always correct; the only caveat is metrics accuracy under stampede (below).
- **Metrics reuse, no new subsystem** — cache counters are one more instrument on the existing ADR
  0018 subsystem (same `IMeterFactory`, meter name, processor, and `AddDarkerInstrumentation`
  registration), so operators get a consistent `paramore.darker` metrics surface and the caching
  package stays free of any OTel/metrics dependency.
- **Replaceable key strategy** — extracting `ICacheKeyGenerator` keeps the decorator's control flow
  independent of key formation; teams can supply a domain-specific strategy without touching Darker.
- **Opt-in and ordered** — caching is per-handler via the attribute and slots into the pipeline by
  `Step`; handlers that don't opt in pay nothing.
- **Honest boundaries** — negative caching, serialization surfacing, and the three fail-fast cases are
  explicit and tested rather than silent.

### Negative

- **Cache metrics require tracing + the metrics pipeline** — because they are metrics-from-traces
  (ADR 0018), the cache counter only materialises when a query span exists (a tracer is configured)
  **and** `AddDarkerInstrumentation` has registered the cache meter. This is the same property the
  existing query-duration and DB metrics already have, and is the deliberate cost of reusing the
  subsystem rather than emitting counters inline. Callers who want cache counts without tracing are
  not served in v1 (see Alternatives).
- **Metric hit/miss counts are approximate under stampede** — see Risks; results stay correct, only
  the counter can under-count misses.
- **Feature spans three packages** — core (convention constants), `Paramore.Darker.Caching` (decorator
  + span attribute), and `Paramore.Darker.Extensions.Diagnostics` (counter). This is the honest shape
  of "span-enrichment in the pipeline, metric-derivation in the observability package", but it does
  mean the metric lives in a different assembly from the decorator that sources it.
- **Two decorator variants + the sync-over-async fast-path** — the sync path's `ValueTask` inspection
  and blocking fallback are subtle and carry the usual sync-over-async deadlock caveat; both branches
  must be tested. Consistent with Darker's existing sync/async duality.
- **Ordering is a footgun** — a mis-placed `Step` silently skips inner decorators (logging, retry) on
  a hit. Mitigated by documentation making the cache decorator's position first-class guidance.
- **net8.0/net9.0 only** — `netstandard2.0` consumers cannot use this package and must supply their
  own caching. Accepted per the targeting NFR.

### Risks and Mitigations

- **Risk: cache-key logic reflects on `typeof(TQuery)` (which is `IQuery<TResult>`) instead of the
  runtime object** — every entry would collide under one key. *Mitigation*: `ICacheKeyGenerator` takes
  `object query` and uses `query.GetType()` / `query is IAmCacheable`; a determinism + distinct-inputs
  test on the FR5 worked example, driven through a real `QueryProcessor`, proves it.
- **Risk: non-deterministic default key** (property-order or culture drift) ⇒ silent cache misses.
  *Mitigation*: fixed ordinal property ordering, `InvariantCulture`, explicit `null`s; a
  same-query-same-key-across-runs test.
- **Risk: hit/miss counter is miscounted under cache-stampede protection.** `GetOrCreateAsync` runs
  the factory **once** for N concurrent callers on the same missing key. Only the caller whose factory
  runs sets `ran = true` (records a miss); the joined callers observe `ran == false` and record a
  **hit**, even though no entry existed when they arrived. So under a concurrent same-key miss the
  miss counter is under-reported and the hit counter inflated. *Mitigation / accepted position*: this
  is a **metrics-only** inaccuracy — the returned results are correct, and the decorator instance and
  `ran` local are per-query so there is no cross-query corruption. It is documented as a known caveat;
  `factory-ran` is treated as a best-effort hit/miss signal, not an exact oracle. (A precise
  hit/miss signal would require cooperation from the cache implementation that `HybridCache` does not
  expose.)
- **Risk: sync-over-async deadlock** on the blocking fallback in a sync context. *Mitigation*: the
  fast-path returns synchronously for the common in-memory hit; the blocking fallback is documented as
  the sync-path cost and covered by a test that forces the non-completed branch.
- **Risk: double-reported metrics** when the underlying cache already emits equivalent counters.
  *Mitigation*: the `AddDarkerInstrumentation` opt-out toggle registers a no-op `IAmADarkerCacheMeter`
  (`Enabled == false`), so Darker's cache counter is never recorded — independent of
  `InstrumentationOptions`.
- **Risk: missing `HybridCache` silently disables caching.** *Mitigation*: fail-fast
  `ConfigurationException` with a message naming the registration required.

## Alternatives Considered

- **Copy the validation ADR's abstract template-method decorator + provider packages.** Rejected:
  validation needed it because there is no single validation abstraction; caching already has one in
  `HybridCache`, so an abstract decorator and per-provider packages would be pure ceremony. FusionCache
  is consumed as a `HybridCache` implementation, not a Darker package.
- **Depend directly on `IMemoryCache` / `IDistributedCache` (or a Darker-defined `ICache`).** Rejected:
  `HybridCache` already unifies L1+L2, stampede protection, tagging, and serialization; reinventing
  that abstraction (or forcing consumers to pick a tier) adds surface for no gain and forfeits
  FusionCache interop.
- **A bespoke `Meter` / `ICacheMetrics` emitter owned by the caching package, recording hit/miss
  counters inline in the decorator.** Rejected: Darker already has a metrics subsystem (ADR 0018,
  `IMeterFactory` + meter name `paramore.darker` + `AddDarkerInstrumentation`), and FR10 asks for
  metrics "via the existing Observability support." A parallel `Meter` (e.g. `paramore.darker.caching`)
  would fragment the metrics surface, duplicate registration machinery, and drag an OTel/metrics
  dependency into the otherwise-lean caching package. The one thing the inline approach buys —
  cache counts **without** tracing — is not required by the requirements and is outweighed by
  consistency; recorded here as the fallback if tracing-independent cache metrics are later needed.
- **Inline the cache-key logic in the decorator.** Rejected: violates the extensibility NFR (key
  strategy must be replaceable without changing the decorator) and mixes a *deciding* responsibility
  into the *coordinator*.
- **Gate cache metrics through `InstrumentationOptions`.** Rejected: that enum gates span-attribute
  groups (a different signal); the counter needs its own on/off so it can be silenced independently to
  avoid double-reporting. The toggle therefore lives on `AddDarkerInstrumentation` (the metrics
  registration), not on the `[Flags]` span-attribute enum. *(Resolved Decision 4)*
- **Special-case `null` results (skip caching them).** Rejected: negative caching for the configured
  TTL is standard, and fighting `GetOrCreateAsync`'s "store whatever the factory returns" contract
  adds complexity for a worse cache-stampede profile. *(FR11)*
- **A Darker-provided invalidation/`RemoveByTag` API.** Rejected as out of scope for v1: lifetime is
  expiry (TTL); eviction is externally driven through the well-known Bag tag + the underlying cache's
  own `RemoveByTagAsync`. Darker ships no invalidation call of its own.
- **Silent default expiry.** Rejected: `expirationSeconds` is a required, compile-time-constant
  argument (a `TimeSpan` cannot be an attribute argument) so every cached query has an explicit,
  visible lifetime; non-positive fails fast. *(FR2)*

## References

- Requirements: [specs/014-Caching-Decorator/requirements.md](../../specs/014-Caching-Decorator/requirements.md)
- Linked issue: [#291 — Add caching decorator with HybridCache and FusionCache support](https://github.com/BrighterCommand/Darker/issues/291)
- Origin: [V5 discussion #273](https://github.com/BrighterCommand/Darker/discussions/273)
- Related ADRs: [0020-validation-decorator-architecture](0020-validation-decorator-architecture.md)
  (the closest decorator-pattern precedent; note this feature is concrete, not template-method),
  [0015-resilience-pipeline-integration](0015-resilience-pipeline-integration.md),
  [0016-pipeline-attribute-memoization](0016-pipeline-attribute-memoization.md) (decorator/pipeline mechanics),
  [0017-query-tracing-and-database-spans](0017-query-tracing-and-database-spans.md),
  [0018-metrics-from-query-traces](0018-metrics-from-query-traces.md) (the metrics-from-traces
  subsystem this feature's cache counter reuses)
- Prior art: Microsoft [`HybridCache`](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/hybrid)
  and the [HybridCache proposal](https://github.com/dotnet/aspnetcore/issues/54647);
  [FusionCache HybridCache support](https://github.com/ZiggyCreatures/FusionCache/blob/main/docs/MicrosoftHybridCache.md)
- Darker mechanics: `src/Paramore.Darker/QueryHandlerAttribute.cs`,
  `src/Paramore.Darker/QueryHandlerAttributeAsync.cs`,
  `src/Paramore.Darker/IQueryHandlerDecorator.cs`,
  `src/Paramore.Darker/PipelineBuilder.cs:253` / `:263` / `:404` (closed over `IQuery<TResult>`;
  `InitializeFromAttributeParams` at build time),
  `src/Paramore.Darker/Observability/DarkerSemanticConventions.cs` (`MeterName`, metric names, and
  allowed-tag sets — where the cache convention constants are added),
  `src/Paramore.Darker.Extensions.Diagnostics/Observability/QueryMeter.cs` and `DarkerMetricsFromTracesProcessor.cs`
  and `src/Paramore.Darker.Extensions.Diagnostics/DarkerMetricsBuilderExtensions.cs` (the
  `IMeterFactory`/processor/`AddDarkerInstrumentation` pattern the cache counter follows),
  `src/Paramore.Darker.Validation.FluentValidation/FluentValidationDarkerBuilderExtensions.cs`
  (the `Use*`/`Add*` DI registration template),
  `src/Paramore.Darker/Observability/InstrumentationOptions.cs` (the span-attribute toggle this
  feature's metrics toggle is deliberately independent of)
