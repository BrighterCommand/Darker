# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: [#289 - Pipeline memoization - cache pipeline construction](https://github.com/BrighterCommand/Darker/issues/289)

## Problem Statement

As an application developer running queries through Darker in a high-throughput service, I would like the cost of discovering a query handler's decorator attributes to be paid once per handler type rather than on every execution, so that I do not incur repeated reflection overhead for a pipeline structure that never changes at runtime.

Currently `PipelineBuilder<TResult>` is constructed per-execution by `QueryProcessor` (`QueryProcessor.cs:52` and `:78`) and performs reflection on **every** call to `Build()` / `BuildAsync()`. The dominant, repeatable cost is **reading and ordering the decorator attributes**:

- `executeMethod.GetCustomAttributes(typeof(QueryHandlerAttribute), true)` then `OrderByDescending(attr => attr.Step)` (sync path, `PipelineBuilder.cs:213`)
- `executeMethod.GetCustomAttributes(typeof(QueryHandlerAttributeAsync), true)` then `OrderByDescending(attr => attr.Step)` (async path, `PipelineBuilder.cs:245`)

For a given handler type the decorator attributes and their order are **invariant** — they are fixed by the type system and cannot change after startup. Recomputing them per execution is wasted work that becomes measurable under load.

This is the query-side equivalent of the optimisation Brighter already implements in `Paramore.Brighter/PipelineBuilder.cs`, which memoises the ordered handler attributes in static `ConcurrentDictionary` fields keyed by handler `Type`. Per issue #289: *"Cache the pipeline attributes, as these are the cost, via reflection, for creation and do not change at runtime."*

## Proposed Solution

Follow Brighter's approach. On the **first** execution for a given handler type, Darker reflects over the handler exactly as it does today and caches the **ordered decorator attributes** in a static cache keyed by handler `Type`. On **subsequent** executions for the same handler type, Darker reuses the cached ordered attributes and skips the `GetCustomAttributes` + sort, building the pipeline by creating fresh handler and decorator **instances** and wiring them into the chain as it does today.

From the developer's perspective nothing changes: the same queries return the same results, decorators run in the same order, exceptions surface the same way, and per-query handler/decorator lifecycle is unchanged. The only difference is reduced per-query reflection overhead.

Only **successfully discovered** attribute sets are cached. A build that throws a configuration error (e.g. mismatched sync/async attributes, missing handler) caches nothing, so the error re-surfaces on every execution until the configuration is fixed.

Because the cached attributes are **deterministic per handler `Type`** (the same type always yields the same ordered attributes), the cache is safe to share across the process and safe to clear at any time — clearing merely forces a harmless re-reflection that produces identical data. A `ClearPipelineCache()` escape hatch is provided for parity with Brighter, but tests do **not** need it for isolation.

## Requirements

### Functional Requirements

1. **Cache ordered attributes per handler type.** The ordered decorator attributes (sorted by `Step` descending) for a handler MUST be computed once per handler `Type` and cached for reuse on subsequent executions, following Brighter's memento pattern.
2. **Separate sync and async caches.** The sync and async paths read different attribute types (`QueryHandlerAttribute` vs `QueryHandlerAttributeAsync`) off different methods (`Execute` vs `ExecuteAsync`). They MUST use separate caches (mirroring Brighter's separate `s_pre`/`s_post` dictionaries) so a handler's sync and async attribute sets never collide.
3. **Key by `Type`, never simple name.** Caches MUST be keyed by the live handler `Type`, never by `Type.Name` (see Constraints / Brighter #4192).
4. **Only successful builds are cached.** If discovering or validating attributes throws a configuration error, nothing is cached and the error MUST re-surface on every execution (the build is not memoised until it succeeds). The cache read/write sits **inside attribute discovery** (`GetDecorators` at `PipelineBuilder.cs:211` / `GetDecoratorsAsync` at `:243`). The existing pre-discovery mismatch validation (`ValidateNoMismatchedAttributes`, called at `PipelineBuilder.cs:65` / `:118`, *before* `GetDecorators` at `:67` / `:121`) remains **outside** the cached region and continues to run — and throw for a misconfigured handler — on every execution.
5. **Handler instances per-query.** Handler instances MUST continue to be created per-query via `IQueryHandlerFactory` / `IQueryHandlerFactoryAsync`.
6. **Decorator instances per-query.** Decorator instances MUST continue to be created per-query, and MUST still receive `Context` and `InitializeFromAttributeParams(...)` per query. (The cached attribute instances supply the parameters; the decorator *instances* are not cached.)
7. **Per-query lifecycle unchanged.** A per-query `IAmALifetime` scope is created, and the handler and decorators are released and the scope disposed after each execution, exactly as today.
8. **Cache-clear escape hatch.** A public `ClearPipelineCache()` (or equivalent) MUST be provided, mirroring Brighter, to clear the caches. (Additive API; not required by tests for isolation — see Constraints.)

### Non-functional Requirements

- **Performance**: Repeated executions of the same handler type MUST avoid re-running `GetCustomAttributes` and the attribute sort. (This is an internal optimisation; see Acceptance Criteria for how it is verified.)
- **Thread safety**: The caches MUST be safe for concurrent reads and first-time population from multiple threads executing queries simultaneously (e.g. `ConcurrentDictionary`). Concurrent first-time builds of the same handler type MUST NOT corrupt the cache or throw. Because cached values are deterministic per `Type`, a concurrent clear-and-repopulate is also safe.
- **Behaviour preservation**: This is a pure performance optimisation. Observable behaviour — results, decorator ordering, fallback wiring, exception types and stack-trace preservation, logging semantics — MUST NOT change.
- **No public API change for callers**: Existing consumers of `QueryProcessor` / `IQueryProcessor` MUST require no code change. (The new static `ClearPipelineCache()` is additive only.)

### Constraints and Assumptions

- **Assumption**: For a given handler type, the decorator attributes are fixed for the lifetime of the process. Registries are populated at startup and not mutated at runtime.
- **Determinism makes clearing harmless (test isolation)**: Because the cache is keyed by `Type` and its content is invariant per type, clearing it never affects correctness — re-population yields identical data. Therefore tests do **not** require xUnit `[Collection]` serialization or `ClearPipelineCache()` calls for isolation. This mirrors Brighter's own conclusion that clearing deterministic reflection caches has no correctness impact (see Brighter spec `0003-remove_clear_event_bus_calls`, which removed ~201 cache-clearing calls and the associated `[Collection]` attributes without test failures).
- **Follow Brighter**: Prefer static `ConcurrentDictionary` fields keyed by handler `Type`, as Brighter does in `Paramore.Brighter/PipelineBuilder.cs`.
- **Constraint (avoid Brighter's bug)**: The cache MUST be keyed by the live `Type` (`GetType()`), **never** by the simple type name (`GetType().Name`). Brighter shipped a defect ([Brighter #4192](https://github.com/BrighterCommand/Brighter/issues/4192)) where its pipeline-metadata mementos were keyed by simple class name with the namespace dropped. Two handlers sharing a simple name in different namespaces (both registered for the same request type) collided on one cache slot: the first-built "winner" populated it, and the second handler then silently ran the winner's decorator/attribute arguments around its own body — no exception, no warning, wrong behaviour for the life of the process. Darker MUST key by `Type` (which also sidesteps the nullability and cross-assembly edge cases that `FullName` would introduce). Brighter's current code keys by `implicitHandler.GetType()` — the fixed form.
- **`TResult` is already segregated**: `PipelineBuilder<TResult>` is a closed generic per `TResult`; any `static` cache field already lives in a distinct closed type per `TResult`, so `TResult` need not be part of the cache key.
- **Constraint**: Handler and decorator *instances* hold mutable per-query state (`Context`) and therefore MUST NOT be cached. Only the ordered attribute set is cached.
- **Cached attribute instances are immutable**: Concrete `QueryHandlerAttribute` subclasses hold read-only configuration (`Step` is `get; private set;`; their parameter fields are `readonly`), and `GetAttributeParams()` returns a fresh array without mutating the attribute. Sharing a single cached attribute instance across concurrent queries is therefore safe; each query still builds its own decorator instance and calls `InitializeFromAttributeParams(...)` on it.
- ADR 0002 currently documents per-query pipeline construction with "no caching of the pipeline structure" as a known negative (`docs/adr/0002-attribute-driven-decorator-pipeline.md` line 115); that ADR will need updating/cross-referencing.

### Out of Scope

- Caching or pooling handler or decorator **instances**.
- Caching query **results** (response caching / memoisation of outputs) — this is purely about decorator-attribute discovery.
- Caching the resolved `Execute`/`Fallback` `MethodInfo`, or the closed decorator generic types from `MakeGenericType`, or compiling `MethodInfo.Invoke` into delegates/expression trees. Per issue #289 the attributes are the cost; these are separate, larger optimisations for a future change.
- Special handling for handlers with **no** decorator attributes — there is nothing meaningful to cache, so no special behaviour is required.
- Changing the decorator attribute model, step-ordering semantics, or the `IQueryHandlerDecorator` contract.
- Any change to the public `IQueryProcessor` execution API surface.

## Acceptance Criteria

How we'll know this is working correctly:

- **Correctness (behaviour unchanged)** — *the primary verification*:
  - Existing `Paramore.Darker.Tests` continue to pass unchanged for both sync and async paths.
  - Decorator ordering, fallback behaviour, and result values are identical before and after the change.
  - Exception behaviour is preserved: handler exceptions surface with original type and stack trace (via `ExceptionDispatchInfo`), and configuration errors are still thrown — and, because failed builds are not cached, they are thrown on **every** execution, not just the first.
- **Memoisation is an internal implementation detail**: It is verified by **code inspection** (the attribute discovery reads from the cache on the second build) and by the **absence of behaviour-test failures** — *not* by a dedicated "reflection ran exactly once" test. (The cache is private static state with no observation seam, and adding one is not warranted for an invisible optimisation.)
- **No simple-name cache collisions (correctness regression guard for Brighter #4192)**:
  - A behaviour test registers two handlers with the **same simple class name** in **different namespaces**, each with **different decorator attributes**, executes both, and asserts each runs its own decorators — proving the cache discriminates by `Type`, not by simple name. (Both sync and async paths.) This is a test of observable behaviour, not of the cache internals.
- **Thread safety**:
  - A test exercising concurrent first-time execution of the same handler type across multiple threads completes without exception and produces correct results.
- **Cache clearing**:
  - `ClearPipelineCache()` exists and is callable. Tests are **not** required to call it for isolation, and are **not** required to use `[Collection]` serialization, because the cache content is deterministic per `Type`.
- **Definition of done**:
  - Memoisation of ordered attributes implemented in `PipelineBuilder<TResult>` for both sync and async paths, keyed by handler `Type`, in separate sync/async caches.
  - Public `ClearPipelineCache()` available.
  - ADR written documenting the decision (and ADR 0002's "reflection cost" negative updated/cross-referenced).
  - Behaviour tests (collision guard, thread safety) written test-first per the TDD workflow and passing.

## Additional Context

- **Reference implementation**: `../Brighter/src/Paramore.Brighter/PipelineBuilder.cs` — note `s_preAttributesMemento` / `s_postAttributesMemento` (`static ConcurrentDictionary<Type, IOrderedEnumerable<RequestHandlerAttribute>>`) and the static `ClearPipelineCache()` method. Brighter caches **only the ordered attributes** keyed by handler `Type`; instances are still built per-request. Darker follows the same shape (two caches — sync and async — instead of pre/post).
- **Lesson learned from Brighter (key by Type)**: [Brighter #4192](https://github.com/BrighterCommand/Brighter/issues/4192) (CLOSED) — Brighter's mementos were originally keyed by simple type name and collided across namespaces, silently applying the wrong decorators. The current Brighter `PipelineBuilder.cs` keys by `implicitHandler.GetType()` (the fixed form). Darker must adopt the fixed approach (`Type` key) from the start.
- **Lesson learned from Brighter (deterministic caches need no clearing)**: Brighter spec `0003-remove_clear_event_bus_calls` is built on the rationale that static reflection caches keyed by type are deterministic, so clearing them has no correctness impact; on that basis it specifies removing ~201 `ClearServiceBus()`/`ClearEventBus()` calls and the associated `[Collection("CommandProcessor")]` attributes. Darker's attribute cache has the same deterministic, type-keyed property, so it does not need Collection serialization in tests.
- Origin: [V5 discussion #273](https://github.com/BrighterCommand/Darker/discussions/273).
- Current implementation under change: `src/Paramore.Darker/PipelineBuilder.cs` (`GetDecorators` at `:211`, `GetDecoratorsAsync` at `:243`).
