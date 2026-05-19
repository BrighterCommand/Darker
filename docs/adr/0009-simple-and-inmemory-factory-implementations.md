# 9. Simple and InMemory Factory Implementations for Testing and Lightweight Usage

Date: 2026-05-18

## Status

Accepted

## Context

**Parent Requirement**: [specs/003-split-handler/requirements.md](../../specs/003-split-handler/requirements.md)

**Scope**: This ADR addresses the addition of public, concrete implementations of the factory and registry abstractions defined in [ADR 0004](0004-factory-registry-abstractions.md). These implementations follow Brighter's established `SimpleHandlerFactory` and `InMemory*` patterns.

### The Problem

ADR 0004 defined four abstraction pairs (handler factory, decorator factory, handler registry, decorator registry) to keep Darker DI-container-independent. The core package provides:

- `QueryHandlerRegistry` / `QueryHandlerRegistryAsync` — concrete registry implementations (public)
- `InMemoryQueryContextFactory` — concrete context factory (public)
- `FactoryFuncWrapper` — delegate adapter for both handler and decorator factories (internal, Builder-only)
- `RegistryActionWrapper` — delegate adapter for decorator registry (internal, Builder-only)

The handler and decorator **factories** have no public concrete implementation in the core package. The only options today are:

1. Use the internal `FactoryFuncWrapper` indirectly through the Builder's fluent API
2. Implement the interfaces yourself (the DI integration packages do this)
3. Use mocking frameworks like Moq in tests

This means tests throughout the codebase rely on `Mock<IQueryHandlerFactory>` and `Mock<IQueryHandlerDecoratorFactory>`. This creates several problems:

- **Tests couple to mock verification** (call counts, argument matching) rather than behavior
- **Mock setup is verbose** and obscures the Arrange section's evident data
- **Mocks cannot be reused** across test projects or by consumers writing their own tests
- **Inconsistency with Brighter**, which provides `SimpleHandlerFactory` as a public, first-class type

With ADR 0008 doubling the number of factory interfaces (sync + async), the mock burden doubles too. Every test that configures a `QueryProcessor` would need mocks for up to six interfaces.

### Constraints

- Must remain in the core package (`Paramore.Darker`) with zero additional dependencies
- Must implement both sync and async factory interfaces (per ADR 0008)
- Must follow Brighter's naming conventions for consistency across the Paramore family
- The existing internal `FactoryFuncWrapper` must continue to work for the Builder

## Decision

Add public Simple and InMemory implementations of the factory and registry abstractions to `src/Paramore.Darker/`. These are lightweight, delegate-based implementations suitable for testing and simple production scenarios without a DI container.

### New Types

#### `SimpleHandlerFactory` (Role: Service Provider)

A delegate-based factory that creates handler instances. Follows Brighter's `SimpleHandlerFactory` pattern.

- **Knowing**: The factory delegates for sync and async handler creation
- **Doing**: `Create(Type)` — delegates to the appropriate `Func<Type, IQueryHandler>`
- **Doing**: `Release(IQueryHandler)` — disposes the handler if it implements `IDisposable`

Implements both `IQueryHandlerFactory` and `IQueryHandlerFactoryAsync` on a single class, since both interfaces have identical signatures (`IQueryHandler Create(Type)`, `void Release(IQueryHandler)`). This mirrors Brighter's `SimpleHandlerFactory` which implements both `IAmAHandlerFactorySync` and `IAmAHandlerFactoryAsync`.

```csharp
public class SimpleHandlerFactory(
    Func<Type, IQueryHandler> factory)
    : IQueryHandlerFactory, IQueryHandlerFactoryAsync
{
    public IQueryHandler Create(Type handlerType) => factory(handlerType);

    public void Release(IQueryHandler handler)
    {
        if (handler is IDisposable disposable)
            disposable.Dispose();
    }
}
```

#### `SimpleHandlerDecoratorFactory` (Role: Service Provider)

A delegate-based factory that creates decorator instances.

- **Knowing**: The factory delegate for decorator creation
- **Doing**: `Create<T>(Type)` — delegates to `Func<Type, IQueryHandlerDecorator>` and casts
- **Doing**: `Release<T>(T)` — disposes if `IDisposable`

Implements both `IQueryHandlerDecoratorFactory` and `IQueryHandlerDecoratorFactoryAsync`, since both share the same `IQueryHandlerDecorator` marker constraint.

```csharp
public class SimpleHandlerDecoratorFactory(
    Func<Type, IQueryHandlerDecorator> factory)
    : IQueryHandlerDecoratorFactory, IQueryHandlerDecoratorFactoryAsync
{
    public T Create<T>(Type decoratorType) where T : IQueryHandlerDecorator
        => (T)factory(decoratorType);

    public void Release<T>(T handler) where T : IQueryHandlerDecorator
    {
        if (handler is IDisposable disposable)
            disposable.Dispose();
    }
}
```

#### `InMemoryDecoratorRegistry` (Role: Information Holder)

A simple in-memory store of registered decorator types. Suitable for testing and lightweight scenarios.

- **Knowing**: The set of registered decorator types
- **Doing**: `Register(Type)` — adds a type to the collection

Implements both `IQueryHandlerDecoratorRegistry` and `IQueryHandlerDecoratorRegistryAsync`.

```csharp
public class InMemoryDecoratorRegistry
    : IQueryHandlerDecoratorRegistry, IQueryHandlerDecoratorRegistryAsync
{
    private readonly List<Type> _registeredTypes = new();

    public IReadOnlyList<Type> RegisteredTypes => _registeredTypes;

    public void Register(Type decoratorType) => _registeredTypes.Add(decoratorType);
}
```

### Relationship to Existing Types

| Existing (Internal) | New (Public) | Notes |
|----------------------|-------------|-------|
| `FactoryFuncWrapper` | `SimpleHandlerFactory` + `SimpleHandlerDecoratorFactory` | `FactoryFuncWrapper` combines both factory roles with a loosely-typed `Func<Type, object>`. The new types separate them and use strongly-typed delegates. `FactoryFuncWrapper` remains for Builder backward compatibility. |
| `RegistryActionWrapper` | `InMemoryDecoratorRegistry` | `RegistryActionWrapper` delegates to `Action<Type>`. `InMemoryDecoratorRegistry` stores types directly, which is more useful for testing (you can inspect what was registered). |

### Impact on Tests

Tests replace mocks with real instances:

```csharp
// Before (with Moq)
var handlerFactory = new Mock<IQueryHandlerFactory>();
handlerFactory.Setup(x => x.Create(typeof(MyHandler))).Returns(handler);

// After (with SimpleHandlerFactory)
var handlerFactory = new SimpleHandlerFactory(type => type switch
{
    _ when type == typeof(MyHandler) => handler,
    _ => throw new InvalidOperationException($"No handler for {type}")
});
```

Tests verify behavior (the handler executed, the result is correct) rather than implementation (the factory was called once with this argument). This aligns with the project's testing guidelines: "test behavior, not implementation."

## Consequences

### Positive

- **Reduced mock dependency**: Core tests can use real factory/registry instances instead of Moq
- **Behavior-focused tests**: Tests assert on outcomes rather than mock call counts
- **Consistency with Brighter**: Follows the established `SimpleHandlerFactory` pattern
- **Reusable by consumers**: Users writing their own tests get production-quality test doubles for free
- **Simpler test setup**: One line to create a factory vs. multiple mock Setup/Verify calls

### Negative

- **Three new public types**: Increases the public API surface of `Paramore.Darker`
- **Parallel with internal types**: `FactoryFuncWrapper` and `RegistryActionWrapper` overlap with the new types. These could be refactored later to delegate to the new public types.

### Risks and Mitigations

**Risk**: Users might use `SimpleHandlerFactory` in production where a proper DI container would be more appropriate.
- **Mitigation**: XML documentation clearly states the intended use case (testing and lightweight scenarios). The naming convention `Simple*` signals this, following Brighter's precedent.

## Alternatives Considered

### Alternative 1: Make `FactoryFuncWrapper` and `RegistryActionWrapper` Public

Promote the existing internal types to public.

**Rejected because**:
- `FactoryFuncWrapper` combines handler factory and decorator factory in one class with a loosely-typed `Func<Type, object>`. The Single Responsibility Principle calls for separate types with strongly-typed delegates.
- The naming (`*Wrapper`) suggests an adapter detail, not a first-class implementation. `Simple*` follows Brighter's convention and communicates purpose.
- The Builder's internal wiring should remain separate from the public testing/lightweight API.

### Alternative 2: Continue Using Mocks

Keep using Moq for all factory/registry test doubles.

**Rejected because**:
- Mock-based tests couple to implementation details (which methods were called, with what arguments)
- The ADR 0008 split doubles the number of interfaces to mock, making test setup increasingly verbose
- The project's testing guidelines explicitly prefer developer tests over mock-isolated unit tests
- Brighter has established the pattern of providing real implementations; Darker should follow

### Alternative 3: Place Types in `Paramore.Darker.Testing`

Put the Simple/InMemory types in the testing package rather than core.

**Rejected because**:
- Brighter's `SimpleHandlerFactory` lives in the core package — it's useful beyond tests
- Users without a DI container need lightweight factory implementations
- The types have zero additional dependencies, so they belong in core

## References

- Requirements: [specs/003-split-handler/requirements.md](../../specs/003-split-handler/requirements.md)
- Related ADRs:
  - [ADR 0004: Factory and Registry Abstractions](0004-factory-registry-abstractions.md) — Defines the interfaces these types implement
  - [ADR 0008: Split Handler Interfaces](0008-split-handler-interfaces.md) — Doubles the factory interfaces, motivating this change
- Brighter reference:
  - `src/Paramore.Brighter/SimpleHandlerFactory.cs` — public factory with sync/async delegates
  - `src/Paramore.Brighter/SimpleMessageMapperFactory.cs` — same pattern for message mappers
  - `src/Paramore.Brighter/InMemoryProducerRegistryFactory.cs` — InMemory pattern for registries
- Source files:
  - `src/Paramore.Darker/Builder/FactoryFuncWrapper.cs` — existing internal delegate adapter
  - `src/Paramore.Darker/Builder/RegistryActionWrapper.cs` — existing internal registry adapter
  - `src/Paramore.Darker/IQueryHandlerFactory.cs`, `IQueryHandlerFactoryAsync.cs`
  - `src/Paramore.Darker/IQueryHandlerDecoratorFactory.cs`, `IQueryHandlerDecoratorFactoryAsync.cs`
  - `src/Paramore.Darker/IQueryHandlerDecoratorRegistry.cs`, `IQueryHandlerDecoratorRegistryAsync.cs`
