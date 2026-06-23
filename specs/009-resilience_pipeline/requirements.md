# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: #293
**Target Release**: V5 (new public API surface; existing Polly policy API is retained, not removed)

## Problem Statement

As an application developer using Darker to apply quality-of-service (retry, circuit
breaker, timeout, hedging, rate limiting, fallback) around my query handlers,
I would like to configure that resilience using Polly V8's modern
`ResiliencePipeline` API, so that I can compose strategies the way I already do in the
rest of my application and in Brighter, without being forced onto Polly's legacy
`Policy` / `AsyncPolicy` API that is now in maintenance mode.

Today, Darker's resilience-via-Polly integration — the `[RetryableQuery]` /
`[RetryableQueryAsync]` attributes and their `RetryableQueryDecorator` handlers, plus the
`Policies(...)` / `AddPolicies(...)` / `DefaultPolicies(...)` builder and DI extensions —
is built entirely on the legacy Polly API. (Darker also has a `[FallbackPolicy]`
decorator, but it is a pure in-process fallback that does not call Polly; it is unaffected
by this work and is treated as legacy surface that must keep working — see NFR1.) The
Polly-backed path:

- It carries an `IPolicyRegistry<string>` on `IQueryContext.Policies`.
- The decorators resolve an `ISyncPolicy` / `IAsyncPolicy` by name and execute the
  pipeline through it.
- Defaults are built with `Policy.Handle<Exception>().WaitAndRetryAsync(...)` and
  `Policy.Handle<Exception>().CircuitBreakerAsync(...)`.

This forces users who want modern Polly composition to author legacy policies just for
Darker, even though Polly V8 (already referenced at `8.6.6`) ships the
`ResiliencePipeline` / `ResiliencePipelineRegistry<TKey>` model and treats the old
`Policy` types as legacy.

Brighter has already faced and resolved exactly this problem. Its decision — explicitly
recorded in the issue — was **not to drop** the legacy Polly policy support, but to
**add** a parallel resilience-pipeline integration alongside it. Darker should reach the
same outcome: developers gain a first-class Polly V8 path while existing
`[RetryableQuery]` users are not broken.

## Proposed Solution

Add first-class Polly V8 `ResiliencePipeline` support to Darker, mirroring Brighter's
additive design, so a developer can:

1. **Register resilience pipelines by name** with a Polly V8
   `ResiliencePipelineRegistry<string>` and hand that registry to Darker through the
   query-processor builder and the DI integration — analogous to how
   `Policies(...)` / `AddPolicies(...)` register an `IPolicyRegistry<string>` today.

2. **Decorate a handler method with a new attribute** that names the pipeline to apply,
   e.g. `[UseResiliencePipeline("MyPipeline", step: 1)]` (with an async counterpart),
   matching Brighter's `UseResiliencePipelineAttribute` / `UseResiliencePipelineAsyncAttribute`.

3. **Optionally scope a pipeline by result type** (Brighter's `UseTypePipeline`), so a
   stateful strategy such as a circuit breaker can have an independent instance per query
   **result type** rather than a single shared instance. (Independence is per result type:
   Polly caches one `ResiliencePipeline<TResult>` per `(key, TResult)`. Two query types with
   distinct result DTOs — the common case — get independent state; two that share a result
   type share state. This requires the registry to register a generic builder per result
   type — see FR8.)

4. **Get sensible default pipelines** through a `DefaultResiliencePipelines()` /
   `AddDefaultResiliencePipelines()` convenience, equivalent in intent to today's
   `DefaultPolicies()` (a default retry pipeline and a default circuit-breaker pipeline).

5. **Keep using the existing legacy policy API unchanged.** The current
   `[RetryableQuery]`, `RetryableQueryDecorator`, `IQueryContext.Policies`, and the
   `Policies(...)` / `AddPolicies(...)` / `DefaultPolicies(...)` extensions continue to
   work exactly as before. The two mechanisms coexist.

From the developer's perspective: "Darker now lets me describe resilience with the same
Polly V8 `ResiliencePipeline` I use everywhere else, registered by name and applied via a
`[UseResiliencePipeline]` attribute — and my existing `[RetryableQuery]` handlers still
work."

## Key Terms

To keep the requirements unambiguous and directly testable:

- **Legacy policy API** — Polly's `Policy` / `AsyncPolicy` / `ISyncPolicy` /
  `IAsyncPolicy` / `IPolicyRegistry<string>` types, surfaced in Darker today via
  `IQueryContext.Policies` and the `[RetryableQuery]` decorators.
- **Resilience pipeline API** — Polly V8's `ResiliencePipeline`,
  `ResiliencePipeline<T>`, `ResiliencePipelineBuilder`,
  `ResiliencePipelineRegistry<TKey>`, and `ResiliencePipelineProvider<TKey>`.
- **Named pipeline** — a `ResiliencePipeline` retrieved from the registry/provider by a
  string key.
- **Type pipeline** — a `ResiliencePipeline<TResult>` resolved by `GetPipeline<TResult>(key)`,
  which Polly caches per `(key, TResult)`, giving an independent instance **per query result
  type** while executing the handler's `TResult`. This is Darker's realisation of Brighter's
  `UseTypePipeline` intent; independence is per result type (not per query type) because the
  handler executes returning `TResult` — see FR8 and ADR-0015.

## Requirements

### Functional Requirements

- **FR1 — Pipeline provider on the context.** `IQueryContext` exposes a resilience
  pipeline provider typed as the narrowest abstraction, Polly V8
  `ResiliencePipelineProvider<string>` (RD1), alongside the existing `Policies` registry,
  so a decorator can resolve a named pipeline at execution time. The new member is
  additive; `Policies` is not removed or changed. The builder/DI registration constructs
  a concrete `ResiliencePipelineRegistry<string>` and supplies it through this provider
  member.
- **FR2 — Resilience-pipeline decorators.** Provide sync and async decorators
  (`IQueryHandlerDecorator` / `IQueryHandlerDecoratorAsync`) that resolve a named
  `ResiliencePipeline` from the context provider and execute the rest of the pipeline
  (`next`) through it, preserving the cancellation token on the async path.
- **FR3 — New attributes.** Provide `[UseResiliencePipeline(policy, step)]` and an async
  counterpart that name the pipeline and (per Darker's pattern) resolve to the new
  decorator types. The attribute exposes a `UseTypePipeline` option to select a
  type-scoped pipeline (FR8).
- **FR4 — Builder registration.** Provide query-processor builder extensions to register
  a caller-supplied `ResiliencePipelineRegistry<string>` (mirroring `Policies(...)` /
  `AddPolicies(...)`), and to register the new decorators so they participate in the
  pipeline. A `null` registry argument is rejected with `ArgumentNullException` (the
  legacy `Policies(...)` path likewise guards its argument). No registration-time
  *content* validation (e.g. requiring specific keys to be present) is required for the
  caller-supplied registry path; missing keys surface later via FR7.
- **FR5 — DI registration.** Provide `Microsoft.Extensions.DependencyInjection`
  extensions (mirroring `AddPolicies` / `AddDefaultPolicies`) to register the resilience
  pipeline registry and the new decorators with the service collection. A `null` registry
  argument is rejected with `ArgumentNullException`. The registry and decorators are
  registered so they resolve under the configured `QueryProcessor` lifetime (no new
  lifetime is introduced).
- **FR6 — Default pipelines.** Provide a `DefaultResiliencePipelines()` /
  `AddDefaultResiliencePipelines()` convenience that registers a default retry pipeline
  and a default circuit-breaker pipeline under new, resilience-pipeline-specific keys that
  do not clash with the legacy `Constants.RetryPolicyName` /
  `Constants.CircuitBreakerPolicyName` (RD4). The exact strategy parameters (retry count
  and backoff, breaker failure threshold and break duration) are an ADR-level decision and
  are intentionally not fixed here; the requirement is only that each default pipeline is
  registered under its well-known key and is resolvable and executable without further
  configuration.
- **FR7 — Configuration validation.** Two failure modes are detected, and — consistent
  with the legacy decorator — both are detected at **pipeline build time**, in the
  decorator's `InitializeFromAttributeParams` (mirroring
  `RetryableQueryDecorator.cs:23-24` for the missing-key case and the registry resolution
  at `RetryableQueryDecorator.cs:34-35` for the missing-provider case), not deferred to
  execution:
  - **No resilience provider configured on the context** — the decorator throws a
    `ConfigurationException` when it initializes.
  - **Named pipeline not registered** — the decorator throws a `ConfigurationException`,
    and the exception message names the unresolved pipeline key so the user can act on it.

  Because detection is at build time, merely building the pipeline for a decorated handler
  (before any query is executed) surfaces the error.
- **FR8 — Type-scoped pipelines.** When `UseTypePipeline` is requested, the decorator
  resolves the pipeline via Polly's generic `GetPipeline<TResult>(key)` rather than the
  shared non-generic `GetPipeline(key)`. Polly caches one `ResiliencePipeline<TResult>` per
  distinct `(key, TResult)`, so a type-scoped circuit breaker gets **independent state per
  result type** and **shared state within a result type**. The handler executes returning
  `TResult` and `ResiliencePipeline<TResult>.Execute` returns `TResult`, so this resolves and
  executes natively with no Darker-authored key scheme.
  - **Independence is per result type, not per query type.** Two query types with *distinct*
    result DTOs (the common case) get independent state; two query types that share a result
    type share state under the same key. (This is the deliberate trade-off for using Polly's
    registry-native generic resolution; see ADR-0015 and `review-design.md` Finding 1 for why
    the per-query-type composite-key alternative was rejected as infeasible for default
    pipelines.)
  - **Precondition for independence:** independent per-result-type state is a property of how
    the registry is *registered*, not something the decorator can force. Polly resolves
    generic and non-generic registrations in **separate namespaces**: the supplied
    `ResiliencePipelineRegistry<string>` must register a generic builder
    `TryAddBuilder<TResult>(key, (builder, ctx) => ...)` for each result type used under that
    key. A registry that only registers a non-generic builder under the key will **not**
    satisfy `GetPipeline<TResult>(key)` (Polly throws), and that is a registration error, not
    a decorator defect.
  - **Defaults do not support `UseTypePipeline`:** `DefaultResiliencePipelines()` /
    `AddDefaultResiliencePipelines()` register non-generic builders only (they cannot
    enumerate the application's result types), so type scoping requires a caller-supplied
    registry with `TryAddBuilder<TResult>` per result type (FR6).
  - **Same-result-type boundary:** two handlers whose queries share a `TResult` resolve the
    same `(key, TResult)` and therefore share breaker/state. Independence is per result type,
    not per handler instance.
- **FR9 — Context propagation.** `IQueryContext` surfaces a Polly `ResilienceContext`
  (RD2). The async decorator selects the execution context as follows:
  - **If `IQueryContext.ResilienceContext` is `null`:** the decorator executes the
    pipeline overload that takes a `CancellationToken`, passing the caller's token
    through unchanged.
  - **If `IQueryContext.ResilienceContext` is non-null:** the decorator executes the
    pipeline overload that takes the `ResilienceContext`, passing that context through so
    user-authored strategies can read its `Properties` and use its `CancellationToken`.
    In this branch the `ResilienceContext.CancellationToken` is authoritative (it is the
    caller's responsibility to construct the `ResilienceContext` with the token they
    want); the decorator does not merge tokens.

  This mirrors Brighter's `ResilienceExceptionPolicyHandlerAsync` branch on
  `Context?.ResilienceContext != null`.

### Non-functional Requirements

- **NFR1 — Backward compatibility.** Existing applications using `[RetryableQuery]`,
  `[FallbackPolicy]`, `Policies(...)`, `AddPolicies(...)`, and `DefaultPolicies(...)`
  compile and behave identically with no source changes. The legacy and
  resilience-pipeline paths are independent and may be used together in the same
  application.
- **NFR2 — Sync/async parity.** Every capability is available on both the synchronous and
  asynchronous handler paths, consistent with ADR-0005 (dual sync/async support).
- **NFR3 — No new third-party dependencies.** Implemented against the already-referenced
  Polly `8.6.6` package only; no additional NuGet packages introduced. (Both the legacy
  `Policy` API and the new `ResiliencePipeline` API ship in that single package, so the
  constraint is self-evidencing from `Directory.Packages.props` and needs no ADR.)
- **NFR4 — Consistency with Brighter.** Public *attribute and option* naming follows
  Brighter's resilience-pipeline integration verbatim (`UseResiliencePipelineAttribute` /
  `UseResiliencePipelineAsyncAttribute`, the `UseTypePipeline` option) so developers
  moving between the two libraries have a consistent mental model. **One deliberate
  divergence:** Brighter surfaces the concrete `ResiliencePipelineRegistry<string>` on
  `IRequestContext` (`src/Paramore.Brighter/IRequestContext.cs`), whereas Darker exposes
  the narrower `ResiliencePipelineProvider<string>` on `IQueryContext` (RD1). This is a
  conscious narrowing, not a parity gap: `ResiliencePipelineRegistry<TKey>` derives from
  `ResiliencePipelineProvider<TKey>`, and the provider exposes everything the decorators
  consume (`GetPipeline(key)` / `GetPipeline<TResult>(key)`). "Verbatim" therefore applies
  to the attribute/option surface, not to the context member type.
- **NFR5 — Testability.** New behaviour is verified with real/Simple/InMemory test
  doubles rather than mocks, per the project's test-double preference, and pipelines are
  built with deterministic (e.g. zero-delay) strategies in tests.

### Constraints and Assumptions

- Polly V8 is already referenced (`Polly` `8.6.6` in `Directory.Packages.props`); the
  legacy `Policy` API and the new `ResiliencePipeline` API both ship in that package, so
  no version change is required to keep legacy support.
- The decorator pipeline is attribute-driven and ordered by `step` (ADR-0002); the new
  attributes integrate with that mechanism unchanged.
- Decorators and handlers are created per query via the factories and released after
  execution (ADR-0004 / ADR-0014); the new decorators follow the same lifecycle.
- Assumption: registering a resilience pipeline registry is opt-in. Applications that
  register neither legacy policies nor resilience pipelines are unaffected.

### Out of Scope

- **Removing or deprecating** the legacy Polly policy API. Despite the issue's "Migrate"
  title, the agreed direction (matching Brighter) is additive; removal is explicitly out
  of scope for this spec.
- Changing the default retry/circuit-breaker timings or behaviour of the existing
  `DefaultPolicies()` path.
- Authoring new resilience strategies beyond what Polly V8 already provides (Darker
  composes Polly; it does not implement strategies).
- Telemetry/metrics integration for Polly pipelines (may be a follow-up).
- Migrating the existing test suite away from legacy policies (the legacy tests remain
  valid coverage for the retained API).

## Acceptance Criteria

How we'll know this is working correctly:

- **AC1** A handler method decorated with `[UseResiliencePipeline("Name", step)]` (and the
  async variant) executes the rest of its pipeline through the named Polly V8
  `ResiliencePipeline` resolved from a registered `ResiliencePipelineRegistry<string>`.
- **AC2** A retry pipeline registered by name causes a transiently-failing handler to be
  retried and ultimately succeed, on both sync and async paths.
- **AC3** A circuit-breaker pipeline registered by name opens after the configured number
  of failures, on both sync and async paths.
- **AC4** With `UseTypePipeline` set and the registry registering a generic builder
  (`TryAddBuilder<TResult>`) per result type under the key (FR8 precondition), two query
  types with *different* result types using the same base circuit-breaker pipeline name
  maintain independent breaker state; two queries with the *same* result type share breaker
  state.
- **AC5** Configuration validation fails at **pipeline build time** (when the decorated
  handler's pipeline is built, before any query executes): naming an unregistered
  pipeline, or building with no resilience provider configured, throws a
  `ConfigurationException`; for the missing-key case the exception message contains the
  unresolved pipeline key.
- **AC6** `DefaultResiliencePipelines()` / `AddDefaultResiliencePipelines()` register a
  default retry pipeline and a default circuit-breaker pipeline under well-known,
  resilience-pipeline-specific keys; each is resolvable from the provider and executable
  without any additional configuration. (Exact strategy parameters are validated at the
  ADR/implementation level, not asserted here — see FR6.)
- **AC7** With no `ResilienceContext` on the query context, the async decorator honours the
  caller's cancellation token: (a) an already-cancelled token surfaces an
  `OperationCanceledException` rather than being ignored, and (b) a token cancelled while
  the handler is in flight propagates and aborts the execution.
- **AC8** When a `ResilienceContext` is present on `IQueryContext`, the async decorator
  executes the pipeline through that context: a strategy registered in the pipeline
  observes the caller-supplied `ResilienceContext` (e.g. a property set on
  `context.Properties` before execution is readable inside the strategy), and the
  `ResilienceContext.CancellationToken` is the one used for the execution.
- **AC9** All existing legacy-policy tests continue to pass unchanged, demonstrating the
  legacy `[RetryableQuery]` / `Policies(...)` path is unaffected (NFR1).
- **AC10** The builder path (`...ResiliencePipelines(...)`) registers a caller-supplied
  registry and the new decorators such that a `[UseResiliencePipeline]`-decorated handler
  resolves and executes through the named pipeline at runtime; passing a `null` registry
  throws `ArgumentNullException`.
- **AC11** The DI path (`Add...ResiliencePipelines(...)`) registers a caller-supplied
  registry and the new decorators such that a `[UseResiliencePipeline]`-decorated handler
  resolves and executes through the named pipeline at runtime; passing a `null` registry
  throws `ArgumentNullException`.
- **Definition of done**: new public API documented with XML comments and MIT licence
  headers (per project documentation standards); all tests green via
  `dotnet test Darker.Filter.slnf -c Release`; an ADR recording the additive design and
  the API shape is approved.

## Additional Context

- Issue #293: *Migrate from Polly to Polly V8 Resilience Pipelines* — labels
  `enhancement`, `Breaking Change`, `0 - Backlog`. Originates from V5 discussion #273.
  The issue records the agreed direction: **"The decision was not to drop Polly policies,
  but to add support for a Resilience pipeline."**
- Brighter reference implementation (already merged), to mirror for naming and shape:
  - `src/Paramore.Brighter/Policies/Attributes/UseResiliencePipelineAttribute.cs`
  - `src/Paramore.Brighter/Policies/Attributes/UseResiliencePipelineAsyncAttribute.cs`
  - `src/Paramore.Brighter/Policies/Handlers/ResilienceExceptionPolicyHandler.cs`
  - `src/Paramore.Brighter/Policies/Handlers/ResilienceExceptionPolicyHandlerAsync.cs`
  - Registry surfaced on `IRequestContext.ResiliencePipeline` and initialized when the
    request context is constructed in `CommandProcessor.InitRequestContext`.
- Darker counterparts to extend (additively):
  - `src/Paramore.Darker/IQueryContext.cs` (`Policies` → add resilience provider)
  - `src/Paramore.Darker/Policies/Handlers/RetryableQueryDecorator[Async].cs` (pattern to follow)
  - `src/Paramore.Darker/Policies/Attributes/RetryableQueryAttribute[Async].cs` (pattern to follow)
  - `src/Paramore.Darker/Policies/QueryProcessorBuilderExtensions.cs` (builder registration)
  - `src/Paramore.Darker.Extensions.DependencyInjection/PolicyDIExtensions.cs` (DI registration)
  - `src/Paramore.Darker/QueryProcessor.cs:104-105` (null-guards then sets `Context.Policies` from the processor; the resilience provider is set analogously)

## Resolved Decisions

These were open during requirements drafting and have since been decided. They are
binding inputs to the ADR.

- **RD1 (was OQ1) — Use the narrowest abstraction.** The resilience provider on
  `IQueryContext` is typed as `ResiliencePipelineProvider<string>`, not the concrete
  `ResiliencePipelineRegistry<string>`. Consumers (the decorators) only need to resolve
  pipelines by key, which the provider abstraction supplies. The ADR should default to
  `ResiliencePipelineProvider<string>` and only widen to the concrete registry type if a
  required method is genuinely unavailable on the narrower interface.
  *(`ResiliencePipelineRegistry<string>` is still what the builder/DI registration
  constructs and registers — it implements/produces the provider.)*
- **RD2 (was OQ2) — Surface a Polly `ResilienceContext` on `IQueryContext`.** It is
  included in this spec rather than deferred. The rationale matches why it emerged in
  Brighter: without it, users authoring their own resilience strategies have no access to
  the ambient context. The decorators use the caller-supplied `ResilienceContext` when
  present, and fall back to executing with the cancellation token otherwise (see FR9).
- **RD3 (was OQ3) — Match Brighter's naming verbatim.** Adopt
  `UseResiliencePipelineAttribute` / `UseResiliencePipelineAsyncAttribute` and the
  `UseTypePipeline` option as-is, rather than aligning to Darker's `...Query` suffix.
  Darker and Brighter are commonly used together, so a consistent mental model wins
  (NFR4).
- **RD4 (was OQ4) — Distinct keys for default pipelines.** `DefaultResiliencePipelines()`
  / `AddDefaultResiliencePipelines()` register their default retry and circuit-breaker
  pipelines under new, resilience-pipeline-specific keys that do **not** clash with the
  legacy `Constants.RetryPolicyName` / `Constants.CircuitBreakerPolicyName`. This keeps
  the legacy and resilience-pipeline paths unambiguous when both are configured.
