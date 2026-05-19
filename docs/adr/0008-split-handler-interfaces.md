# 8. Split Handler Interfaces into Separate Sync and Async Types

Date: 2026-05-18

## Status

Accepted

**Supersedes**: [ADR 0005: Dual Sync/Async Handler Support](0005-dual-sync-async-support.md) (Alternative 2, previously rejected)

## Context

**Parent Requirement**: [specs/003-split-handler/requirements.md](../../specs/003-split-handler/requirements.md)

**Scope**: This ADR addresses the split of `IQueryHandler<TQuery, TResult>`, its decorator counterpart, their attributes, and the supporting infrastructure (registries, factories, configuration) into separate sync and async type hierarchies. `IQueryProcessor` is **not** affected — it retains both `Execute` and `ExecuteAsync` on the same interface (splitting it was attempted in #288 and reverted).

### The Problem

`IQueryHandler<TQuery, TResult>` defines four methods — `Execute`, `Fallback`, `ExecuteAsync`, `FallbackAsync`. Both base classes throw `NotImplementedException` for the path they don't support:

```csharp
// QueryHandler<,> — sync base class
public virtual Task<TResult> ExecuteAsync(TQuery query, CancellationToken ct)
{
    throw new NotImplementedException("Please derive from QueryHandlerAsync<,>...");
}

// QueryHandlerAsync<,> — async base class
public virtual TResult Execute(TQuery query)
{
    throw new NotImplementedException("Please derive from QueryHandler<,>...");
}
```

Every handler carries dead methods. Every decorator must implement both `Execute` and `ExecuteAsync` even though only one is called for a given pipeline. This violates the Interface Segregation Principle and confuses developers encountering `NotImplementedException` at runtime.

### Why ADR 0005's Rejection No Longer Applies

ADR 0005 rejected separate interfaces (Alternative 2) citing three concerns:

1. **"PipelineBuilder and factory infrastructure would need to be duplicated"** — Brighter has proven this works in practice. The duplication is mechanical and each path is simpler than the current unified code that must handle both.

2. **"Registry lookup would need to return different types depending on execution path"** — Solved by having separate registries, one per path. `PipelineBuilder.Build()` uses the sync registry; `BuildAsync()` uses the async registry. No conditional logic needed.

3. **"Decorator types would need separate interfaces too, multiplying the type surface area"** — True, but the alternative is every decorator carrying dead code for the path it doesn't serve. Two focused types are better than one bloated type.

The shift to async-first development means most handlers are async-only. The `NotImplementedException` pattern is encountered frequently, not occasionally. The cost of ISP violation now outweighs the cost of type surface area.

## Decision

Split the handler, decorator, attribute, and infrastructure types into separate sync and async hierarchies. Each execution path — sync (`Execute`) and async (`ExecuteAsync`) — has its own complete set of types. No type serves both paths.

### Architecture Overview

```
                    IQueryHandler (marker: Context)
                    /                              \
   IQueryHandler<TQuery,TResult>      IQueryHandlerAsync<TQuery,TResult>
   - Execute(query)                   - ExecuteAsync(query, ct)
   - Fallback(query)                  - FallbackAsync(query, ct)
        |                                      |
   QueryHandler<,>                    QueryHandlerAsync<,>
   (abstract base)                    (abstract base)


                 IQueryHandlerDecorator (marker: Context, InitializeFromAttributeParams)
                    /                              \
   IQueryHandlerDecorator<TQuery,TResult>   IQueryHandlerDecoratorAsync<TQuery,TResult>
   - Execute(query, next, fallback)         - ExecuteAsync(query, next, fallback, ct)
        |                                      |
   FallbackPolicyDecorator<,>           FallbackPolicyDecoratorAsync<,>
   RetryableQueryDecorator<,>           RetryableQueryDecoratorAsync<,>


   QueryHandlerAttribute               QueryHandlerAttributeAsync
   - GetDecoratorType() → sync          - GetDecoratorType() → async
        |                                      |
   FallbackPolicyAttribute              FallbackPolicyAttributeAsync
   RetryableQueryAttribute              RetryableQueryAttributeAsync
   QueryLoggingAttribute                QueryLoggingAttributeAsync


   IQueryHandlerRegistry               IQueryHandlerRegistryAsync
   IQueryHandlerFactory                 IQueryHandlerFactoryAsync
   IQueryHandlerDecoratorRegistry       IQueryHandlerDecoratorRegistryAsync
   IQueryHandlerDecoratorFactory        IQueryHandlerDecoratorFactoryAsync
```

### Key Components

#### Handler Interfaces (Roles: Service Provider)

Each interface has a single cohesive responsibility — dispatching a query through one execution model.

**`IQueryHandler<TQuery, TResult> : IQueryHandler`** — sync handler role:
- **Doing**: `TResult Execute(TQuery query)` — executes the query synchronously
- **Doing**: `TResult Fallback(TQuery query)` — provides a fallback result on failure

**`IQueryHandlerAsync<TQuery, TResult> : IQueryHandler`** — async handler role:
- **Doing**: `Task<TResult> ExecuteAsync(TQuery query, CancellationToken ct)` — executes the query asynchronously
- **Doing**: `Task<TResult> FallbackAsync(TQuery query, CancellationToken ct)` — provides an async fallback

**`IQueryHandler`** — shared marker role (unchanged):
- **Knowing**: `IQueryContext Context { get; set; }` — the query context for the current pipeline execution

#### Decorator Interfaces (Roles: Coordinator)

Each decorator wraps a handler call, coordinating between `next` and `fallback`.

**`IQueryHandlerDecorator<TQuery, TResult> : IQueryHandlerDecorator`** — sync decorator:
- **Doing**: `TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)`

**`IQueryHandlerDecoratorAsync<TQuery, TResult> : IQueryHandlerDecorator`** — async decorator:
- **Doing**: `Task<TResult> ExecuteAsync(TQuery query, Func<TQuery, CancellationToken, Task<TResult>> next, Func<TQuery, CancellationToken, Task<TResult>> fallback, CancellationToken ct)`

**`IQueryHandlerDecorator`** — shared marker (unchanged):
- **Knowing**: `IQueryContext Context { get; set; }`
- **Doing**: `void InitializeFromAttributeParams(object[] attributeParams)`

#### Attributes (Roles: Information Holder)

Attributes declare which decorator to use. Each attribute type maps to one decorator type for its path.

**`QueryHandlerAttribute`** — sync attribute base. Placed on `Execute` methods. `GetDecoratorType()` returns a sync decorator type.

**`QueryHandlerAttributeAsync`** — async attribute base. Placed on `ExecuteAsync` methods. `GetDecoratorType()` returns an async decorator type.

Each existing attribute becomes two classes:

| Sync | Async | Decorator (sync) | Decorator (async) |
|------|-------|-------------------|---------------------|
| `FallbackPolicyAttribute` | `FallbackPolicyAttributeAsync` | `FallbackPolicyDecorator<,>` | `FallbackPolicyDecoratorAsync<,>` |
| `RetryableQueryAttribute` | `RetryableQueryAttributeAsync` | `RetryableQueryDecorator<,>` | `RetryableQueryDecoratorAsync<,>` |
| `QueryLoggingAttribute` | `QueryLoggingAttributeAsync` | `QueryLoggingDecorator<,>` | `QueryLoggingDecoratorAsync<,>` |

#### Registries and Factories (Roles: Service Provider)

Separate registries and factories for each path, following Brighter's established pattern.

**Handler registries**:
- `IQueryHandlerRegistry` — `Register<TQuery, TResult, THandler>() where THandler : IQueryHandler<TQuery, TResult>` + `Register(Type, Type, Type)` + `Type Get(Type queryType)`
- `IQueryHandlerRegistryAsync` — same signature pattern with `where THandler : IQueryHandlerAsync<TQuery, TResult>`

**Handler factories**:
- `IQueryHandlerFactory` — `IQueryHandler Create(Type handlerType)` / `void Release(IQueryHandler handler)`
- `IQueryHandlerFactoryAsync` — same signatures (both return `IQueryHandler` marker; the separation is for DI independence)

**Decorator registries**:
- `IQueryHandlerDecoratorRegistry` — `void Register(Type decoratorType)` for sync decorators
- `IQueryHandlerDecoratorRegistryAsync` — same for async decorators

**Decorator factories**:
- `IQueryHandlerDecoratorFactory` — `T Create<T>(Type decoratorType) where T : IQueryHandlerDecorator` / `void Release<T>(T handler) where T : IQueryHandlerDecorator`
- `IQueryHandlerDecoratorFactoryAsync` — `T Create<T>(Type decoratorType) where T : IQueryHandlerDecorator` / `void Release<T>(T handler) where T : IQueryHandlerDecorator`

Both factories use the shared `IQueryHandlerDecorator` marker constraint rather than path-specific generic interfaces. This is deliberate: `PipelineBuilder` resolves all decorators from attributes on the handler method, then validates that each decorator matches the expected path (sync or async). Using the shared constraint allows the factory to create any decorator type; mismatches are caught at validation time with a `ConfigurationException` and a clear message rather than being silently ignored. If the factory constrained to a path-specific generic interface, mismatched attributes would fail with an opaque cast error instead of a helpful configuration message.

#### Handler Configuration (Role: Information Holder)

`IHandlerConfiguration` is extended with async counterparts:

```csharp
public interface IHandlerConfiguration
{
    // Sync infrastructure (existing)
    IQueryHandlerRegistry HandlerRegistry { get; }
    IQueryHandlerFactory HandlerFactory { get; }
    IQueryHandlerDecoratorRegistry DecoratorRegistry { get; }
    IQueryHandlerDecoratorFactory DecoratorFactory { get; }

    // Async infrastructure (new)
    IQueryHandlerRegistryAsync HandlerRegistryAsync { get; }
    IQueryHandlerFactoryAsync HandlerFactoryAsync { get; }
    IQueryHandlerDecoratorRegistryAsync DecoratorRegistryAsync { get; }
    IQueryHandlerDecoratorFactoryAsync DecoratorFactoryAsync { get; }
}
```

`QueryProcessor` passes the sync set to `PipelineBuilder.Build()` and the async set to `PipelineBuilder.BuildAsync()`.

#### PipelineBuilder Changes

`PipelineBuilder` already has separate `Build()` and `BuildAsync()` methods with separate pipelines. The change is:

- `Build()` resolves from `IQueryHandlerRegistry` via `IQueryHandlerFactory`, reflects on `Execute`, reads `QueryHandlerAttribute` instances, resolves sync decorators from `IQueryHandlerDecoratorFactory`
- `BuildAsync()` resolves from `IQueryHandlerRegistryAsync` via `IQueryHandlerFactoryAsync`, reflects on `ExecuteAsync`, reads `QueryHandlerAttributeAsync` instances, resolves async decorators from `IQueryHandlerDecoratorFactoryAsync`

The constructor changes to accept both sync and async infrastructure sets. Internally, each method uses only its own path's types.

#### Error Handling

**Handler not found**: If `Build()` cannot find a sync handler, it throws `ConfigurationException` with: "No sync handler registered for query type {QueryType}. If you have an async handler, use ExecuteAsync instead." Same pattern for `BuildAsync()` in reverse.

**Attribute mismatch**: `PipelineBuilder` resolves all decorators from the handler method's attributes, then validates that each decorator implements the correct path interface. If `Build()` finds a decorator that does not implement `IQueryHandlerDecorator<TQuery, TResult>`, or `BuildAsync()` finds one that does not implement `IQueryHandlerDecoratorAsync<TQuery, TResult>`, it throws `ConfigurationException` with a message indicating the mismatch. This prevents silent failure — without validation, mismatched decorators would simply be skipped or fail with an opaque cast error at runtime.

**Decorator not found**: If the decorator factory cannot create a decorator for a given type, it throws `ConfigurationException` (replacing `MissingHandlerDecoratorException`). This covers both sync and async paths.

**Exception type change**: All configuration errors — handler-not-found (currently `MissingHandlerException`), decorator-not-found (currently `MissingHandlerDecoratorException`), and attribute mismatch (new) — use the existing `ConfigurationException` in V5. `MissingHandlerException` and `MissingHandlerDecoratorException` are retired; their call sites migrate to `ConfigurationException` with more descriptive messages.

#### DI Registration

`AddHandlersFromAssemblies` scans for types implementing `IQueryHandler<,>` (sync registry) and `IQueryHandlerAsync<,>` (async registry). `AddDarker()` wires both sync and async registries, factories, and decorator registries.

### Implementation Approach

The implementation follows a tidy-first approach:

1. **Structural**: Split `IQueryHandler<TQuery, TResult>` into two files; update base classes to remove `NotImplementedException` methods; split decorator interface
2. **Structural**: Create async attribute base class and async attribute variants
3. **Structural**: Create async registry, factory, and decorator registry/factory interfaces
4. **Behavioral**: Update `PipelineBuilder` to use separate infrastructure per path
5. **Behavioral**: Update `IHandlerConfiguration` and `QueryProcessor`
6. **Behavioral**: Update DI registration to wire both paths
7. **Behavioral**: Add `ConfigurationException` for mismatch and configuration errors

## Consequences

### Positive

- **ISP compliance**: Handlers and decorators implement only the methods they use
- **No `NotImplementedException`**: Dead methods eliminated from the type hierarchy
- **Compile-time safety**: A sync handler cannot accidentally have async methods; they don't exist on the type
- **Clearer intent**: A handler's interface declaration tells you which path it serves
- **Simpler decorators**: Each decorator class has one focused execution method
- **Alignment with Brighter**: Consistent patterns across the Paramore project family
- **Better error messages**: `ConfigurationException` with specific guidance replaces opaque `NotImplementedException`

### Negative

- **More types**: Approximately doubles the number of handler, decorator, attribute, registry, and factory types
- **Breaking change**: Consumers implementing `IQueryHandler<,>` directly must choose an interface. Consumers using `QueryHandlerAttribute` on async handlers must switch to `QueryHandlerAttributeAsync`. This attribute rename affects **every decorated async handler** — it is the most disruptive migration item. V5 will ship with preview releases, documentation, and a migration guide to ease this transition.
- **Decorator duplication**: Each decorator's logic is implemented twice (sync and async variants). The logic is often similar but cannot be shared due to different signatures.
- **Infrastructure surface area**: Eight properties on `IHandlerConfiguration` instead of four

### Risks and Mitigations

**Risk**: The type proliferation makes the library harder to navigate.
- **Mitigation**: The naming convention (`*Async` suffix) is consistent and self-documenting. Most developers only interact with one path. IDE navigation handles the rest.

**Risk**: Decorator logic drift between sync and async variants.
- **Mitigation**: Tests cover both paths. The logic is typically identical except for `await` keywords. Each decorator pair should be reviewed together.

**Risk**: Existing consumers face a significant migration.
- **Mitigation**: This is a V5 major version. The base class experience (`QueryHandler<,>` / `QueryHandlerAsync<,>`) is unchanged — developers inherit and override the same methods. The breaking change is at the interface level, which most consumers don't interact with directly.

## Alternatives Considered

### Alternative 1: Keep Unified Interface (Status Quo — ADR 0005)

Keep `IQueryHandler<TQuery, TResult>` with all four methods and `NotImplementedException` stubs.

**Rejected because**: The ISP violation is a real developer experience problem. The `NotImplementedException` pattern is confusing. ADR 0005's concerns about infrastructure duplication have been addressed by Brighter's successful implementation of the separate-interfaces pattern.

### Alternative 2: Async-Only (Drop Sync Support)

Remove sync handlers entirely; all handlers are async.

**Rejected because**: Some handlers are genuinely synchronous (in-memory lookups, cache reads). Forcing `Task.FromResult` wrappers adds overhead and obscures intent. Brighter supports both; consistency matters.

### Alternative 3: Single Interface with Default Interface Methods (C# 8+)

Use C# default interface methods to provide `NotImplementedException` defaults, keeping one interface but making the stubs less visible.

**Rejected because**: This hides the ISP violation rather than fixing it. The dead methods still exist on the type. Default interface methods have runtime performance implications and tooling quirks. The problem is structural, not syntactic.

## References

- Requirements: [specs/003-split-handler/requirements.md](../../specs/003-split-handler/requirements.md)
- Related ADRs:
  - [ADR 0005: Dual Sync/Async Handler Support](0005-dual-sync-async-support.md) — **Superseded** by this ADR on the unified interface decision
  - [ADR 0002: Attribute-Driven Decorator Pipeline](0002-attribute-driven-decorator-pipeline.md) — Decorator attributes now split by path
- Issue: [#304](https://github.com/BrighterCommand/Darker/issues/304)
- Follow-on: [#305](https://github.com/BrighterCommand/Darker/issues/305) — Pipeline validation at startup
- Discussion: [V5 Roadmap #273](https://github.com/BrighterCommand/Darker/discussions/273)
- Brighter reference:
  - `IHandleRequests<T>` / `IHandleRequestsAsync<T>` — separate handler interfaces
  - `IAmASubscriberRegistry` / `IAmAnAsyncSubscriberRegistry` — separate registries
  - `IAmAHandlerFactorySync` / `IAmAHandlerFactoryAsync` — separate factories
- Source files affected:
  - `src/Paramore.Darker/IQueryHandler.cs` — split into sync + async
  - `src/Paramore.Darker/QueryHandler.cs` — remove `NotImplementedException` methods
  - `src/Paramore.Darker/QueryHandlerAsync.cs` — remove `NotImplementedException` methods
  - `src/Paramore.Darker/IQueryHandlerDecorator.cs` — split into sync + async
  - `src/Paramore.Darker/Attributes/QueryHandlerAttribute.cs` — add async variant
  - `src/Paramore.Darker/Decorators/FallbackPolicyDecorator.cs` — split into sync + async
  - `src/Paramore.Darker/IQueryHandlerRegistry.cs` — add async variant
  - `src/Paramore.Darker/QueryHandlerRegistry.cs` — add async counterpart; `RegisterFromAssemblies` currently scans for `IQueryHandler<,>` only, needs async equivalent scanning for `IQueryHandlerAsync<,>` (FR17)
  - `src/Paramore.Darker/IQueryHandlerFactory.cs` — add async variant
  - `src/Paramore.Darker/IQueryHandlerDecoratorFactory.cs` — add async variant
  - `src/Paramore.Darker/IQueryHandlerDecoratorRegistry.cs` — add async variant
  - `src/Paramore.Darker/IHandlerConfiguration.cs` — extend with async properties
  - `src/Paramore.Darker/HandlerConfiguration.cs` — extend with async properties
  - `src/Paramore.Darker/PipelineBuilder.cs` — use separate infrastructure per path
  - `src/Paramore.Darker/QueryProcessor.cs` — pass correct config per path
  - `src/Paramore.Darker/Builder/` — extend `QueryProcessorBuilder`, `IBuildTheQueryProcessor`, `INeedHandlers`, builder interfaces, `FactoryFuncWrapper`, `RegistryActionWrapper` for dual-path configuration; `RegisterDefaultDecorators()` must register async decorator variants
  - `src/Paramore.Darker/Exceptions/` — retire `MissingHandlerException` and `MissingHandlerDecoratorException`; migrate their call sites to the existing `ConfigurationException`
  - `src/Paramore.Darker.Extensions.DependencyInjection/` — `ServiceCollectionExtensions.cs` (wire both paths), `ServiceCollectionHandlerRegistry.cs` (needs async counterpart), `ServiceCollectionDarkerHandlerBuilder.cs` (dual-path `RegisterFromAssemblies` and decorator registration), `IDarkerHandlerBuilder.cs` (async handler registration overloads), `ServiceProviderHandlerFactory.cs` (async counterpart), `ServiceProviderHandlerDecoratorFactory.cs` (async counterpart)
  - `src/Paramore.Darker.Policies/` — split retry decorator and attribute into sync and async variants; update `QueryProcessorBuilderExtensions.cs` to register both sync and async decorator types
  - `src/Paramore.Darker.QueryLogging/` — split logging decorator and attribute into sync and async variants; update `QueryProcessorBuilderExtensions.cs` to register both sync and async decorator types
