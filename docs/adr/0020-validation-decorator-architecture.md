# 20. Validation Decorator Architecture

Date: 2026-07-18

## Status

Accepted

## Context

Darker has no first-class stage for validating a query's inputs before its handler runs. Although
queries are read-only, malformed parameters can throw deep in a handler, drive needless database
load, or open injection risks. Brighter (Darker's command-side counterpart) already provides
request validation via a provider-agnostic abstraction with FluentValidation and DataAnnotations
providers; because the two libraries are used as a pair, Darker should offer an equivalent,
consistent capability.

**Parent Requirement**: [specs/013-validation-decorator/requirements.md](../../specs/013-validation-decorator/requirements.md)

**Scope**: This ADR decides the **architecture of query validation** as a single cohesive decision:
the provider-agnostic core abstraction (attribute, decorator, exception, error record), how it plugs
into Darker's existing decorator pipeline, and the provider strategy that maps that abstraction to
FluentValidation and DataAnnotations implementations via DI. It resolves the four decisions recorded
in the requirements (fail-fast on missing validator; two provider packages over a dependency-free
core; Brighter-style `Use*` registration; a shared `QueryValidationError` record).

### Forces at play

- **Consistency with Brighter (NFR)** — naming, packaging, and registration ergonomics should mirror
  Brighter so paired usage is predictable: `[ValidateRequest]` → `[ValidateQuery]`,
  `RequestValidationException` → `QueryValidationException`, `RequestValidationError` →
  `QueryValidationError`, `UseFluentValidation()` / `UseDataAnnotations()` unchanged.
- **Darker uses decorators, not a handler chain** — Brighter maps an abstract
  `ValidateRequestHandler<TRequest>` to a concrete provider handler. Darker's equivalent seam is the
  **decorator**: a `[QueryHandlerAttribute]` on the handler's execute method returns a decorator
  *type* via `GetDecoratorType()`, and `PipelineBuilder` resolves that type from the
  `IQueryHandlerDecoratorFactory`. The Brighter pattern therefore translates to Darker as
  *abstract decorator → concrete provider decorator*, not handler-to-handler.
- **Decorators are DI-resolved open generics** — with the DI extension, decorators are registered as
  open generic service descriptors and resolved through `ServiceProviderComponentFactory`
  (`serviceProvider.GetService(closedDecoratorType)` or the per-query child scope). This is exactly
  the seam Brighter's `UseDataAnnotations()` exploits: an open-generic `ServiceDescriptor` mapping the
  provider-agnostic decorator type to a concrete provider type lets DI close the generic per query
  and constructor-inject a per-`TQuery` validator. No new resolution machinery is required.
- **Dependency-free core (FR10)** — the core abstractions must not reference FluentValidation or
  `System.ComponentModel.DataAnnotations`; those dependencies live only in their provider packages.
- **Fail fast (FR9)** — a query marked `[ValidateQuery]` with no available validator is a
  configuration error, not a silent no-op. Darker already has `Paramore.Darker.Exceptions.ConfigurationException`
  (used by the policy decorators for exactly this "you asked for X but didn't configure it" case).
- **Async-first (NFR)** — Darker exposes paired sync/async decorator interfaces
  (`IQueryHandlerDecorator<TQuery,TResult>` / `IQueryHandlerDecoratorAsync<TQuery,TResult>`) and
  paired attributes (`QueryHandlerAttribute` / `QueryHandlerAttributeAsync`). Validation must supply
  both, honouring async validators on the async path.
- **Extensible to Specification later (NFR)** — a future Specification provider must be addable as a
  new package + `UseSpecification()` with no change to core or existing providers.

### The core seam (why this is one decision, not a bolt-on)

The crux is *where the provider-agnostic/provider-specific boundary sits*. Brighter puts it at the
handler type. Darker's natural equivalent is a **template-method decorator**: a core abstract
decorator owns the pipeline-facing behaviour ("validate; on failure throw and short-circuit; on
success call `next`"), and defers only "produce the list of errors for this query" to a provider
subclass. The attribute names the *abstract* decorator; the `Use*` call decides which *concrete*
subclass DI resolves. Splitting core-vs-provider into separate ADRs would obscure that this single
boundary is what makes both the dependency-free core and the pluggable providers work.

## Decision

Adopt a **provider-agnostic template-method decorator** plugged into Darker's existing decorator
pipeline, with provider packages that supply concrete decorators mapped in DI via Brighter-style
`Use*` extensions.

### Architecture Overview

```
[ValidateQuery(step)]  ─ attribute on handler's Execute/ExecuteAsync
        │  GetDecoratorType() → typeof(ValidateQueryDecoratorAsync<,>)   (the ABSTRACT core type)
        ▼
PipelineBuilder ── resolves decorator type from IQueryHandlerDecoratorFactory
        │
        ▼
DI:  ValidateQueryDecoratorAsync<,>  ──mapped by UseX()──►  <Provider>QueryValidatorDecoratorAsync<,>
        │                                                          (concrete subclass, provider pkg)
        ▼
 ┌─────────────────────────────────────────────────────────────────────────────┐
 │ abstract ValidateQueryDecoratorAsync<TQuery,TResult>  (Paramore.Darker.Validation) │
 │   ExecuteAsync(query, next, fallback, ct):                                    │
 │     errors = await ValidateAsync(query, ct)     ◄── abstract, provider-supplied │
 │     if (errors.Count > 0) throw new QueryValidationException(errors)           │
 │     return await next(query, ct)                                              │
 └─────────────────────────────────────────────────────────────────────────────┘
        ▲                                             ▲
        │ overrides ValidateAsync                     │ overrides ValidateAsync
FluentValidationQueryValidatorDecoratorAsync   DataAnnotationsQueryValidatorDecoratorAsync
  resolve IValidator<TQuery> (FluentValidation)   Validator.TryValidateObject (reflection)
  no validator → ConfigurationException           map ValidationResult → QueryValidationError
  map ValidationFailure → QueryValidationError
```

Validation runs **before** `next`, so on failure the handler (and any inner decorators) never runs.
Placement within the pipeline relative to other decorators is controlled by the attribute's `Step`,
exactly like every other Darker decorator.

### Key Components

**Core — `Paramore.Darker.Validation` (dependency-free):**

- `QueryValidationError` — an *information holder* record for one failure. Mirrors Brighter's
  `RequestValidationError`:
  ```csharp
  public sealed record QueryValidationError(
      string PropertyName,
      string ErrorMessage,
      object? AttemptedValue = null,
      string? ErrorCode = null);
  ```
  `ErrorCode` is nullable so the DataAnnotations provider (no native error-code concept) may leave it
  null.
- `QueryValidationException` — an *information holder* exception carrying
  `IReadOnlyCollection<QueryValidationError> Errors`. Thrown when validation fails.
- `ValidateQueryAttribute` / `ValidateQueryAttributeAsync` — *interfacer/structurer*: mark a handler
  for validation and, via `GetDecoratorType()`, name the **abstract** decorator open generic
  (`typeof(ValidateQueryDecorator<,>)` / `typeof(ValidateQueryDecoratorAsync<,>)`).
  `GetAttributeParams()` returns empty (no per-attribute state beyond `Step`).
- `ValidateQueryDecorator<TQuery,TResult>` / `ValidateQueryDecoratorAsync<TQuery,TResult>` —
  **abstract** *coordinators*. They implement Darker's decorator interface, own the
  decide-and-short-circuit behaviour, and declare one abstract member the provider supplies:
  `IReadOnlyCollection<QueryValidationError> Validate(TQuery query)` /
  `Task<IReadOnlyCollection<QueryValidationError>> ValidateAsync(TQuery query, CancellationToken)`.

**Provider — `Paramore.Darker.Validation.FluentValidation`:**

- `FluentValidationQueryValidatorDecorator<,>` / `...Async<,>` — concrete *service providers*
  subclassing the core decorators. Constructor-inject the per-query FluentValidation
  `IValidator<TQuery>` (resolved from DI); if none is available, throw `ConfigurationException`
  (fail-fast, FR9). Override `Validate`/`ValidateAsync` to run the validator and map each
  `ValidationFailure` → `QueryValidationError` (property, message, attempted value, error code).
- `UseFluentValidation()` — `IDarkerHandlerBuilder` extension that registers the abstract→concrete
  open-generic mapping so DI resolves the FluentValidation decorator.

**Provider — `Paramore.Darker.Validation.DataAnnotations`:**

- `DataAnnotationsQueryValidatorDecorator<,>` / `...Async<,>` — concrete subclasses using
  `System.ComponentModel.DataAnnotations.Validator.TryValidateObject(..., validateAllProperties:true)`;
  map each `ValidationResult` → `QueryValidationError` (member name, message; `AttemptedValue`/
  `ErrorCode` left null as DataAnnotations has no equivalent). Validation constraints are declared as
  attributes on the query type — nothing per-query needs registering, so this provider does **not**
  fail-fast on a "missing validator".
- `UseDataAnnotations()` — the mapping extension.

### Technology Choices

- **FluentValidation** (primary) via the provider package; version pinned in
  `Directory.Packages.props` (CPM).
- **System.ComponentModel.DataAnnotations** (in-box) for the lightweight provider — no external
  dependency beyond the BCL.
- **Reuse `Paramore.Darker.Exceptions.ConfigurationException`** for the fail-fast rather than adding
  a new exception type — consistent with the policy decorators.
- **Reuse `ServiceProviderComponentFactory` / decorator registry** — no new DI plumbing; providers
  only add open-generic service descriptors.

### Implementation Approach

1. Add core `Paramore.Darker.Validation` with the error record, exception, attributes (sync + async),
   and abstract decorators (sync + async). No third-party references.
2. The abstract decorator's `Execute`/`ExecuteAsync` implements the template method; only
   `Validate`/`ValidateAsync` is abstract.
3. Add `Paramore.Darker.Validation.FluentValidation` with concrete decorators, failure mapping, and
   `UseFluentValidation()`. Fail-fast with `ConfigurationException` when no `IValidator<TQuery>` is
   registered.
4. Add `Paramore.Darker.Validation.DataAnnotations` with concrete decorators, failure mapping, and
   `UseDataAnnotations()`.
5. Each `Use*` registers the open-generic mapping
   (`typeof(ValidateQueryDecorator<,>)` → concrete, and the async pair) with the configured handler
   lifetime, and registers the decorator with Darker's decorator registry so the pipeline can resolve
   it — mirroring how policy decorators register today.
6. Tests (TDD) cover: valid pass-through, invalid → `QueryValidationException` (+ handler not
   invoked), missing FluentValidation validator → `ConfigurationException`, identical
   `QueryValidationError` shape across both providers, and both sync and async paths — following
   Brighter's test structure.

## Consequences

### Positive

- **Consistent with Brighter** — same names, same `Use*` ergonomics; paired teams get one mental
  model across commands and queries.
- **Dependency-free core** — applications and providers depend only on what they use; FluentValidation
  is not forced on DataAnnotations users or on the core.
- **Purely additive extensibility** — a future `Paramore.Darker.Validation.Specification` +
  `UseSpecification()` needs no change to core or existing providers (satisfies the extensibility NFR
  even though Specification is out of scope now).
- **No new pipeline machinery** — reuses the existing attribute/decorator/DI seam and
  `ConfigurationException`; low surface-area, low risk.
- **Opt-in and ordered** — validation is per-handler via the attribute and slots into the pipeline by
  `Step`, so handlers that don't opt in pay nothing.

### Negative

- **Two decorator variants per provider** (sync + async) plus a `Use*` method — more types than a
  single-path design, but required by Darker's existing sync/async duality and consistent with every
  other decorator.
- **Abstract-decorator indirection** — the attribute names an abstract type that DI must have been
  told how to resolve. Forgetting the `Use*` call yields a resolution failure; mitigated by
  documenting `Use*` as mandatory and by the fail-fast messaging.

### Risks and Mitigations

- **Risk: `[ValidateQuery]` present but no `Use*` provider registered** → DI cannot resolve the
  abstract decorator. *Mitigation*: document that a provider `Use*` call is required; ensure the
  resolution failure surfaces a clear message (consider a guard that rethrows as
  `ConfigurationException` explaining a provider must be selected).
- **Risk: DataAnnotations error codes differ from FluentValidation**, weakening the "identical shape"
  guarantee. *Mitigation*: `QueryValidationError.ErrorCode` is nullable; the contract is *same shape*,
  not *same values*. Documented explicitly.
- **Risk: async validator invoked on the sync path** (FluentValidation supports async rules).
  *Mitigation*: the sync decorator uses the synchronous `Validate`; async-only rules on a
  sync-validated query are a caller error, consistent with FluentValidation's own guidance.
- **Risk: decorator lifetime vs injected validator lifetime mismatch.** *Mitigation*: register the
  provider decorators with the same handler lifetime the existing decorator registry uses; validators
  are resolved through the same per-query scope as other components.

## Alternatives Considered

- **Copy Brighter's handler-chain verbatim (abstract `ValidateQueryHandler<TQuery>` mapped to
  provider handlers).** Rejected: Darker's pipeline is decorator-based, not a Brighter-style handler
  chain; forcing a handler abstraction would fork the pipeline machinery instead of reusing the
  established attribute/decorator seam.
- **Single core decorator that resolves the provider via a strategy interface
  (`IQueryValidator<TQuery>`) injected into one concrete decorator.** Viable, but it moves provider
  selection from the well-understood DI open-generic mapping into a second abstraction and a second
  registration step. The template-method + abstract-decorator approach maps 1:1 onto Brighter's
  `Use*` mechanic, keeping the two libraries aligned; recorded as the leading fallback if the
  abstract-decorator indirection proves awkward.

  **Decision (confirmed):** the essential trade-off is *one `[ValidateQuery]` whose provider is
  swapped at registration* versus *one attribute per validator type baked in at the call site*. The
  single-attribute Brighter approach is chosen because it lets an application switch validation
  providers (FluentValidation ↔ DataAnnotations ↔ a future Specification provider) via the `Use*`
  registration without touching any handler's attributes. That switchability outweighs the abstract
  decorator indirection.
- **Bake FluentValidation into the core package.** Rejected: violates FR10 (dependency-free core) and
  forces the dependency on DataAnnotations-only users.
- **Global/automatic validation of every query.** Rejected: out of scope; validation is opt-in per
  handler via the attribute.
- **Return a result object instead of throwing.** Rejected: inconsistent with Brighter (which throws
  `RequestValidationException`) and with Darker's existing exception-propagation model; throwing lets
  ASP.NET Core middleware translate to a 400 uniformly.

## References

- Requirements: [specs/013-validation-decorator/requirements.md](../../specs/013-validation-decorator/requirements.md)
- Linked issue: [#300 — Add validation decorator with FluentValidation support](https://github.com/BrighterCommand/Darker/issues/300)
- Related ADRs: [0015-resilience-pipeline-integration](0015-resilience-pipeline-integration.md),
  [0016-pipeline-attribute-memoization](0016-pipeline-attribute-memoization.md) (decorator/pipeline mechanics)
- Prior art (Brighter): [ADR 0063 — request-validation-handler](https://github.com/BrighterCommand/Brighter/blob/master/docs/adr/0063-request-validation-handler.md),
  [Validation implementation](https://github.com/BrighterCommand/Brighter/tree/master/src/Paramore.Brighter/Validation),
  [tests](https://github.com/BrighterCommand/Brighter/tree/master/tests/Paramore.Brighter.Core.Tests/Validation)
- Darker mechanics: `src/Paramore.Darker/QueryHandlerAttribute.cs`,
  `src/Paramore.Darker/IQueryHandlerDecorator.cs`,
  `src/Paramore.Darker/Policies/Handlers/RetryableQueryDecoratorAsync.cs` (template for a DI-resolved decorator),
  `src/Paramore.Darker.Extensions.DependencyInjection/ServiceProviderComponentFactory.cs`,
  `src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionDecoratorRegistry.cs`
