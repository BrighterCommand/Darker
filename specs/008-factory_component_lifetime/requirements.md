# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: #329
**Target Release**: V5 (breaking changes to public factory interfaces are permitted)

## Problem Statement

As an application developer using Darker with `Microsoft.Extensions.DependencyInjection`,
I would like my query handlers and decorators — and the dependencies they consume —
to be created and released according to the lifetime I configured (Singleton, Scoped,
or Transient), so that I can safely use scoped resources such as an EF Core `DbContext`
inside a handler running in an ASP.NET Core request pipeline without leaks, premature
disposal, or cross-request sharing.

Today, Darker's DI factories ignore the configured lifetime. Although
`DarkerOptions.HandlerLifetime` is captured at registration and applied to the
`ServiceDescriptor`, the factories themselves do not act on it. This produces two
concrete, reproducible defects:

1. **Singleton handlers/decorators are disposed after a single query.**
   `ServiceProviderHandlerFactory.Release` (and the decorator equivalent) disposes any
   `IDisposable` it is handed. With `HandlerLifetime = Singleton`, the shared instance is
   disposed after the first query, and the next query resolves a disposed instance,
   throwing `ObjectDisposedException`.

2. **Scoped dependencies resolve from the wrong scope.**
   With the default `QueryProcessorLifetime = Singleton`, .NET invokes the
   `QueryProcessor` factory delegate with the **root** service provider, which the
   handler/decorator factories then capture. Scoped dependencies (e.g. EF Core
   `DbContext`) therefore resolve against the root scope — either throwing under scope
   validation, or returning an instance not bound to the current request. The only
   current mitigation is the blunt instrument of forcing the entire `QueryProcessor` to
   be Scoped, which is not the default and forces all collaborators to be scoped too.

Brighter already solves this; Darker should reach equivalent correctness.

## Proposed Solution

Make the DI-backed handler and decorator factories lifetime-aware so that, for each
query executed:

- **Singleton** components are created once, reused across queries, and **not** disposed
  by Darker on release (the container owns their lifetime).
- **Scoped** components are resolved from a child scope created for that query's pipeline,
  shared within that single pipeline execution, and disposed when the pipeline completes.
- **Transient** components are created fresh and disposed when the pipeline completes.

A handler's (or decorator's) scoped dependencies must resolve from the same per-query
child scope, so that an EF Core `DbContext` registered as Scoped behaves correctly even
when the `QueryProcessor` itself is a Singleton.

From the developer's perspective the experience is: "I register my handler with the
lifetime I want via `AddDarker(o => o.HandlerLifetime = ...)` and Darker honours it."

## Key Terms and Observability

To keep the acceptance criteria unambiguous and directly testable, the following terms
have a fixed, observable meaning throughout this document:

- **"Same instance" / "shared instance"** means **reference equality** (`ReferenceEquals`)
  of the resolved object, corroborated by a **construction counter**. The canonical probe
  is a test dependency, `ITrackedDependency`, registered into the container, that
  increments a static/shared counter in its constructor and exposes an `IsDisposed` flag
  set in `Dispose()`. Handlers and decorators receive this dependency via constructor
  injection. Identity and disposal are asserted on **that injected dependency**, not on
  the handler/decorator object itself — because a handler and its decorators are distinct
  types and are never literally "the same object." This avoids any reliance on whether a
  resolved handler is wrapped or proxied.
- **"Resolve from the same scope"** means: a dependency registered as **Scoped** and
  injected into two different components (e.g. the handler and a decorator) in the **same**
  pipeline execution yields the **same** `ITrackedDependency` reference (counter
  incremented exactly once for that pipeline).
- **"Disposed when the pipeline completes"** means `Dispose()` has been observed on the
  tracked dependency / component by the time control returns from
  `QueryProcessor.Execute` / `ExecuteAsync` for that query — including when that call
  returns by **throwing** (see FR9).
- **"Pipeline execution"** is one call to `QueryProcessor.Execute` / `ExecuteAsync`, whose
  per-query boundary is the `PipelineBuilder<TResult>` created and disposed for that call.

### Brighter Baseline Semantics (the parity target)

"Parity with Brighter" in this document means matching the following specific, observable
behaviours (derived from Brighter's `ServiceProviderHandlerFactory` and
`ServiceProviderLifetimeScope`). These are the assertions to satisfy; the Brighter source
files in *Additional Context* are provided only as the reference implementation, not as a
moving definition:

- **B1 — Singleton:** one instance per type for the lifetime of the application, returned
  on every request; **never disposed** by the factory on release.
- **B2 — Scoped:** one child scope per pipeline execution; all Scoped resolutions within
  that execution share the scope; the scope (and its disposables) is disposed when the
  execution completes.
- **B3 — Transient:** a new instance per resolution, created **via a per-execution child
  scope** so that the instance and any disposables it captures are disposed when the
  execution completes (deterministic disposal rather than relying on the root container).
  Note: a Darker pipeline resolves the handler once and each decorator once, so each
  Transient *type* is resolved at most once per pipeline — "per resolution" and
  "per-execution" coincide here, and no AC needs to exercise two resolutions of the same
  Transient type within a single pipeline.

## Requirements

### Functional Requirements

- **FR1 — Honour configured lifetime on creation.** The DI handler factory and decorator
  factory must create components according to the configured `HandlerLifetime`
  (Singleton, Scoped, Transient).
- **FR2 — Honour configured lifetime on release.** Singleton components must not be
  disposed by Darker on release. Scoped and Transient components must be disposed when
  their owning pipeline execution completes.
- **FR3 — Singleton reuse.** A handler/decorator configured as Singleton must return the
  same instance across multiple query executions, and must remain usable for the lifetime
  of the application (no premature disposal).
- **FR4 — Per-query scope.** When the lifetime is Scoped or Transient, components and
  their dependencies must resolve from a child scope created for, and bound to, the
  execution of a single query pipeline. **Transient is deliberately resolved via this
  per-execution child scope (not the root container)** so that a Transient component, and
  any disposables it captures, are disposed deterministically when the execution completes
  (baseline B3). For the default Transient path, the component itself continues to be created
  fresh per query and disposed after use (preserving today's behaviour for the handler/decorator
  object); the change introduced here is that **injected** transient disposables are now disposed
  **deterministically at pipeline completion** via the child scope, whereas today they are
  root-resolved and retained until the root provider is disposed (see NFR2 / AC9).
- **FR5 — Scoped dependency correctness under Singleton processor.** A Scoped dependency
  consumed by a handler/decorator (e.g. EF Core `DbContext`) must resolve and dispose
  correctly even when `QueryProcessorLifetime = Singleton`.
- **FR6 — Concurrency safety.** Because a Singleton `QueryProcessor` (and therefore a
  shared factory) may execute multiple queries concurrently, per-query scopes must be
  isolated; concurrent pipelines must not share or dispose each other's scoped/transient
  instances.
- **FR7 — Apply to both sync and async paths.** The behaviour must apply consistently to
  `IQueryHandlerFactory` / `IQueryHandlerFactoryAsync` and
  `IQueryHandlerDecoratorFactory` / `IQueryHandlerDecoratorFactoryAsync`.
- **FR8 — Apply to handlers and decorators uniformly.** Both handlers and decorators
  follow the same lifetime rules.
- **FR9 — Disposal on the failure path.** Per-query scopes, and Scoped/Transient
  components and their disposable dependencies, must be released/disposed even when the
  pipeline terminates abnormally — i.e. when the handler or a decorator throws, or when
  the operation is cancelled (`OperationCanceledException`). Scope disposal must occur
  regardless of how the pipeline execution ends (e.g. via `try/finally` around execution),
  so a thrown query never leaks an EF Core `DbContext` scope.

### Non-functional Requirements

- **NFR1 — Parity with Brighter.** The resulting behaviour must match the baseline
  semantics B1–B3 defined in *Key Terms and Observability* above (Singleton never disposed;
  Scoped one-scope-per-execution; Transient new-instance-per-resolution disposed via the
  per-execution scope), so users moving between the two libraries get consistent results.
- **NFR2 — No regression on the default path.** Users on the default `Transient` lifetime must
  continue to get a fresh handler/decorator per query that is disposed after use; nothing existing
  is silently dropped. The one deliberate, documented change is an **improvement**: disposal becomes
  deterministic and complete — injected transient disposables consumed by the component are now
  disposed at pipeline completion (via the per-execution child scope, FR4/B3), whereas the current
  implementation root-resolves them and retains them until the root provider is disposed. This is
  pinned by **AC9**: with no lifetime configured, the component is created once per query and the
  injected `ITrackedDependency` is disposed after the pipeline completes.
- **NFR3 — Thread safety.** Scope bookkeeping must be safe under concurrent pipeline
  execution.
- **NFR4 — Test coverage.** All three lifetimes × {handlers, decorators} × {sync, async}
  must be covered, including the singleton-reuse and scoped-dependency scenarios, via the
  project's TDD workflow.

### Constraints and Assumptions

- **C1 — Breaking change permitted (V5).** The public factory interfaces
  (`IQueryHandlerFactory`, `IQueryHandlerFactoryAsync`, `IQueryHandlerDecoratorFactory`,
  `IQueryHandlerDecoratorFactoryAsync`) currently expose `Create`/`Release` with **no**
  per-request lifetime token. Faithfully managing per-query scopes requires a way to
  associate creation and release with a single pipeline execution. Changing these public
  interfaces is acceptable for the V5 release.
- **C2 — `PipelineBuilder` is the natural request boundary.** A `PipelineBuilder<TResult>`
  is already created per query and disposed per query in `QueryProcessor.Execute` /
  `ExecuteAsync`. It is the natural owner of a per-query lifetime token/scope.
- **C3 — Lifetime is configured globally.** As in Brighter, the lifetime is a single
  global setting (`DarkerOptions.HandlerLifetime`), not a per-handler setting.
- **C4 — Darker can pass the lifetime directly.** `BuildQueryProcessor` already closes
  over `options`, so the configured `HandlerLifetime` can be passed straight into the
  factory constructors; registering an `IDarkerOptions` service in the container (as
  Brighter does) is not required.
- **A1 — Assumption.** The relevant scoping scenarios target the
  `Paramore.Darker.Extensions.DependencyInjection` package; non-DI factories
  (Simple/InMemory) are unaffected.

### Out of Scope

- Per-handler / per-decorator lifetime configuration (lifetime remains a single global
  setting).
- Introducing a general `IDarkerOptions` service-registration mechanism for unrelated
  global configuration (e.g. tracing/instrumentation options). That is noted in the issue
  as a separate, later concern.
- Changes to non-DI factories such as `SimpleHandlerFactory`, `InMemoryQueryContextFactory`,
  or the testing ports, beyond what is needed to compile against any changed interface.
- Changing the default `HandlerLifetime` (remains `Transient`) or the default
  `QueryProcessorLifetime` (remains `Singleton`).

## Acceptance Criteria

How we'll know this is working correctly (to be expressed as TDD tests). All identity and
disposal assertions use the `ITrackedDependency` probe and the meanings fixed in *Key
Terms and Observability*.

- **AC1 — Singleton reuse.** Given `HandlerLifetime = Singleton`, executing the same query
  twice resolves the **same** handler instance — asserted by injecting `ITrackedDependency`
  into the handler and observing its construction counter equals **1** across both
  executions (`ReferenceEquals` on the injected dependency across the two queries is true)
  — and the second execution succeeds with no `ObjectDisposedException`.
- **AC2 — Singleton not disposed.** Given a Singleton handler/decorator (and its injected
  `ITrackedDependency`), `IsDisposed` is **false** after a pipeline completes.
- **AC3 — Transient disposed.** Given `HandlerLifetime = Transient`, the handler/decorator
  (and its injected `ITrackedDependency`) is constructed fresh per query (counter
  increments by 1 each execution) and `IsDisposed` is **true** after the pipeline completes.
- **AC4 — Scoped: one scope per execution, shared, disposed after.** Given
  `HandlerLifetime = Scoped` and an `ITrackedDependency` registered as **Scoped** and
  injected into **both** the handler and a decorator in the same pipeline, both receive the
  **same** reference (constructor counter increments by exactly 1 for that execution), and
  `IsDisposed` becomes **true** when the pipeline completes. (This tests "one shared scope
  per execution", not a single shared handler/decorator object.)
- **AC5 — Scoped dependency under Singleton processor.** Given `QueryProcessorLifetime =
  Singleton` and an `ITrackedDependency` registered as **Scoped** (representing an EF Core
  `DbContext`) consumed by a handler, the dependency resolves successfully (no "Cannot
  resolve scoped service from root provider" failure) and `IsDisposed` is **true** after
  the pipeline completes.
- **AC6 — Concurrency isolation (deterministic).** Two queries are started concurrently
  through one shared Singleton `QueryProcessor`; each handler, upon entry, records its
  injected Scoped `ITrackedDependency` and then blocks on a shared barrier
  (`TaskCompletionSource` / `Barrier`) so both pipelines are provably in flight
  simultaneously. The test asserts the two dependencies are **distinct** references
  (distinct scopes). The barrier is then released and pipeline A is allowed to complete;
  the test asserts A's dependency `IsDisposed == true` while B's dependency
  `IsDisposed == false` (neither pipeline disposes the other's scope). The same assertion
  applies with the dependency registered as **Transient**; because both Scoped and
  Transient resolve through the one per-execution child scope, the Scoped case is
  representative of both.
- **AC7 — Failure-path disposal.** Given `HandlerLifetime = Transient` (and separately
  Scoped) and a handler that **throws**, the injected `ITrackedDependency` has
  `IsDisposed == true` once `Execute`/`ExecuteAsync` returns by propagating the exception;
  a second test asserts the same for a cancelled `ExecuteAsync`
  (`OperationCanceledException`). Covers FR9.
- **AC8 — Decorators follow the same rules.** AC1–AC7 hold for decorators as well as
  handlers (using a decorator with an injected `ITrackedDependency`).
- **AC9 — Default-path guard.** With **no** lifetime configured (default `Transient`, default
  Singleton `QueryProcessor`), the invariant pinned is exactly: the injected `ITrackedDependency`
  construction counter increments by **1 per query** (fresh per query) and its `IsDisposed == true`
  after the pipeline completes — the same assertion as AC3. **Note:** fresh-per-query construction
  and disposal of the handler/decorator object are *preserved* from the current implementation; the
  *injected* transient dependency being disposed per-query is the **intended new
  deterministic-disposal behaviour** (FR4/B3), **not** a property of the current implementation
  (which root-resolves and retains transient dependencies until root teardown). AC9 thus guards the
  fresh-per-query + disposal invariant against regression while documenting this deliberate change.
  Pins NFR2.
- **AC10 — Sync and async parity.** AC1–AC9 hold for both the sync (`Execute`) and async
  (`ExecuteAsync`) factory paths.

Each AC maps to at least one functional requirement: AC1→FR3; AC2→FR2/FR3; AC3→FR1/FR2;
AC4→FR4; AC5→FR5; AC6→FR6/NFR3; AC7→FR9; AC8→FR7/FR8; AC9→NFR2; AC10→FR7.

**Definition of done:** All acceptance criteria are covered by passing tests written via
the `/test-first` TDD workflow; the build passes on the CI target frameworks; and the
behaviour satisfies baseline semantics B1–B3 defined in *Key Terms and Observability*.

## Additional Context

- Brighter reference implementation:
  - `src/Paramore.Brighter.Extensions.DependencyInjection/ServiceProviderHandlerFactory.cs`
  - `src/Paramore.Brighter.Extensions.DependencyInjection/ServiceProviderLifetimeScope.cs`
  - `src/Paramore.Brighter/IAmALifetime.cs`
- Darker code under review:
  - `src/Paramore.Darker.Extensions.DependencyInjection/ServiceProviderHandlerFactory.cs`
  - `src/Paramore.Darker.Extensions.DependencyInjection/ServiceProviderHandlerDecoratorFactory.cs`
  - `src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionExtensions.cs`
  - `src/Paramore.Darker/PipelineBuilder.cs`
  - `src/Paramore.Darker/QueryProcessor.cs`
- **Key open design decision (for the ADR):** whether to mirror Brighter's
  `IAmALifetime` token keyed in a `ConcurrentDictionary`, or to make the per-query
  `PipelineBuilder` itself own the child scope and pass it (or a token) to the factories.
  The latter may avoid the dictionary bookkeeping given Darker's per-query builder.
