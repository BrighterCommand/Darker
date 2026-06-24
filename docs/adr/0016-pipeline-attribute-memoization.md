# 16. Memoise Ordered Decorator Attributes Per Handler Type

Date: 2026-06-24

## Status

Proposed

## Context

`PipelineBuilder<TResult>` is constructed per-execution by `QueryProcessor`
(`QueryProcessor.cs:52` and `:78`) and reflects over the handler on **every** call to
`Build()` / `BuildAsync()`. The dominant repeatable cost is reading and ordering the
decorator attributes:

- `executeMethod.GetCustomAttributes(typeof(QueryHandlerAttribute), true)` then
  `OrderByDescending(attr => attr.Step)` (sync path, `PipelineBuilder.cs:213`)
- `executeMethod.GetCustomAttributes(typeof(QueryHandlerAttributeAsync), true)` then
  `OrderByDescending(attr => attr.Step)` (async path, `PipelineBuilder.cs:245`)

For a given handler `Type` the decorator attributes and their order are **invariant** —
fixed by the type system, unchangeable after startup. Recomputing them per execution is
wasted work that becomes measurable under load.

**Parent Requirement**: [specs/010-pipeline_memoization/requirements.md](../../specs/010-pipeline_memoization/requirements.md)

**Scope**: This ADR addresses a single decision — **where and how to cache the ordered
decorator attributes** so reflection over a handler's attributes is paid once per handler
`Type` rather than once per execution. It does **not** cover caching handler/decorator
*instances*, query *results*, `MethodInfo`, or closed generic decorator types — those are
explicitly out of scope (see Requirements / Out of Scope).

### Forces at play

- **Performance vs. behaviour preservation**: this is a pure optimisation; observable
  behaviour (results, decorator ordering, fallback wiring, exception types and
  stack-trace preservation, logging semantics) MUST NOT change.
- **Concurrency**: queries execute on many threads; first-time population of a cache slot
  can race. The cache must be safe under concurrent reads and concurrent first writes.
- **Correctness of the cache key**: the cache must discriminate handlers by identity.
  Brighter shipped a defect ([#4192](https://github.com/BrighterCommand/Brighter/issues/4192))
  where keying by simple class name caused two handlers sharing a simple name in different
  namespaces to collide on one cache slot — the second silently ran the first's decorators.
- **Failure semantics**: a misconfigured handler must keep throwing on every execution, not
  be "remembered" as a poisoned cache entry.
- **Parity with Brighter**: Darker mirrors Brighter's command-side architecture; following
  the same memento shape keeps the two codebases conceptually aligned.

### Constraints

- The cache MUST be keyed by the live handler `Type` (`GetType()`), **never** by
  `Type.Name` (Brighter #4192). Keying by `Type` also sidesteps the nullability and
  cross-assembly edge cases that `FullName` would introduce.
- `PipelineBuilder<TResult>` is a closed generic per `TResult`; any `static` field already
  lives in a distinct closed type per `TResult`, so `TResult` need not be part of the key.
- Handler and decorator *instances* hold mutable per-query state (`Context`) and MUST NOT
  be cached. Only the ordered attribute set is cached.
- The cached attribute instances are immutable (`Step` is `get; private set;`, parameter
  fields are `readonly`, `GetAttributeParams()` returns a fresh array), so sharing one
  cached instance across concurrent queries is safe.

## Decision

Memoise the **ordered decorator attributes** for each handler `Type` in **static
`ConcurrentDictionary` fields** on `PipelineBuilder<TResult>`, following Brighter's memento
pattern. The cache read/write sits **inside attribute discovery** (`GetDecorators` /
`GetDecoratorsAsync`); everything else in the build — resolving the handler, validating
mismatched attributes, creating fresh handler and decorator instances, wiring the chain,
and per-query lifetime — is unchanged.

Two separate caches are used because the sync and async paths read different attribute
types (`QueryHandlerAttribute` vs `QueryHandlerAttributeAsync`) off different methods
(`Execute` vs `ExecuteAsync`); separate caches guarantee a handler's sync and async
attribute sets never collide. This mirrors Brighter's separate `s_pre`/`s_post` mementos.

A public static `ClearPipelineCache()` is provided for parity with Brighter. Because the
cached value is deterministic per `Type` (the same type always yields the same ordered
attributes), clearing it is always safe — re-population produces identical data. Tests
therefore do **not** need `[Collection]` serialization or `ClearPipelineCache()` calls for
isolation.

### Architecture Overview

```
                 PipelineBuilder<TResult>  (closed generic — one static cache pair per TResult)
  ┌──────────────────────────────────────────────────────────────────────────────┐
  │  static ConcurrentDictionary<Type, IOrderedEnumerable<QueryHandlerAttribute>>      s_syncAttributesMemento   │
  │  static ConcurrentDictionary<Type, IOrderedEnumerable<QueryHandlerAttributeAsync>> s_asyncAttributesMemento  │
  └──────────────────────────────────────────────────────────────────────────────┘

  Build(query)                              BuildAsync(query)
    ResolveHandler                            ResolveHandlerAsync
    ValidateNoMismatchedAttributes  ← stays OUTSIDE the cache (runs every execution, throws on misconfig)
    GetDecorators(executeMethod) ─┐           GetDecoratorsAsync(executeMethod) ─┐
                                  │                                              │
       ┌──────────────────────────┘              ┌──────────────────────────────┘
       ▼  cache MISS                              ▼  cache HIT
  GetCustomAttributes + OrderByDescending     return cached ordered attributes (no reflection)
  TryAdd(handlerType, ordered)                       │
       │                                             │
       └─────────────► for each attribute: create fresh decorator instance,
                       set Context, InitializeFromAttributeParams(attr.GetAttributeParams())
```

First execution for a handler `Type`: cache miss → reflect and order exactly as today →
`TryAdd`. Subsequent executions: cache hit → reuse ordered attributes → skip
`GetCustomAttributes` + sort. Either way, fresh decorator instances are built per query
from the (cached or freshly read) attributes.

### Key Components

- **`PipelineBuilder<TResult>`** (`src/Paramore.Darker/PipelineBuilder.cs`) — the only
  type that changes. It gains two private static `ConcurrentDictionary` fields and a public
  static `ClearPipelineCache()`. `GetDecorators` / `GetDecoratorsAsync` change from
  "always reflect" to "reflect-on-miss, then cache". Its responsibility (structuring the
  pipeline from a handler's attributes) is unchanged; it now **knows** the previously
  discovered ordered attributes for a type rather than rediscovering them.
- **`QueryHandlerAttribute` / `QueryHandlerAttributeAsync`** — unchanged. Their existing
  immutability (read-only `Step` and params) is what makes a shared cached instance safe.
- **`ValidateNoMismatchedAttributes`** — unchanged and deliberately **outside** the cached
  region (`PipelineBuilder.cs:65` / `:118`, before `GetDecorators` at `:67` / `:121`), so a
  misconfigured handler keeps throwing on every execution.

### Technology Choices

- **`ConcurrentDictionary<Type, …>`** keyed by handler `Type` — lock-free reads, atomic
  first-write via `TryAdd`. Matches Brighter's choice. Concurrent first-time builds of the
  same type either both compute and one `TryAdd` wins (the loser's identical value is
  discarded) — no corruption, no exception.
- **`Type` key** (not `Type.Name`, not `FullName`) — the regression fix for Brighter #4192;
  also avoids `FullName` nullability/cross-assembly edge cases.
- **`IOrderedEnumerable<…>` as the cached value** — mirrors Brighter's memento type exactly
  (`s_preAttributesMemento` / `s_postAttributesMemento` are
  `ConcurrentDictionary<Type, IOrderedEnumerable<RequestHandlerAttribute>>`). The cached
  value is the result of `GetCustomAttributes(...).Cast<…>().OrderByDescending(attr => attr.Step)`
  stored directly — without the trailing `.ToList()`. This keeps the two codebases'
  pipeline caches structurally identical. The per-query instance loop enumerates the cached
  `IOrderedEnumerable` once per build, exactly as Brighter does.

### Implementation Approach

1. Add two private static fields to `PipelineBuilder<TResult>`:
   - `s_syncAttributesMemento : ConcurrentDictionary<Type, IOrderedEnumerable<QueryHandlerAttribute>>`
   - `s_asyncAttributesMemento : ConcurrentDictionary<Type, IOrderedEnumerable<QueryHandlerAttributeAsync>>`
2. In `GetDecorators` / `GetDecoratorsAsync`, derive `handlerType` from
   `executeMethod.DeclaringType` (or thread the already-resolved `handlerType` in), then
   `TryGetValue`; on miss, run the existing
   `GetCustomAttributes(...).Cast<…>().OrderByDescending(attr => attr.Step)` (dropping the
   current trailing `.ToList()`) and `TryAdd` the `IOrderedEnumerable`. The per-query
   instance-creation loop that follows is unchanged.
3. Add `public static void ClearPipelineCache()` clearing both dictionaries.
4. Update ADR 0002's "reflection cost" negative (line 115) to cross-reference this ADR.
5. Behaviour tests (collision guard for #4192 across both paths, concurrent first-build)
   written test-first per the TDD workflow.

## Consequences

### Positive

- Repeated executions of the same handler type skip `GetCustomAttributes` and the attribute
  sort — the per-query reflection cost named in issue #289 is eliminated.
- No public API change for callers; `ClearPipelineCache()` is purely additive.
- Conceptual parity with Brighter's `PipelineBuilder`, easing cross-codebase reasoning.
- The Brighter #4192 defect is avoided by construction (key by `Type` from day one).

### Negative

- Introduces process-wide mutable static state (the caches). Mitigated by the determinism
  argument: content is invariant per `Type`, so the state cannot become "wrong".
- A small amount of long-lived memory per distinct handler `Type` (one ordered attribute
  list each). Bounded by the number of handler types, which is fixed at startup.
- Two parallel near-identical cache-and-discover code paths (sync/async) — an existing
  duplication in `PipelineBuilder` that this change slightly deepens.

### Risks and Mitigations

- **Risk: simple-name cache collision (Brighter #4192).** Two handlers with the same simple
  name in different namespaces collide and silently run the wrong decorators.
  - **Mitigation**: key strictly by `Type`. A behaviour test registers two same-simple-name
    handlers in different namespaces with different attributes and asserts each runs its own
    decorators (both sync and async).
- **Risk: poisoned cache from a failed build.** A configuration error gets cached and masks
  the real fix.
  - **Mitigation**: only successful discoveries are cached (`TryAdd` runs after a successful
    ordered read); `ValidateNoMismatchedAttributes` stays outside the cached region and
    throws on every execution for a misconfigured handler.
- **Risk: concurrency corruption on first-time population.**
  - **Mitigation**: `ConcurrentDictionary` with `TryAdd`; cached values are deterministic so
    a race merely recomputes identical data. A concurrency test exercises first-time
    execution of one handler type across multiple threads.
- **Risk: stale cache after a (hypothetical) runtime registry change.**
  - **Mitigation**: out of scope by assumption (registries are populated at startup and not
    mutated); `ClearPipelineCache()` exists as an escape hatch and is harmless to call.

## Alternatives Considered

- **Cache by `Type.Name` (Brighter's original form).** Rejected — this is exactly the
  Brighter #4192 defect: namespace collisions silently apply the wrong decorators.
- **Cache by `Type.FullName` (string).** Rejected — introduces nullability (`FullName` can be
  null for some constructed types) and cross-assembly edge cases for no benefit over the live
  `Type`, which is a perfect, collision-free key already in hand.
- **Cache the fully built pipeline / decorator instances.** Rejected — instances hold mutable
  per-query `Context`; sharing them across queries would corrupt state. Out of scope per
  requirements.
- **Cache `MethodInfo`, closed generic decorator types, or compiled invocation delegates.**
  Rejected for this change — issue #289 identifies the *attributes* as the cost; these are
  separate, larger optimisations deferred to a future change.
- **Instance-level (non-static) cache.** Rejected — `PipelineBuilder` is created per
  execution, so an instance cache would never see a second hit. Brighter uses static fields
  for the same reason.
- **Materialised `IReadOnlyList` as the cached value.** Considered, then rejected in favour
  of Brighter's exact `IOrderedEnumerable` type so the two codebases' pipeline caches stay
  structurally identical. The per-build enumeration cost is negligible against the reflection
  it replaces.

## References

- Requirements: [specs/010-pipeline_memoization/requirements.md](../../specs/010-pipeline_memoization/requirements.md)
- Related ADRs:
  - [0002-attribute-driven-decorator-pipeline.md](0002-attribute-driven-decorator-pipeline.md) — documents per-query pipeline construction; its "reflection cost" negative is updated to cross-reference this ADR.
  - [0014-factory-component-lifetime.md](0014-factory-component-lifetime.md) — per-query handler/decorator lifetime, which this change preserves.
- Reference implementation: `../Brighter/src/Paramore.Brighter/PipelineBuilder.cs` —
  `s_preAttributesMemento` / `s_postAttributesMemento`
  (`static ConcurrentDictionary<Type, IOrderedEnumerable<RequestHandlerAttribute>>`),
  keyed by `implicitHandler.GetType()`, and the static `ClearPipelineCache()`.
- Lessons from Brighter:
  - [Brighter #4192](https://github.com/BrighterCommand/Brighter/issues/4192) — simple-name key collision (key by `Type`).
  - Brighter spec `0003-remove_clear_event_bus_calls` — deterministic type-keyed caches need no clearing for test isolation.
- Origin: [Issue #289](https://github.com/BrighterCommand/Darker/issues/289), [V5 discussion #273](https://github.com/BrighterCommand/Darker/discussions/273).
