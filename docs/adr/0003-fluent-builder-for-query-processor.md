# 3. Fluent Builder with Interface Progression for QueryProcessor Construction

Date: 2026-05-15

## Status

Accepted

## Context

**Scope**: How the `QueryProcessor` is constructed and configured.

### The Problem

Constructing a `QueryProcessor` requires several collaborators to be provided in the correct order:
1. Handler configuration (registry, handler factory, decorator registry, decorator factory)
2. A query context factory
3. Optional extensions (context bag items, additional decorator registrations)

If any required collaborator is missing, the processor will fail at runtime. If collaborators are provided in the wrong combination, the result is an invalid configuration. We need a construction mechanism that guides the developer through the required steps and makes invalid configurations impossible (or at least difficult) to express.

### Constraints

- The builder must support multiple DI container strategies (direct interfaces, delegate wrappers, pre-built configuration objects)
- Default decorators (e.g. `FallbackPolicyDecorator`) must be registered automatically
- Consistency with Brighter's builder pattern is desirable

## Decision

We use a **fluent builder with interface progression** (`QueryProcessorBuilder`) where each step in the construction returns a different interface, constraining what the caller can do next. The builder implements all interfaces but only exposes the appropriate one at each stage.

### Interface Progression

```
QueryProcessorBuilder.With()     -> INeedHandlers
    .Handlers(...)               -> INeedAQueryContext
    .InMemoryQueryContextFactory()  -> IBuildTheQueryProcessor
    .Build()                     -> IQueryProcessor
```

The interfaces enforce ordering at compile time:

| Interface | Role | Methods |
|-----------|------|---------|
| `INeedHandlers` | Require handler configuration | Three `Handlers()` overloads |
| `INeedAQueryContext` | Require context factory | `QueryContextFactory()`, `InMemoryQueryContextFactory()` |
| `IBuildTheQueryProcessor` | Allow building | `Build()`, inherits `IQueryProcessorExtensionBuilder` |
| `IQueryProcessorExtensionBuilder` | Optional extensions | `AddContextBagItem()`, `RegisterDecorator()`, `Build()` |

### Three Handler Configuration Strategies

The `INeedHandlers` interface provides three overloads of `Handlers()`, each targeting a different integration style:

1. **Pre-built configuration**: `Handlers(IHandlerConfiguration)` - Pass a fully constructed `HandlerConfiguration` (used by DI container integrations like `Paramore.Darker.AspNetCore`)

2. **Individual interfaces**: `Handlers(IQueryHandlerRegistry, IQueryHandlerFactory, IQueryHandlerDecoratorRegistry, IQueryHandlerDecoratorFactory)` - Pass each collaborator separately

3. **Delegate factories**: `Handlers(IQueryHandlerRegistry, Func<Type, IQueryHandler>, Action<Type>, Func<Type, IQueryHandlerDecorator>)` - Pass factory functions and registration actions, which are wrapped in `FactoryFuncWrapper` and `RegistryActionWrapper` internally

### Default Decorator Registration

The `Build()` method calls `RegisterDefaultDecorators()` which registers `FallbackPolicyDecorator<,>`. This ensures every query processor has fallback support without requiring explicit configuration.

### Roles and Responsibilities

- **`QueryProcessorBuilder`** (coordinator): Orchestrates the construction sequence, holds intermediate state, enforces step ordering through interface narrowing
- **`HandlerConfiguration`** (information holder): Aggregates the four handler infrastructure components into a single object
- **`FactoryFuncWrapper`** (interfacer): Adapts `Func<Type, object>` delegates to the `IQueryHandlerFactory` and `IQueryHandlerDecoratorFactory` interfaces, including `IDisposable` handling on release
- **`RegistryActionWrapper`** (interfacer): Adapts `Action<Type>` delegates to the `IQueryHandlerDecoratorRegistry` interface

## Consequences

### Positive

- **Compile-time guidance**: The interface progression prevents calling `Build()` before providing handlers, or providing a context factory before handlers
- **Discoverability**: IDE autocompletion shows only valid next steps at each stage
- **Multiple integration styles**: The three `Handlers()` overloads support direct DI container integration, manual wiring, and simple delegate-based construction
- **Sensible defaults**: `FallbackPolicyDecorator` is always registered; `InMemoryQueryContextFactory` provides a zero-configuration context option

### Negative

- **Interface proliferation**: Four interfaces for a single builder adds type surface area
- **Single class implements all interfaces**: `QueryProcessorBuilder` implements all four interfaces, which is an accepted trade-off for the fluent API pattern but means the type system enforcement can be circumvented by casting

### Risks and Mitigations

**Risk**: Developers bypass the builder and construct `QueryProcessor` directly.
- **Mitigation**: `QueryProcessor` has a public constructor by design (for DI container integration), but the builder is the documented and recommended path. The constructor validates all required parameters.

## Alternatives Considered

### Alternative 1: Constructor with Optional Parameters

A single constructor with optional/default parameters.

**Rejected because**:
- No compile-time enforcement of required vs optional dependencies
- Parameter ordering is fragile and scales poorly as options grow
- No guided workflow through IDE autocompletion

### Alternative 2: Configuration Object Pattern

A single `QueryProcessorOptions` object passed to the constructor.

**Rejected because**:
- Allows partially configured objects to be passed
- No progressive disclosure of what's needed next
- Less discoverable than the fluent API

### Alternative 3: Separate Builders per DI Container

Each DI container integration provides its own builder.

**Rejected because**:
- Duplicates builder logic across packages
- DI container integrations work better as extension methods on the existing builder interfaces (which is what `Paramore.Darker.SimpleInjector` and `Paramore.Darker.AspNetCore` do)

## References

- Related ADRs:
  - [ADR 0004: Factory and Registry Abstractions](0004-factory-registry-abstractions.md) - The components the builder wires together
- Source files:
  - `src/Paramore.Darker/Builder/QueryProcessorBuilder.cs` - The builder
  - `src/Paramore.Darker/Builder/INeedHandlers.cs` - First step interface
  - `src/Paramore.Darker/Builder/INeedAQueryContext.cs` - Second step interface
  - `src/Paramore.Darker/Builder/IBuildTheQueryProcessor.cs` - Final step interface
  - `src/Paramore.Darker/Builder/FactoryFuncWrapper.cs` - Delegate adapter
  - `src/Paramore.Darker/Builder/RegistryActionWrapper.cs` - Delegate adapter
- External references:
  - [Fluent Builder Pattern](https://en.wikipedia.org/wiki/Builder_pattern)
  - Brighter's `CommandProcessorBuilder` (same pattern)
