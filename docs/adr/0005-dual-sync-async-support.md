# 5. Dual Sync/Async Handler Support

Date: 2026-05-15

## Status

Accepted

## Context

**Scope**: How Darker supports both synchronous and asynchronous query execution.

### The Problem

Query handlers may need to perform I/O-bound work (database queries, HTTP calls) where async execution avoids blocking threads, or CPU-bound/in-memory work where synchronous execution is simpler and has less overhead. Forcing all handlers to be async adds unnecessary complexity for simple synchronous cases (requiring `Task.FromResult` wrappers). Forcing all handlers to be synchronous blocks threads during I/O operations.

### Constraints

- The `IQueryProcessor` must expose both `Execute` and `ExecuteAsync` to callers
- Decorators must support both paths (a logging decorator should work with both sync and async handlers)
- The handler's declared path (sync or async) determines which pipeline the `QueryProcessor` builds
- Consistency with how Brighter handles this split is desirable

## Decision

We provide a **single unified interface with separate base classes** that each implement one execution path and throw `NotImplementedException` for the other.

### The Interface

`IQueryHandler<TQuery, TResult>` defines four methods:

```csharp
TResult Execute(TQuery query);
TResult Fallback(TQuery query);
Task<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken);
Task<TResult> FallbackAsync(TQuery query, CancellationToken cancellationToken);
```

Both execution paths and both fallback paths exist on every handler type. This allows the `PipelineBuilder` to treat all handlers uniformly when reflecting on methods.

### The Base Classes

Two abstract base classes guide developers to implement one path:

**`QueryHandler<TQuery, TResult>`** (synchronous):
- `abstract TResult Execute(TQuery query)` - must be implemented
- `virtual TResult Fallback(TQuery query)` - returns `default(TResult)`
- `ExecuteAsync` / `FallbackAsync` - throw `NotImplementedException` with a message directing the developer to use `QueryHandlerAsync`

**`QueryHandlerAsync<TQuery, TResult>`** (asynchronous):
- `abstract Task<TResult> ExecuteAsync(TQuery query, CancellationToken ct)` - must be implemented
- `virtual Task<TResult> FallbackAsync(TQuery query, CancellationToken ct)` - returns `Task.FromResult(default(TResult))`
- `Execute` / `Fallback` - throw `NotImplementedException` with a message directing the developer to use `QueryHandler`

### Pipeline Separation

The `QueryProcessor` maintains two distinct execution paths:

- `Execute<TResult>(IQuery<TResult> query)` calls `PipelineBuilder.Build()` which reflects on the `Execute` method for attributes
- `ExecuteAsync<TResult>(IQuery<TResult> query, CancellationToken ct)` calls `PipelineBuilder.BuildAsync()` which reflects on the `ExecuteAsync` method for attributes

Each path builds its own pipeline with the appropriate delegate signatures:
- Sync: `Func<IQuery<TResult>, TResult>`
- Async: `Func<IQuery<TResult>, CancellationToken, Task<TResult>>`

### Decorator Dual Support

Decorators must implement both paths via `IQueryHandlerDecorator<TQuery, TResult>`:

```csharp
TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback);
Task<TResult> ExecuteAsync(TQuery query,
    Func<TQuery, CancellationToken, Task<TResult>> next,
    Func<TQuery, CancellationToken, Task<TResult>> fallback,
    CancellationToken ct);
```

This means every decorator works with both handler types. The `FallbackPolicyDecorator`, for example, implements both sync and async versions of the try/catch/fallback logic.

### Attribute Placement

Developers place decorator attributes on whichever method they implement:

```csharp
// Sync handler - attributes on Execute
[QueryLogging(1)]
public override string Execute(GetFoo query) { ... }

// Async handler - attributes on ExecuteAsync
[QueryLogging(1)]
public override async Task<string> ExecuteAsync(GetFoo query, CancellationToken ct) { ... }
```

The `PipelineBuilder` only reflects on the method for the path being built, so attributes on the "wrong" method are ignored.

## Consequences

### Positive

- **No forced async overhead**: Simple synchronous handlers avoid `Task.FromResult` wrapping and async state machines
- **Clear developer guidance**: Base classes make the choice explicit - inherit from `QueryHandler` or `QueryHandlerAsync`
- **Uniform infrastructure**: `PipelineBuilder`, factories, and registries work identically for both paths
- **Helpful error messages**: If a developer calls `Execute` on an async handler (or vice versa), the `NotImplementedException` tells them which base class to use

### Negative

- **Interface has dead methods**: Every handler type has two methods that throw `NotImplementedException`. This is a pragmatic trade-off for infrastructure uniformity
- **Decorator duplication**: Every decorator must implement both `Execute` and `ExecuteAsync`, even though only one will be called for a given handler. The logic is often similar but cannot be shared due to the different signatures
- **Caller responsibility**: The caller must know whether to call `Execute` or `ExecuteAsync`. Calling the wrong one on a handler produces a runtime exception, not a compile-time error

### Risks and Mitigations

**Risk**: A developer places decorator attributes on `Execute` but calls `ExecuteAsync` (or vice versa), resulting in an undecorated pipeline.
- **Mitigation**: Convention and documentation make it clear that attributes go on the method being implemented. The `QueryHandler` / `QueryHandlerAsync` base classes make one method abstract, naturally guiding attribute placement.

**Risk**: Mixing sync and async in the same handler.
- **Mitigation**: The base classes prevent this by making one path abstract and the other throw. Implementing `IQueryHandler<,>` directly is possible but documented as an advanced scenario.

## Alternatives Considered

### Alternative 1: Async-Only

Require all handlers to be async. Synchronous handlers wrap results in `Task.FromResult`.

**Rejected because**:
- Adds overhead and complexity for truly synchronous operations (in-memory lookups, cache reads)
- `Task.FromResult` wrappers obscure intent
- Brighter supports both paths; consistency is valuable

### Alternative 2: Separate Interfaces

Define `ISyncQueryHandler<TQuery, TResult>` and `IAsyncQueryHandler<TQuery, TResult>` as completely separate types.

**Rejected because**:
- The `PipelineBuilder` and factory infrastructure would need to be duplicated or significantly complicated to handle two unrelated type hierarchies
- Registry lookup (`Type Get(Type queryType)`) would need to return different types depending on execution path
- Decorator types would need separate interfaces too, multiplying the type surface area

### Alternative 3: Single Method Returning ValueTask

Use `ValueTask<TResult>` for all handlers, which avoids allocation for synchronous completions.

**Rejected because**:
- `ValueTask` was not widely available when Darker was designed
- Still requires async ceremony in handler signatures even for purely synchronous code
- `ValueTask` has usage restrictions (cannot be awaited multiple times) that complicate decorator implementation

## References

- Related ADRs:
  - [ADR 0002: Attribute-Driven Decorator Pipeline](0002-attribute-driven-decorator-pipeline.md) - Pipeline builds separate paths for sync/async
- Source files:
  - `src/Paramore.Darker/IQueryHandler.cs` - Unified interface
  - `src/Paramore.Darker/QueryHandler.cs` - Sync base class
  - `src/Paramore.Darker/QueryHandlerAsync.cs` - Async base class
  - `src/Paramore.Darker/IQueryHandlerDecorator.cs` - Dual decorator contract
  - `src/Paramore.Darker/QueryProcessor.cs` - Both execution paths
  - `src/Paramore.Darker/PipelineBuilder.cs` - `Build()` vs `BuildAsync()`
