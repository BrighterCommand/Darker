# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: [#300 — Add validation decorator with FluentValidation support](https://github.com/BrighterCommand/Darker/issues/300)

## Problem Statement

As a **developer using Darker**, I would like **to validate query inputs before the handler executes**, so that **malformed query parameters are rejected early with structured error details, rather than reaching the handler where they can cause exceptions, injection risks, or unnecessary database load**.

Queries are read-only and typically need less validation than commands, but input validation is still valuable. Darker currently has no first-class validation stage in the pipeline. Brighter (Darker's command-side counterpart) already provides request validation via a decorator/handler and a provider-agnostic validation abstraction. Because Brighter and Darker are used as a pair, Darker should offer an equivalent, consistent capability.

## Proposed Solution

Provide an opt-in validation decorator that a developer applies to a query handler. When a query flows through the pipeline, the decorator validates the query object before invoking the rest of the pipeline. If validation fails, the pipeline short-circuits and surfaces a structured, framework-agnostic validation failure to the caller.

From the developer's perspective:

- Annotate a handler's execute method with a `[ValidateQuery]` attribute to enable validation for that query.
- Register a validator for the query type in the DI container.
- Choose a validation provider: **FluentValidation** (primary) or **System.ComponentModel.DataAnnotations** (lightweight alternative), enabled via a registration call (e.g. `UseFluentValidation()` / `UseDataAnnotations()`), mirroring Brighter.
- On failure, receive a `QueryValidationException` carrying a provider-independent collection of errors (property name, message, attempted value, error code) suitable for structured reporting (e.g. HTTP 400 responses).

The design should follow Brighter's approach as closely as possible so the two libraries feel consistent to developers who use both.

## Requirements

### Functional Requirements

- **FR1** — Provide a `[ValidateQuery]` decorator attribute (with sync and async variants consistent with existing Darker decorators) that enables validation for the annotated query handler, participating in pipeline step ordering like other decorators.
- **FR2** — Provide a `ValidateQueryDecorator` that runs before the rest of the pipeline / handler (`next`).
- **FR3** — The decorator resolves the validator for the query type (`IValidator<TQuery>`) from the DI container.
- **FR4** — On successful validation, the query proceeds unchanged to the next step in the pipeline.
- **FR5** — On failed validation, throw a `QueryValidationException` and do **not** invoke the handler.
- **FR6** — `QueryValidationException` exposes a framework-agnostic, read-only collection of `QueryValidationError` records. Each error is the record `QueryValidationError(string PropertyName, string ErrorMessage, object? AttemptedValue = null, string? ErrorCode = null)` (mirroring Brighter's `RequestValidationError`). Both providers wrap their native failures into this record so the interface is identical regardless of provider.
- **FR7** — Provide FluentValidation support as a separate provider package (`Paramore.Darker.Validation.FluentValidation`) that maps FluentValidation failures to `QueryValidationError` and is enabled via a `UseFluentValidation()` builder-extension registration method.
- **FR8** — Provide `System.ComponentModel.DataAnnotations` support as a separate provider package (`Paramore.Darker.Validation.DataAnnotations`), enabled via a `UseDataAnnotations()` builder-extension registration method.
- **FR9** — Fail fast: if a query is marked for validation but no validator is registered for it, throw an exception (mirroring Brighter's `ConfigurationException` fail-fast behaviour) rather than silently skipping validation.
- **FR10** — The core validation abstractions must live in a dependency-free assembly; provider-specific dependencies (FluentValidation, DataAnnotations) live only in their respective provider packages.

### Non-functional Requirements

- **Consistency** — Naming, packaging, and registration ergonomics should mirror Brighter's validation implementation (`[ValidateRequest]` → `[ValidateQuery]`, `RequestValidationException` → `QueryValidationException`, `RequestValidationError` → equivalent) so paired usage is predictable.
- **Extensibility** — Adding a new validation provider must require only a new provider package + registration method, with no changes to core abstractions or existing providers (purely additive). In particular, the design must be extensible to a future Specification-pattern provider even though it is out of scope now.
- **Performance** — Validation adds a pipeline step; overhead should be proportional to the validation rules and must not impose measurable cost on handlers that do not opt in.
- **Async support** — Async validation paths must be honoured (async validators / `ExecuteAsync`), consistent with Darker's async-first handler model.
- **Targeting** — Must build and test against the framework targets currently supported by Darker (net8.0 and net9.0).

### Constraints and Assumptions

- Follow Brighter's approach as much as possible; prior art:
  - ADR: [0063-request-validation-handler](https://github.com/BrighterCommand/Brighter/blob/master/docs/adr/0063-request-validation-handler.md)
  - Implementation: [Paramore.Brighter/Validation](https://github.com/BrighterCommand/Brighter/tree/master/src/Paramore.Brighter/Validation)
  - Tests: [Paramore.Brighter.Core.Tests/Validation](https://github.com/BrighterCommand/Brighter/tree/master/tests/Paramore.Brighter.Core.Tests/Validation)
- Central Package Management (CPM) via `Directory.Packages.props` governs FluentValidation and any new package versions.
- Providers resolve validators from Microsoft.Extensions.DependencyInjection, consistent with Darker's DI integration.
- Assumption: validation is opt-in per handler (via attribute), not global.

### Out of Scope

- **Specification-pattern validation** — Brighter supports writing validation rules via the Specification pattern; Darker has no Specification implementation, so a Specification provider is explicitly out of scope for this feature. The design must remain extensible to add it later.
- Automatic/global validation of all queries without opt-in.
- Cross-query or cross-field validation requiring external I/O beyond what the chosen validator performs.
- Localization/translation of validation messages beyond what the underlying provider supplies.

## Acceptance Criteria

How we'll know this is working correctly:

- A query handler annotated with `[ValidateQuery]` and backed by a registered validator:
  - passes valid queries through to the handler and returns the result unchanged;
  - throws `QueryValidationException` for invalid queries **without** executing the handler.
- `QueryValidationException` exposes structured errors (property, message, attempted value, error code) that are identical in shape regardless of whether FluentValidation or DataAnnotations produced them.
- FluentValidation support is delivered as `Paramore.Darker.Validation.FluentValidation` and enabled via its registration method; DataAnnotations support is enabled via its registration method.
- Core validation abstractions carry no dependency on FluentValidation or DataAnnotations.
- A query marked for validation with no registered validator throws a fail-fast configuration exception (does not silently skip validation).
- Both sync and async validation paths are covered.
- Unit tests exist for: valid pass-through, invalid failure, missing-validator behaviour, error mapping for each provider, and async paths — following the TDD workflow and Brighter's test structure.
- Builds and tests pass on net8.0 and net9.0 via `Darker.Filter.slnf`.

## Resolved Decisions

The following were confirmed with the product owner and now inform the design:

1. **Missing validator behaviour** — **Mirror Brighter: fail fast by throwing an exception** (Brighter uses `ConfigurationException`). A query marked for validation with no registered validator is a configuration error, not a silent no-op. *(→ FR9)*
2. **Package layout** — **Follow Brighter with two provider packages:** `Paramore.Darker.Validation.FluentValidation` and `Paramore.Darker.Validation.DataAnnotations`. Core, dependency-free abstractions (attribute, decorator/handler, `QueryValidationException`, `QueryValidationError`) live in a core validation assembly. *(→ FR7, FR8, FR10)*
3. **Registration ergonomics** — **Follow Brighter's builder-extension pattern.** Each provider exposes a fluent extension (`UseFluentValidation()` / `UseDataAnnotations()`) that maps the provider-agnostic validate handlers to their provider implementations, registered as `Transient` (effective lifetime managed by the handler factory). Nothing else needs registering — validation is declared via the `[ValidateQuery]` attribute. Brighter reference:

   ```csharp
   public static IBrighterBuilder UseDataAnnotations(this IBrighterBuilder brighterBuilder)
   {
       if (brighterBuilder is null)
           throw new ArgumentNullException(nameof(brighterBuilder));

       brighterBuilder.Services.Add(new ServiceDescriptor(typeof(ValidateRequestHandler<>), typeof(DataAnnotationsRequestHandler<>), ServiceLifetime.Transient));
       brighterBuilder.Services.Add(new ServiceDescriptor(typeof(ValidateRequestHandlerAsync<>), typeof(DataAnnotationsRequestHandlerAsync<>), ServiceLifetime.Transient));

       return brighterBuilder;
   }
   ```
   *(→ FR7, FR8)*
4. **Error code semantics** — **Use a shared record for both providers**, mirroring Brighter's `RequestValidationError`:

   ```csharp
   public sealed record QueryValidationError(
       string PropertyName,
       string ErrorMessage,
       object? AttemptedValue = null,
       string? ErrorCode = null);
   ```
   Both FluentValidation and DataAnnotations providers wrap their native failures into `QueryValidationError`, giving a consistent interface. `ErrorCode` is nullable, so the DataAnnotations provider (which has no native error-code concept) may leave it null. *(→ FR6)*

## Additional Context

Darker mirrors Brighter as the query-side (CQRS read-side) counterpart. This feature brings Brighter's validation capability to Darker so teams using both get a consistent validation story across commands and queries. Prior-art links are listed under Constraints and Assumptions.
