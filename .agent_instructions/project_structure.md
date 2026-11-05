# Project Structure

- `src/Paramore.Darker/` - Core framework (QueryProcessor, PipelineBuilder, registries), including the built-in decorators: query logging in `Logging/`, and retry/fallback policies and Polly resilience pipelines in `Policies/` (merged into core by ADR 0011)
- `src/Paramore.Darker.Extensions.DependencyInjection/` - Microsoft.Extensions.DependencyInjection integration
- `src/Paramore.Darker.Extensions.AspNetCore/` - ASP.NET Core request-scope opt-in (`AddDarkerRequestScope`)
- `src/Paramore.Darker.Testing/` - Testing utilities
- `test/` - Test suites organized by component
- `test/Paramore.Darker.SampleMinimalApi.Tests/` - xunit v3 test that starts `samples/SampleMinimalApi` in memory through `WebApplicationFactory<Program>`, with one shared host per assembly
- `test/Paramore.Darker.Tests.EndToEnd/` - Console harness that runs scenarios in a Generic Host with no ASP.NET Core and reports through its exit code; CI runs it as its own step

Both end-to-end projects use only public API and project references.

## Testing Framework

- **Test Framework**: xUnit with Moq for mocking and Shouldly for assertions
- **Test Patterns**: Behavior-driven test naming
- **Test Doubles**: each test project keeps its test-specific queries, handlers and decorators in its own `TestDoubles/` directory (e.g. `test/Paramore.Darker.Core.Tests/TestDoubles/`)

## Package Management

- Uses Central Package Management via `Directory.Packages.props`
- Multi-targeting: .NET 8.0 and .NET 9.0
- Global tools: MinVer for versioning, SourceLink for debugging
- Solution filter: `Darker.Filter.slnf` excludes MAUI test app
