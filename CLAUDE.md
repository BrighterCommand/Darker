# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Darker is the query-side counterpart of [Brighter](https://github.com/BrighterCommand/Paramore.Brighter). It implements the Query pattern (CQRS read-side) using a pipeline architecture with decorator-based cross-cutting concerns.

**Key Concepts:**
- **IQuery<TResult>**: Query objects that return results
- **IQueryHandler<TQuery, TResult>**: Handlers that execute queries
- **IQueryProcessor**: Central processor that dispatches queries to handlers through a pipeline
- **Pipeline Architecture**: Queries flow through a chain of decorators (logging, retry, fallback policies) before reaching the handler
- **Decorator Attributes**: `[QueryLogging]`, `[RetryableQuery]`, `[FallbackPolicy]` with step ordering to control pipeline execution

## Architecture

### Core Flow
1. Client injects `IQueryProcessor` and calls `Execute()` or `ExecuteAsync()`
2. `QueryProcessor` creates a `PipelineBuilder` for the query
3. `PipelineBuilder` resolves the handler from `IQueryHandlerRegistry`
4. Decorators are resolved from attributes on the handler's Execute method (ordered by step number)
5. Pipeline is built: decorators wrap the handler execution in reverse order
6. Query flows through the pipeline: decorators → handler → result

### Key Components
- **src/Paramore.Darker**: Core library with QueryProcessor, PipelineBuilder, registries
- **src/Paramore.Darker.AspNetCore**: ASP.NET Core integration with DI extensions
- **src/Paramore.Darker.Policies**: Polly-based retry and circuit breaker decorators
- **src/Paramore.Darker.QueryLogging**: Request/response logging decorator
- **src/Paramore.Darker.SimpleInjector**: SimpleInjector DI integration
- **src/Paramore.Darker.LightInject**: LightInject DI integration
- **src/Paramore.Darker.Testing**: Testing utilities

### Handler Lifecycle
- Handlers are created per-query via `IQueryHandlerFactory`
- Decorators are created per-query via `IQueryHandlerDecoratorFactory`
- Both are released after pipeline execution in `PipelineBuilder.Dispose()`
- With ASP.NET Core + EF Core: QueryProcessor must be Scoped (matches DbContext lifetime)

## Development Commands

### Build
```bash
# Build the solution using the filter (excludes MAUI test app)
dotnet build Darker.Filter.slnf -c Release

# Build full solution (includes MAUI)
dotnet build Darker.sln -c Release
```

### Test
```bash
# Run all tests
dotnet test Darker.Filter.slnf -c Release --no-build

# Run tests for a specific project
dotnet test test/Paramore.Darker.Tests/Paramore.Darker.Tests.csproj

# Run a single test
dotnet test test/Paramore.Darker.Tests/Paramore.Darker.Tests.csproj --filter "FullyQualifiedName~QueryProcessorTests.ExecutesQueries"
```

### Package Management
The project uses Central Package Management (CPM) via `Directory.Packages.props`:
- All package versions are centrally managed
- Individual projects reference packages without version attributes
- MinVer is used for automatic semantic versioning from git tags

### Running Sample
```bash
# Run the minimal API sample
dotnet run --project samples/SampleMinimalApi/SampleMinimalApi.csproj

# Endpoints:
# GET http://localhost:5000/people - returns all people
# GET http://localhost:5000/people/{id} - returns person name by id
```

## Important Patterns

### Handler Implementation
Handlers can inherit from:
- `QueryHandler<TQuery, TResult>` - synchronous only
- `QueryHandlerAsync<TQuery, TResult>` - async only (most common)
- `IQueryHandler<TQuery, TResult>` - full control (implement both sync/async)

### Decorator Ordering
Decorator attributes specify step numbers (higher executes first):
```csharp
[QueryLogging(1)]              // Executes third (logs outer timing)
[FallbackPolicy(2)]            // Executes second (handles failures)
[RetryableQuery(3)]            // Executes first (retries on transient failures)
public override async Task<TResult> ExecuteAsync(...)
```

### ASP.NET Core Registration
```csharp
services.AddDarker(options => {
    options.QueryProcessorLifetime = ServiceLifetime.Scoped; // Required for EF Core
})
.AddHandlersFromAssemblies(typeof(MyHandler).Assembly)  // Scans for handlers
.AddJsonQueryLogging()                                   // Adds logging decorator
.AddDefaultPolicies();                                   // Adds retry/fallback policies
```

### Exception Handling
- `TargetInvocationException` is unwrapped via `ExceptionDispatchInfo.Capture().Throw()`
- This preserves the original stack trace when reflection is used to invoke handlers
- See `QueryProcessor.cs:50-54` and `PipelineBuilder.cs:57-61`

## Testing

Test projects use:
- **xunit** for test framework
- **Moq** for mocking
- **Shouldly** for assertions
- **Paramore.Darker.Testing.Ports**: Test doubles and test queries/handlers

When testing QueryProcessor:
1. Create `QueryHandlerRegistry` and register query → handler mappings
2. Mock `IQueryHandlerFactory` to return handler instances
3. Mock decorator factory/registry if not testing decorators
4. Create `QueryProcessor` with `HandlerConfiguration` and `InMemoryQueryContextFactory`

## Build & CI

- GitHub Actions workflow: `.github/workflows/dotnet-core.yml`
- Builds with .NET 8.0 and 9.0
- Uses `Darker.Filter.slnf` solution filter (excludes MAUI test app)
- Packages are pushed to GitHub Packages on push/merge
- NuGet.org releases happen on git tags matching `*.*.*`
- Cache key: `Linux-nuget-${{ hashFiles('**/Directory.Packages.props') }}`
