# Tasks — Spec 008: Lifetime-Aware Handler and Decorator Factories

**Linked Issue**: #329 · **Target Release**: V5 · **Branch**: `329-factory-component-lifetime`
**Design**: [docs/adr/0014-factory-component-lifetime.md](../../docs/adr/0014-factory-component-lifetime.md) (Accepted)
**Requirements**: [requirements.md](requirements.md) (FR1–FR9, NFR1–NFR4, AC1–AC10, baseline B1–B3)

## How to work this list (MANDATORY)

- **Tidy First** — Phase 1 is *structural only* (no behaviour change; suite stays green). Each
  structural task is its own commit. Use `/tidy-first` where it helps. **Do not** mix a
  structural change into a behavioural commit.
- **TDD** — Every Phase 2 task is a `TEST + IMPLEMENT` task. You **MUST** run the listed
  `/test-first` command, then **STOP and wait for the user to approve the test in their IDE**
  before writing any implementation. Do **not** hand-write the test and proceed.
- **Build/test after every task**: `dotnet build Darker.Filter.slnf -c Release` then
  `dotnet test Darker.Filter.slnf -c Release`.
- **No scope creep** — do not change the default `HandlerLifetime` (`Transient`) or
  `QueryProcessorLifetime` (`Singleton`); add no general `IDarkerOptions` service; non-DI
  factories ignore the token (behaviour unchanged).
- **Sync + async parity (AC10)** is woven into the relevant tasks: each behavioural test below
  includes **both** an `Execute` and an `ExecuteAsync` variant. Task B9 is a final parity audit.

### Where things live

- Core (no DI dependency): `src/Paramore.Darker/`
- DI package: `src/Paramore.Darker.Extensions.DependencyInjection/`
- DI behavioural tests + the `ITrackedDependency` probe: `test/Paramore.Darker.Extensions.Tests/`
  (it references the DI package; the core test project does not).
- Test doubles: `test/Paramore.Darker.Extensions.Tests/TestDoubles/`.

---

## Phase 1 — Structural (Tidy First, no behaviour change)

> Goal: introduce the lifetime role, widen the four factory interfaces, update **all**
> implementors and `PipelineBuilder` to create + thread + dispose the lifetime, and put the DI
> factory into its final single-class shape — all while keeping behaviour byte-for-byte
> identical (the token is threaded but the DI factory is still naive and the lifetime owns no
> scope yet). The full suite must stay green after each task.

- [x] **S1 — Add the `IAmALifetime` role and its default `QueryLifetimeScope` implementation**
  - New file `src/Paramore.Darker/IAmALifetime.cs`: public `interface IAmALifetime : IDisposable`
    with `void Add(IDisposable disposable);` (ADR Decision 1; copy the XML doc from the ADR).
  - New file `src/Paramore.Darker/QueryLifetimeScope.cs`: public/internal class implementing
    `IAmALifetime` that holds tracked disposables and, on `Dispose()`, disposes them **in reverse
    order, exactly once** (idempotent). No dependency on `Microsoft.Extensions.DependencyInjection`.
  - Include the MIT licence header block (match `SimpleHandlerFactory.cs`).
  - No call sites yet → suite stays green.

- [x] **S2 — Widen the four public factory interfaces with an `IAmALifetime` parameter** (ADR Decision 2)
  - `src/Paramore.Darker/IQueryHandlerFactory.cs`:
    `IQueryHandler Create(Type handlerType, IAmALifetime lifetime);` and
    `void Release(IQueryHandler handler, IAmALifetime lifetime);`
  - `src/Paramore.Darker/IQueryHandlerFactoryAsync.cs`: same two signatures.
  - `src/Paramore.Darker/IQueryHandlerDecoratorFactory.cs`:
    `T Create<T>(Type decoratorType, IAmALifetime lifetime) where T : IQueryHandlerDecorator;` and
    `void Release<T>(T handler, IAmALifetime lifetime) where T : IQueryHandlerDecorator;`
  - `src/Paramore.Darker/IQueryHandlerDecoratorFactoryAsync.cs`: same two signatures.
  - This won't compile until S3/S4 update the implementors and call sites — land S2+S3+S4 together
    as one structural commit, or in immediate succession, so the build is green at the commit.

- [x] **S3 — Update every factory implementor to the new signatures (token ignored)**
  - Core production doubles (ignore the token; behaviour unchanged):
    - `src/Paramore.Darker/SimpleHandlerFactory.cs`
    - `src/Paramore.Darker/SimpleHandlerDecoratorFactory.cs`
    - `src/Paramore.Darker/Builder/FactoryFuncWrapper.cs` (explicit-interface impls)
  - Test doubles (ignore the token; preserve the recording behaviour):
    - `test/Paramore.Darker.Core.Tests/TestDoubles/RecordingHandlerFactory.cs`
    - `test/Paramore.Darker.Core.Tests/TestDoubles/RecordingDecoratorFactory.cs`
  - The DI factories are handled by S5 (merge). For *this* task, update the two existing DI
    classes (`ServiceProviderHandlerFactory`, `ServiceProviderHandlerDecoratorFactory`) to the new
    signatures, still ignoring the token, so the solution compiles green before the merge.
  - ADR Decision 6 (non-DI factories ignore the token). No behaviour change.

- [x] **S4 — `PipelineBuilder` creates, threads, and disposes the lifetime**
  - `src/Paramore.Darker/PipelineBuilder.cs`:
    - Add a `private IAmALifetime _lifetime;` field; create one `QueryLifetimeScope` at the
      **start** of `Build` (`:49`) and `BuildAsync` (`:95`), **before** any `Create` call (ADR
      Decision 5 — create-first makes partial builds safe).
    - Pass `_lifetime` to every `Create`: `ResolveHandler` (`:172`), `ResolveHandlerAsync`
      (`:191`), `GetDecorators` (`:217`), `GetDecoratorsAsync` (`:253`).
    - Pass `_lifetime` to every `Release` in `Dispose()` (`:274`, `:280`, `:288`); keep the
      existing null-safe `?.` pattern. Then dispose `_lifetime` (`_lifetime?.Dispose();`) **after**
      releasing the handler/decorators.
    - Preserve the existing quirk: the async pipeline *creates* via the async factory but
      *releases* the handler via the **sync** factory slot at `:274` — keep that, just add the
      lifetime arg.
  - Behaviour identical: the lifetime owns no scope yet, so disposing it is a no-op. Suite green.

- [x] **S5 — Merge the two DI factories into `ServiceProviderComponentFactory` (still naive)**
  - ADR Decision 4. New file
    `src/Paramore.Darker.Extensions.DependencyInjection/ServiceProviderComponentFactory.cs`:
    `internal sealed class ServiceProviderComponentFactory : IQueryHandlerFactory,
    IQueryHandlerFactoryAsync, IQueryHandlerDecoratorFactory, IQueryHandlerDecoratorFactoryAsync`.
    - Ctor takes `(IServiceProvider serviceProvider, ServiceLifetime handlerLifetime)` (ADR
      Decision 6 — pass the lifetime in; store both). The lifetime is **not used yet** (naive
      create/dispose-all, identical to today) — this keeps S5 structural.
  - Add skeleton `src/Paramore.Darker.Extensions.DependencyInjection/ServiceProviderLifetimeScope.cs`
    (internal) only if needed as a placeholder; otherwise defer its creation to Phase 2 where the
    behavioural tests drive it. (Prefer: defer — don't add dead code.)
  - Delete `ServiceProviderHandlerFactory.cs` and `ServiceProviderHandlerDecoratorFactory.cs`.
  - `ServiceCollectionExtensions.BuildQueryProcessor` (`:42-50`): construct **one**
    `ServiceProviderComponentFactory(provider, options.HandlerLifetime)` and pass that **single
    instance** into all four `HandlerConfiguration` slots (handler + decorator, sync + async).
  - Behaviour identical (still naive). Suite green. **This refines the ADR's suggested
    step-3-merge into the structural phase so behavioural tests target the final class with no
    rework — Tidy First (structural before behavioural); all six locked Decisions are honoured.**

---

## Phase 2 — Behavioural (TDD, `/test-first` per AC)

> Goal: make `ServiceProviderComponentFactory` honour the configured `HandlerLifetime` via a
> per-query child `IServiceScope` owned by the `IAmALifetime`, with a thread-safe singleton
> cache. Each task below drives the next slice of that logic. **STOP for IDE approval after each
> test.** All identity/disposal assertions use the `ITrackedDependency` probe.

- [x] **B0 — Test infrastructure: the `ITrackedDependency` probe and tracked components**
  - *Structural test-support, not a behaviour change — no `/test-first` gate, but write no
    production code.* Create in `test/Paramore.Darker.Extensions.Tests/TestDoubles/`:
    - `ITrackedDependency` + `TrackedDependency` — increments a shared/injected construction
      counter in its ctor and sets `IsDisposed = true` in `Dispose()` (requirements
      "Key Terms and Observability").
    - A query + handler that take `ITrackedDependency` via constructor injection
      (`TrackedQuery` / `TrackedQueryHandler`), and a decorator that takes `ITrackedDependency`
      via constructor injection (`TrackedDecorator` + its attribute) for the decorator ACs.
    - A small helper to build a real `IServiceCollection` + `AddDarker(o => o.HandlerLifetime =
      …)` and resolve `IQueryProcessor`, so AC tests use the real DI container (Real > Simple >
      InMemory > Mock).
  - Counter must support concurrency (AC6) — use `Interlocked`/per-test fresh instance.

- [x] **B1 — TEST + IMPLEMENT: Transient handler dependency is created fresh and disposed after the pipeline (AC3 / FR1, FR2)**
  - **USE COMMAND**: `/test-first when handler lifetime is transient should create fresh dependency per query and dispose it after pipeline completes`
  - Test location: `test/Paramore.Darker.Extensions.Tests`
  - Test file: `When_handler_lifetime_is_transient_should_create_fresh_and_dispose_after_pipeline.cs`
  - Test should verify (both `Execute` **and** `ExecuteAsync`):
    - With `HandlerLifetime = Transient`, executing the query twice increments the construction
      counter by 1 each time (fresh per query).
    - The injected `ITrackedDependency.IsDisposed == true` after each `Execute`/`ExecuteAsync` returns.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add internal `ServiceProviderLifetimeScope` (ADR Decision 3) in the DI package: resolves
      Transient from a per-query child `IServiceScope` created via the captured provider's
      `IServiceScopeFactory`.
    - In `ServiceProviderComponentFactory.Create`, when `handlerLifetime == Transient`, create (or
      reuse, within this execution) the child scope, attach it to the `IAmALifetime` via
      `lifetime.Add(scope)` on first use, and resolve from the scope.
    - `Release` becomes a no-op for the scope (scope teardown is owned by
      `PipelineBuilder.Dispose()` → `IAmALifetime.Dispose()` → `IServiceScope.Dispose()`); keep
      `Release` null-tolerant on both component and lifetime (ADR Decision 5).

- [x] **B2 — TEST + IMPLEMENT: Singleton handler dependency is reused across queries and never disposed by Darker (AC1 + AC2 / FR3, FR2)**
  - **Outcome (acceptance lock, no production change):** the test passes green against the
    current branch with **no new production code**. Defect 1 was already neutralised
    *structurally* in **S5** (`a4ec2d6`): the merged `ServiceProviderComponentFactory.Release`
    is a **no-op**, and a `HandlerLifetime = Singleton` handler is a container-managed singleton
    (`ServiceCollectionHandlerRegistry.cs:20`), so `GetService` returns the same instance and
    nothing disposes it. The ADR-mandated thread-safe singleton **cache** (Decision 3) produces
    **no observable behavioural difference** for a container-managed singleton — the ADR itself
    states this in **Decision 4 §2** ("Singleton sharing needs no merge — MS DI already
    guarantees it… there is no double-construction to fix"). Per the mandatory TDD rule (no code
    without a red test) and "no scope creep", the cache was **not** added; the B2 test stands as
    the **AC1/AC2 acceptance lock** — exactly the pattern tasks.md anticipates for B4/B5/B7/B8.
    User approved this approach. Test-only commit.
  - **USE COMMAND**: `/test-first when handler lifetime is singleton should reuse same dependency across queries and not dispose it`
  - Test location: `test/Paramore.Darker.Extensions.Tests`
  - Test file: `When_handler_lifetime_is_singleton_should_reuse_dependency_and_not_dispose.cs`
  - Test should verify (both `Execute` **and** `ExecuteAsync`):
    - With `HandlerLifetime = Singleton`, executing the query twice keeps the construction counter
      at **1** and the injected `ITrackedDependency` is `ReferenceEquals` across both queries.
    - The second execution succeeds with **no `ObjectDisposedException`** (defect 1 fixed).
    - `ITrackedDependency.IsDisposed == false` after the pipeline completes.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - In `ServiceProviderLifetimeScope`/`ServiceProviderComponentFactory`, resolve Singleton from
      the **captured provider** and cache it in a **thread-safe** get-or-create store on the
      factory (`ConcurrentDictionary<Type, …>` with `Lazy<>`), shared across queries (NFR3).
    - Singletons are **never** disposed by `Release` (B1/AC2).

- [x] **B3 — TEST + IMPLEMENT: Scoped dependency is shared across handler and decorator in one execution and disposed after (AC4 / FR4)**
  - **Outcome:** red→green. Red reason: `Resolve` created a *new* child scope per `Create`, so
    handler and decorator got distinct scoped instances (`ConstructionCount == 2`). Fix:
    `ServiceProviderComponentFactory` now get-or-creates **one** `ServiceProviderLifetimeScope`
    per execution, keyed on the `IAmALifetime` token via a
    `ConditionalWeakTable<IAmALifetime, ServiceProviderLifetimeScope>` (weak keys, no per-query
    mutable field on the shared factory; scope attached to the token via `Add` and disposed once
    by `PipelineBuilder.Dispose()`). Singleton branch unchanged. Both `Execute` and `ExecuteAsync`.
  - **USE COMMAND**: `/test-first when handler lifetime is scoped should share one scoped dependency across handler and decorator then dispose after pipeline`
  - Test location: `test/Paramore.Darker.Extensions.Tests`
  - Test file: `When_handler_lifetime_is_scoped_should_share_dependency_across_handler_and_decorator.cs`
  - Test should verify (both `Execute` **and** `ExecuteAsync`):
    - `HandlerLifetime = Scoped`, with `ITrackedDependency` registered **Scoped** and injected into
      **both** the handler and a decorator on the same query: both receive the **same** reference
      (counter increments by exactly 1 for that execution).
    - `ITrackedDependency.IsDisposed == true` after the pipeline completes.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Resolve Scoped from the **one** per-query child scope held on the `IAmALifetime` (created
      once per execution, on first scoped/transient resolution).
    - The shared single factory instance + the scope-on-the-lifetime guarantee handler and
      decorator read the **same** scope (ADR Decision 4 §1).

- [x] **B4 — TEST + IMPLEMENT: Scoped dependency resolves and disposes under a Singleton QueryProcessor (AC5 / FR5) — fixes defect 2**
  - **Outcome (acceptance lock, no production change):** green on this branch — the
    `IServiceScopeFactory`-rooted child scope from **B1** already fixes defect 2. Verified the
    lock has **teeth**: temporarily reverting `Resolve`'s non-Singleton branch to master-like root
    resolution (`_serviceProvider.GetService(...)`) makes both variants fail with
    `InvalidOperationException: Cannot resolve scoped service ... from root provider` under
    `ValidateScopes = true`; restoring the child-scope resolution makes them pass. So the test is a
    genuine AC5/FR5 acceptance lock (red-on-master, green-now), not a no-op. Test-only commit.
  - **USE COMMAND**: `/test-first when query processor is singleton and dependency is scoped should resolve from per-query scope and dispose after pipeline`
  - Test location: `test/Paramore.Darker.Extensions.Tests`
  - Test file: `When_query_processor_is_singleton_and_dependency_is_scoped_should_resolve_and_dispose.cs`
  - Test should verify (both `Execute` **and** `ExecuteAsync`):
    - `QueryProcessorLifetime = Singleton` (the default) with a **Scoped** `ITrackedDependency`
      (representing an EF Core `DbContext`) consumed by the handler: resolution **succeeds** (no
      "Cannot resolve scoped service from root provider").
    - `ITrackedDependency.IsDisposed == true` after the pipeline completes.
    - Build the container with **scope validation on** (`ValidateScopes = true`) to prove the
      root-provider defect is gone.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Ensure the child scope is created from the captured provider's `IServiceScopeFactory`, which
      yields a correctly-rooted scope even when the captured provider is the **root** (ADR
      Decision 3). **Author the test red-first:** build the container with `ValidateScopes = true`
      so that, on the pre-change (master) code, resolving the Scoped dependency throws "Cannot
      resolve scoped service from root provider" — the test fails on master and passes only once
      Scoped/Transient components resolve from the per-query child scope. If B1's child-scope branch
      already satisfies this scenario, no further production code is needed and the test stands as
      the acceptance lock for AC5/FR5 (defect 2) — do not delete it; if a gap remains, it drives the fix.

- [x] **B5 — TEST + IMPLEMENT: Concurrent pipelines get isolated scopes and don't dispose each other (AC6 / FR6, NFR3)**
  - **Outcome (acceptance lock, no production change):** green on this branch — each execution owns
    its own `IAmALifetime` (created per `Build`/`BuildAsync`) and thus its own child scope via the
    `ConditionalWeakTable`, so two concurrent pipelines are isolated. New barrier-aware test double
    `ConcurrentTrackedQuery` (records its scoped dependency, signals `Started`, then parks on a
    release gate) holds both pipelines provably in flight; the test asserts distinct scoped
    dependencies and that completing A disposes A's scope while B's stays alive. Verified **teeth**:
    temporarily resolving the non-Singleton branch from the captured root provider makes both
    variants fail (the two queries share one root-scoped dependency → isolation breaks); the
    child-scope resolution passes. Test + test-infra only.
  - **USE COMMAND**: `/test-first when two queries run concurrently through one singleton processor should use isolated scopes and not dispose each others dependencies`
  - Test location: `test/Paramore.Darker.Extensions.Tests`
  - Test file: `When_two_queries_run_concurrently_should_isolate_scopes.cs`
  - Test should verify (both `Execute` **and** `ExecuteAsync`):
    - Two queries started concurrently through **one** shared Singleton `QueryProcessor`; each
      handler records its injected **Scoped** `ITrackedDependency` then blocks on a shared barrier
      (`TaskCompletionSource`/`Barrier`) so both pipelines are provably in flight.
    - The two dependencies are **distinct** references (distinct scopes).
    - After the barrier releases and pipeline A completes: A's dependency `IsDisposed == true`
      while B's is still `false` (neither disposes the other's scope).
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Per-query scope lives on the `IAmALifetime` (no per-query mutable field on the shared factory)
      and the singleton cache uses thread-safe get-or-create (NFR3). **Author the test red-first:**
      against the pre-change (master) code there are no per-query child scopes, so two concurrent
      pipelines cannot obtain **distinct** scoped dependencies — the test fails on master. It passes
      once each execution owns an isolated child scope. If B1's child-scope branch already provides
      isolation, no further production code is needed and the test stands as the acceptance lock for
      AC6/FR6/NFR3; if the singleton cache is not thread-safe, this test (or a focused variant)
      drives that fix.

- [x] **B6 — TEST + IMPLEMENT: Per-query scope is disposed on the failure path — throw and cancellation (AC7 / FR9)**
  - **Outcome (acceptance lock, no production change):** green on this branch — `PipelineBuilder` is
    disposed in `QueryProcessor`'s `using` *finally* (S4), so the per-query lifetime (and its child
    scope) is disposed even when the handler throws or the async pipeline is cancelled. New doubles
    `ThrowingTrackedQuery` (sync + async handlers that record the dependency then throw) and
    `CancellingTrackedQuery` (async handler that records then observes a cancelled token). `[Theory]`
    over `Transient` **and** `Scoped`; covers throw-sync, throw-async, cancel-async (6 cases ×
    2 TFMs). Verified **teeth**: master-like root resolution leaves the dependency undisposed on the
    failure path → all 6 fail; child-scope resolution passes. Test + test-infra only.
  - **USE COMMAND**: `/test-first when handler throws or async is cancelled should still dispose the per-query dependency`
  - Test location: `test/Paramore.Darker.Extensions.Tests`
  - Test file: `When_pipeline_fails_should_dispose_dependency.cs`
  - Test should verify (cover `HandlerLifetime = Transient` **and** `Scoped`):
    - A handler that **throws**: after `Execute`/`ExecuteAsync` propagates the exception, the
      injected `ITrackedDependency.IsDisposed == true`.
    - A cancelled `ExecuteAsync` (`OperationCanceledException`): same disposal guarantee.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Rely on `PipelineBuilder` being disposed in `QueryProcessor`'s `using` *finally*
      (`QueryProcessor.cs:49-71` sync, `:75-99` async) → `IAmALifetime.Dispose()` →
      `IServiceScope.Dispose()`. Confirm `PipelineBuilder.Dispose()` disposes `_lifetime` even when
      `Build`/`BuildAsync` threw mid-way (partial build — lifetime created first, S4). Add code
      only if a leak is found.

- [x] **B7 — TEST + IMPLEMENT: Decorators follow the same lifetime rules as handlers (AC8 / FR7, FR8)**
  - **Outcome (acceptance lock, no production change):** green on this branch — the single merged
    `ServiceProviderComponentFactory` applies identical logic to the decorator `Create<T>`/`Release<T>`
    paths it does for handlers, so B1's per-query child-scope disposal and S5's no-op `Release` already
    cover decorators. Test asserts on the **decorator's** injected `ITrackedDependency` (via
    `DecoratedTrackedQuery`/`TrackedDecorator`): Singleton → constructed once, same instance reused,
    `IsDisposed == false` (mirrors B2/AC1+AC2); Transient → fresh per query, `IsDisposed == true`
    (mirrors B1/AC3). 4 methods (Singleton + Transient × sync + async), 8 cases × 2 TFMs. The
    task-text-predicted `ObjectDisposedException` on the Singleton case does **not** occur here (S5
    neutralised that defect). Verified **teeth** on the Transient case: master-like root resolution
    (`return _serviceProvider.GetService(componentType);` as first line of `Resolve`) leaves the
    decorator dependency undisposed → both Transient tests fail; restored → all 4 green. The Singleton
    guard is the no-op `Release` (teeth-verified in B2; same factory serves decorators). Test-only.
  - **USE COMMAND**: `/test-first when decorator has a configured lifetime should create reuse and dispose its dependency like a handler`
  - Test location: `test/Paramore.Darker.Extensions.Tests`
  - Test file: `When_decorator_lifetime_configured_should_follow_same_rules_as_handler.cs`
  - Test should verify (both `Execute` **and** `ExecuteAsync`), for a **decorator** with an
    injected `ITrackedDependency`:
    - Singleton: reused across queries, `IsDisposed == false` (mirrors B2/AC1+AC2).
    - Transient: fresh per query, `IsDisposed == true` after (mirrors B1/AC3).
    - (Scoped sharing across handler+decorator is already covered by B3/AC4.)
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - The single `ServiceProviderComponentFactory` applies identical logic to the decorator
      `Create<T>`/`Release<T>` paths (one cache, one scope-on-lifetime). **Author the test
      red-first:** against the pre-change (master) code a Singleton decorator is disposed on first
      release, so the second query throws `ObjectDisposedException` — the test fails on master. It
      passes once the decorator paths honour the lifetime. If the shared factory logic already
      covers decorators, no further production code is needed and the test stands as the acceptance
      lock for AC8/FR7/FR8; if a decorator path diverges, this test drives the fix.

- [x] **B8 — TEST + IMPLEMENT: Default-path guard — no lifetime configured creates fresh per query and deterministically disposes the dependency (AC9 / NFR2)**
  - **Outcome (acceptance lock, no production change):** green on this branch — the default-config
    wiring routes through B1's per-query child-scope disposal. Added test-infra
    `TrackedDependencyScenario.BuildWithDefaultLifetime(dependencyLifetime)` which calls `AddDarker()`
    **without** setting `HandlerLifetime`, so it exercises the *default* value (`Transient`,
    `DarkerOptions.cs:8`) rather than an explicit setting; extracted a private `BuildCore(configure, …)`
    so both builders share registration. Default Singleton `QueryProcessor` unchanged. Test asserts
    (sync + async) `ConstructionCount == 2` over two queries (fresh per query) and `IsDisposed == true`
    after each pipeline — the AC3 invariant through the default path. Verified **teeth**: master-like
    root resolution leaves the dependency undisposed → both tests fail; restored → green. Unlike B7's
    Singleton case, the default path *is* the Transient branch, so the teeth bite directly. Test +
    test-infra only.
  - **USE COMMAND**: `/test-first when no lifetime configured should create dependency once per query and dispose it after pipeline`
  - Test location: `test/Paramore.Darker.Extensions.Tests`
  - Test file: `When_no_lifetime_configured_should_create_once_per_query_and_dispose.cs`
  - Test should verify (both `Execute` **and** `ExecuteAsync`):
    - With **no** lifetime configured (default `Transient`, default Singleton `QueryProcessor`):
      the injected `ITrackedDependency` construction counter increments by **1 per query** (fresh
      per query) and `IsDisposed == true` after the pipeline completes — the same assertion as AC3,
      exercised through the **default configuration path** rather than an explicit `Transient` setting.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Confirm the default-config wiring routes through the same child-scope-for-Transient logic
      added in B1. **This test is NOT a no-op against master:** the pre-change implementation
      resolves the handler (and its injected transient dependency) from the **root** provider and
      disposes only the handler object on release — the injected transient dependency is retained
      until the root provider is disposed, so `IsDisposed` is **false** after a query there. The
      test therefore fails on master and passes only after B1's per-query child-scope disposal. It
      pins AC9/NFR2: the **preserved** invariant is fresh-per-query construction + handler disposal;
      the **intended change** is that injected transient disposables are now disposed
      deterministically per-query (FR4/B3). Do not change the default lifetime; add code only if the
      default-config path bypasses B1's logic.

> **Removed (was B9) — "Singleton dependency shared across handler and decorator is constructed
> once".** Dropped after round-2 review. A type registered `Singleton` is **container-managed** by
> MS DI: it is constructed once per container and the same instance is injected into every consumer,
> regardless of how many Darker factory caches exist or which provider resolves the consumer. The
> factory's singleton cache holds the **component** (handler/decorator object), not its injected
> dependencies — so a shared Singleton dependency resolves to one instance **with or without** the
> merge. There is no double-construction to drive out, so this would have been a tautological
> (always-green) test, not a red→green TDD task. Singleton coverage that *is* meaningful and
> red-on-master already exists: **B2** (Singleton handler reused/never-disposed) and **B7**
> (Singleton decorator reused/never-disposed). The merge (S5) is justified structurally (one cache,
> no duplicated logic), not as a singleton-correctness fix — see ADR Decision 4 (corrected).

- [x] **B9 — Sync/async parity audit (AC10 / FR7)**
  - **Outcome (audit, no new behaviour, no gaps found):** every behavioural test file pairs a
    `void` (`Execute`) variant with an `async Task` (`ExecuteAsync`) variant —
    B1 transient, B2 singleton, B3 scoped-sharing, B4 scoped-under-singleton-processor,
    B5 concurrent-isolation, B7 decorator (singleton + transient), B8 default-path all have both.
    B6 (failure path) covers throw-sync, throw-async **and** cancel-async; sync cancellation has no
    `CancellationToken` on the sync path, so async-only cancellation is the complete coverage (noted
    in B6's Outcome). No missing variant ⇒ no `/test-first` task needed. AC→test map below filled.
  - *Verification task, not new behaviour.* Confirm every behavioural test (B1–B8) exercises
    **both** `Execute` and `ExecuteAsync`. Add any missing async/sync variant as a `/test-first`
    task (STOP for approval) if a gap is found. Document the AC→test mapping at the bottom of this
    file once complete.

---

## Phase 3 — Finalisation

- [ ] **F1 — Full build and test on CI target frameworks**
  - `dotnet build Darker.Filter.slnf -c Release` and `dotnet test Darker.Filter.slnf -c Release`
    (net8.0 + net9.0). All green.

- [ ] **F2 — Coverage audit against NFR4 and the AC map**
  - Verify all three lifetimes × {handlers, decorators} × {sync, async} are covered, plus the
    singleton-reuse (AC1) and scoped-dependency (AC5) scenarios. Fill the mapping table below.

- [ ] **F3 — Record acceptance verification**
  - Add an `acceptance.md` (or equivalent) noting each AC1–AC10 → passing test file, mirroring how
    spec 007 recorded AC verification. Update issue #329 with a validation comment.

- [ ] **F4 — Housekeeping**
  - Delete `PROMPT.md` (working file) before merge. Do **not** commit `docs/.DS_Store`.
  - Ensure each commit is either purely structural or purely behavioural (Tidy First).

---

## Task dependencies

```
S1 ─► S2 ─► S3 ─► S4 ─► S5          (structural, in order; S2+S3+S4 land green together)
                         │            (S5 = merge into ServiceProviderComponentFactory, still naive)
                         ▼
B0 (probe) ─► B1 ─► B2 ─► B3         (B3 depends on S5 — cross-role sharing via one merged factory)
                         │
                         └─► B4, B5, B6, B7, B8   (depend on B1/B2 mechanics)
                                       │
B9 (parity audit) depends on B1–B8 ─► F1 ─► F2 ─► F3 ─► F4
```

- **B1 and B2 build the two core branches** — the per-query child-scope branch for Scoped/Transient
  components (B1) and the thread-safe, never-disposed singleton cache (B2); B3 adds cross-role
  scoped sharing. **B3–B8 are behavioural acceptance tests, each authored test-first and
  IDE-reviewed, that lock a *distinct observable scenario*** (cross-role scoped sharing,
  `ValidateScopes` under a Singleton processor, concurrency isolation, failure-path disposal,
  decorator parity, default-path). Each is written to be **red against the pre-change (master)
  baseline**: where the mechanism from B1/B2 already satisfies a scenario, the test stands as that
  AC's acceptance lock (it is not deleted); where a gap remains, it drives the fix. None is a
  tautological no-op pin. **B9** is a final sync/async parity audit, not a new behaviour.

## Risk mitigation (from ADR "Risks and Mitigations")

- **Premature scope disposal** (Release disposing the scope) → scope owned by `IAmALifetime`,
  disposed once by `PipelineBuilder.Dispose()`, never per-`Release`. Pinned by B3/B5.
- **Scope leak on exception** → builder disposal in the `using` *finally*; pinned by **B6 (AC7)**.
- **Singleton resolved from root keeps root alive / double dispose** → singletons cached, never
  disposed by Darker; pinned by **B2 (AC2)**.
- **Behavioural drift on the default Transient path** → pinned by **B8 (AC9)** regression guard.
- **Concurrency corruption of shared factory state** → no per-query mutable factory field;
  thread-safe singleton cache; pinned by **B5 (AC6)**.

## AC → test mapping (fill during Phase 3)

| AC | Requirement | Task | Test file | Status |
|----|-------------|------|-----------|--------|
| AC1 | FR3 | B2 | `When_handler_lifetime_is_singleton_should_reuse_dependency_and_not_dispose.cs` | ✅ |
| AC2 | FR2/FR3 | B2 | ↑ | ✅ |
| AC3 | FR1/FR2 | B1 | `When_handler_lifetime_is_transient_should_create_fresh_and_dispose_after_pipeline.cs` | ✅ |
| AC4 | FR4 | B3 | `When_handler_lifetime_is_scoped_should_share_dependency_across_handler_and_decorator.cs` | ✅ |
| AC5 | FR5 | B4 | `When_query_processor_is_singleton_and_dependency_is_scoped_should_resolve_and_dispose.cs` | ✅ |
| AC6 | FR6/NFR3 | B5 | `When_two_queries_run_concurrently_should_isolate_scopes.cs` | ✅ |
| AC7 | FR9 | B6 | `When_pipeline_fails_should_dispose_dependency.cs` | ✅ |
| AC8 | FR7/FR8 | B7 | `When_decorator_lifetime_configured_should_follow_same_rules_as_handler.cs` | ✅ |
| AC9 | NFR2 | B8 | `When_no_lifetime_configured_should_create_once_per_query_and_dispose.cs` | ✅ |
| AC10 | FR7 | B9 | (sync+async variants across B1–B8) | ✅ |
