# 15. Polly V8 Resilience Pipeline Integration

Date: 2026-06-15

## Status

Accepted

## Context

**Parent Requirement**: [specs/009-resilience_pipeline/requirements.md](../../specs/009-resilience_pipeline/requirements.md)

**Scope**: This ADR covers the **whole additive Polly V8 `ResiliencePipeline` integration** for
Darker as a single architectural decision: the new members on `IQueryContext`, the
sync/async decorators and their attributes, type-scoped pipelines, builder/DI registration,
default pipelines, and execution-context propagation. Registration and defaults are folded
in here because they are the configuration surface of the same decision, not an independent
architectural concern.

Darker's only Polly-backed resilience path today is the legacy `Policy` API. The
decorators resolve an `ISyncPolicy` / `IAsyncPolicy` by name from an
`IPolicyRegistry<string>` carried on `IQueryContext.Policies`
(`RetryableQueryDecorator.cs:31`, `RetryableQueryDecoratorAsync.cs:36`), the attributes
(`RetryableQueryAttribute`, `RetryableQueryAttributeAsync`) name the policy and return the
open-generic decorator type, and the builder/DI extensions (`Policies(...)` /
`AddPolicies(...)` / `DefaultPolicies()`) register the registry and decorators. Polly V8
(already referenced at `8.6.6`) treats `Policy` as legacy and ships
`ResiliencePipeline` / `ResiliencePipelineRegistry<TKey>` / `ResiliencePipelineProvider<TKey>`
as the modern model.

The architectural problem: **how to add a first-class Polly V8 path that mirrors Brighter's
additive design and integrates with Darker's attribute-driven, per-query decorator pipeline
(ADR-0002, ADR-0004, ADR-0014) without disturbing the legacy path (NFR1)**, while resolving
three forces specific to Darker:

1. **Queries return results.** A Darker query is `TQuery : IQuery<TResult>` and the pipeline
   `next` returns `TResult`. Brighter commands carry the request type as their execution
   type, so Brighter's `GetPipeline<TRequest>(key)` yields per-request-type state *and*
   executes returning the request. In Darker the execution result type (`TResult`) differs
   from the query type (`TQuery`), and `ResiliencePipeline<T>.Execute<TResult>` constrains
   `TResult : T` (it has no bare `Execute` that returns the class parameter `T`). This breaks
   a mechanical mirror of Brighter and forces a deliberate choice for type-scoped pipelines
   (see Decision).
2. **Narrowest abstraction on the context (RD1).** The context member must be the consumer's
   minimal need (`ResiliencePipelineProvider<string>`), even though the builder/DI side
   constructs the concrete `ResiliencePipelineRegistry<string>`.
3. **Build-time validation parity (FR7).** The legacy decorators validate configuration in
   `InitializeFromAttributeParams` — i.e. when the pipeline is built, before any query runs
   (`RetryableQueryDecorator.cs:19-25,34-35`). The new decorators must fail at the same point.

## Decision

Add a parallel resilience-pipeline integration that follows the existing legacy structure
component-for-component, keeping the two paths independent and usable together.

### 1. Context surface (FR1, FR9 / RD1, RD2)

Extend `IQueryContext` with two additive members; `Policies` is untouched:

```csharp
public interface IQueryContext
{
    IDictionary<string, object> Bag { get; set; }
    IPolicyRegistry<string> Policies { get; set; }                              // legacy, unchanged
    ResiliencePipelineProvider<string> ResiliencePipeline { get; set; }         // FR1 / RD1 (narrow)
    ResilienceContext? ResilienceContext { get; set; }                          // FR9 / RD2
}
```

`ResiliencePipeline` is typed as the **provider** (knowing-by-key is all the decorators
need), not the concrete registry — a deliberate, documented narrowing from Brighter, which
surfaces `ResiliencePipelineRegistry<string>` on `IRequestContext` (NFR4). The registry
*derives from* the provider, so the builder/DI side constructs a concrete
`ResiliencePipelineRegistry<string>` and assigns it through this member.

`QueryProcessor` gains an optional constructor parameter
`ResiliencePipelineProvider<string> resiliencePipelineProvider = null`, held in a field and
written onto the context in `InitQueryContext` with the same fill-if-absent guard used for
`Policies` (`QueryProcessor.cs:102-106`). `ResilienceContext` is **not** populated by the
processor — it is caller-supplied (the caller owns its lifetime and cancellation token).

### 2. Decorators (FR2, FR7, FR9)

Two new decorators mirror the `RetryableQueryDecorator[Async]` shape one-to-one:

- `UseResiliencePipelineHandler<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>`
- `UseResiliencePipelineHandlerAsync<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>`

`InitializeFromAttributeParams` reads the pipeline key and the `useTypePipeline` flag,
resolves the provider, and **validates at build time** (FR7): throw `ConfigurationException`
if no provider is configured ("No resilience pipeline provider is configured…"), and throw
`ConfigurationException` naming the unresolved key if the pipeline does not resolve — using
`TryGetPipeline(key)` for the shared path and `TryGetPipeline<TResult>(key)` for the
type-scoped path (§4). This matches the legacy decorator's failure point exactly.

Execution depends on `useTypePipeline` (§4), because the shared path uses the **non-generic**
`ResiliencePipeline` (executes any `TResult`) while the type-scoped path uses the
**result-typed** `ResiliencePipeline<TResult>` (Polly caches one per `(key, TResult)`):

- **Sync, shared:** `Context.ResiliencePipeline.GetPipeline(key).Execute(() => next(query))`
  (non-generic `Execute<TResult>`).
- **Sync, type-scoped:** `Context.ResiliencePipeline.GetPipeline<TResult>(key).Execute(() => next(query))`.
- **Async** branches on the context (FR9), mirroring Brighter's
  `ResilienceExceptionPolicyHandlerAsync`, on whichever pipeline (`GetPipeline` /
  `GetPipeline<TResult>`) the flag selects:
  - `IQueryContext.ResilienceContext == null` → `await pipeline.ExecuteAsync((ct) => new ValueTask<TResult>(next(query, ct)), cancellationToken)` — the caller's token flows through unchanged.
  - `IQueryContext.ResilienceContext != null` → execute the `ResilienceContext` overload, passing that context so user strategies read its `Properties`; the `ResilienceContext.CancellationToken` is authoritative and tokens are **not** merged.

`ResilienceContext` propagation is **async-only by design** (FR9 scopes the branch to the async
decorator). The sync decorator executes through the non-context `Execute` overload and does **not**
read `IQueryContext.ResilienceContext`, so a sync caller should not expect `Properties` propagation.
This is consistent with NFR2: the *capability* (resolve a named pipeline, type-scoping, build-time
validation) is present on both paths; ambient-context propagation is a deliberately async-only
concern, matching Brighter's async-only `ResilienceExceptionPolicyHandlerAsync` branch.

### 3. Attributes (FR3, FR8)

`UseResiliencePipelineAttribute : QueryHandlerAttribute` and
`UseResiliencePipelineAsyncAttribute : QueryHandlerAttributeAsync` — names follow Brighter
**verbatim** (RD3/NFR4). "Verbatim" applies to the *spelling* of the attribute and option
(`UseResiliencePipeline[Async]`, `UseTypePipeline`), not to the isolation semantics: Brighter's
typed mode keys per `TRequest` whereas Darker's keys per `TResult` (§4), a deliberate divergence
documented under Negative consequences. Each takes `(int step, string policy, bool useTypePipeline = false)`,
returns `new object[] { policy, useTypePipeline }` from `GetAttributeParams()`, and returns
the matching open-generic decorator type from `GetDecoratorType()`. `useTypePipeline` is the
`UseTypePipeline` option (FR8).

### 4. Type-scoped pipelines — Polly's native generic provider, keyed by result type (FR8, AC4)

**We deliberately mirror Brighter's two-mode `UseTypePipeline` toggle (NFR4) so developers
moving between the libraries meet the same model.** Brighter resolves a typed pipeline by
`GetPipeline<TRequest>(key)`; Darker's analogue is `GetPipeline<TResult>(key)`, because a query
executes returning `TResult` (where a Brighter command returns its `TRequest`). When
`UseTypePipeline` is set the decorator resolves the typed pipeline; otherwise it uses the
non-generic `GetPipeline(key)`. The exact Polly 8.6.6 registry behaviour this relies on was
verified (see "Verification"):

```csharp
TResult Run(Func<TResult> body) =>
    _useTypePipeline
        ? Context.ResiliencePipeline.GetPipeline<TResult>(_policy).Execute(body)   // per (key, TResult)
        : Context.ResiliencePipeline.GetPipeline(_policy).Execute(body);           // single shared
```

Polly's registry caches one `ResiliencePipeline<TResult>` per distinct `(key, TResult)`
(verified), so a type-scoped circuit breaker gets **independent state per result type** and
**shared state within a result type**. The result type is the natural generic discriminator
because the handler executes returning `TResult`, and `ResiliencePipeline<TResult>.Execute<TResult>`
(constraint `TResult : TResult`, identity-satisfied) returns `TResult` — no type gymnastics,
no Darker-authored key format, no string parsing.

**Independence is per result type, not per query type** — the direct consequence of matching
Brighter's typed mode (Brighter's is per `TRequest`; ours per `TResult`). Two query types with
*distinct* result DTOs (the common case — each query usually has a bespoke result) get
independent state; two query types that happen to share a result type share state under the
same key. This is registry-native and needs no Darker-authored key scheme; the
composite-string-key alternative that would have given strict per-query-type isolation was
considered and rejected as infeasible against Polly's exact-key registry (see Alternatives,
and `specs/009-resilience_pipeline/review-design.md` Finding 1).

**Registration precondition (FR8), verified:** Polly resolves *generic* and *non-generic*
registrations in **separate namespaces** — a non-generic `TryAddBuilder(key, …)` does **not**
satisfy `GetPipeline<TResult>(key)`, and a `GetPipeline<TResult>(key)` for an unregistered
`TResult` throws `KeyNotFoundException`. Therefore a registry that supports `UseTypePipeline`
**must** register a generic builder `TryAddBuilder<TResult>(key, (builder, ctx) => …)` for
each result type used under that key. This is idiomatic Polly and is the caller's
responsibility on a **caller-supplied** registry.

**Consequence for defaults:** because `DefaultResiliencePipelines()` cannot enumerate the
application's result types, it registers **non-generic** builders only and therefore supports
the **shared** path but **not** `UseTypePipeline`. Pairing `useTypePipeline:true` with a
default pipeline key fails FR7 build-time validation with the unresolved-key message. This
limitation is explicit (see Consequences/Negative), and replaces the earlier draft's
infeasible claim that defaults seed per-type builders.

### 5. Builder & DI registration, default pipelines (FR4, FR5, FR6 / RD4)

Mirror the legacy extensions verbatim in shape:

- Builder: `ResiliencePipelines(this IBuildTheQueryProcessor, ResiliencePipelineRegistry<string>)`
  and `AddResiliencePipelines<TBuilder>(...)` register the two decorators and set a new
  `QueryProcessorBuilder.ResiliencePipelineRegistry` property (threaded into the
  `QueryProcessor` constructor). A `null` registry throws `ArgumentNullException` — parity
  with the legacy path on the **null-guard only** (`AddPolicies` guards null at
  `QueryProcessorBuilderExtensions.cs:27-28`). The resilience path **deliberately omits** the
  legacy `AddPolicies` *content* check (which requires the two well-known keys to be present,
  `QueryProcessorBuilderExtensions.cs:30-34`), per FR4: a caller-supplied registry is not
  required to contain any particular key, and a missing/unresolved key surfaces at build time
  via FR7 instead.
- DI: `AddResiliencePipelines(this IDarkerHandlerBuilder, ResiliencePipelineRegistry<string>)`
  registers the registry as the `ResiliencePipelineProvider<string>` service and the
  decorators; `null` throws `ArgumentNullException`. No new lifetime is introduced.
- Defaults: `DefaultResiliencePipelines()` / `AddDefaultResiliencePipelines()` register a
  default retry pipeline and a default circuit-breaker pipeline under **new constants**
  (`Constants.RetryPipelineName`, `Constants.CircuitBreakerPipelineName`) that do **not**
  clash with the legacy `Constants.RetryPolicyName` / `Constants.CircuitBreakerPolicyName`
  (RD4). Exact strategy parameters are chosen at implementation time (FR6); the contract is
  only that each is resolvable and executable with no further configuration. Defaults register
  **non-generic** builders, so they serve the shared path; they do **not** support
  `UseTypePipeline` (§4).

### Architecture Overview

```
[UseResiliencePipeline(step, "Retry", useTypePipeline:false)]   (attribute, FR3)
        │  GetAttributeParams() -> { "Retry", false }
        │  GetDecoratorType()   -> UseResiliencePipelineHandler<,>
        ▼
PipelineBuilder builds decorator  ──► InitializeFromAttributeParams (FR7: validate @ build)
        ▼
UseResiliencePipelineHandler<TQuery,TResult>          (decorator, FR2)
        │  pipeline = useTypePipeline                               (FR8)
        │    ? Context.ResiliencePipeline.GetPipeline<TResult>("Retry")  // per (key,TResult)
        │    : Context.ResiliencePipeline.GetPipeline("Retry")           // shared
        ▼
   async: ResilienceContext==null ? ExecuteAsync(ct) : ExecuteAsync(resilienceContext)   (FR9/RD2)
        ▼
        next(query)  ──►  handler

Registration (FR4/FR5/FR6):
  builder.ResiliencePipelines(registry) | services.AddResiliencePipelines(registry)
  builder.DefaultResiliencePipelines()  | services.AddDefaultResiliencePipelines()
        └─ constructs concrete ResiliencePipelineRegistry<string>, supplied as provider
```

### Key Components

| Component | Role (stereotype) | Responsibility |
|-----------|-------------------|----------------|
| `IQueryContext.ResiliencePipeline` / `.ResilienceContext` | information holder | knows the configured provider and ambient resilience context |
| `UseResiliencePipelineAttribute[Async]` | interfacer / structurer | declares intent: names pipeline, type-scope flag, decorator type, step |
| `UseResiliencePipelineHandler[Async]` | service provider / coordinator | selects shared vs type-scoped resolution (FR8), resolves the pipeline, executes `next` through it, branches on `ResilienceContext` (FR9) |
| Builder/DI extensions | structurer | wire registry + decorators; own argument validation |

### Technology Choices

Polly `8.6.6` only — both APIs ship in it, so NFR3 holds with no new package. The decorator
uses **two** Polly resolution APIs by the `useTypePipeline` flag: the non-generic
`ResiliencePipeline` (`GetPipeline(key)`) for the shared path, which executes callbacks of any
result type and so serves default pipelines; and the result-typed `ResiliencePipeline<TResult>`
(`GetPipeline<TResult>(key)`) for the type-scoped path, where Polly caches one instance per
`(key, TResult)` and so yields per-result-type breaker state with no Darker-authored key
scheme. The behaviour of both APIs was verified against Polly 8.6.6 (see Verification).

### Verification

The Polly 8.6.6 registry semantics this decision relies on were checked with a throwaway probe
(`ResiliencePipelineRegistry<string>`, since removed):

- `GetPipeline<TResult>(key)` resolves when `TryAddBuilder<TResult>(key, …)` is registered, and
  returns the **same cached instance** for repeated `(key, TResult)` — confirms per-result-type
  state with sharing inside a result type.
- `GetPipeline<TResult>(key)` for an **unregistered** `TResult` throws `KeyNotFoundException`
  ("Unable to find a generic resilience pipeline of '…' associated with the key …").
- A **non-generic** `TryAddBuilder(key, …)` does **not** satisfy `GetPipeline<TResult>(key)`
  (throws `KeyNotFoundException`); generic and non-generic registrations are separate
  namespaces. This is why defaults (non-generic only) cannot serve `UseTypePipeline`.

The probe was throwaway and is removed, but these properties become **permanent** regression
coverage: AC4 exercises the per-`(key, TResult)` caching/independence, and AC5 exercises the
unregistered-key `ConfigurationException` (built on the `KeyNotFoundException` above).

### Implementation Approach

Structural first (Tidy First — the `/tidy-first` skill, `.claude/commands/refactor`): add the
two `IQueryContext` members and thread the optional provider through `QueryProcessor` +
`QueryProcessorBuilder` (no behaviour change to existing paths; legacy tests stay green). Then
behavioural, test-first per AC: attributes → decorators (sync, then async with both FR9
branches) → builder/DI extensions → default pipelines → type-scoped `GetPipeline<TResult>` path.
Each step follows `/test-first`.

## Consequences

### Positive

- Developers get the modern Polly V8 model registered by name and applied by attribute, with
  the same mental model as Brighter for the attribute/option surface (NFR4).
- Legacy `[RetryableQuery]` / `Policies(...)` is byte-for-byte unchanged; the two paths coexist
  (NFR1). The new members are purely additive.
- The type-scoped path is **registry-native**: Polly's per-`(key, TResult)` caching gives
  independent breaker state with no Darker-authored key format, no string parsing, and no
  custom provider wrapper.
- The narrow provider on the context keeps the consumer contract minimal (RD1).

### Negative

- `IQueryContext` grows two members; every `IQueryContext` implementation must add them. The
  only production implementation is `QueryContext` (`src/Paramore.Darker/QueryContext.cs`), so
  that is where the members are added. The test-double *factory* `TrackingQueryContextFactory`
  (`test/Paramore.Darker.Core.Tests/TestDoubles/TrackingQueryContextFactory.cs`) is an
  `IQueryContextFactory` that constructs a `QueryContext`, so it inherits the members; only a
  bespoke hand-rolled `IQueryContext` would need to add them. (There is no `InMemoryQueryContext`
  type; `InMemoryQueryContextFactory` also returns a `QueryContext`.)
- **Independence is per result type, not per query type** (§4). Two query types sharing a
  result DTO share breaker state under the same key. Callers needing strict per-query isolation
  use distinct pipeline keys per handler. This is a documented narrowing of FR8's original
  "per query type" intent (requirements updated to match).
- **Default pipelines do not support `UseTypePipeline`** (§4/§5): defaults register non-generic
  builders, and Polly's generic resolution is a separate namespace, so type scoping requires a
  caller-supplied registry with `TryAddBuilder<TResult>` per result type.
- Darker deliberately diverges from Brighter on the context member type (provider, not the
  concrete registry — RD1). A documented narrowing, not a parity gap.

### Risks and Mitigations

- **Risk:** a user enables `useTypePipeline:true` against a registry (or the defaults) that has
  no `TryAddBuilder<TResult>` for the handler's result type. *Mitigation:* FR7 build-time
  validation (via `TryGetPipeline<TResult>`) surfaces it immediately with the unresolved key in
  the message; the ADR and XML docs state the `TryAddBuilder<TResult>` precondition and the
  defaults limitation explicitly.
- **Risk:** async context-propagation branch (FR9) is the most error-prone area. *Mitigation:*
  dedicated ACs (AC7 both already-cancelled and in-flight; AC8 context observed) with
  deterministic zero-delay strategies (NFR5).
- **Risk:** the per-result-type semantics surprise a user expecting per-query-type isolation.
  *Mitigation:* requirements FR8/AC4 and the "Type pipeline" key term updated to per-result-type
  wording; documented in Negative consequences and XML docs on the attribute.

## Alternatives Considered

- **Composite query-type-qualified string key** (e.g. `"Retry:My.Ns.GetFooQuery"`) resolved via
  the non-generic `GetPipeline(key)`. This *would* give true per-query-type independence. Rejected
  after verifying Polly 8.6.6: the registry resolves by **exact key** with no wildcard/dynamic
  builder hook, so `DefaultResiliencePipelines()` cannot pre-seed builders for query types it
  cannot enumerate, making defaults + `UseTypePipeline` infeasible without a custom provider
  wrapper that widens RD1. The chosen `GetPipeline<TResult>` is registry-native and needs no
  such wrapper. (Decision after `review-design.md` Finding 1; user decision 2026-06-17. Trade-off:
  independence is per result type, not per query type.)
- **`GetPipeline<TQuery>(key)` returning `ResiliencePipeline<TQuery>`.** Rejected: cannot
  execute a `TResult`-returning handler — `ResiliencePipeline<TQuery>.Execute<TResult>`
  constrains `TResult : TQuery`, and a query's result type is not generally assignable to its
  query type, so the call does not compile.
- **Custom `ResiliencePipelineProvider<string>` wrapper** that lazily `GetOrAddPipeline`s a
  query-type-qualified key from a base builder. Rejected for this iteration: it restores true
  per-query-type independence but widens RD1 (the context/registration must carry add-on-demand
  capability) and adds a bespoke provider to own and test. Recorded as a future option if
  per-query-type isolation becomes a hard requirement.
- **Expose the concrete `ResiliencePipelineRegistry<string>` on the context** (Brighter parity).
  Rejected per RD1: the decorators only resolve by key; the provider is the minimal contract.
  The registry is still what registration constructs.
- **Populate `ResilienceContext` from the processor.** Rejected: ownership/lifetime and the
  authoritative cancellation token belong to the caller (FR9); the processor only reads it.
- **Deprecate/replace the legacy path.** Explicitly out of scope (issue #293 direction is
  additive); would break NFR1.

## References

- Requirements: [specs/009-resilience_pipeline/requirements.md](../../specs/009-resilience_pipeline/requirements.md)
- Related ADRs: [0002 Attribute-Driven Decorator Pipeline](0002-attribute-driven-decorator-pipeline.md),
  [0004 Factory & Registry Abstractions](0004-factory-registry-abstractions.md),
  [0005 Dual Sync/Async Support](0005-dual-sync-async-support.md),
  [0010 Pass Query Context](0010-pass-query-context.md),
  [0014 Factory Component Lifetime](0014-factory-component-lifetime.md)
- Legacy pattern followed: `src/Paramore.Darker/Policies/Handlers/RetryableQueryDecorator[Async].cs`,
  `Attributes/RetryableQueryAttribute[Async].cs`, `Policies/QueryProcessorBuilderExtensions.cs`,
  `src/Paramore.Darker.Extensions.DependencyInjection/PolicyDIExtensions.cs`,
  `QueryProcessor.cs:102-106`
- Brighter reference (naming/shape): `UseResiliencePipelineAttribute[Async]`,
  `ResilienceExceptionPolicyHandler[Async]`, `IRequestContext.ResiliencePipeline`
- Design review that forced the §4 redesign: [specs/009-resilience_pipeline/review-design.md](../../specs/009-resilience_pipeline/review-design.md) (Finding 1)
- Polly V8: `ResiliencePipelineProvider<TKey>` (`GetPipeline(key)` / `GetPipeline<TResult>(key)` / `TryGetPipeline[<TResult>]`), `ResiliencePipelineRegistry<TKey>` (`TryAddBuilder[<TResult>]`), `ResilienceContext`
- Polly V8: `ResiliencePipelineProvider<TKey>`, `ResiliencePipelineRegistry<TKey>`,
  `ResilienceContext`
