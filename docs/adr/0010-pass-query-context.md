# 10. Pass QueryContext into QueryProcessor

Date: 2026-05-20

## Status

Accepted

## Context

**Parent Requirement**: [specs/004-pass_query_context/requirements.md](../../specs/004-pass_query_context/requirements.md)

**Scope**: This ADR covers the complete design for passing an external `IQueryContext` into `IQueryProcessor.Execute` and `ExecuteAsync`, the introduction of `InitQueryContext`, the addition of a typed `Policies` property to `IQueryContext` (per FR6, following Brighter's `RequestContext.Policies` pattern), the removal of the `contextBagData` mechanism, and the resulting changes to the builder API, decorator injection, and policy decorator access patterns.

### Problem

Today, `QueryProcessor` always creates the `IQueryContext` internally via `IQueryContextFactory.Create()` and replaces the entire `Bag` dictionary with a shallow copy of `_contextBagData` (line 106 of `QueryProcessor.cs`):

```csharp
queryContext.Bag = _contextBagData.ToDictionary(d => d.Key, d => d.Value);
```

This prevents callers from passing contextual information (OpenTelemetry trace context, correlation IDs, tenant identifiers) into the processing pipeline. This was solved in Brighter V10 by making `RequestContext` an optional parameter on `CommandProcessor.Send()`, `Publish()`, and `DepositPost()`.

### Forces

- **External context propagation**: Callers need to inject cross-cutting data (trace spans, correlation IDs) that decorators and handlers consume.
- **Consistency with Brighter**: Darker should mirror Brighter V10's `RequestContext` pattern so users of both libraries encounter the same design.
- **Separation of concerns**: The current `contextBagData` mechanism conflates processor-level defaults (policy registry) with the context creation pathway. Global references like the policy registry belong to the processor, not to a generic bag.
- **Constructor injection for decorators**: The DI-based decorator factory can resolve constructor parameters; the current `Context.Bag` lookup for the serializer in logging decorators bypasses DI and makes dependencies invisible.
- **V5 breaking changes accepted**: This is a major version bump; breaking changes to `IQueryProcessor`, builder APIs, and positional `CancellationToken` usage are acceptable.

## Decision

### Architecture Overview

```
Caller
  |
  v
IQueryProcessor.Execute(query, queryContext?)
  |
  |--- queryContext == null? --> IQueryContextFactory.Create()
  |--- queryContext != null? --> use as-is
  |
  v
InitQueryContext(context)   <-- sets typed properties (e.g. context.Policies)
  |                             only if null (caller-supplied values win)
  v
PipelineBuilder.Build(query, context)
  |
  v
[Decorator chain] --> Handler --> TResult
  (each receives the same IQueryContext instance)
```

### Roles and Responsibilities

**IQueryProcessor** (Coordinator)
- *Doing*: Orchestrates query execution through the pipeline
- *Deciding*: Whether to use a caller-provided context or create one via the factory
- *Doing*: Initializes the context with processor-level globals before pipeline execution

**IQueryContext** (Information Holder)
- *Knowing*: Holds the `Bag` dictionary that carries arbitrary cross-cutting data through the pipeline
- *Knowing*: Holds a typed `Policies` property for the policy registry (following Brighter's `RequestContext.Policies`)
- Change: Adds `IPolicyRegistry<string>? Policies` property to the interface

**IQueryContextFactory** (Service Provider)
- *Doing*: Creates a fresh `IQueryContext` when the caller does not provide one
- No change to its interface; still used for the default (null) path

**InitQueryContext** (private method on QueryProcessor)
- *Doing*: Sets processor-level globals on the context's typed properties (e.g. `Policies`)
- *Deciding*: Only sets a property if it is currently null (caller-supplied values win)

**QueryLoggingDecorator / QueryLoggingDecoratorAsync** (Service Provider)
- *Knowing*: Holds a `JsonSerializerSettings` instance received via constructor injection
- *Doing*: Serializes query/response for logging
- Change: Moves from `Context.Bag` lookup to constructor injection for the serializer

**RetryableQueryDecorator / RetryableQueryDecoratorAsync** (Service Provider)
- *Knowing*: Reads policy registry from `Context.Policies` (typed property, no longer `Context.Bag` lookup)
- *Doing*: Applies Polly retry/circuit-breaker policies around the next pipeline step
- Change: Switches from `Context.Bag[Constants.ContextBagKey]` cast to `Context.Policies` typed property access

**FallbackPolicyDecorator / FallbackPolicyDecoratorAsync** (Service Provider)
- *Doing*: Catches exceptions and invokes the handler's `Fallback`/`FallbackAsync` method
- *Knowing*: Writes `Context.Bag[CauseOfFallbackException]` to record the exception for downstream decorators (e.g. logging)
- No change: These decorators use `Context.Bag` for fallback exception tracking, not for policy registry access. Unaffected by the `Context.Policies` change.

**QueryProcessorBuilder** (Controller)
- *Doing*: Assembles a `QueryProcessor` with its dependencies
- Change: Stores policy registry directly; passes it to `QueryProcessor` constructor; removes `_contextBagData` and `AddContextBagItem`

### Key Design Decisions

#### 1. Optional `IQueryContext` parameter on `IQueryProcessor`

```csharp
public interface IQueryProcessor
{
    TResult Execute<TResult>(IQuery<TResult> query, IQueryContext queryContext = null);

    Task<TResult> ExecuteAsync<TResult>(
        IQuery<TResult> query,
        IQueryContext queryContext = null,
        CancellationToken cancellationToken = default);
}
```

The parameter type is `IQueryContext` (the interface), not the concrete `QueryContext`, allowing callers to provide custom implementations. The `queryContext` parameter comes before `CancellationToken` following C# conventions.

**Breaking change**: Callers using positional `CancellationToken` (`ExecuteAsync(query, ct)`) must update to named parameter syntax (`ExecuteAsync(query, cancellationToken: ct)`) or pass null explicitly.

#### 2. `IQueryContext` gets a typed `Policies` property

Following Brighter's `RequestContext`, `IQueryContext` gains a typed property for the policy registry:

```csharp
public interface IQueryContext
{
    IDictionary<string, object> Bag { get; set; }
    IPolicyRegistry<string>? Policies { get; set; }
}
```

```csharp
public sealed class QueryContext : IQueryContext
{
    public IDictionary<string, object> Bag { get; set; } = new Dictionary<string, object>();
    public IPolicyRegistry<string>? Policies { get; set; }
}
```

This makes the policy registry a first-class, strongly-typed part of the context contract rather than a stringly-typed bag entry. Decorators access `Context.Policies` directly instead of casting from `Context.Bag`. The `Bag` remains available for arbitrary user data (correlation IDs, tenant info, etc.) that doesn't warrant a typed property.

This is a **breaking change** to `IQueryContext` — any custom implementations must add the `Policies` property. This is acceptable as a V5 change and follows the same pattern Brighter established with `RequestContext.Policies`.

#### 3. `InitQueryContext` with null-check semantics

```csharp
// In QueryProcessor (private method)
private void InitQueryContext(IQueryContext queryContext)
{
    queryContext.Policies ??= _policyRegistry;
}
```

This mirrors Brighter's `InitRequestContext`. The processor sets its globals on the context's typed properties, but only if the caller hasn't already set them. If a caller-provided context already has `Policies` set, the caller's value is preserved (caller wins). This gives callers full control.

#### 4. Policy registry as a constructor parameter

```csharp
public QueryProcessor(
    IHandlerConfiguration handlerConfiguration,
    IQueryContextFactory queryContextFactory,
    IPolicyRegistry<string> policyRegistry = null)
```

The policy registry is a processor-level dependency with the same lifetime as the `QueryProcessor`. It belongs as a constructor parameter, not as data stuffed into a generic bag. `InitQueryContext` sets it on `context.Policies` so decorators read the typed property.

**Note**: Both `IQueryContext.Policies` and the `QueryProcessor` constructor introduce a Polly dependency (`IPolicyRegistry<string>`) to the core `Paramore.Darker` package. This is acceptable because #321 will merge `Paramore.Darker.Policies` into core, following Brighter's precedent where `src/Paramore.Brighter/Policies` lives in the core package.

#### 5. Policy decorators use typed `Context.Policies` property

The policy decorators currently look up the registry from the untyped `Bag`:

```csharp
// Current: stringly-typed bag lookup with cast
var policyRegistry = Context.Bag[Constants.ContextBagKey] as IPolicyRegistry<string>;
```

This changes to a typed property access:

```csharp
// New: typed property access
private IPolicyRegistry<string> GetPolicyRegistry()
{
    return Context.Policies
        ?? throw new ConfigurationException("Policy registry is not set on the QueryContext.");
}
```

This eliminates the string key lookup, the type cast, and the separate `Constants.ContextBagKey`. The `ConfigurationException` behavior is preserved — if the registry is null (not configured), the decorator throws at execution time.

#### 6. Constructor injection for logging decorators

The logging decorators currently look up a `NewtonsoftJsonSerializer` from `Context.Bag`:

```csharp
// Current: hidden dependency via service locator pattern
var serializer = Context.Bag[Constants.ContextBagKey] as NewtonsoftJsonSerializer;
```

This changes to constructor injection:

```csharp
public class QueryLoggingDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    private readonly JsonSerializerSettings _serializerSettings;

    public QueryLoggingDecorator(JsonSerializerSettings serializerSettings)
    {
        _serializerSettings = serializerSettings;  // nullable — checked at execution time
    }
    // ...
}
```

The DI-based decorator factory (`ServiceProviderHandlerDecoratorFactory`) already resolves decorators from `IServiceProvider`, which supports constructor injection naturally. `AddJsonQueryLogging` registers `JsonSerializerSettings` as a singleton in DI.

If `JsonSerializerSettings` is not registered (null), the decorator throws `ConfigurationException` at execution time with a message explaining the missing setup.

**Accepted limitation**: The fluent builder path (`SimpleHandlerDecoratorFactory`) uses delegate-based factory functions and does not support parameterized construction. This is documented as a limitation of the lightweight test/manual path.

#### 7. Remove `contextBagData` mechanism

The following are removed:

| Component | Location | Reason |
|-----------|----------|--------|
| `IReadOnlyDictionary<string, object> contextBagData` constructor param | `QueryProcessor` | Replaced by typed constructor params (`IPolicyRegistry<string>`) and `InitQueryContext` |
| `_contextBagData` field | `QueryProcessor` | No longer needed |
| `CreateQueryContext()` bag replacement logic | `QueryProcessor` | Replaced by `InitQueryContext` with typed property assignment |
| `AddContextBagItem(string, object)` | `IQueryProcessorExtensionBuilder` | No longer needed; each concern has a typed registration path |
| `AddContextBagItem` implementation | `QueryProcessorBuilder` | Removed with the interface method |
| `AddContextBagItem` implementation | `ServiceCollectionDarkerHandlerBuilder` | Removed with the interface method |
| `DarkerContextBag` class | `Paramore.Darker.Extensions.DependencyInjection` | No longer needed |
| `Constants.ContextBagKey` (policy bag key) | `Paramore.Darker.Policies` | Replaced by typed `Context.Policies` property |
| `Context.Bag` policy registry lookup | `RetryableQueryDecorator`, `RetryableQueryDecoratorAsync` | Replaced by `Context.Policies` typed property access |
| `Constants.ContextBagKey` (serializer bag key) | `Paramore.Darker.QueryLogging` | Replaced by constructor injection of `JsonSerializerSettings` |
| `Context.Bag` serializer lookup | `QueryLoggingDecorator`, `QueryLoggingDecoratorAsync` | Replaced by constructor-injected `JsonSerializerSettings` field |
| `NewtonsoftJsonSerializer` wrapper class | `Paramore.Darker.QueryLogging` | Removed — decorators create `JsonSerializer` directly from injected `JsonSerializerSettings` |

#### 8. Builder changes

**`QueryProcessorBuilder` relocation** (per FR12):
- The `QueryProcessorBuilder` implementation moves from `Paramore.Darker` (`src/Paramore.Darker/Builder/QueryProcessorBuilder.cs`) to `Paramore.Darker.Extensions.DependencyInjection`. Only the builder interface definitions (`INeedHandlers`, `INeedAQueryContext`, `IBuildTheQueryProcessor`, `IQueryProcessorExtensionBuilder`) remain in `Paramore.Darker`.
- This relocation is deferred until after #321 (merge Policies and QueryLogging into core), because the current `Policies()` and `JsonQueryLogging()` extension methods cast to `QueryProcessorBuilder` (concrete type), which would create a circular dependency if the builder moved before the policies package is merged. Once #321 is complete, the cast targets and the builder live in the same package.
- In this PR, the builder stays in its current location but its API changes as described below.

**`QueryProcessorBuilder`** (fluent builder):
- Removes `_contextBagData` dictionary
- Adds `_policyRegistry` field (set by `Policies()` / `DefaultPolicies()` extension methods)
- `Build()` passes `_policyRegistry` to `QueryProcessor` constructor
- `AddContextBagItem` removed from interface and implementation

**`ServiceCollectionExtensions.AddDarker()`** (DI builder):
- Removes `DarkerContextBag` creation and usage
- `BuildQueryProcessor` resolves `IPolicyRegistry<string>` from `IServiceProvider` (optional, may be null)
- Passes resolved registry to `QueryProcessor` constructor

**Policy extension methods** (`AddDefaultPolicies`, `AddPolicies`):
- DI path: Register `IPolicyRegistry<string>` in the `IServiceCollection` instead of calling `AddContextBagItem`
- Fluent builder path: Store registry on the builder for `Build()` to pass to constructor

**Logging extension methods** (`AddJsonQueryLogging`):
- DI path: Register `JsonSerializerSettings` in the `IServiceCollection` instead of calling `AddContextBagItem`. The full DI registration chain: decorator types are registered via assembly scanning in `AddHandlersFromAssemblies()`, and `JsonSerializerSettings` is registered as a singleton by `AddJsonQueryLogging()` — both are needed for constructor injection to work at runtime.
- Fluent builder path: Accepted limitation — logging decorators require DI for constructor injection

#### 9. Updated `QueryProcessor` execution flow

```csharp
public TResult Execute<TResult>(IQuery<TResult> query, IQueryContext queryContext = null)
{
    using (var pipelineBuilder = new PipelineBuilder<TResult>(...))
    {
        queryContext ??= _queryContextFactory.Create();
        InitQueryContext(queryContext);

        var entryPoint = pipelineBuilder.Build(query, queryContext);
        // ... invoke and exception handling unchanged
    }
}
```

The same pattern applies to `ExecuteAsync`. `PipelineBuilder` is unchanged — it already receives `IQueryContext` and sets it on the handler and decorators.

#### 10. `FakeQueryProcessor` update

```csharp
public class FakeQueryProcessor : IQueryProcessor
{
    public IQueryContext LastProvidedContext { get; private set; }

    public TResponse Execute<TResponse>(IQuery<TResponse> query, IQueryContext queryContext = null)
    {
        LastProvidedContext = queryContext;
        // ... existing fake logic unchanged
    }

    public Task<TResponse> ExecuteAsync<TResponse>(
        IQuery<TResponse> query,
        IQueryContext queryContext = null,
        CancellationToken cancellationToken = default)
    {
        LastProvidedContext = queryContext;
        return Task.FromResult(Execute(query, queryContext));
    }
}
```

Stores the provided context so tests can assert that the correct context was passed through.

### Technology Choices

- **Polly `IPolicyRegistry<string>`**: Already used by the Policies package; now referenced directly by the core `IQueryContext` interface and `QueryProcessor` constructor. This requires adding `<PackageReference Include="Polly" />` to `Paramore.Darker.csproj` (version managed centrally in `Directory.Packages.props`). This is a temporary coupling that resolves when #321 merges Policies into core.
- **Newtonsoft.Json `JsonSerializerSettings`**: Already used by QueryLogging; now registered in DI for constructor injection. Future migration to `System.Text.Json` swaps `JsonSerializerSettings` for `JsonSerializerOptions`.

## Consequences

### Positive

- Callers can propagate external context (OpenTelemetry spans, correlation IDs, tenant info) into the pipeline
- Aligns with Brighter V10's proven `RequestContext` pattern
- Typed `Policies` property on `IQueryContext` replaces stringly-typed `Bag` lookups — eliminates string key constants, type casts, and runtime type errors
- Policy decorators use `Context.Policies` directly — clear, compile-time-safe contract
- Logging decorator dependencies become explicit via constructor injection (visible in DI registrations)
- `InitQueryContext` null-check semantics give callers full control over context properties

### Negative

- Breaking change to `IQueryProcessor` interface — all implementations must update
- Breaking change to `IQueryContext` interface — custom implementations must add `Policies` property
- Breaking change to positional `CancellationToken` usage in `ExecuteAsync`
- Breaking change to builder API — `AddContextBagItem` removed, `DarkerContextBag` removed
- Breaking change to policy decorators — `Context.Bag` lookup replaced by `Context.Policies`
- Temporary Polly dependency in core (resolved by #321)
- Fluent builder path cannot support constructor-injected decorators (accepted limitation)

### Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Third-party `IQueryProcessor` implementations break | V5 major version; clear compiler errors guide updates |
| Third-party `IQueryContext` implementations break | V5 major version; adding a nullable property is straightforward |
| Polly dependency in core before #321 | Acceptable; #321 is planned as next V5 step |
| Fluent builder users can't use logging decorators with constructor injection | Document as limitation; DI path is the recommended production path |

## Alternatives Considered

### 1. Policy registry in `Context.Bag` instead of typed property

Keep the policy registry in `Context.Bag` (as today) and have `InitQueryContext` add it there with add-if-not-present semantics. Rejected because: (a) stringly-typed bag lookups require string key constants, type casts, and produce runtime errors instead of compile-time errors; (b) Brighter has already established the precedent of typed `Policies` property on `RequestContext`; (c) the `Bag` should be reserved for arbitrary user data, not framework infrastructure.

### 2. Keep `contextBagData` alongside the new `IQueryContext` parameter

Keep the existing bag data mechanism and add the new parameter. Rejected because it creates two competing mechanisms for the same purpose, violating "there should be one obvious way to do it."

### 3. Serializer stays on `Context.Bag` (no constructor injection for logging decorators)

Keep the current `Context.Bag` lookup for the serializer. Rejected because it's a service locator pattern that hides dependencies and makes the DI graph incomplete. Constructor injection makes the dependency explicit and aligns with standard DI practices.

### 4. `QueryContext` (concrete class) as parameter type instead of `IQueryContext`

Use the concrete type for simplicity. Rejected because `IQueryContext` preserves extensibility — callers can implement custom context types with additional properties beyond `Bag`.

## References

- Requirements: [specs/004-pass_query_context/requirements.md](../../specs/004-pass_query_context/requirements.md)
- Related Issue: #320 (Pass QueryContext into QueryProcessor)
- Prerequisite Issue: #321 (Merge Policies and QueryLogging into core)
- Brighter precedent: `CommandProcessor.Send(T command, RequestContext requestContext = null)` in Brighter V10
- Brighter `RequestContext.Policies`: Typed `IPolicyRegistry<string>?` property on the context (see `src/Paramore.Brighter/RequestContext.cs`)
- Brighter `InitRequestContext`: Sets typed properties (`Policies`, `FeatureSwitches`) on `RequestContext` with null-check semantics
