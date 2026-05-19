# Requirements: Split IQueryHandler into Separate Sync and Async Interfaces

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: #304

## Problem Statement

As a developer implementing a query handler, I would like to implement only the execution path I use (sync or async), so that my handler does not carry `NotImplementedException` stubs for methods it will never support.

Currently `IQueryHandler<TQuery, TResult>` defines all four methods — `Execute`, `Fallback`, `ExecuteAsync`, `FallbackAsync`. The two abstract base classes handle this by throwing `NotImplementedException` for the path they don't support:

- `QueryHandler<,>` throws `NotImplementedException` from `ExecuteAsync` and `FallbackAsync`
- `QueryHandlerAsync<,>` throws `NotImplementedException` from `Execute` and `Fallback`

This violates the Interface Segregation Principle. Every handler type carries dead methods that exist only to satisfy the combined interface.

### Why this reverses ADR 0005

ADR 0005 chose the unified `IQueryHandler<TQuery, TResult>` interface and explicitly rejected separate interfaces (Alternative 2), citing concerns about duplicating `PipelineBuilder` and factory infrastructure, complicating registry lookup, and multiplying the type surface area.

Since ADR 0005 was written, the landscape has shifted:

- **Async-first is now dominant**: Most modern applications only implement the async path, particularly for I/O-bound operations like database queries and HTTP calls. The `NotImplementedException` pattern is encountered frequently, not occasionally.
- **Brighter has proven the pattern**: Brighter successfully uses separate `IHandleRequests<T>` and `IHandleRequestsAsync<T>` interfaces with separate registries, factories, and decorator types. The infrastructure duplication concerns from ADR 0005 have been addressed in practice.
- **The ISP cost is real**: Forcing every handler to carry four methods when it uses two creates a confusing API surface. New developers consistently encounter `NotImplementedException` and wonder what they did wrong.

A new ADR will supersede ADR 0005's decision on this point.

## Proposed Solution

Split `IQueryHandler<TQuery, TResult>` into two focused interfaces, following the pattern established by Brighter:

- `IQueryHandler<TQuery, TResult>` — sync only: `Execute(TQuery query)` and `Fallback(TQuery query)`
- `IQueryHandlerAsync<TQuery, TResult>` — async only: `ExecuteAsync(TQuery query, CancellationToken)` and `FallbackAsync(TQuery query, CancellationToken)`
- The marker interface `IQueryHandler` (non-generic, with `Context`) remains shared as a base for both
- `QueryHandler<,>` implements only `IQueryHandler<,>` — no `NotImplementedException` methods
- `QueryHandlerAsync<,>` implements only `IQueryHandlerAsync<,>` — no `NotImplementedException` methods
- `IQueryHandlerDecorator` splits into sync and async variants — two separate decorator types, no mixing
- Separate handler registries, factories, and decorator factories for each path (following Brighter)
- `PipelineBuilder` resolves the correct interface per execution path
- A `ConfigurationException` is thrown at runtime if a handler pipeline has a path mismatch (e.g., async handler resolved for a sync `Execute` call)

## Requirements

### Functional Requirements

#### Handler Interfaces

- **FR1**: Provide a sync-only handler interface `IQueryHandler<TQuery, TResult> : IQueryHandler where TQuery : IQuery<TResult>` with methods:
  - `TResult Execute(TQuery query)`
  - `TResult Fallback(TQuery query)`
- **FR2**: Provide an async-only handler interface `IQueryHandlerAsync<TQuery, TResult> : IQueryHandler where TQuery : IQuery<TResult>` with methods:
  - `Task<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken)`
  - `Task<TResult> FallbackAsync(TQuery query, CancellationToken cancellationToken)`
- **FR3**: Both interfaces share the marker `IQueryHandler` base which provides `IQueryContext Context { get; set; }`.
- **FR4**: `QueryHandler<TQuery, TResult>` implements only `IQueryHandler<TQuery, TResult>`. It has no `ExecuteAsync`, `FallbackAsync`, or any `NotImplementedException` methods.
- **FR5**: `QueryHandlerAsync<TQuery, TResult>` implements only `IQueryHandlerAsync<TQuery, TResult>`. It has no `Execute`, `Fallback`, or any `NotImplementedException` methods.

#### Decorator Interfaces

- **FR5a**: Both generic decorator interfaces share the existing non-generic `IQueryHandlerDecorator` marker base which provides `IQueryContext Context { get; set; }` and `void InitializeFromAttributeParams(object[] attributeParams)`.
- **FR6**: Provide a sync-only decorator interface `IQueryHandlerDecorator<TQuery, TResult> : IQueryHandlerDecorator` with method:
  - `TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)`
- **FR7**: Provide an async-only decorator interface `IQueryHandlerDecoratorAsync<TQuery, TResult> : IQueryHandlerDecorator` with method:
  - `Task<TResult> ExecuteAsync(TQuery query, Func<TQuery, CancellationToken, Task<TResult>> next, Func<TQuery, CancellationToken, Task<TResult>> fallback, CancellationToken cancellationToken)`
- **FR8**: Each existing decorator becomes two separate classes — one for each path. For example, `FallbackPolicyDecorator<,>` (sync) and `FallbackPolicyDecoratorAsync<,>` (async). No decorator implements both interfaces.

#### Decorator Attributes

- **FR8a**: Provide separate attribute base classes for each path:
  - `QueryHandlerAttribute` — sync attributes. `GetDecoratorType()` returns a sync decorator type (e.g., `typeof(FallbackPolicyDecorator<,>)`). Placed on `Execute` methods.
  - `QueryHandlerAttributeAsync` — async attributes. `GetDecoratorType()` returns an async decorator type (e.g., `typeof(FallbackPolicyDecoratorAsync<,>)`). Placed on `ExecuteAsync` methods.
- **FR8b**: Each existing attribute becomes two separate classes — one for each path. For example:
  - `FallbackPolicyAttribute` (sync, derives from `QueryHandlerAttribute`) and `FallbackPolicyAttributeAsync` (async, derives from `QueryHandlerAttributeAsync`)
  - `RetryableQueryAttribute` / `RetryableQueryAttributeAsync`
  - `QueryLoggingAttribute` / `QueryLoggingAttributeAsync`
- **FR8c**: It is a `ConfigurationException` to place a sync attribute (`QueryHandlerAttribute`) on an async handler's `ExecuteAsync` method, or an async attribute (`QueryHandlerAttributeAsync`) on a sync handler's `Execute` method. `PipelineBuilder` validates this when reading attributes from the handler method.

#### Decorator Registry

- **FR8d**: Provide separate decorator registries for each path:
  - `IQueryHandlerDecoratorRegistry` for registering sync decorator types
  - `IQueryHandlerDecoratorRegistryAsync` for registering async decorator types

#### Registry and Factory

- **FR9**: Provide separate handler registries for each path:
  - `IQueryHandlerRegistry` with `Register<TQuery, TResult, THandler>() where THandler : IQueryHandler<TQuery, TResult>` for sync handlers
  - `IQueryHandlerRegistryAsync` with `Register<TQuery, TResult, THandler>() where THandler : IQueryHandlerAsync<TQuery, TResult>` for async handlers
  - Both support `Type Get(Type queryType)` to look up the handler type for a given query type
  - Both support `Register(Type queryType, Type resultType, Type handlerType)` (non-generic overload used by assembly scanning in FR17)
- **FR10**: Provide separate handler factories for each path. Both return `IQueryHandler` (the shared marker interface) since the factory resolves by `Type` and the marker carries `Context`:
  - `IQueryHandlerFactory` with `IQueryHandler Create(Type handlerType)` and `void Release(IQueryHandler handler)`
  - `IQueryHandlerFactoryAsync` with `IQueryHandler Create(Type handlerType)` and `void Release(IQueryHandler handler)`
  - The separation ensures DI containers can register sync and async handler factories independently, even though the return type is the same shared marker.
- **FR11**: Provide separate decorator factories for each path:
  - `IQueryHandlerDecoratorFactory` with `T Create<T>(Type decoratorType) where T : IQueryHandlerDecorator` and `void Release<T>(T handler) where T : IQueryHandlerDecorator`
  - `IQueryHandlerDecoratorFactoryAsync` with `T Create<T>(Type decoratorType) where T : IQueryHandlerDecorator` and `void Release<T>(T handler) where T : IQueryHandlerDecorator`
  - Both factories use the shared `IQueryHandlerDecorator` marker constraint. `PipelineBuilder` resolves all decorators via the factory, then validates path compatibility at runtime — mismatched decorators produce a clear `ConfigurationException` rather than being silently ignored.

#### Pipeline

- **FR12**: `PipelineBuilder.Build()` resolves handlers from the sync registry via the sync factory and builds a sync decorator pipeline.
- **FR13**: `PipelineBuilder.BuildAsync()` resolves handlers from the async registry via the async factory and builds an async decorator pipeline.
- **FR14**: `IHandlerConfiguration` is extended to hold both sync and async infrastructure:
  - Existing properties: `IQueryHandlerRegistry HandlerRegistry`, `IQueryHandlerFactory HandlerFactory`, `IQueryHandlerDecoratorRegistry DecoratorRegistry`, `IQueryHandlerDecoratorFactory DecoratorFactory`
  - New properties: `IQueryHandlerRegistryAsync HandlerRegistryAsync`, `IQueryHandlerFactoryAsync HandlerFactoryAsync`, `IQueryHandlerDecoratorRegistryAsync DecoratorRegistryAsync`, `IQueryHandlerDecoratorFactoryAsync DecoratorFactoryAsync`
  - `QueryProcessor` passes the sync set to `PipelineBuilder.Build()` and the async set to `PipelineBuilder.BuildAsync()`.

#### Error Handling

- **FR15**: If `PipelineBuilder.Build()` cannot resolve a sync handler for a query type, it throws a `ConfigurationException` with a message indicating the query type — e.g., "No sync handler registered for query type {QueryType}. If you have an async handler, use ExecuteAsync instead."
- **FR16**: If `PipelineBuilder.BuildAsync()` cannot resolve an async handler for a query type, it throws a `ConfigurationException` with a message indicating the query type — e.g., "No async handler registered for query type {QueryType}. If you have a sync handler, use Execute instead."
- **FR16a**: If `PipelineBuilder` detects a sync attribute (`QueryHandlerAttribute`) on an async handler method, or an async attribute (`QueryHandlerAttributeAsync`) on a sync handler method, it throws a `ConfigurationException` with a message indicating the mismatch.
- **FR16b**: If the decorator factory cannot create a decorator for a given type (returns null), `PipelineBuilder` throws a `ConfigurationException` with a message indicating the decorator type. This replaces `MissingHandlerDecoratorException` in V5.

#### DI Registration

- **FR17**: `AddHandlersFromAssemblies` scans assemblies and registers handlers against the correct interface:
  - Types implementing `IQueryHandler<TQuery, TResult>` are registered in the sync registry
  - Types implementing `IQueryHandlerAsync<TQuery, TResult>` are registered in the async registry

#### Builder

- **FR18**: `QueryProcessorBuilder` configures both sync and async handler infrastructure:
  - `Handlers()` accepts both sync and async registry/factory configuration
  - `RegisterDefaultDecorators()` registers both sync and async fallback decorator variants
  - `IQueryProcessorExtensionBuilder.RegisterDecorator` supports registering both sync and async decorator types

### Non-functional Requirements

- **NFR1**: No runtime performance impact — the pipeline already uses reflection; the interface split does not add overhead.
- **NFR2**: The base class experience should be unchanged — developers inherit from `QueryHandler<,>` or `QueryHandlerAsync<,>` as before, with the same method signatures.

### Constraints and Assumptions

- This is a V5 breaking change. Consumers implementing `IQueryHandler<TQuery, TResult>` directly (rather than using base classes) must choose which interface to implement.
- The marker interface `IQueryHandler` (non-generic, with `Context`) remains as a shared base for both sync and async interfaces.
- Decorator attributes are split: `QueryHandlerAttribute` (sync, placed on `Execute`) and `QueryHandlerAttributeAsync` (async, placed on `ExecuteAsync`). Each returns the decorator type for its path via `GetDecoratorType()`.
- `QueryProcessor` will need modification to hold both sync and async handler configurations and pass the correct one to `PipelineBuilder` for each execution path.
- The exception type for handler-not-found changes from `MissingHandlerException` to `ConfigurationException` in V5 (FR15/FR16).
- The DI integration package (`Paramore.Darker.Extensions.DependencyInjection`) must be updated to wire both sync and async registries, factories, and decorator registries in `AddDarker()`.

### Out of Scope

- Splitting `IQueryProcessor` into sync/async interfaces (previously attempted in #288, reverted).
- Changes to the query types (`IQuery<TResult>`).
- Removing reflection from `PipelineBuilder` — that is a separate concern.
- Pipeline validation at startup (filed as #305 — `ValidatePipelines()` similar to Brighter). This spec addresses runtime error handling via `ConfigurationException`; startup validation is a follow-on.

## Acceptance Criteria

- **AC1**: The sync handler interface `IQueryHandler<TQuery, TResult>` declares only `Execute` and `Fallback`. A class inheriting `QueryHandler<,>` does not have `ExecuteAsync` or `FallbackAsync` as members.
- **AC2**: The async handler interface `IQueryHandlerAsync<TQuery, TResult>` declares only `ExecuteAsync` and `FallbackAsync`. A class inheriting `QueryHandlerAsync<,>` does not have `Execute` or `Fallback` as members.
- **AC3**: No `NotImplementedException` exists in the handler base classes.
- **AC4**: The sync decorator interface `IQueryHandlerDecorator<TQuery, TResult>` declares only `Execute`. The async decorator interface `IQueryHandlerDecoratorAsync<TQuery, TResult>` declares only `ExecuteAsync`.
- **AC5**: Each existing decorator has a sync and async variant (e.g., `FallbackPolicyDecorator` and `FallbackPolicyDecoratorAsync`). No decorator implements both interfaces.
- **AC5a**: Each existing attribute has a sync and async variant (e.g., `FallbackPolicyAttribute` derives from `QueryHandlerAttribute`, `FallbackPolicyAttributeAsync` derives from `QueryHandlerAttributeAsync`). Each returns the decorator type for its path.
- **AC5b**: Placing a sync attribute on an async handler's `ExecuteAsync` method (or vice versa) throws a `ConfigurationException` at pipeline build time.
- **AC6**: Sync and async handlers are registered in separate registries. `AddHandlersFromAssemblies` scans and registers against the correct interface.
- **AC7**: `PipelineBuilder.Build()` resolves handlers from the sync registry and builds a sync decorator pipeline. `PipelineBuilder.BuildAsync()` does the same for async.
- **AC8**: When `Execute` is called for a query that only has an async handler registered, a `ConfigurationException` is thrown with a helpful message. Vice versa for `ExecuteAsync` with a sync-only handler.
- **AC9**: The existing test suite passes (with updates to reflect the new interfaces).
- **AC10**: The sample application (`SampleMinimalApi`) continues to build and run correctly.
- **AC11**: `QueryProcessorBuilder` builds a `QueryProcessor` with both sync and async handler infrastructure wired.

## Additional Context

- Discussed in the V5 roadmap: https://github.com/BrighterCommand/Darker/discussions/273
- Supersedes ADR 0005's decision on unified handler interfaces. A new ADR will document the rationale.
- Supersedes #288 which incorrectly targeted `IQueryProcessor`.
- Brighter's approach: separate `IHandleRequests<T>` / `IHandleRequestsAsync<T>`, separate registries (`IAmASubscriberRegistry` / `IAmAnAsyncSubscriberRegistry`), separate factories (`IAmAHandlerFactorySync` / `IAmAHandlerFactoryAsync`), and separate decorator types.
- Pipeline validation at startup is tracked in #305.
