# 8. Split IQueryProcessor into Separate Sync and Async Interfaces

Date: 2026-05-15

## Status

Accepted

## Context

**Parent Requirement**: [specs/003-split-processor/requirements.md](../../specs/003-split-processor/requirements.md)

**Scope**: This ADR focuses on splitting the `IQueryProcessor` interface into separate sync and async contracts, applying the Interface Segregation Principle to the processor's public API.

### The Problem

`IQueryProcessor` currently defines both execution paths in a single interface:

```csharp
public interface IQueryProcessor
{
    TResult Execute<TResult>(IQuery<TResult> query);
    Task<TResult> ExecuteAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}
```

Most modern applications only use the async path. A consumer injecting `IQueryProcessor` to call `ExecuteAsync` also depends on `Execute`, which it never uses. This violates the Interface Segregation Principle.

ADR 0005 already recognises that sync and async are separate pipelines internally (`PipelineBuilder.Build()` vs `PipelineBuilder.BuildAsync()`). The handler layer is already split (`QueryHandler<,>` vs `QueryHandlerAsync<,>`). The processor interface is the one place where the two paths are still conflated.

### Forces

- **ISP**: Consumers should not depend on methods they do not call
- **Clear intent**: The split should encourage consumers to pick one path, not provide a combined interface that undermines the purpose of the change
- **Testing**: `FakeQueryProcessor` and test doubles should be easy to implement for either path alone
- **DI registration**: The `AddDarker()` extension must register the processor against both interfaces
- **Simplicity**: Minimise the number of new types; do not over-engineer the split

## Decision

### Introduce Two Narrow Interfaces

Define two focused interfaces, each representing a single role:

```csharp
namespace Paramore.Darker
{
    /// <summary>
    /// Dispatches queries through the sync pipeline.
    /// Role: Service Provider (sync query execution)
    /// </summary>
    public interface IQueryProcessor
    {
        TResult Execute<TResult>(IQuery<TResult> query);
    }

    /// <summary>
    /// Dispatches queries through the async pipeline.
    /// Role: Service Provider (async query execution)
    /// </summary>
    public interface IQueryProcessorAsync
    {
        Task<TResult> ExecuteAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
    }
}
```

### Naming Convention

- **`IQueryProcessor`** keeps the sync method. This preserves the existing interface name for the simpler contract, and avoids forcing every existing consumer to change their injection type.
- **`IQueryProcessorAsync`** gets the async method. The `Async` suffix follows the established C# convention for async variants (cf. `IQueryHandler` / `QueryHandlerAsync`).

This is a **breaking change** for consumers who inject `IQueryProcessor` and call `ExecuteAsync` — they must switch to `IQueryProcessorAsync`. This is acceptable for a V5 major release.

### No Combined Interface

We deliberately do **not** provide a combined `IQueryProcessor + IQueryProcessorAsync` interface. A combined interface would undermine the purpose of the split by giving consumers a convenient way to keep depending on both paths. The whole point is to encourage consumers to choose one. Consumers who genuinely need both paths can inject both interfaces — adding two constructor parameters is not a significant burden.

### QueryProcessor Implements Both

```
QueryProcessor : IQueryProcessor, IQueryProcessorAsync
       ├── IQueryProcessor          (Execute)
       └── IQueryProcessorAsync     (ExecuteAsync)
```

`QueryProcessor` implements both interfaces directly. No changes needed to the internal implementation — only the type declaration line changes.

### DI Registration

`ServiceCollectionExtensions.AddDarker()` registers `QueryProcessor` as the concrete type, then registers both interfaces forwarding to it:

```csharp
services.TryAdd(new ServiceDescriptor(
    typeof(QueryProcessor),
    provider => BuildQueryProcessor(...),
    options.QueryProcessorLifetime));

services.TryAdd(new ServiceDescriptor(
    typeof(IQueryProcessor),
    provider => provider.GetRequiredService<QueryProcessor>(),
    options.QueryProcessorLifetime));

services.TryAdd(new ServiceDescriptor(
    typeof(IQueryProcessorAsync),
    provider => provider.GetRequiredService<QueryProcessor>(),
    options.QueryProcessorLifetime));
```

The primary registration is the concrete `QueryProcessor`. Both interface registrations forward to it, ensuring a single instance per scope.

### FakeQueryProcessor

`FakeQueryProcessor` in the testing package updates to implement both `IQueryProcessor` and `IQueryProcessorAsync`. Consumers inject it as whichever interface their code under test requires.

### QueryProcessorBuilder

`QueryProcessorBuilder.Build()` currently returns `IQueryProcessor`. Since the builder creates a `QueryProcessor` that implements both interfaces, the caller can cast or assign to either. The return type remains `IQueryProcessor` for the sync path; a new `BuildAsync()` method is not needed since the caller has the concrete type available via the builder.

### Responsibility-Driven Design Analysis

Using RDD stereotypes:

| Role | Interface | Responsibility | Stereotype |
|------|-----------|---------------|------------|
| Sync query dispatcher | `IQueryProcessor` | **Doing**: dispatches a query through the sync pipeline and returns a result | Service Provider |
| Async query dispatcher | `IQueryProcessorAsync` | **Doing**: dispatches a query through the async pipeline and returns a result | Service Provider |

Each interface has a single cohesive responsibility. `QueryProcessor` implements both roles because it genuinely supports both paths and both share the same dependencies (handler registry, factories, context). This is a class implementing multiple related roles — acceptable because the roles are cohesive (both dispatch queries, differing only in execution model).

## Consequences

### Positive

- **ISP compliance**: Async-only consumers (the majority) depend only on `IQueryProcessorAsync`
- **Clearer intent**: A constructor taking `IQueryProcessorAsync` documents that the class only uses async queries
- **Easier testing**: Test doubles only need to implement the interface they're testing against
- **Aligned with existing split**: The handler layer is already split; now the processor layer matches
- **No temptation to conflate**: Without a combined interface, consumers are guided to pick the path they actually use

### Negative

- **Breaking change**: Consumers injecting `IQueryProcessor` who call `ExecuteAsync` must update their injection type to `IQueryProcessorAsync`
- **Two constructor parameters**: Consumers who genuinely need both paths must inject two interfaces instead of one. This is a minor inconvenience that correctly signals unusual usage
- **DI complexity**: Registration needs forwarding descriptors, slightly more complex than the current single registration

### Risks and Mitigations

**Risk**: Consumers miss the breaking change and get a compile error.
- **Mitigation**: This is a V5 major version. Document the migration in release notes. The compile error is clear — `IQueryProcessor` no longer has `ExecuteAsync`, and the fix is to change the injection type to `IQueryProcessorAsync`.

**Risk**: The forwarding DI registrations resolve to different instances.
- **Mitigation**: The primary registration is the concrete `QueryProcessor`; both interfaces forward to it. This is a well-established DI pattern. Unit tests will verify single-instance resolution.

## Alternatives Considered

### Alternative 1: Keep IQueryProcessor as the Combined Interface

Keep the name `IQueryProcessor` for the combined interface and create `ISyncQueryProcessor` / `IAsyncQueryProcessor` as the narrow ones.

**Rejected because**:
- Existing consumers injecting `IQueryProcessor` would still compile but would depend on more than they need — the split would be additive but wouldn't encourage migration to the narrow interfaces
- The goal is to nudge consumers toward the narrow interface, not to keep the status quo as the default

### Alternative 2: Provide a Combined Interface (IQueryProcessorProvider)

Split into two narrow interfaces but also provide `IQueryProcessorProvider : IQueryProcessor, IQueryProcessorAsync` for consumers who want both.

**Rejected because**:
- A combined interface undermines the purpose of the split — it gives consumers a convenient way to avoid choosing a path
- It implies the library expects consumers to use both paths, which is the opposite of the intent
- Consumers who genuinely need both can inject both interfaces; the added constructor parameter correctly signals unusual usage

### Alternative 3: Remove Sync Support Entirely

Make `IQueryProcessor` async-only, drop the sync path.

**Rejected because**:
- ADR 0005 establishes that both paths have value
- Brighter supports both; consistency matters
- Some consumers genuinely use sync handlers for in-memory/cache queries

### Alternative 4: Use IQueryProcessor and IQueryProcessor<Async> (Generic Marker)

Use a generic marker to distinguish sync from async.

**Rejected because**:
- Unconventional and confusing
- The `Async` suffix is the established C# convention

## References

- Requirements: [specs/003-split-processor/requirements.md](../../specs/003-split-processor/requirements.md)
- Related ADRs:
  - [ADR 0005: Dual Sync/Async Handler Support](0005-dual-sync-async-support.md) - Establishes the two execution paths
  - [ADR 0003: Fluent Builder for QueryProcessor](0003-fluent-builder-for-query-processor.md) - Builder returns `IQueryProcessor`
- Issue: [#288](https://github.com/BrighterCommand/Darker/issues/288)
- Discussion: [V5 Roadmap #273](https://github.com/BrighterCommand/Darker/discussions/273)
- Source files:
  - `src/Paramore.Darker/IQueryProcessor.cs` - Current combined interface
  - `src/Paramore.Darker/QueryProcessor.cs` - Concrete implementation
  - `src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionExtensions.cs` - DI registration
  - `src/Paramore.Darker.Testing/FakeQueryProcessor.cs` - Test double
  - `src/Paramore.Darker/Builder/QueryProcessorBuilder.cs` - Fluent builder
