# 2. Attribute-Driven Decorator Pipeline for Query Handling

Date: 2026-05-15

## Status

Accepted

## Context

**Scope**: How cross-cutting concerns (logging, retry, fallback) are applied to query handlers.

### The Problem

Query handlers need cross-cutting concerns such as logging, retry policies, circuit breakers, and fallback behaviour. These concerns must be composable - a handler might need logging and retry but not fallback, or all three in a specific order. The mechanism must support both synchronous and asynchronous execution paths.

This is the same problem that Brighter solves on the command side. Darker needed an equivalent solution for the query side.

### Requirements Context

- Cross-cutting concerns must be independently composable per handler
- The order of concern execution must be explicit and controllable
- Adding a new cross-cutting concern should not require modifying existing handlers
- The solution must work with any DI container (see ADR 0004)

### Constraints

- Consistency with Brighter's architectural approach is desirable
- Handlers are created per-query and disposed after execution
- Both sync and async execution paths must be supported (see ADR 0005)

## Decision

We use an **attribute-driven decorator pipeline**, where cross-cutting concerns are declared as attributes on the handler's `Execute` or `ExecuteAsync` method and composed into a chain of decorators at runtime.

### Pipeline Construction

The `PipelineBuilder<TResult>` (internal, sealed) builds the pipeline through these steps:

1. **Resolve the handler**: Look up the handler type from `IQueryHandlerRegistry` by query type, then create an instance via `IQueryHandlerFactory`.

2. **Discover decorators**: Reflect on the handler's `Execute`/`ExecuteAsync` method for `QueryHandlerAttribute` subclasses (e.g. `[QueryLogging(1)]`, `[RetryableQuery(3)]`).

3. **Order by step**: Sort attributes by their `Step` property in descending order. Higher step numbers execute first (outermost in the pipeline).

4. **Build the chain**: Starting with the handler's `Execute` method as the innermost function, wrap each decorator around it. Each decorator receives:
   - `next` - a delegate to the next stage in the pipeline
   - `fallback` - a delegate to the handler's `Fallback`/`FallbackAsync` method

5. **Return the entry point**: The outermost function in the chain, which the `QueryProcessor` invokes.

### Step Ordering

Decorators are ordered by step number, with higher numbers executing first (outermost):

```
Step 3 (outermost) -> Step 2 -> Step 1 -> Handler.Execute (innermost)
```

For example:
```csharp
[QueryLogging(1)]       // Step 1: innermost decorator, logs around handler
[FallbackPolicy(2)]     // Step 2: catches exceptions, invokes fallback
[RetryableQuery(3)]     // Step 3: outermost, retries the entire inner pipeline
public override async Task<string> ExecuteAsync(GetFoo query, CancellationToken ct)
```

### Attribute Architecture

`QueryHandlerAttribute` is the abstract base class. Each subclass:
- Specifies its `Step` in the constructor (controls ordering)
- Returns its decorator type via `GetDecoratorType()` (an open generic like `typeof(FallbackPolicyDecorator<,>)`)
- Passes configuration to the decorator via `GetAttributeParams()`

The decorator type is closed over `IQuery<TResult>` and `TResult` at pipeline build time using `MakeGenericType`.

### Decorator Contract

Decorators implement `IQueryHandlerDecorator<TQuery, TResult>`, which defines:

```csharp
TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback);
Task<TResult> ExecuteAsync(TQuery query,
    Func<TQuery, CancellationToken, Task<TResult>> next,
    Func<TQuery, CancellationToken, Task<TResult>> fallback,
    CancellationToken ct);
```

This signature gives each decorator full control: it can invoke `next` to continue the pipeline, invoke `fallback` to trigger the handler's fallback path, or do both (e.g. try `next`, catch, then `fallback`).

### Fallback as a First-Class Concept

Every handler has `Fallback`/`FallbackAsync` methods (defaulting to `default(TResult)`). The fallback delegate is passed through the entire decorator chain, separate from the `next` delegate. This means any decorator - not just the innermost - can decide to invoke the fallback path. The `FallbackPolicyDecorator` is registered by default and catches configured exception types, storing the exception in the query context bag under `CauseOfFallbackException`.

### Per-Query Pipeline Lifecycle

A new `PipelineBuilder` is created for each query execution and disposed afterwards. On disposal, the handler is released via `IQueryHandlerFactory.Release()` and each decorator via `IQueryHandlerDecoratorFactory.Release()`. This ensures no shared mutable state between query executions.

### Reflection and Exception Handling

Because the handler's `Execute` method is invoked via `MethodInfo.Invoke()`, exceptions are wrapped in `TargetInvocationException`. Both `PipelineBuilder` and `QueryProcessor` unwrap these using `ExceptionDispatchInfo.Capture(ex.InnerException).Throw()` to preserve the original stack trace.

## Consequences

### Positive

- **Declarative composition**: Cross-cutting concerns are visible at the handler method declaration, making it easy to see what pipeline a handler uses
- **Independent extensibility**: New decorators can be added (e.g. caching, authorisation) without modifying existing code - just create an attribute and decorator pair
- **Flexible ordering**: Step numbers give explicit control over execution order
- **Separation of concerns**: Each decorator handles exactly one concern
- **Consistency with Brighter**: The same mental model applies to both command and query sides

### Negative

- **Reflection cost**: Pipeline is built via reflection on every query execution (no caching of the pipeline structure)
- **Runtime discovery**: Decorator configuration errors (missing registrations, wrong types) surface at runtime, not compile time
- **Step number management**: Developers must manually coordinate step numbers; there is no compile-time check for conflicts or gaps

### Risks and Mitigations

**Risk**: Reflection-based invocation adds overhead per query.
- **Mitigation**: For most query workloads, the cost of the actual query (database, network) dominates. The reflection overhead is negligible in practice.

**Risk**: Incorrect step ordering produces subtle bugs.
- **Mitigation**: Logging at pipeline build time records the decorator chain, making it observable. The README documents the ordering convention.

## Alternatives Considered

### Alternative 1: Middleware Pipeline (ASP.NET Core style)

A `Use()` / `Run()` style middleware pipeline configured in startup.

**Rejected because**:
- Applies the same middleware to all queries; per-handler composition requires additional filtering logic
- Less discoverable - you must check startup configuration, not the handler itself
- Inconsistent with Brighter's approach

### Alternative 2: Manual Decorator Wrapping

Explicitly wrap handlers in decorators in the composition root.

**Rejected because**:
- Verbose and error-prone for many handler/decorator combinations
- Cross-cutting concern configuration is separated from the handler it applies to
- Does not scale as the number of handlers grows

### Alternative 3: Aspect-Oriented Programming (AOP) with IL Weaving

Use a framework like PostSharp or Fody to weave cross-cutting concerns at compile time.

**Rejected because**:
- Adds a build-time dependency and complexity
- Less transparent - behaviour is injected invisibly
- Harder to debug and reason about

## References

- Related ADRs:
  - [ADR 0004: Factory and Registry Abstractions](0004-factory-registry-abstractions.md) - How decorators are created
  - [ADR 0005: Dual Sync/Async Handler Support](0005-dual-sync-async-support.md) - Both pipeline paths
- Source files:
  - `src/Paramore.Darker/PipelineBuilder.cs` - Pipeline construction
  - `src/Paramore.Darker/Attributes/QueryHandlerAttribute.cs` - Base attribute
  - `src/Paramore.Darker/IQueryHandlerDecorator.cs` - Decorator contract
  - `src/Paramore.Darker/Decorators/FallbackPolicyDecorator.cs` - Default decorator
- External references:
  - [Decorator Pattern (GoF)](https://en.wikipedia.org/wiki/Decorator_pattern)
  - [Brighter's Pipeline Architecture](https://github.com/BrighterCommand/Paramore.Brighter)
