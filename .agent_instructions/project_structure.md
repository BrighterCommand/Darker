# Project Structure

- `src/Paramore.Darker/` - Core framework (QueryProcessor, PipelineBuilder, registries)
- `src/Paramore.Darker.AspNetCore/` - ASP.NET Core integration with DI extensions
- `src/Paramore.Darker.Policies/` - Polly-based retry and circuit breaker decorators
- `src/Paramore.Darker.QueryLogging/` - Request/response logging decorator
- `src/Paramore.Darker.Testing/` - Testing utilities
- `test/` - Test suites organized by component

## Testing Framework

- **Test Framework**: xUnit with Moq for mocking and Shouldly for assertions
- **Test Patterns**: Behavior-driven test naming
- **Test Doubles**: `Paramore.Darker.Testing.Ports` provides test queries and handlers

## Package Management

- Uses Central Package Management via `Directory.Packages.props`
- Multi-targeting: .NET 8.0 and .NET 9.0
- Global tools: MinVer for versioning, SourceLink for debugging
- Solution filter: `Darker.Filter.slnf` excludes MAUI test app
