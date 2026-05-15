# Build and Development Commands

## Building the Solution

```bash
# Build using the solution filter (excludes MAUI test app)
dotnet build Darker.Filter.slnf

# Build full solution (includes MAUI)
dotnet build Darker.slnx

# Build in Release mode
dotnet build Darker.Filter.slnf -c Release

# Build specific project
dotnet build src/Paramore.Darker/Paramore.Darker.csproj
```

## Running Tests

```bash
# Run all tests
dotnet test Darker.Filter.slnf -c Release --no-build

# Run tests for specific project
dotnet test test/Paramore.Darker.Tests/

# Run tests matching pattern
dotnet test test/Paramore.Darker.Tests/ --filter "FullyQualifiedName~QueryProcessorTests"

# Run a single test
dotnet test test/Paramore.Darker.Tests/Paramore.Darker.Tests.csproj --filter "FullyQualifiedName~QueryProcessorTests.ExecutesQueries"
```

## Running Samples

```bash
# Run the minimal API sample
dotnet run --project samples/SampleMinimalApi/SampleMinimalApi.csproj

# Endpoints:
# GET http://localhost:5000/people - returns all people
# GET http://localhost:5000/people/{id} - returns person name by id
```
