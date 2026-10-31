# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: #305

## Problem Statement

As a developer using Darker, I would like configuration mistakes in my query pipelines to be reported when the host starts, so that I do not discover them in production when a particular query is first executed.

Today the following only fail, or silently misbehave, at first execution:

- A query with no registered handler, or a decorator that is not registered, throws `ConfigurationException` (or `RoutingException` for a router) when that query is first run.
- Handler interfaces are split into sync and async (#304), so it is possible to register an async-only handler and call `Execute()`, or the reverse. This fails only on that code path.
- Only public handlers are supported. A handler is the public facade of a module and is tested through that facade. Assembly scanning skips non-public types, so a handler declared `internal` is silently never registered. Registering an internal handler explicitly is also unsupported.
- Decorator attributes carry a `Step` number that orders the pipeline (the decorator with the lowest `Step` is outermost and runs first). Two decorators with the same step have no defined order, and an ordering can leave a decorator unable to do its job.
- Lifetime and scope configuration (spec 015, #377) can be coherent-looking but inert or unsafe, and none of it is reported:
  - `JoinAmbient` is requested but `HandlerLifetime` is not `Scoped`, or no scope provider is registered, so the request scope can never be adopted.
  - A handler or decorator configured `Singleton` takes a constructor dependency registered `Scoped` (a captive dependency).
  - More than one `IAmAScopeProvider` implementation is registered, and only one is used.

Brighter solves the same problem with an opt-in `ValidatePipelines()` that runs at host start (Brighter ADR 0053), with lifetime and scope rules added beside it (Brighter ADR 0074). Darker's `QueryProcessorBuilder`/`AddDarker` users should get the equivalent.

## Proposed Solution

An opt-in `ValidatePipelines()` call on the Darker builder that registers a startup validator. When the host starts, the validator checks every registered query pipeline and the lifetime/scope configuration, and reports every problem it finds, not just the first. Errors stop the host from starting (by default); warnings are logged and never block.

```csharp
services.AddDarkerRequestScope();
services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
    .AddHandlersFromAssemblies(typeof(MyHandler).Assembly)
    .AddDefaultPolicies()
    .ValidatePipelines();   // call last
```

The validator is also available to inject and call on demand, returning a result object, so a test or a health check can use it.

## Requirements

### Functional Requirements

**FR1 — Opt-in registration.** `ValidatePipelines(throwOnError = true)` is available on `IDarkerHandlerBuilder`. Without it, nothing is validated and no behaviour changes.

**FR2 — Runs at host start.** When `ValidatePipelines()` has been called, validation runs once as the host starts, before queries are served. It runs before the web server starts. It does not depend on the registration order of other hosted services' `StartAsync`.

**FR3 — Report everything.** Validation collects all findings into one result and does not stop at the first. Each finding has a severity (error or warning), a rule identifier, and a message naming the offending query, handler, decorator or option.

**FR4 — Outcome.** If there are errors and `throwOnError` is true, host start fails with a `PipelineValidationException` whose message lists every finding. If `throwOnError` is false, errors are logged at Error level and start proceeds. Warnings are always logged at Warning level and never block start. A result with no findings logs nothing above Debug.

**FR5 — On-demand use.** The validator is available on demand, through the container or a combined entry point that runs every registered validator, and returns the result object without throwing, so tests and (future) health checks can use it.

**FR6 — Handler resolution and visibility (errors).**
- Every query type in the handler registries (sync, async and stream) has a handler registered.
- The handler type is public. Only public handlers are supported (the policy that assembly scanning already enforces), so an internal handler registered explicitly is an error.
- The handler type is registered with the container, as the resolvability probe reports. Validation asks the question and does not create the handler.

**FR7 — Decorator resolution (errors).** Every decorator type named by a decorator attribute on a registered handler's `Execute`/`ExecuteAsync`/stream method is registered with the container, as the resolvability probe reports. Validation does not create the decorator.

**FR8 — Sync/async consistency (errors).** A handler is checked against the execution paths it can be reached by, so that these are reported:
- a query registered only in the async registry whose handler has no async implementation, and the reverse;
- a handler whose decorator attributes are of the wrong kind for its path (a sync decorator attribute on an async handler, or the reverse), which would otherwise be silently ignored.

**FR9 — Decorator step ordering.**
- Two decorator attributes on the same handler method with the same `Step` are reported (warning), since their order is undefined.
- A decorator ordering that makes a decorator ineffective is reported. The specific ordering rules (for example fallback relative to retry) are decided in design and listed in the ADR.

**FR10 — Inert scope opt-in (error).** `DefaultScopeAffinity` or a registered scope affinity is `JoinAmbient` (including via `AddDarkerRequestScope()`), but `HandlerLifetime` is not `Scoped`, or no (unkeyed) `IAmAScopeProvider` is registered. Either way the opt-in cannot take effect.

**FR11 — Captive dependency (warning).** A handler or decorator whose configured lifetime is `Singleton` takes a constructor dependency whose service registration is `Scoped`.

**FR12 — Duplicate scope providers (warning).** More than one distinct `IAmAScopeProvider` implementation is registered.

**FR13 — Guidance in messages.** Every lifetime/scope finding (FR10–FR12) points at the documentation that explains the fix. The README/docs gain a section on `ValidatePipelines()` and the rule list.

**FR14 — Call-order independence of reporting.** Findings do not depend on whether `ValidatePipelines()` was called before or after the handlers and `AddDarkerRequestScope()` were registered, or the documentation states clearly that it must be called last. (Brighter snapshots descriptors at call time, so it must be called last; design decides which Darker adopts.)

### Non-functional Requirements

- **Opt-in and non-breaking.** Existing applications that do not call `ValidatePipelines()` behave exactly as before. No existing public signature changes.
- **Startup cost.** Validation uses reflection once, at start. It must not run per query and must not change per-query performance.
- **No side effects.** Validation must not execute a handler or a decorator, run a query, or create a handler, a decorator or a scope. It reads types, attributes and registrations only, so there is nothing to release.
- **Core purity.** Container-specific checks (lifetimes, descriptors) live in the DI package. Core gains no `Microsoft.Extensions.DependencyInjection` types.
- **Messages.** Messages are actionable: they name the type and say what to change.
- **Multi-target.** Works on every target framework the packages already support.

### Constraints and Assumptions

- Follows the structure of Brighter's `PipelineValidator` and `ScopeConfigurationValidator` where it fits Darker, but is not bound to its type names.
- The Darker DI package does not currently reference `Microsoft.Extensions.Hosting.Abstractions`. Running at host start needs a hosted service, so design must choose how to take that dependency (or an equivalent hook) without breaking non-hosted users. Tracked as a design question, not decided here.
- Brighter implements its rules with a Specification pattern (`ISpecification<T>` instances that each hold the predicate and the metadata to report on failure; Brighter ADR 0053). Darker takes no dependency on Brighter, and Brighter takes none on Darker, so Darker will copy the pattern into its own codebase, in Darker's namespaces, rather than reference it. The copy is adapted to Darker's types and is not kept in sync by a shared package. Design decides which parts are copied and where they live (core versus the DI package).
- Handler registration is by assembly scanning and explicit registration; the set of registered queries is knowable from the registries at validation time.
- Lifetimes read from `DarkerOptions` at host start; descriptors read from the service collection.
- Spec 015 types (`ScopeAffinity`, `IAmAScopeProvider`, `ScopeAffinityRegistration`) are the inputs for FR10–FR12.

### Out of Scope

- ASP.NET Core health-check integration (follow-up issue; FR5 leaves the door open).
- Automatic validation in Development via `IHostEnvironment.IsDevelopment()`.
- Making validation mandatory or on by default.
- A diagnostic report that describes the pipelines (Brighter's `DescribePipelines()`).
- A Roslyn analyzer.
- Validating the query *context* factory, caching, or validation-decorator configuration beyond registration.
- Proving that a registered handler's constructor dependencies resolve. That needs instantiation. The container's `ValidateOnBuild` option covers it.
- Changing runtime behaviour of the pipeline, or the runtime adoption diagnostics from spec 015.

## Acceptance Criteria

- Calling `ValidatePipelines()` on a correctly configured application starts cleanly and reports no errors.
- For each of FR6–FR12 there is a test that builds a misconfiguration, runs validation, and asserts the rule identifier, severity and that the message names the offender.
- Several misconfigurations at once are all reported in a single result (FR3).
- `throwOnError = true` fails start with `PipelineValidationException`; `false` logs and proceeds; warnings alone never fail start (FR4).
- An application that does not call `ValidatePipelines()` is unaffected, and the existing suite passes unchanged.
- Validation creates no handler, decorator or scope (a test with a handler whose constructor records that it ran).
- README documents the call, the order (if it must be last), the rules and how to fix each lifetime finding.

## Additional Context

- Issue #305 and its comment (lifetime rules added in the scope of #377).
- V5 discussion #273.
- Brighter: ADR 0053 (pipeline validation at startup), ADR 0074 (lifetime validation evaluation site); `src/Paramore.Brighter/Validation/PipelineValidator.cs`; `src/Paramore.Brighter.Extensions.DependencyInjection/ScopeConfigurationValidator.cs`.
- Darker ADRs 0022–0024 (spec 015) define the lifetime and scope model the FR10–FR12 rules check.
- Next ADR number: 0025.
