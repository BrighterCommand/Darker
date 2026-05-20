# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: #320

## Problem Statement

As a developer using Darker, I would like to pass a `QueryContext` into `IQueryProcessor.Execute` and `ExecuteAsync`, so that I can provide additional information that the processing pipeline needs from outside the pipeline itself.

Currently, the `QueryContext` is always created internally by `QueryProcessor` via `IQueryContextFactory.Create()`. This prevents callers from passing in contextual information (such as OpenTelemetry trace context, correlation IDs, or other cross-cutting data) that decorators or handlers in the pipeline need access to.

This mirrors the same problem that was solved in Brighter V10, where `RequestContext` was made passable into `CommandProcessor` as an optional parameter.

## Proposed Solution

Add an optional (nullable) `QueryContext` parameter to both `Execute` and `ExecuteAsync` on `IQueryProcessor`:

- `Execute<TResult>(IQuery<TResult> query, IQueryContext queryContext = null)`
- `ExecuteAsync<TResult>(IQuery<TResult> query, IQueryContext queryContext = null, CancellationToken cancellationToken = default)`

In `QueryProcessor`'s implementation:
- If `queryContext` is null, create the context via `IQueryContextFactory` (current behavior)
- If `queryContext` is not null, use the provided context directly
- In both cases, call `InitQueryContext` to inject "global" references (e.g. policy registry) into the context before pipeline execution. This follows Brighter's `InitRequestContext` pattern where the `CommandProcessor` takes the policy registry as a constructor parameter (injected via DI) and sets it on the context in every `Send`/`Publish` call.

For `ExecuteAsync`, the `QueryContext` parameter should be the second parameter (before `CancellationToken`), following C# conventions where optional/defaulted parameters come last and `CancellationToken` is always the final parameter.

The `QueryProcessor` constructor takes the policy registry as a parameter (easily injected by DI). The `InitQueryContext` method then sets it on the context's `Bag`, so policy decorators continue to read from `Context.Bag` as they do today.

The serializer is a decorator-only concern — `QueryProcessor` has no reason to know about it. `QueryLoggingDecorator`/`QueryLoggingDecoratorAsync` receive `JsonSerializerSettings` via constructor injection from DI and new up their own `JsonSerializer`. This makes the future migration to `System.Text.Json` straightforward.

## Requirements

### Functional Requirements
- FR1: `IQueryProcessor.Execute` accepts an optional `IQueryContext` parameter (default null)
- FR2: `IQueryProcessor.ExecuteAsync` accepts an optional `IQueryContext` parameter as the second parameter, before `CancellationToken` (default null)
- FR3: When `queryContext` is null, `QueryProcessor` creates the context via `IQueryContextFactory.Create()`
- FR4: When `queryContext` is not null, `QueryProcessor` uses the provided context directly without calling `IQueryContextFactory`
- FR5: In both cases, `QueryProcessor` calls `InitQueryContext` to inject global references (policy registry) into the context's `Bag` before pipeline execution. `InitQueryContext` adds entries to the existing `Bag` dictionary instance (not replacing the dictionary reference) only if the key is not already present — caller-supplied Bag entries take precedence over processor-level defaults. These globals have the same lifetime as the `QueryProcessor` instance. This follows Brighter's `InitRequestContext` pattern.
- FR6: `QueryProcessor` takes `IPolicyRegistry<string>?` as a nullable constructor parameter (injected via DI, default null). `InitQueryContext` sets it on `Context.Bag` if non-null. If a decorator requires the policy registry but it was not configured, the decorator throws `ConfigurationException` (existing behavior preserved). Note: this introduces a Polly dependency to the core `Paramore.Darker` package. This is acceptable because Policies and Logging packages will be merged into the core package in a later V5 step (see Brighter's `src/Paramore.Brighter/Policies` and `src/Paramore.Brighter/Logging` for precedent). See #321 for the merge.
- FR7: `QueryLoggingDecorator` and `QueryLoggingDecoratorAsync` take `JsonSerializerSettings` via constructor injection (resolved from DI by the decorator factory) instead of looking up a serializer from `Context.Bag`. Each decorator creates its own `JsonSerializer` from the settings. The serializer is a decorator-only concern and does not belong on `QueryProcessor`. The `JsonSerializerSettings` constructor parameter is nullable; if null (not registered in DI), the decorator throws `ConfigurationException` at execution time with context about the missing setup. Note: the fluent builder path (`SimpleHandlerDecoratorFactory`) does not support parameterized construction — this is an accepted limitation of the lightweight test/manual path. This approach makes the future migration to `System.Text.Json` straightforward — swap `JsonSerializerSettings` for `JsonSerializerOptions`.
- FR8: Remove the `IReadOnlyDictionary<string, object> contextBagData` constructor parameter from `QueryProcessor`. This was a workaround for the inability to pass context from outside. Callers who need to set additional bag data should now populate `IQueryContext.Bag` before passing it to `Execute`/`ExecuteAsync`.
- FR9: Remove the `_contextBagData` field and the old `Bag` population logic from `CreateQueryContext()` (replaced by `InitQueryContext`)
- FR10: Remove `AddContextBagItem` from `IQueryProcessorExtensionBuilder` and its implementations (`QueryProcessorBuilder`, `ServiceCollectionDarkerHandlerBuilder`)
- FR11: Remove `DarkerContextBag` class from the DI extensions package
- FR12: Update `AddDefaultPolicies` to register `IPolicyRegistry<string>` in DI (so it can be injected into `QueryProcessor`'s constructor). Note: the builder must move out of `Paramore.Darker` into `Paramore.Darker.Extensions.DependencyInjection`, with only the builder interface definition remaining in `Paramore.Darker` (see Brighter's `src/Paramore.Brighter.Extensions.DependencyInjection` for precedent).
- FR13: Update `AddJsonQueryLogging` to register `JsonSerializerSettings` in DI (so it can be constructor-injected into the logging decorators by the DI-based decorator factory)
- FR14: Update `QueryProcessorBuilder.Build()` to pass the policy registry to the `QueryProcessor` constructor (replacing the removed `_contextBagData`)
- FR15: Update the `Policies()` / `DefaultPolicies()` fluent builder extension methods to store the policy registry on the builder, rather than using `AddContextBagItem`. Update `JsonQueryLogging()` to store the `JsonSerializerSettings` for injection into the decorator.
- FR16: All first-party projects must compile and run correctly after all breaking changes: sample applications (`SampleMinimalApi`, MAUI test app), the Testing package (`FakeQueryProcessor`), and the Benchmarks project
- FR17: Update `FakeQueryProcessor` in `Paramore.Darker.Testing` to match the new `IQueryProcessor` signature (accepting the optional `IQueryContext` parameter). `FakeQueryProcessor` should store the provided context so tests can assert on it.

### Non-functional Requirements
- NFR1: No performance regression for the default (null context) path
- NFR2: This is a V5 release with intentional breaking changes. Callers using positional `CancellationToken` in `ExecuteAsync(query, cancellationToken)` will need to update to named parameter syntax `ExecuteAsync(query, cancellationToken: cancellationToken)` or pass null explicitly `ExecuteAsync(query, null, cancellationToken)`

### Constraints and Assumptions
- This is a V5 release with intentional breaking changes across the `IQueryProcessor` interface, builder APIs, and caller sites using positional `CancellationToken`
- Existing implementors of `IQueryProcessor` will need to update their implementations
- The parameter type is `IQueryContext` (the interface, not the concrete class) for extensibility
- Assumes the caller is responsible for creating and configuring the `IQueryContext` when passing one in
- Removing `contextBagData` and `AddContextBagItem` is a breaking change for the builder API and for any code that used `AddContextBagItem` or `DarkerContextBag` directly
- Policy decorators continue to read from `Context.Bag` — no changes to decorator code for accessing the policy registry
- Logging decorators change from `Context.Bag` lookup to constructor injection for the serializer (DI path only; fluent builder path has accepted limitations)
- Introducing Polly dependency to core `Paramore.Darker` is acceptable; Policies and Logging will be merged into the core package in a later V5 step (#321)
- `InitQueryContext` uses additive (non-overwriting) semantics for all contexts, including factory-created ones. This is a behavioral change from the current full-replacement approach, documented as part of V5. In practice this has no effect since no existing `IQueryContextFactory` implementation pre-populates the Bag
- The builder implementation (`QueryProcessorBuilder`) moves from `Paramore.Darker` to `Paramore.Darker.Extensions.DependencyInjection`; only the interface definition stays in `Paramore.Darker`. The Policies/Logging merge (#321) should happen first to simplify this, as the Policies and QueryLogging extension methods currently cast to `QueryProcessorBuilder`

### Out of Scope
- Changing `IQueryContext` or `QueryContext` type definitions
- Adding OpenTelemetry integration (this feature enables it but does not implement it)
- Removing `IQueryContextFactory` (it is still used for the default path)
- Changes to handler signatures

## Acceptance Criteria

- AC1: Calling `Execute(query)` without a context parameter creates a context via `IQueryContextFactory`, injects globals via `InitQueryContext`, and executes the pipeline returning the expected result
- AC2: Calling `ExecuteAsync(query, cancellationToken: cancellationToken)` creates a context via `IQueryContextFactory`, injects globals via `InitQueryContext`, and executes the pipeline returning the expected result. Note: callers using positional `ExecuteAsync(query, ct)` must update — this is an intentional V5 breaking change.
- AC3: Calling `Execute(query, myContext)` with a provided `IQueryContext` uses that context in the pipeline
- AC4: Calling `ExecuteAsync(query, myContext, cancellationToken)` with a provided `IQueryContext` uses that context in the pipeline
- AC5: When a context is provided, handlers and decorators in the pipeline receive the caller-provided context (with globals added by `InitQueryContext`)
- AC6: When a caller-provided context already has a key that `InitQueryContext` would set, the caller's value is preserved (caller wins)
- AC7: `QueryProcessor` no longer accepts `contextBagData` in its constructor
- AC8: `AddContextBagItem` is removed from builder interfaces and implementations
- AC9: `DarkerContextBag` class is removed
- AC10: `QueryProcessor` accepts `IPolicyRegistry<string>?` as a nullable constructor parameter; when non-null, `InitQueryContext` sets it on the context's Bag
- AC11: When policy registry is null but a decorator requires it, `ConfigurationException` is thrown (existing decorator behavior preserved)
- AC12: `QueryLoggingDecorator`/`QueryLoggingDecoratorAsync` receive `JsonSerializerSettings` via constructor injection and create their own `JsonSerializer`, not from `Context.Bag`
- AC13: When `JsonSerializerSettings` is not registered in DI, the logging decorator receives null and throws `ConfigurationException` at execution time with context about the missing setup
- AC14: Policy decorators (`RetryableQueryDecorator`, etc.) continue to read from `Context.Bag` — no changes to policy decorator lookup code
- AC15: `AddDefaultPolicies` registers `IPolicyRegistry<string>` in DI
- AC16: `AddJsonQueryLogging` registers `JsonSerializerSettings` in DI
- AC17: `QueryProcessorBuilder` fluent builder and its extension methods (`Policies()`, `DefaultPolicies()`) pass the policy registry to the `QueryProcessor` constructor
- AC18: All first-party projects compile correctly after all breaking changes: sample applications, Testing package (`FakeQueryProcessor`), and Benchmarks
- AC19: `FakeQueryProcessor` accepts the optional `IQueryContext` parameter and stores it for test assertions
- AC20: Existing code that implements `IQueryProcessor` or used `contextBagData`/`AddContextBagItem` receives clear compiler errors (intentional V5 breaking changes)

## Additional Context

This change aligns Darker with Brighter V10's approach. In Brighter, the `RequestContext` can be passed into `CommandProcessor.Send()`, `Publish()`, and `DepositPost()` as an optional parameter. This was essential for OpenTelemetry support and other scenarios requiring external context propagation into the processing pipeline.
