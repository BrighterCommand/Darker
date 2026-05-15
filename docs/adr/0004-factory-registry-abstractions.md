# 4. Factory and Registry Abstractions for DI Container Independence

Date: 2026-05-15

## Status

Accepted

## Context

**Scope**: How Darker creates and manages handler and decorator instances without coupling to a specific DI container.

### The Problem

Darker needs to create query handler and decorator instances at runtime. The `PipelineBuilder` must:
1. Look up which handler type handles a given query type
2. Create an instance of that handler
3. Create instances of the decorators declared on the handler
4. Release all instances after the query completes

Different applications use different DI containers (Microsoft.Extensions.DependencyInjection, SimpleInjector, LightInject, Autofac) or no container at all. Darker must work with all of them without taking a dependency on any specific one.

### Design Approach

We follow the approach outlined by Mark Seemann in [DI-Friendly Framework](https://blog.ploeh.dk/2014/05/19/di-friendly-framework/). The core principle is that a framework should be DI-friendly without depending on any specific DI container. Instead of creating a generic abstraction over DI containers (which Seemann identifies as the "conforming container" anti-pattern), we define specific Abstract Factory interfaces for each extensibility point, with `Create` and `Release` methods. This allows:

- Hand-coded composition without any container (using `Activator.CreateInstance` or custom factories)
- Integration with any DI container via a thin adapter that implements our factory interfaces
- Default implementations for users who don't need customisation

The framework (Darker) is the client of user-provided handlers and decorators, so following the Dependency Inversion Principle, the framework owns the abstract interfaces that user code implements.

### Constraints

- Darker's core package (`Paramore.Darker`) must have zero DI container dependencies
- Container-specific integrations should be separate packages
- The abstraction must support both creation and cleanup (to work with scoped lifetimes)
- Registration of handlers and decorators must also be abstracted

## Decision

We define four abstractions that separate registration from creation, and handlers from decorators:

### Registration Abstractions

**`IQueryHandlerRegistry`** (information holder): Maps query types to handler types.
- `Type Get(Type queryType)` - retrieves the handler type for a query
- `Register<TQuery, TResult, THandler>()` - type-safe registration with compile-time checking
- `Register(Type queryType, Type resultType, Type handlerType)` - dynamic registration for assembly scanning

The default implementation, `QueryHandlerRegistry`, stores mappings in a `Dictionary<Type, Type>` and validates that:
- The result type matches the query's generic argument
- No duplicate registrations exist (throws `ConfigurationException`)
- Assembly scanning via `RegisterFromAssemblies()` discovers `IQueryHandler<,>` implementations automatically

**`IQueryHandlerDecoratorRegistry`** (information holder): Registers decorator types with the DI container.
- `void Register(Type decoratorType)` - registers a decorator type for resolution

This is intentionally simple - it exists so the builder can tell the DI container about decorator types that need to be resolvable.

### Creation Abstractions

**`IQueryHandlerFactory`** (service provider): Creates and releases handler instances.
- `IQueryHandler Create(Type handlerType)` - creates a handler instance
- `void Release(IQueryHandler handler)` - releases the instance (for scoped cleanup)

**`IQueryHandlerDecoratorFactory`** (service provider): Creates and releases decorator instances.
- `T Create<T>(Type decoratorType)` - creates a decorator instance with generic type safety
- `void Release<T>(T handler)` - releases the instance

### Aggregation

**`IHandlerConfiguration`** (information holder): Groups all four abstractions into a single object, implemented by the sealed `HandlerConfiguration` class. This is passed to `QueryProcessor`'s constructor, keeping it from needing four separate parameters.

### Delegate Adapters

For simple scenarios where a full DI container is unnecessary, `FactoryFuncWrapper` adapts a `Func<Type, object>` to both factory interfaces, and `RegistryActionWrapper` adapts an `Action<Type>` to `IQueryHandlerDecoratorRegistry`. This enables usage like:

```csharp
QueryProcessorBuilder.With()
    .Handlers(registry, Activator.CreateInstance, t => {}, Activator.CreateInstance)
    .InMemoryQueryContextFactory()
    .Build();
```

The `FactoryFuncWrapper` also handles cleanup: on `Release()`, if the instance implements `IDisposable`, it disposes it. This provides correct resource management even without a DI container.

### Container Integration Pattern

DI container integration packages (e.g. `Paramore.Darker.AspNetCore`) provide:
1. Implementations of the factory and registry interfaces that delegate to the container
2. Extension methods on the builder interfaces for fluent configuration

For example, `Paramore.Darker.AspNetCore` provides `AddDarker()` on `IServiceCollection` which registers all components with `Microsoft.Extensions.DependencyInjection` and constructs the `QueryProcessor`.

> **Note**: The `Paramore.Darker.SimpleInjector` and `Paramore.Darker.LightInject` integration packages were removed in V5 (see [ADR 0006](0006-remove-third-party-di-packages.md)). Users of third-party DI containers should use their container's MS DI adapter with the ASP.NET Core integration, or implement the factory/registry interfaces directly.

## Consequences

### Positive

- **Zero coupling**: The core package has no dependency on any DI framework
- **Pluggable**: New container integrations can be added as separate packages without modifying the core
- **Testable**: Factories and registries are easily mocked for unit testing
- **Resource management**: The create/release pattern ensures proper cleanup regardless of container strategy
- **Progressive complexity**: Simple scenarios use delegate wrappers; complex scenarios use full container integrations

### Negative

- **Four abstractions for one concern**: The separation of handler vs decorator and registration vs creation produces four interfaces where some developers might expect one or two
- **Indirection**: The `PipelineBuilder` calls `_handlerFactory.Create()` rather than resolving directly from a container, adding a layer of indirection

### Risks and Mitigations

**Risk**: Factory implementations may not properly release resources, causing memory leaks.
- **Mitigation**: `PipelineBuilder.Dispose()` explicitly releases both the handler and all decorators. `FactoryFuncWrapper` disposes `IDisposable` instances on release.

**Risk**: Registry and factory get out of sync (handler registered but factory can't create it).
- **Mitigation**: `PipelineBuilder.ResolveHandler()` throws `MissingHandlerException` with a clear message. Assembly scanning (`RegisterFromAssemblies`) ensures registration matches available types.

## Alternatives Considered

### Alternative 1: Depend on Microsoft.Extensions.DependencyInjection.Abstractions

Use `IServiceProvider` directly in the core library.

**Rejected because**:
- Forces a dependency on a specific abstraction package
- `IServiceProvider.GetService()` returns `object` with no release mechanism for scoped resources
- Not all DI containers implement `IServiceProvider` natively (though most now do via adapters)

### Alternative 2: Conforming Container

Define a single generic abstraction over DI containers (e.g. `IContainer` with `Resolve<T>()` and `Register<T>()`), and require all container integrations to implement it.

**Rejected because**:
- This is the ["conforming container" anti-pattern](https://blog.ploeh.dk/2014/05/19/di-friendly-framework/) identified by Mark Seemann - it forces a lowest-common-denominator interface onto containers with fundamentally different capabilities
- Obscures container-specific features and lifecycle management
- Creates a false abstraction that leaks in practice

### Alternative 3: Service Locator Pattern

Pass a service locator that resolves any type.

**Rejected because**:
- Service Locator is widely considered an anti-pattern as it hides dependencies
- No distinction between handler and decorator concerns
- No explicit lifecycle management (create/release)

### Alternative 4: Convention-Based Resolution

Resolve handlers by naming convention (e.g. `GetFooQuery` -> `GetFooQueryHandler`).

**Rejected because**:
- Fragile - renaming breaks resolution
- Cannot support multiple handler strategies or custom mappings
- No compile-time safety

## References

- Related ADRs:
  - [ADR 0002: Attribute-Driven Decorator Pipeline](0002-attribute-driven-decorator-pipeline.md) - Consumes these abstractions
  - [ADR 0003: Fluent Builder](0003-fluent-builder-for-query-processor.md) - Wires these abstractions together
- Source files:
  - `src/Paramore.Darker/IQueryHandlerRegistry.cs`
  - `src/Paramore.Darker/IQueryHandlerFactory.cs`
  - `src/Paramore.Darker/IQueryHandlerDecoratorFactory.cs`
  - `src/Paramore.Darker/IQueryHandlerDecoratorRegistry.cs`
  - `src/Paramore.Darker/IHandlerConfiguration.cs`
  - `src/Paramore.Darker/HandlerConfiguration.cs`
  - `src/Paramore.Darker/QueryHandlerRegistry.cs`
  - `src/Paramore.Darker/Builder/FactoryFuncWrapper.cs`
  - `src/Paramore.Darker/Builder/RegistryActionWrapper.cs`
- External references:
  - Mark Seemann, [DI-Friendly Framework](https://blog.ploeh.dk/2014/05/19/di-friendly-framework/) - The approach Darker follows: specific Abstract Factory interfaces per extensibility point, not a conforming container
  - Mark Seemann, [Dependency Injection in .NET](https://www.manning.com/books/dependency-injection-in-dot-net)
  - [Dependency Inversion Principle](https://en.wikipedia.org/wiki/Dependency_inversion_principle)
