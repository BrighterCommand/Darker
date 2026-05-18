# Tasks: Split IQueryHandler into Separate Sync and Async Interfaces

**Spec**: 003-split-handler
**ADR**: [0008-split-handler-interfaces](../../docs/adr/0008-split-handler-interfaces.md)
**Issue**: #304

## Task Overview

Tasks are ordered tidy-first: structural changes (interface splits, new types) before behavioral changes (pipeline, processor, DI wiring). Each behavioral task uses TDD with `/test-first`.

**Structural tasks (1-6) are a batch**: The solution will not compile until all six are complete. Individual structural tasks may leave the build broken as intermediate steps. The build verification point is after Task 6 completes.

### Dependencies

```
                    ┌──────────────────────────────────┐
                    │  Structural Batch (Tasks 1-6)    │
                    │  1 → 2 → 3 → 4 → 5 → 6          │
                    │  Build compiles after Task 6     │
                    └──────────────┬───────────────────┘
                                   │
                    ┌──────────────┴───────────────────┐
                    ▼                                   ▼
         ┌─────────────────┐                ┌─────────────────┐
         │ Task 7 (sync    │                │ Task 8 (async   │
         │ pipeline)       │                │ pipeline)       │
         └────────┬────────┘                └────────┬────────┘
                  │                                   │
                  └──────────────┬─────────────────────┘
                                 │
                  ┌──────────────┴───────────────────┐
                  ▼                                   ▼
       ┌─────────────────┐                ┌─────────────────┐
       │ Task 9 (handler │                │ Task 10 (attr   │
       │ not found)      │                │ mismatch)       │
       └────────┬────────┘                └────────┬────────┘
                │                                   │
                └──────────────┬─────────────────────┘
                               │
                               ▼
                  ┌─────────────────────┐
                  │ Task 11 (Config +   │
                  │ QueryProcessor)     │
                  └────────┬────────────┘
                           │
            ┌──────────────┼───────────────┐
            ▼              ▼               ▼
  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
  │ Task 12      │ │ Task 12a     │ │ Task 13 (DI  │
  │ (retire      │ │ (decorator   │ │ wiring)      │
  │ exceptions)  │ │ not found)   │ └──────┬───────┘
  └──────────────┘ └──────────────┘        │
                                           │
            ┌──────────────┬───────────────┤
            ▼              ▼               ▼
  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
  │ Task 14      │ │ Task 15      │ │ Task 16      │
  │ (Policies)   │ │ (Logging)    │ │ (Builder)    │
  └──────────────┘ └──────────────┘ └──────┬───────┘
                                           │
                                           ▼
                                  ┌──────────────┐
                                  │ Task 17      │
                                  │ (sample app) │
                                  └──────────────┘
```

**Parallel tracks**: Tasks 7/8 can run in parallel. Tasks 9/10 can run in parallel. Tasks 12/12a/13 can run in parallel. Tasks 14/15/16 can run in parallel.

---

## Structural Tasks (Tidy-First — Batch)

> **Note**: Tasks 1-6 form a single structural batch. The solution will not fully compile until all six are done. Each task describes its changes; build verification is at the end of Task 6.

### Task 1: Split `IQueryHandler<TQuery, TResult>` into sync and async interfaces
- [x] **TIDY: Split handler interface into two focused interfaces**
  - FR1, FR2, FR3
  - In `src/Paramore.Darker/IQueryHandler.cs`:
    - Keep `IQueryHandler` marker interface (non-generic) unchanged
    - Modify `IQueryHandler<TQuery, TResult>` to contain only `Execute` and `Fallback`
  - Create new file `src/Paramore.Darker/IQueryHandlerAsync.cs`:
    - `IQueryHandlerAsync<TQuery, TResult> : IQueryHandler` with `ExecuteAsync` and `FallbackAsync`

### Task 2: Update base classes to remove `NotImplementedException` methods
- [x] **TIDY: Remove dead methods from handler base classes**
  - FR4, FR5, AC3
  - In `src/Paramore.Darker/QueryHandler.cs`:
    - Change to implement `IQueryHandler<TQuery, TResult>` (sync only)
    - Remove `ExecuteAsync` and `FallbackAsync` methods
  - In `src/Paramore.Darker/QueryHandlerAsync.cs`:
    - Change to implement `IQueryHandlerAsync<TQuery, TResult>` (async only)
    - Remove `Execute` and `Fallback` methods

### Task 3: Split `IQueryHandlerDecorator<TQuery, TResult>` into sync and async interfaces
- [x] **TIDY: Split decorator interface into two focused interfaces**
  - FR5a, FR6, FR7
  - In `src/Paramore.Darker/IQueryHandlerDecorator.cs`:
    - Keep `IQueryHandlerDecorator` marker interface (non-generic) unchanged
    - Modify `IQueryHandlerDecorator<TQuery, TResult>` to contain only sync `Execute`
  - Create new file `src/Paramore.Darker/IQueryHandlerDecoratorAsync.cs`:
    - `IQueryHandlerDecoratorAsync<TQuery, TResult> : IQueryHandlerDecorator` with `ExecuteAsync`

### Task 4: Create async attribute base class and split existing attributes
- [x] **TIDY: Create async attribute hierarchy**
  - FR8a, FR8b
  - Create `src/Paramore.Darker/Attributes/QueryHandlerAttributeAsync.cs`:
    - Same structure as `QueryHandlerAttribute` but for async path
  - In `src/Paramore.Darker/Attributes/FallbackPolicyAttribute.cs`:
    - Ensure it derives from `QueryHandlerAttribute` (already does) and returns sync decorator type
  - Create `src/Paramore.Darker/Attributes/FallbackPolicyAttributeAsync.cs`:
    - Derives from `QueryHandlerAttributeAsync`, returns `FallbackPolicyDecoratorAsync<,>` type
  - Split decorator: `src/Paramore.Darker/Decorators/FallbackPolicyDecorator.cs` becomes sync-only (remove `ExecuteAsync`)
  - Create `src/Paramore.Darker/Decorators/FallbackPolicyDecoratorAsync.cs` — async-only

### Task 5: Create async registry, factory, and decorator registry/factory interfaces
- [x] **TIDY: Create async infrastructure interfaces and concrete implementations**
  - FR8d, FR9, FR10, FR11
  - Create interfaces:
    - `src/Paramore.Darker/IQueryHandlerRegistryAsync.cs` — `Register<TQuery, TResult, THandler>()` with async constraint, `Register(Type, Type, Type)`, `Type Get(Type)`
    - `src/Paramore.Darker/IQueryHandlerFactoryAsync.cs` — `Create(Type)`, `Release(IQueryHandler)`
    - `src/Paramore.Darker/IQueryHandlerDecoratorRegistryAsync.cs` — `Register(Type)`
    - `src/Paramore.Darker/IQueryHandlerDecoratorFactoryAsync.cs` — `Create<T>(Type)`, `Release<T>(T)` with shared marker constraint
  - Create concrete implementation:
    - `src/Paramore.Darker/QueryHandlerRegistryAsync.cs` — mirrors `QueryHandlerRegistry` but `Register` constrains to `IQueryHandlerAsync<,>` and `RegisterFromAssemblies` scans for `IQueryHandlerAsync<,>`

### Task 6: Update test helpers for split interfaces
- [x] **TIDY: Split test handler and update test infrastructure**
  - Split `test/Paramore.Darker.Testing.Ports/TestQueryHandler.cs`:
    - `TestQueryHandler` implements `IQueryHandler<TestQueryA, Guid>` (sync only — remove `ExecuteAsync`, `FallbackAsync`)
    - Create `TestQueryHandlerAsync` implementing `IQueryHandlerAsync<TestQueryA, Guid>` (async only)
  - Update any test queries if needed
  - **Build verification**: `dotnet build Darker.Filter.slnf` must compile after this task
  - **Existing test file updates** (tracked by later tasks):
    - `QueryProcessorTests.cs` → updated in Task 7
    - `QueryProcessorAsyncTests.cs` → updated in Task 8
    - `PipelineBuilderExceptionTests.cs` → updated in Tasks 9/10, cleaned up in Task 12
    - `Integrations/AspNetTests.cs` → renamed and updated in Task 13 (see note)
    - `FakeQueryProcessorTests.cs` → updated in Task 11
    - `Decorators/FallbackPolicyTests.cs` → updated in Task 7

### Task 6a: Add Simple and InMemory factory/registry implementations
- [x] **TIDY: Create public lightweight factory and registry implementations following Brighter's patterns**
  - ADR: [0009-simple-and-inmemory-factory-implementations](../../docs/adr/0009-simple-and-inmemory-factory-implementations.md)
  - In `src/Paramore.Darker/SimpleHandlerFactory.cs`:
    - Public class implementing `IQueryHandlerFactory` and `IQueryHandlerFactoryAsync`
    - Primary constructor taking `Func<Type, IQueryHandler>` delegate
    - `Create` delegates to the func; `Release` disposes if `IDisposable`
  - In `src/Paramore.Darker/SimpleHandlerDecoratorFactory.cs`:
    - Public class implementing `IQueryHandlerDecoratorFactory` and `IQueryHandlerDecoratorFactoryAsync`
    - Primary constructor taking `Func<Type, IQueryHandlerDecorator>` delegate
    - `Create<T>` delegates to the func and casts; `Release<T>` disposes if `IDisposable`
  - In `src/Paramore.Darker/InMemoryDecoratorRegistry.cs`:
    - Public class implementing `IQueryHandlerDecoratorRegistry` and `IQueryHandlerDecoratorRegistryAsync`
    - Stores registered types in a list; exposes `IReadOnlyList<Type> RegisteredTypes`
  - **Build verification**: `dotnet build Darker.Filter.slnf` must compile, all existing tests must pass
  - **Note**: Existing tests are NOT updated here — each behavioral task (7+) will use the new types when writing new tests

---

## Behavioral Tasks (TDD)

### Task 7: Sync pipeline resolves from sync registry and builds sync decorator chain
- [x] **TEST + IMPLEMENT: Sync pipeline builds correctly with split interfaces**
  - **USE COMMAND**: `/test-first when sync query is executed the sync pipeline resolves handler from sync registry and builds sync decorator chain`
  - FR12, AC7
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `SyncPipelineTests.cs`
  - Test should verify:
    - `PipelineBuilder.Build()` resolves handler from `IQueryHandlerRegistry` via `IQueryHandlerFactory`
    - Sync handler's `Execute` is invoked and returns correct result
    - Handler is released after execution
    - Query context is set on handler
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Update `PipelineBuilder` constructor to accept sync infrastructure (registry, factory, decorator factory)
    - Update `Build()` to use sync registry and factory
    - Update `GetDecorators` for sync path to read `QueryHandlerAttribute` and resolve sync decorators
    - Update `ResolveHandler` to use sync registry/factory
    - Update existing `QueryProcessorTests.cs` to use split interfaces
    - Update `Decorators/FallbackPolicyTests.cs` to use sync-only decorator interface

### Task 8: Async pipeline resolves from async registry and builds async decorator chain
- [x] **TEST + IMPLEMENT: Async pipeline builds correctly with split interfaces**
  - **USE COMMAND**: `/test-first when async query is executed the async pipeline resolves handler from async registry and builds async decorator chain`
  - FR13, AC7
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `AsyncPipelineTests.cs`
  - Test should verify:
    - `PipelineBuilder.BuildAsync()` resolves handler from `IQueryHandlerRegistryAsync` via `IQueryHandlerFactoryAsync`
    - Async handler's `ExecuteAsync` is invoked and returns correct result
    - Handler is released after execution
    - Query context is set on handler
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Update `PipelineBuilder` constructor to also accept async infrastructure
    - Update `BuildAsync()` to use async registry and factory
    - Add `GetDecoratorsAsync` that reads `QueryHandlerAttributeAsync` and resolves async decorators
    - Update existing `QueryProcessorAsyncTests.cs` to use split interfaces

### Task 9: ConfigurationException thrown when handler not found for execution path
- [x] **TEST + IMPLEMENT: ConfigurationException thrown when no handler registered for query execution path**
  - **USE COMMAND**: `/test-first when Execute is called for a query with only an async handler registered then ConfigurationException is thrown with helpful message`
  - FR15, FR16, AC8
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `HandlerNotFoundTests.cs`
  - Test should verify:
    - `Execute` with no sync handler throws `ConfigurationException` mentioning "use ExecuteAsync instead"
    - `ExecuteAsync` with no async handler throws `ConfigurationException` mentioning "use Execute instead"
    - Exception message includes the query type name
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Update `PipelineBuilder.ResolveHandler` (sync path) to throw `ConfigurationException` instead of `MissingHandlerException`
    - Add `ResolveHandlerAsync` method for async path with corresponding `ConfigurationException`
    - Include helpful guidance in error messages
    - Update `PipelineBuilderExceptionTests.cs`: migrate handler-not-found tests to use `ConfigurationException`

### Task 10: ConfigurationException thrown when decorator attribute mismatches execution path
- [ ] **TEST + IMPLEMENT: ConfigurationException thrown when decorator attribute does not match handler execution path**
  - **USE COMMAND**: `/test-first when a sync attribute is placed on an async handler method then ConfigurationException is thrown at pipeline build time`
  - FR8c, FR16a, AC5b
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `AttributeMismatchTests.cs`
  - Test should verify:
    - Sync attribute (`QueryHandlerAttribute`) on async handler's `ExecuteAsync` → `ConfigurationException`
    - Async attribute (`QueryHandlerAttributeAsync`) on sync handler's `Execute` → `ConfigurationException`
    - Exception message indicates the mismatch
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - In `PipelineBuilder.Build()`, after resolving decorators, validate each implements `IQueryHandlerDecorator<,>`
    - In `PipelineBuilder.BuildAsync()`, validate each implements `IQueryHandlerDecoratorAsync<,>`
    - Throw `ConfigurationException` with clear mismatch message
    - Update `PipelineBuilderExceptionTests.cs`: migrate decorator exception tests to use split interfaces

### Task 11: HandlerConfiguration and QueryProcessor use dual-path infrastructure
- [ ] **TEST + IMPLEMENT: QueryProcessor dispatches to correct pipeline based on Execute vs ExecuteAsync**
  - **USE COMMAND**: `/test-first when QueryProcessor has both sync and async handlers registered it dispatches each query to the correct pipeline`
  - FR14, AC7
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `DualPathQueryProcessorTests.cs`
  - Test should verify:
    - `QueryProcessor.Execute` uses sync registry/factory/decorator infrastructure
    - `QueryProcessor.ExecuteAsync` uses async registry/factory/decorator infrastructure
    - Both paths work independently in the same processor instance
    - Handler configuration holds both sync and async infrastructure sets
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Extend `IHandlerConfiguration` with async properties (FR14)
    - Extend `HandlerConfiguration` constructor to accept async parameters
    - Update `QueryProcessor` constructor to extract both sync and async infrastructure
    - `QueryProcessor.Execute` passes sync infrastructure to `PipelineBuilder`
    - `QueryProcessor.ExecuteAsync` passes async infrastructure to `PipelineBuilder`
    - Update `FakeQueryProcessorTests.cs` to use split interfaces

### Task 12: Retire `MissingHandlerException` and `MissingHandlerDecoratorException`
- [ ] **CLEANUP: Remove retired exception types and migrate remaining call sites**
  - Depends on: Tasks 9, 10, 12a (all usages replaced with `ConfigurationException`)
  - Known reference sites to verify are migrated:
    - `src/Paramore.Darker/PipelineBuilder.cs` — `MissingHandlerException` (ResolveHandler), `MissingHandlerDecoratorException` (GetDecorators)
    - `src/Paramore.Darker/Exceptions/MissingHandlerException.cs` — delete
    - `src/Paramore.Darker/Exceptions/MissingHandlerDecoratorException.cs` — delete
    - `test/Paramore.Darker.Tests/PipelineBuilderExceptionTests.cs` — delete or verify fully replaced by HandlerNotFoundTests/AttributeMismatchTests
  - Verification: `dotnet build Darker.Filter.slnf -c Release` compiles, `dotnet test Darker.Filter.slnf -c Release --no-build` passes

### Task 12a: Decorator-not-found migrates from `MissingHandlerDecoratorException` to `ConfigurationException`
- [ ] **TEST + IMPLEMENT: ConfigurationException thrown when decorator factory cannot create a decorator**
  - **USE COMMAND**: `/test-first when decorator factory cannot create a decorator for a given type then ConfigurationException is thrown`
  - FR16b
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `DecoratorNotFoundTests.cs`
  - Test should verify:
    - When decorator factory returns null for a requested type, `ConfigurationException` is thrown
    - Exception message includes the decorator type name
    - Covers both sync and async decorator factory paths
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - In `PipelineBuilder.GetDecorators` (sync): replace `MissingHandlerDecoratorException` with `ConfigurationException`
    - In `PipelineBuilder.GetDecoratorsAsync` (async): use `ConfigurationException` for null decorator
    - Error message: "Decorator could not be created for type: {type}. Ensure it is registered in the decorator registry."

### Task 13: DI registration wires both sync and async paths
- [ ] **TEST + IMPLEMENT: AddDarker and AddHandlersFromAssemblies wire both sync and async registries**
  - **USE COMMAND**: `/test-first when AddHandlersFromAssemblies scans an assembly it registers sync handlers in the sync registry and async handlers in the async registry`
  - FR17, AC6
  - Test location: `test/Paramore.Darker.Tests/Integrations`
  - Test file: `DependencyInjectionTests.cs`
  - Test should verify:
    - Assembly scanning finds types implementing `IQueryHandler<,>` → sync registry
    - Assembly scanning finds types implementing `IQueryHandlerAsync<,>` → async registry
    - `AddDarker()` wires both sync and async registries, factories, decorator registries, and decorator factories
    - A handler implementing only `IQueryHandlerAsync<,>` is not in the sync registry and vice versa
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Create `IQueryHandlerRegistryAsync` implementation (`ServiceCollectionHandlerRegistryAsync`)
    - Create `IQueryHandlerFactoryAsync` implementation (`ServiceProviderHandlerFactoryAsync`)
    - Create `IQueryHandlerDecoratorRegistryAsync` implementation (`ServiceCollectionDecoratorRegistryAsync`)
    - Create `IQueryHandlerDecoratorFactoryAsync` implementation (`ServiceProviderHandlerDecoratorFactoryAsync`)
    - Update `ServiceCollectionExtensions.AddDarker()` to wire both paths
    - Update `ServiceCollectionDarkerHandlerBuilder` for dual-path scanning
    - Update `IDarkerHandlerBuilder` with async handler registration
    - Rename `Integrations/AspNetTests.cs` → `Integrations/QueryProcessorIntegrationTests.cs` and update for split interfaces (AspNet is a misnomer — there is no ASP.NET dependency in this solution)

### Task 14: Split Policies package decorators and attributes into sync and async variants
- [ ] **TEST + IMPLEMENT: RetryableQuery decorator works in both sync and async pipelines**
  - **USE COMMAND**: `/test-first when a retryable query attribute is used on sync and async handlers the correct decorator variant is applied`
  - FR8, FR8b, AC5, AC5a
  - Test location: `test/Paramore.Darker.Tests/Decorators`
  - Test file: `RetryableQueryDecoratorTests.cs`
  - Test should verify:
    - `RetryableQueryAttribute` (sync) returns `RetryableQueryDecorator<,>` from `GetDecoratorType()`
    - `RetryableQueryAttributeAsync` (async) returns `RetryableQueryDecoratorAsync<,>` from `GetDecoratorType()`
    - Sync decorator's `Execute` wraps the pipeline correctly
    - Async decorator's `ExecuteAsync` wraps the pipeline correctly
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Split `RetryableQueryDecorator` into sync-only (remove `ExecuteAsync`)
    - Create `RetryableQueryDecoratorAsync` — async-only
    - Create `RetryableQueryAttributeAsync` deriving from `QueryHandlerAttributeAsync`
    - Update `QueryProcessorBuilderExtensions.AddDefaultPolicies()` to register both sync and async decorator types

### Task 15: Split QueryLogging package decorators and attributes into sync and async variants
- [ ] **TEST + IMPLEMENT: QueryLogging decorator works in both sync and async pipelines**
  - **USE COMMAND**: `/test-first when a query logging attribute is used on sync and async handlers the correct logging decorator variant is applied`
  - FR8, FR8b, AC5, AC5a
  - Test location: `test/Paramore.Darker.Tests/Decorators`
  - Test file: `QueryLoggingDecoratorTests.cs`
  - Test should verify:
    - `QueryLoggingAttribute` (sync) returns `QueryLoggingDecorator<,>` from `GetDecoratorType()`
    - `QueryLoggingAttributeAsync` (async) returns `QueryLoggingDecoratorAsync<,>` from `GetDecoratorType()`
    - Sync decorator logs request/response for sync pipeline
    - Async decorator logs request/response for async pipeline
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Split `QueryLoggingDecorator` into sync-only (remove `ExecuteAsync`)
    - Create `QueryLoggingDecoratorAsync` — async-only
    - Create `QueryLoggingAttributeAsync` deriving from `QueryHandlerAttributeAsync`
    - Update `QueryProcessorBuilderExtensions.AddJsonQueryLogging()` to register both sync and async decorator types

### Task 16: Update Builder for dual-path configuration
- [ ] **TEST + IMPLEMENT: QueryProcessorBuilder creates processor with both sync and async infrastructure**
  - **USE COMMAND**: `/test-first when QueryProcessorBuilder builds a processor it configures both sync and async handler infrastructure`
  - FR18, AC11
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `QueryProcessorBuilderTests.cs`
  - Test should verify:
    - Builder's `Handlers()` accepts both sync and async registry/factory configuration
    - Built `QueryProcessor` can execute both sync and async queries
    - `RegisterDefaultDecorators()` registers both sync and async fallback decorator variants
    - `IQueryProcessorExtensionBuilder.RegisterDecorator` supports both sync and async registration
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Update `INeedHandlers` / `IBuildTheQueryProcessor` interfaces for dual-path
    - Update `QueryProcessorBuilder` to build `HandlerConfiguration` with both sync and async infrastructure
    - Update `FactoryFuncWrapper` / `RegistryActionWrapper` for async counterparts
    - Update `IQueryProcessorExtensionBuilder` with async decorator registration
    - Update `RegisterDefaultDecorators()` to register `FallbackPolicyDecoratorAsync<,>`

### Task 17: Sample application builds and runs with split interfaces
- [ ] **TEST + IMPLEMENT: SampleMinimalApi builds and runs correctly with split interfaces**
  - **USE COMMAND**: `/test-first when sample minimal API is configured with AddDarker it resolves and executes async query handlers correctly`
  - AC10
  - Test location: `test/Paramore.Darker.Tests/Integrations`
  - Test file: `SampleAppIntegrationTests.cs`
  - Test should verify:
    - Sample app's handler registration works with async registry
    - Sample app's query execution returns expected results
    - No build errors in sample project
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Update `samples/SampleMinimalApi/` handler to use `IQueryHandlerAsync<,>` / `QueryHandlerAsync<,>`
    - Update any attribute usage to async variants
    - Verify the app builds and runs (`dotnet build`, `dotnet run`)

---

## Verification

After all tasks complete:
- [ ] `dotnet build Darker.Filter.slnf -c Release` — clean build
- [ ] `dotnet test Darker.Filter.slnf -c Release --no-build` — all tests pass
- [ ] `dotnet run --project samples/SampleMinimalApi/SampleMinimalApi.csproj` — sample runs
- [ ] No `NotImplementedException` in handler base classes (AC3)
- [ ] No references to `MissingHandlerException` or `MissingHandlerDecoratorException`

## FR-to-Task Coverage

| FR | Task(s) |
|----|---------|
| FR1 (sync handler interface) | Task 1 |
| FR2 (async handler interface) | Task 1 |
| FR3 (shared marker) | Task 1 |
| FR4 (QueryHandler sync-only) | Task 2 |
| FR5 (QueryHandlerAsync async-only) | Task 2 |
| FR5a (decorator marker shared) | Task 3 |
| FR6 (sync decorator interface) | Task 3 |
| FR7 (async decorator interface) | Task 3 |
| FR8 (decorator variants) | Task 4, 14, 15 |
| FR8a (attribute base classes) | Task 4 |
| FR8b (attribute variants) | Task 4, 14, 15 |
| FR8c (attribute mismatch error) | Task 10 |
| FR8d (decorator registries) | Task 5 |
| FR9 (handler registries) | Task 5 |
| FR10 (handler factories) | Task 5 |
| FR11 (decorator factories) | Task 5 |
| FR12 (sync pipeline) | Task 7 |
| FR13 (async pipeline) | Task 8 |
| FR14 (handler configuration) | Task 11 |
| FR15 (sync handler not found) | Task 9 |
| FR16 (async handler not found) | Task 9 |
| FR16a (attribute mismatch) | Task 10 |
| FR16b (decorator not found) | Task 12a |
| FR17 (DI scanning) | Task 13 |
| FR18 (Builder dual-path) | Task 16 |

## AC-to-Task Coverage

| AC | Task(s) |
|----|---------|
| AC1 (sync interface members) | Task 1, 2 |
| AC2 (async interface members) | Task 1, 2 |
| AC3 (no NotImplementedException) | Task 2 |
| AC4 (decorator interfaces) | Task 3 |
| AC5 (decorator variants) | Task 4, 14, 15 |
| AC5a (attribute variants) | Task 4, 14, 15 |
| AC5b (attribute mismatch throws) | Task 10 |
| AC6 (separate registries + DI scan) | Task 5, 13 |
| AC7 (pipeline per path) | Task 7, 8, 11 |
| AC8 (handler not found error) | Task 9 |
| AC9 (test suite passes) | Verification |
| AC10 (sample app runs) | Task 17 |
| AC11 (Builder dual-path) | Task 16 |

## Existing Test File Migration Map

| Existing Test File | Updated In Task(s) |
|--------------------|---------------------|
| `QueryProcessorTests.cs` | Task 7 |
| `QueryProcessorAsyncTests.cs` | Task 8 |
| `PipelineBuilderExceptionTests.cs` | Tasks 9, 10 (migrate tests); Task 12 (delete if fully replaced) |
| `Integrations/AspNetTests.cs` | Task 13 (rename to `QueryProcessorIntegrationTests.cs` + update) |
| `FakeQueryProcessorTests.cs` | Task 11 |
| `Decorators/FallbackPolicyTests.cs` | Task 7 (update to use sync-only decorator interface; split into sync/async test cases) |
| `QueryHandlerRegistryTests.cs` | Task 5 (registry `Register` generic constraint changes from `IQueryHandler<,>` to path-specific; tests must be updated) |
