# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: [#377 — Bring DI lifetime handling into conformance with Brighter](https://github.com/BrighterCommand/Darker/issues/377) (sub-issues [#378](https://github.com/BrighterCommand/Darker/issues/378) teardown, [#379](https://github.com/BrighterCommand/Darker/issues/379) pipeline scope handle, [#380](https://github.com/BrighterCommand/Darker/issues/380) ambient scope adoption)

## Problem Statement

As a **developer using Darker and Brighter together in an ASP.NET Core application**, I would like **a Darker query handler to resolve the same Scoped services (for example an EF Core `DbContext`) as the controller and the Brighter command handlers in the same request**, so that **reads and writes in one request see one unit of work, and I am not misled by guidance that says a Scoped `QueryProcessor` achieves this when it does not**.

As a **developer relying on Darker to manage component lifetimes**, I would like **pipeline teardown to always release every component and dispose the per-query scope, without ever replacing my handler's exception**, so that **a faulty `Dispose` in one component cannot stop the per-query scope from being disposed, or hide the real failure**.

Today:

1. Darker always creates its own per-query DI scope. Microsoft DI scopes do not nest, so even with `QueryProcessorLifetime = Scoped` the query handler gets a sibling scope and a **different** `DbContext` from the rest of the request. ADR 0014 and `CLAUDE.md` both say otherwise.
2. A throwing release during teardown skips the remaining releases **and** the scope disposal, and the teardown exception replaces the handler's exception (sync, async and stream paths). `QueryLifetimeScope.Dispose()` has the same shape.
3. The per-query scope is found through a table held on a shared singleton factory, keyed on the lifetime. Brighter removed this shape (Brighter ADR 0071). It also gives no place to decide whether a scope is owned or adopted, which (1) needs.

## Proposed Solution

Bring Darker's lifetime handling into line with Brighter's scoped-lifetime work (Brighter ADRs 0066–0076), mirroring the seam with Darker's own types rather than a shared assembly:

- **Robust teardown** (#378): every component is released independently, the pipeline scope is disposed last and unconditionally, and teardown never throws.
- **Pipeline scope handle** (#379): the pipeline asks the handler factory for its scope up front and carries it on the lifetime; factories resolve from that handle and hold no per-query state.
- **Ambient scope adoption** (#380): an opt-in `ScopeAffinity` lets a Scoped handler join an ambient scope (the ASP.NET Core request scope, or any other provider) instead of creating its own. A new `Paramore.Darker.Extensions.AspNetCore` package provides `AddDarkerRequestScope()`, the counterpart of Brighter's `AddBrighterRequestScope()`.
- **Documentation**: a new ADR replaces ADR 0014's scope-ownership account, and the "make `QueryProcessor` Scoped for EF Core" guidance is corrected.

## Requirements

### Functional Requirements

#### Teardown (#378)

- **FR1** — When a query pipeline completes (success or failure), Darker releases the handler and every decorator independently: a release that throws is logged at **Error** and does not stop the remaining releases.
- **FR2** — The pipeline scope is disposed **after** all releases and **regardless** of whether any release failed.
- **FR3** — Teardown never throws. A teardown fault does not fail a query that succeeded, and does not replace an exception thrown by the handler or a decorator: the caller sees the original exception.
- **FR4** — `QueryLifetimeScope.Dispose()` disposes every tracked item even when one throws, logs each failure, and is idempotent (a second call does nothing).
- **FR5** — FR1–FR4 hold on the **sync** (`Execute`), **async** (`ExecuteAsync`) and **stream** (`ExecuteStream`) paths.
- **FR5a** — On the **async** and **stream** paths, teardown is asynchronous: Darker awaits the disposal of components, and of the pipeline scope, so a component that implements only `IAsyncDisposable` (for example an EF Core `DbContext` held by a Scoped service) is disposed without blocking. FR1–FR4 apply to asynchronous teardown as they do to synchronous teardown. The **sync** path keeps synchronous teardown.

#### Pipeline scope handle (#379)

- **FR6** — Core exposes a scope handle abstraction (`IAmAScope`) that is disposable (sync and async) and has no resolution members, so core stays container-agnostic.
- **FR7** — A handler factory can be asked for a pipeline scope before any component is created. It returns no scope for Singleton handlers, and a scope for Scoped and Transient handlers.
- **FR8** — The lifetime carries the pipeline scope, fixed when the lifetime is created. Handler and decorator factories resolve components from that scope.
- **FR9** — Factories hold **no per-query state** (the lifetime-keyed table is removed).
- **FR10** — Asking a container-backed factory to create a non-Singleton component without a pipeline scope it recognises fails fast with a `ConfigurationException`; there is no silent fallback to the root provider.
- **FR11** — The handler and all decorators of one query resolve from one pipeline scope. Concurrent queries never share an owned scope. Whenever `HandlerLifetime` is Scoped or Transient, each query gets its own decorator instances, even when several queries share one adopted ambient scope. Under `JoinAmbient`, concurrent queries in one ambient scope share that scope's Scoped services, including a Scoped handler instance of the same handler type, as every other consumer of the scope does.
- **FR12** — `SimpleHandlerFactory` and `SimpleHandlerDecoratorFactory` (no container) continue to work; they supply no container scope.

#### Ambient scope adoption (#380)

- **FR13** — Core exposes an ambient scope provider seam (`IAmAScopeProvider`) that can offer an ambient scope for a requested `ScopeAffinity`, and a `ScopeAffinity` enum with `AlwaysNew` (default) and `JoinAmbient`.
- **FR14** — `DarkerOptions` gains `DefaultScopeAffinity`, defaulting to `ScopeAffinity.AlwaysNew`. With the default, or with no provider registered, behaviour is unchanged: Darker creates and owns a per-query scope for Scoped and Transient handlers.
- **FR15** — Darker adopts an ambient scope only when **all** hold: affinity is `JoinAmbient`; `HandlerLifetime` is `Scoped`; a provider offers a scope; the offered scope exposes an `IServiceProvider` (`IAmAServiceProviderScope`); and that provider is usable (not null, not the root provider, Darker is registered in it, resolution does not throw). Otherwise Darker keeps its non-adopting behaviour: an owned scope, or no scope for a Singleton `HandlerLifetime`. A fallback caused by misconfiguration (`JoinAmbient` with a non-Scoped `HandlerLifetime`, `JoinAmbient` with no provider registered, or an offered scope that fails one of the checks above) logs a Warning once per reason (latched, not per query). A provider that offers no scope, for example outside a request, is the normal case and logs nothing.
- **FR16** — A failure inside the ambient scope provider is surfaced as an `AmbientScopeSourceException` wrapping the original exception.
- **FR17** — **Ownership:** Darker disposes a scope it created; it never disposes an adopted or declined ambient scope. An ambient scope that is already disposed when the pipeline is built is declined, with a Warning (FR15). Disposal of a borrowed scope after the probe is its owner's responsibility, and surfaces unwrapped as `ObjectDisposedException`: from the container while the pipeline is built, or from the user's own services after it.
- **FR18** — Decorators resolve from the same scope as the handler, whether owned or adopted.
- **FR19** — The ambient seam is not tied to ASP.NET Core: a non-ASP.NET provider (for example one backed by `AsyncLocal`) can be registered and adopted.
- **FR20** — A new package **`Paramore.Darker.Extensions.AspNetCore`** provides `services.AddDarkerRequestScope(ScopeAffinity affinity = ScopeAffinity.JoinAmbient)`, which registers `IHttpContextAccessor`, an HTTP-context scope provider and the affinity. It needs no middleware. Inside a request the provider offers `HttpContext.RequestServices` (with a no-op dispose); outside a request it offers nothing.
- **FR21** — Adoption works with both Singleton and Scoped `QueryProcessorLifetime`.

#### Documentation

- **FR22** — A new ADR describes scope ownership, the pipeline scope handle, teardown and ambient adoption, and supersedes the parts of ADR 0014 that describe scope ownership and the Scoped-processor interaction.
- **FR23** — `CLAUDE.md` ("Handler Lifecycle", "ASP.NET Core Registration") and the README's EF Core guidance are corrected: a Scoped `QueryProcessor` does not share the request's `DbContext`; `AddDarkerRequestScope()` with `HandlerLifetime = Scoped` does. The docs show `AddDarkerRequestScope()` alongside Brighter's `AddBrighterRequestScope()`.
- **FR24** — The new package is listed in `CLAUDE.md` Key Components, `CONTRIBUTING.md` Project Structure and `.agent_instructions/project_structure.md`.
- **FR25** — The breaking and behavioural changes (factory interfaces, `IAmALifetime`, teardown, decorator registration lifetime) and the new public surface are recorded in `release_notes.md`.

### Non-functional Requirements

- **Performance:** no added per-query allocation on the Singleton-handler path; the ambient-provider probe result that drives the fallback Warning is latched so the Warning is not logged per query.
- **Reliability:** on every failure path in teardown, Darker still disposes the pipeline scope it owns; no teardown fault in a component Darker releases leaks that scope. Faults inside one DI scope's disposal of its own services are the container's to contain (see Constraints).
- **Diagnosability:** release failures logged at Error with the component type; a misconfiguration fallback logged at Warning with the reason.
- **Parity:** sync, async and stream pipelines behave the same for teardown and scope handling.
- **Concurrency:** concurrent queries never share an owned scope, and never share a decorator instance unless `HandlerLifetime` is Singleton.

### Constraints and Assumptions

- **No shared assembly with Brighter.** Darker defines its own `IAmAScope`, `IAmAScopeProvider`, `ScopeAffinity`, `IAmAServiceProviderScope`. Both libraries read `HttpContext.RequestServices`, so they see the same Scoped services. Users therefore make two opt-in calls, and Brighter's `AmbientScopeSuppression` does not govern Darker.
- **Breaking changes are acceptable** (V5): `IQueryHandlerFactory`, `IQueryHandlerFactoryAsync`, `IAmALifetime` and related types may change.
- `Paramore.Darker` and `Paramore.Darker.Extensions.DependencyInjection` keep their current target frameworks (`netstandard2.0;net8.0;net9.0`) and the DI package takes **no** ASP.NET Core dependency.
- `Paramore.Darker.Extensions.AspNetCore` targets net8.0+ via `FrameworkReference Microsoft.AspNetCore.App`.
- Darker has one registration path (`AddDarker`), so Brighter ADR 0076's write-through `ScopeAffinityOverride` machinery may not be needed; the ADR settles this.
- Assumes Microsoft DI semantics: scopes created from a scoped provider are root-parented.
- Microsoft DI disposes a scope's services in one loop with no per-service fault isolation: when one service's `Dispose` throws, services it has not yet reached stay undisposed. Darker isolates its own teardown steps, but cannot change this.
- Darker's DI factory is internal, and `AddDarker` always gives it a pipeline scope it recognises. Tests reach it only through the public surface (no `InternalsVisibleTo`; `.agent_instructions/testing.md`).

### Out of Scope

- **Lifetime validation** (`ValidatePipelines` rules, e.g. Scoped dependency in a Singleton handler) — tracked in #305.
- Brighter ADR 0070 (mapper/transform pipeline scopes) — Darker has no mappers or transforms.
- Brighter ADR 0075 (Publish/pump suppression brackets) — Darker has no Publish fan-out or message pump.
- A shared lifetime/scope assembly between Brighter and Darker.
- Containers other than Microsoft.Extensions.DependencyInjection.
- ASP.NET Core middleware for request scope.
- Decorator instances under a Singleton `HandlerLifetime`: they stay Singletons, shared by every query with the same result type, as today. A separate issue.

## Acceptance Criteria

**Teardown**
- A handler whose `Dispose` throws (Simple factory): decorators are still released and the lifetime is still disposed.
- A Scoped service whose `Dispose` throws inside the pipeline scope (DI factory): the query returns its result and the fault is logged at Error.
- A handler that throws plus a release that throws: the caller sees the handler's exception.
- A successful query plus a release that throws: the query returns its result.
- A decorator whose creation or initialisation throws: the caller sees that exception, and the decorators created before it are released.
- On `ExecuteAsync` and `ExecuteStream`, the handler is released by the factory that created it.
- Covered on sync, async and stream paths.
- On the async and stream paths, a Scoped dependency that implements only `IAsyncDisposable` is disposed by teardown, and its `DisposeAsync` is awaited.

**Pipeline scope handle**
- Handler and decorators of one query resolve the same Scoped instance; concurrent queries with owned scopes get different instances (existing lifetime tests keep passing).
- `ServiceProviderComponentFactory` holds no per-query state.
- With `HandlerLifetime` Scoped or Transient, and an owned scope, nothing is resolved from the root provider: a disposable handler is disposed at the end of each query. (FR10's fail-fast guard sits inside the internal DI factory and is not reachable through `AddDarker`, so it has no direct test.)

**Ambient adoption**
- Within one ASP.NET Core request, with `HandlerLifetime = Scoped` and `AddDarkerRequestScope()`, a query handler resolves the same Scoped instance as `HttpContext.RequestServices`, and Darker does not dispose it.
- Without the opt-in, or with `AlwaysNew`, Darker uses an owned per-query scope (unchanged behaviour).
- Outside a request (no `HttpContext`), Darker falls back to an owned scope and logs no Warning.
- Holds with both Singleton and Scoped `QueryProcessorLifetime`.
- An `AsyncLocal`-backed test provider is adopted the same way.
- Transient or Singleton `HandlerLifetime` with `JoinAmbient` does not adopt, and logs one Warning.
- An ambient scope already disposed when the pipeline is built is declined: Darker uses an owned scope and logs one Warning.
- With `HandlerLifetime = Scoped` and an adopted scope, two queries of different types with the same result type get distinct decorator instances, and each decorator resolves the ambient scope's Scoped services.
- With `HandlerLifetime = Scoped` and an adopted scope, two queries of one handler type resolve the same Scoped handler instance (the documented sharing of FR11).

**Testing approach**
- TDD via `/test-first`, preferring real registries, `ServiceCollection`-built providers and `Simple*` factories over mocks. Tests use only the public surface.
- Tests in `test/Paramore.Darker.Core.Tests`, `test/Paramore.Darker.Extensions.Tests`, and a new test project for `Paramore.Darker.Extensions.AspNetCore` (using `DefaultHttpContext` / `TestServer` or equivalent).

**Definition of done**
- All FRs covered by tests; full suite green on net8.0 and net9.0.
- ADR accepted; docs and release notes updated.

## Additional Context

- Brighter `docs/adr/`: 0066–0069 (lease/ownership/leak fixes), 0071 (pipeline scope handle, teardown hardening), 0072 (ambient scope adoption seam), 0073 (ASP.NET Core request scope package), 0074 (lifetime validation), 0076 (scope affinity option). Brighter spec `0036-scoped-lifetime-per-pipeline`.
- Darker: ADR 0014 (factory component lifetime), #329, spec `008-factory_component_lifetime`.
- Brighter ADR 0072's Alternative 1 states that a Darker query handler resolves the controller's `DbContext`; that is not true of Darker before this work.
