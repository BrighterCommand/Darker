# 14. Lifetime-Aware Handler and Decorator Factories with a Per-Query Lifetime Scope

Date: 2026-06-08

## Status

Accepted

## Context

**Parent Requirement**: [specs/008-factory_component_lifetime/requirements.md](../../specs/008-factory_component_lifetime/requirements.md)

**Scope**: This ADR decides *how* Darker's DI-backed handler and decorator factories
become lifetime-aware (Singleton / Scoped / Transient), and *who owns* the per-query
child scope through which Scoped/Transient components and their dependencies are resolved
and disposed. It addresses issue [#329](https://github.com/BrighterCommand/Darker/issues/329)
and targets **V5**, where breaking changes to the public factory interfaces are permitted.

### The Problem

`DarkerOptions.HandlerLifetime` is captured at registration and applied to each
`ServiceDescriptor` (see `ServiceCollectionHandlerRegistry` /
`ServiceCollectionDecoratorRegistry`), but the factories that resolve and release those
components ignore it. This produces two reproducible defects (requirements Problem
Statement):

1. **Singleton components are disposed after one query.** `ServiceProviderHandlerFactory.Release`
   (and `ServiceProviderHandlerDecoratorFactory.Release`) disposes any `IDisposable` it is
   handed. With `HandlerLifetime = Singleton`, the shared instance is disposed after the
   first query; the next query resolves a disposed instance → `ObjectDisposedException`.

2. **Scoped dependencies resolve from the wrong scope.** With the default
   `QueryProcessorLifetime = Singleton`, .NET invokes the `QueryProcessor` factory delegate
   (`BuildQueryProcessor`) with the **root** provider, which the factories capture. Scoped
   dependencies (e.g. an EF Core `DbContext`) then resolve against the root scope — either
   throwing under scope validation, or returning an instance not bound to the request.

The root causes are: (a) the factories do not *know* the configured lifetime; (b) they
resolve from a captured (often root) provider rather than a per-query scope; and (c) the
public factory interfaces carry **no per-query token**, so a factory — which is itself a
singleton shared across concurrent queries — has no safe place to anchor per-query scope
state.

### Forces

- **Honour the configured lifetime (FR1–FR5, B1–B3).** Singleton: created once, reused,
  never disposed by Darker. Scoped: one child scope per pipeline execution, shared across
  the components of that execution. Transient: new instance, created via the per-execution
  child scope so disposal is deterministic.
- **Two factories must share one scope (AC4).** Handlers and decorators are created by
  *different* factory instances (`ServiceProviderHandlerFactory` vs.
  `ServiceProviderHandlerDecoratorFactory`). A Scoped dependency injected into both the
  handler and a decorator in the same pipeline must be the **same** instance — so the
  per-query scope cannot live privately inside either factory.
- **Concurrency (FR6, NFR3).** A singleton `QueryProcessor` executes queries concurrently;
  per-query scope state must be isolated per execution with no shared mutable field on the
  factory.
- **Failure-path disposal (FR9).** The per-query scope must be disposed even when the
  pipeline throws or is cancelled — no leaked `DbContext`.
- **Breaking change is permitted but should be minimal (C1).** The four public factory
  interfaces may change shape, but we should change them once, consistently.
- **`PipelineBuilder` is already the per-query boundary (C2).** A `PipelineBuilder<TResult>`
  is created and disposed (via `using`) per call to `QueryProcessor.Execute` /
  `ExecuteAsync` — the natural owner of a per-query lifetime.
- **No new global options service needed (C4).** `BuildQueryProcessor` already closes over
  `options`, so `HandlerLifetime` can be passed directly into the factory constructor; we
  need not register an `IDarkerOptions` service as Brighter does.
- **Parity with Brighter (NFR1).** Behaviour should match baseline semantics B1–B3.

### Constraints

- Public surface change confined to the `Paramore.Darker` factory interfaces and one new
  public lifetime role; non-DI factories (Simple/InMemory, testing ports) must still
  compile and behave unchanged.
- Default `HandlerLifetime` (`Transient`) and default `QueryProcessorLifetime`
  (`Singleton`) must not change (Out of Scope).

## Decision

Make the factories lifetime-aware and introduce a **per-query lifetime scope** that owns
the child `IServiceScope`. The per-query `PipelineBuilder` creates the lifetime, threads it
through every `Create`/`Release`, and disposes it in its own `Dispose()` (which runs in the
`using` *finally*, covering the failure path). The DI factory consults the configured
`HandlerLifetime` and resolves Singleton from a shared cache, Scoped/Transient from the
lifetime's child scope.

Six concrete decisions:

### Decision 1 — Introduce a per-query lifetime role: `IAmALifetime`

Add a public role in `Paramore.Darker` representing the lifetime of **one pipeline
execution**. Its responsibilities (RDD stereotypes):

- **Knowing / structuring**: it is the identity and owner of the per-query resources
  (the child DI scope and any components requiring deterministic teardown).
- **Doing**: it disposes those resources when the query ends.

```csharp
namespace Paramore.Darker
{
    /// <summary>
    /// Owns the lifetime of objects created for a single query pipeline execution.
    /// Disposed by the PipelineBuilder when the pipeline completes (including on failure).
    /// </summary>
    public interface IAmALifetime : IDisposable
    {
        /// <summary>Track a per-query disposable (e.g. a child service scope) for
        /// disposal when the pipeline completes.</summary>
        void Add(IDisposable disposable);
    }
}
```

The default concrete implementation (`QueryLifetimeScope`) holds the tracked disposables
and disposes them, in reverse order, exactly once. The `Add(IDisposable)` surface is
deliberately generic so the *core* has no dependency on
`Microsoft.Extensions.DependencyInjection` — the DI layer attaches its `IServiceScope`
through it.

**Naming.** We keep the generic role name `IAmALifetime` rather than a target-specific name
such as `IAmAQueryLifetime`. The lifetime governs **both** handlers and decorators (and any
future pipeline component); a target-specific name would wrongly imply a single kind of
object. The name also mirrors Brighter's `IAmALifetime`, aiding parity (NFR1).

### Decision 2 — Thread the lifetime through the factory interfaces (breaking change)

The four public factory interfaces gain an `IAmALifetime` parameter on `Create` and
`Release`:

```csharp
public interface IQueryHandlerFactory            // and ...FactoryAsync
{
    IQueryHandler Create(Type handlerType, IAmALifetime lifetime);
    void Release(IQueryHandler handler, IAmALifetime lifetime);
}

public interface IQueryHandlerDecoratorFactory   // and ...FactoryAsync
{
    T Create<T>(Type decoratorType, IAmALifetime lifetime) where T : IQueryHandlerDecorator;
    void Release<T>(T handler, IAmALifetime lifetime) where T : IQueryHandlerDecorator;
}
```

This is the breaking change C1 authorises. It is the *single* mechanism by which a
factory — a shared singleton — receives per-query context without holding mutable per-query
state itself, satisfying FR6/NFR3.

### Decision 3 — DI lifetime mechanics: `ServiceProviderLifetimeScope`

Port Brighter's mechanics as an **internal** type in
`Paramore.Darker.Extensions.DependencyInjection`. It is a *service provider* that knows how
to get-or-create per `ServiceLifetime`:

- **Singleton** → resolve from the factory's **captured provider** and cache for reuse;
  **never** disposed by Darker (B1, AC2). Note: in MS DI a Singleton-registered type returns
  the same container-managed instance whichever provider resolves it, and the **container**
  owns its disposal — so disposal correctness does not depend on resolving from a particular
  provider. We still resolve Singletons from the captured provider (not a child scope) and
  cache them so repeat resolutions never touch a child scope.
- **Scoped** → resolve from the per-query child `IServiceScope`; same instance for repeat
  resolutions within the execution (B2, AC4).
- **Transient** → resolve from the per-query child `IServiceScope` so the instance and its
  captured disposables are torn down when the scope is disposed (B3, AC3).

The child `IServiceScope` is created from the captured provider's `IServiceScopeFactory`,
which yields a correctly-rooted scope regardless of whether the captured provider is the
root (default `QueryProcessorLifetime = Singleton`) or a request scope
(`QueryProcessorLifetime = Scoped`). This is what fixes defect 2 (scoped dependency from the
root provider) even under the default Singleton processor.

The singleton cache lives on the factory (shared across **queries**, not per-query). Because
concurrent pipelines share that one factory, the cache **must** use thread-safe
get-or-create (e.g. a `ConcurrentDictionary<Type, …>` with `Lazy<>`), satisfying NFR3. The
child scope, by contrast, lives on the per-query **lifetime** (not the factory), attached via
`IAmALifetime.Add(scope)` on first scoped/transient resolution — so there is no per-query
mutable state on the factory.

**`QueryProcessorLifetime = Scoped` interaction.** When the processor is Scoped, the captured
provider is already a request scope; Singletons still resolve correctly (container-owned),
and Scoped/Transient resolve from a child scope of that request scope. This nests cleanly and
remains correct; it is not a required configuration (the fix works with the default Singleton
processor) but it is not broken by this design.

### Decision 4 — One DI factory implements all four roles

**Starting point.** Today there are **two** DI factory classes, each already covering two
interfaces: `ServiceProviderHandlerFactory` (`IQueryHandlerFactory` +
`IQueryHandlerFactoryAsync`) and `ServiceProviderHandlerDecoratorFactory`
(`IQueryHandlerDecoratorFactory` + `IQueryHandlerDecoratorFactoryAsync`).
`BuildQueryProcessor` already shares one handler-factory instance across the two sync/async
handler slots and one decorator-factory instance across the two decorator slots.

The decorator–handler *model* (separate handler and decorator interfaces) is preserved. We
collapse the two DI *implementations* into a **single class** implementing all four
interfaces, and `BuildQueryProcessor` passes that one instance for all four slots of
`HandlerConfiguration`.

Why merge — and why it is **not** a singleton-correctness requirement:

1. **Scoped sharing (AC4)** is guaranteed by the child scope living on the shared
   `IAmALifetime` (Decision 1) — handler and decorators read the same scope from the same
   token, independent of how many factory objects exist. The merge does **not** strictly
   require this, but it makes the shared path self-evident.
2. **Singleton sharing needs no merge — MS DI already guarantees it.** A type registered
   `Singleton` is **container-managed**: the root `ServiceProvider` constructs it once and
   injects that same instance into every constructor that asks for it, regardless of which
   provider resolves the consumer or how many Darker factory caches exist (the same semantics
   Decision 3 relies on for "never disposed by Darker"). The factory's singleton cache
   (Decision 3) caches the **component** (the handler/decorator object), **not** the
   component's injected dependencies; a handler type and a decorator type are distinct types,
   each constructed at most once per pipeline anyway. So a Singleton dependency injected into
   **both** a handler and a decorator resolves to **one** instance (`ReferenceEquals`,
   construction counter == 1) *with or without* the merge — there is no double-construction to
   fix. *(An earlier draft of this ADR claimed the merge was a shared-Singleton **correctness**
   contributor; that was incorrect and is retracted here, reconciling this decision with
   Decision 3.)*
3. **Tidiness / consolidation (the actual reason).** Two classes duplicate the identical
   lifetime-resolution logic and each carry a redundant singleton cache. One class removes the
   duplication, keeps a single cache, and gives handler and decorator one obvious shared
   resolution path. This is a **structural** improvement (Tidy First), not a behavioural one —
   which is why it is sequenced into the structural phase of implementation.

Because the merged class creates **both** handlers and decorators, it is renamed from the
handler-only `ServiceProviderHandlerFactory` to **`ServiceProviderComponentFactory`**
("component" = handler or decorator); the existing `ServiceProviderHandlerDecoratorFactory` is
removed. This is an internal DI-package type, so the rename is not a public breaking change.

### Decision 5 — `PipelineBuilder` owns the lifetime lifecycle

`PipelineBuilder<TResult>` creates one `IAmALifetime` at the **start** of `Build`/`BuildAsync`
— before any `Create` call — passes it to every `Create` (handler + decorators) and to the
corresponding `Release` calls, and disposes it in `PipelineBuilder.Dispose()`. Creating the
lifetime first is what makes partial-build failures safe: if a decorator `Create` throws
after the child scope was attached, the lifetime already exists and owns that scope, so
disposal still finds and disposes it. Disposal cascades: `IAmALifetime.Dispose()` →
`IServiceScope.Dispose()` → all Scoped/Transient components and their dependencies. Because
`QueryProcessor.Execute`/`ExecuteAsync` call `Build`/`BuildAsync` **inside** a `using`
(the `using` blocks at `QueryProcessor.cs:49-71` for `Execute` and `:75-99` for
`ExecuteAsync`), disposal runs in the *finally* — covering the exception/cancellation path
(FR9, AC7) and a partial build. `Release` for the DI factory becomes the Singleton guard
(no-op for singletons; scoped/transient teardown is owned by scope disposal). The new
`Release(handler, lifetime)` / `Release<T>(decorator, lifetime)` signatures must be
**null-tolerant** on both the component (a build that throws before resolving the handler
leaves nothing to release) and the lifetime (no scope may have been attached yet), matching
today's null-safe `Dispose` path (`PipelineBuilder.cs:274` already calls
`_handlerFactory?.Release(_handler)` with a possibly-null handler).

### Decision 6 — Pass `HandlerLifetime` into the factory constructor; non-DI factories ignore the token

`BuildQueryProcessor` passes `options.HandlerLifetime` directly into the factory constructor
(C4) — no `IDarkerOptions` service is registered. The non-DI factories
(`SimpleHandlerFactory`, `SimpleHandlerDecoratorFactory`, and the testing ports) implement
the new signatures but **ignore** the `IAmALifetime` parameter, preserving their current
behaviour (they already create/dispose directly). This keeps the breaking change to a
signature shape, not a behavioural change, outside the DI package.

### Architecture Overview

```
QueryProcessor.Execute/ExecuteAsync           (using => finally disposes builder)
        │
        ▼
PipelineBuilder<TResult>                       owns one IAmALifetime per query
   creates lifetime ──────────────► IAmALifetime (QueryLifetimeScope)
   Create(type, lifetime) for handler             │ Add(childScope)
   Create(type, lifetime) for each decorator      │
   Release(..., lifetime)                          ▼
        │                                  IServiceScope (per query)
        ▼                                          ▲
ServiceProviderComponentFactory (singleton)        │ Scoped/Transient resolve here
   ├─ Singleton ► singleton cache (root provider) ─┘ (never disposed)
   └─ Scoped/Transient ► lifetime's child scope
        ▲
        │ ctor: HandlerLifetime (from DarkerOptions, via BuildQueryProcessor)

Dispose path:  PipelineBuilder.Dispose() ► IAmALifetime.Dispose()
               ► IServiceScope.Dispose() ► scoped/transient components + deps disposed
```

### Key Components

| Component | Role (stereotype) | Responsibility |
|-----------|-------------------|----------------|
| `IAmALifetime` / `QueryLifetimeScope` | Coordinator / structurer (knowing, doing) | Identity of one pipeline execution; tracks and disposes per-query disposables (the child scope) |
| `IQueryHandlerFactory` (+ async, + decorator) | Interfacer / service provider | Create/Release a component **for a given lifetime** |
| `ServiceProviderComponentFactory` (single class, all 4 roles) | Service provider | Resolve per configured `HandlerLifetime`; own the singleton cache |
| `ServiceProviderLifetimeScope` | Service provider | Per-lifetime get-or-create + child-scope disposal mechanics |
| `PipelineBuilder<TResult>` | Controller | Create the lifetime, thread it through Create/Release, dispose it in `finally` |

### Technology Choices

- `Microsoft.Extensions.DependencyInjection.IServiceScopeFactory` / `IServiceScope` for the
  per-query child scope — the idiomatic .NET mechanism that also disposes Transient
  disposables it resolves.
- No new external dependency. No reflection beyond what `PipelineBuilder` already uses.

### Implementation Approach

Following Tidy First, separate structural from behavioural changes:

1. **Structural**: add `IAmALifetime` + `QueryLifetimeScope`; widen the four factory
   interfaces with the lifetime parameter; update *all* implementors (DI, Simple, InMemory,
   testing ports) and `PipelineBuilder` call sites to pass the lifetime — keeping behaviour
   identical (token created and threaded, but DI factory still naive). **Also merge the two DI
   factories into one `ServiceProviderComponentFactory` and wire `BuildQueryProcessor`
   (Decisions 4 & 6)** while still naive — the merge is a pure structural restructuring (no
   behaviour change), so Tidy First places it here, *before* the behavioural step, letting the
   behavioural tests target the final class with no rework. Tests stay green.
2. **Behavioural** (TDD, `/test-first` per AC): make the (already-merged) DI factory
   lifetime-aware — singleton cache + never-dispose (AC1, AC2); scoped/transient via child
   scope (AC3–AC5); concurrency isolation (AC6); failure-path disposal (AC7); decorator parity
   (AC8); default-path regression guard (AC9); sync/async parity (AC10).

*(Note: an earlier draft sequenced the merge as a separate step 3 **after** the behavioural
work. Because the merge is structural, Tidy First requires it in the structural step; it has
been moved into step 1 above. All six numbered Decisions are unchanged.)*

## Consequences

### Positive

- Singleton, Scoped, and Transient lifetimes behave correctly; the two issue-#329 defects
  are fixed (AC1, AC5).
- EF Core `DbContext` (Scoped) works correctly even with the default Singleton
  `QueryProcessor`, removing the need for the blunt "make everything Scoped" workaround.
- Deterministic disposal on success **and** failure paths (FR9), so no leaked scopes.
- No **per-query** mutable state on the shared factory (per-query scope lives on the
  lifetime token), so concurrent pipelines cannot corrupt each other's scopes. The one piece
  of shared mutable state — the cross-query singleton cache — is made concurrency-safe with
  thread-safe get-or-create (Decision 3), satisfying NFR3.
- Sharing one `IAmALifetime` across the single DI factory removes cross-factory scope
  bookkeeping (simpler than Brighter's per-factory `ConcurrentDictionary`).

### Negative

- **Breaking public API change** to the four factory interfaces; every custom factory
  implementor must add the `IAmALifetime` parameter (acceptable for V5).
- A new public type (`IAmALifetime`) enters the core surface area.
- `PipelineBuilder` gains responsibility for the lifetime lifecycle, slightly increasing its
  size.

### Risks and Mitigations

- **Risk: disposal ordering / premature scope disposal** if Release also disposed the child
  scope (Brighter disposes the scope on each Release). *Mitigation*: the scope is owned by
  `IAmALifetime` and disposed once, by `PipelineBuilder.Dispose()`, not per-Release.
- **Risk: scope leak on exception.** *Mitigation*: builder disposal runs in the `using`
  *finally*; AC7 tests the throwing and cancelled paths.
- **Risk: Singleton resolved from root provider keeps the root scope alive / double
  disposal.** *Mitigation*: singletons are cached and never disposed by Darker (B1, AC2);
  their lifetime is the container's.
- **Risk: behavioural drift on the default Transient path.** *Mitigation*: AC9 pins the
  exact create-once-per-query + dispose-after-pipeline invariant as a regression guard.

## Alternatives Considered

- **A. Brighter-literal: factory-owned `ConcurrentDictionary<IAmALifetime, scope>`.**
  Faithful to Brighter, where the factory keeps its own dictionary keyed by lifetime. This
  works cleanly in Brighter because **Brighter has no decorator–handler separation — it
  models the pipeline as handlers only** (decorators are themselves `IHandleRequests`
  resolved by the single handler factory). One factory therefore owns the one scope, and the
  dual-scope problem cannot arise. **Darker keeps a distinct decorator–handler model**
  (`IQueryHandlerFactory` vs. `IQueryHandlerDecoratorFactory`), so a literal port would have
  *two* factories each with its own dictionary, and the handler and its decorators would get
  *different* child scopes — breaking AC4 (a Scoped dependency shared across handler and
  decorator). For now we deliberately preserve Darker's decorator–handler model intact, which
  implies a different solution: hoist the single shared scope onto the per-query
  `IAmALifetime` token (Decision 1) and have one DI factory class serve both roles
  (Decision 4). This is simpler than per-factory dictionaries and satisfies AC4 directly.
  (Collapsing decorators into the handler model, Brighter-style, was considered out of scope:
  it is a far larger change to Darker's public pipeline abstractions.)
- **B. Document "make QueryProcessor Scoped" as the supported pattern.** This is the current
  state; it forces all collaborators to be scoped, is not the default, and does not fix the
  Singleton-disposal defect. Rejected.
- **C. No token; factory creates a child scope per `Create`.** Cannot share a scope across
  handler and decorators (AC4), and gives the factory no deterministic point to dispose the
  scope on the failure path (FR9). Rejected.
- **D. Register an `IDarkerOptions` service so the factory reads `HandlerLifetime` via the
  provider (as Brighter does).** Unnecessary in Darker because `BuildQueryProcessor` already
  closes over `options` (C4); registering a service adds surface area for no benefit.
  Deferred — a general options service may come later but is Out of Scope here.

## References

- Requirements: [specs/008-factory_component_lifetime/requirements.md](../../specs/008-factory_component_lifetime/requirements.md)
- Issue: [#329](https://github.com/BrighterCommand/Darker/issues/329)
- Brighter reference implementation:
  `Paramore.Brighter.Extensions.DependencyInjection/ServiceProviderHandlerFactory.cs`,
  `.../ServiceProviderLifetimeScope.cs`, `Paramore.Brighter/IAmALifetime.cs`
- Related ADRs: [0009](0009-simple-and-inmemory-factory-implementations.md) (Simple/InMemory
  factory doubles affected by the interface change)
