# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: #296

## Problem Statement

As a developer using Darker in a console app, worker service, or any non-ASP.NET Core host, I would like the DI integration package to have a name that accurately reflects its scope, so that I'm not misled into thinking ASP.NET Core is required to use Darker with Microsoft's DI container.

The current package `Paramore.Darker.AspNetCore` only depends on `Microsoft.Extensions.DependencyInjection` — it has no ASP.NET Core dependencies whatsoever. The name incorrectly suggests a hard coupling to ASP.NET Core, which discourages adoption in non-web scenarios (console apps, background workers, MAUI apps, etc.).

## Proposed Solution

Rename the package from `Paramore.Darker.AspNetCore` to `Paramore.Darker.Extensions.DependencyInjection` to align with the actual dependency and follow the Microsoft naming convention (similar to how `Microsoft.Extensions.DependencyInjection` itself is named).

The public API surface (`AddDarker()` extension method on `IServiceCollection`, `IDarkerHandlerBuilder`, `DarkerOptions`) remains unchanged — only the package name, project name, and namespace change.

## Requirements

### Functional Requirements
- FR1: Rename the project directory from `Paramore.Darker.AspNetCore` to `Paramore.Darker.Extensions.DependencyInjection`
- FR2: Rename the `.csproj` file accordingly
- FR3: Change the root namespace from `Paramore.Darker.AspNetCore` to `Paramore.Darker.Extensions.DependencyInjection`
- FR4: Update all `using Paramore.Darker.AspNetCore` statements across the solution to use the new namespace
- FR5: Update all project references (`.csproj` files) that reference the old project path
- FR6: Update solution files (`.slnx`, `.slnf`) to reference the new project path
- FR7: The `AddDarker()` extension method on `IServiceCollection` must continue to work identically
- FR8: All existing tests must pass after the rename

### Non-functional Requirements
- NFR1: The NuGet package ID must become `Paramore.Darker.Extensions.DependencyInjection`
- NFR2: Consider publishing a final version of `Paramore.Darker.AspNetCore` that depends on the new package as a transition aid for existing users (implementation detail for release, not this PR)
- NFR3: Update documentation (CLAUDE.md, README, samples) to reference the new package name

### Constraints and Assumptions
- This is a breaking change for consumers: they must update package references and `using` statements
- The public API (types, methods, signatures) does not change — only the namespace
- MinVer handles versioning from git tags; no manual version bumps needed
- The CI/CD pipeline (GitHub Actions) should work without changes since it builds via solution filter

### Out of Scope
- Changing the public API surface (method signatures, types, behavior)
- Creating a compatibility/forwarding shim package (NFR2 is a release concern, not a code concern)
- Renaming other DI-specific packages (SimpleInjector, LightInject) — those are correctly named for their specific containers

## Acceptance Criteria

- AC1: The solution builds successfully with `dotnet build Darker.Filter.slnf -c Release`
- AC2: All tests pass with `dotnet test Darker.Filter.slnf -c Release`
- AC3: No references to `Paramore.Darker.AspNetCore` remain in source code (excluding git history and any transition notes)
- AC4: The sample app (`SampleMinimalApi`) runs correctly with the new package
- AC5: The MAUI test app project file references the renamed project correctly

## Affected Files

Based on codebase analysis, the following files require changes:

### Source files (namespace change)
- `src/Paramore.Darker.AspNetCore/*.cs` (9 files) — all use `namespace Paramore.Darker.AspNetCore`

### Project references
- `test/Paramore.Darker.Tests/Paramore.Darker.Tests.csproj`
- `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj`
- `samples/SampleMinimalApi/SampleMinimalApi.csproj`
- `SampleMauiTestApp/SampleMauiTestApp.csproj`

### Using statements
- `test/Paramore.Darker.Tests/Integrations/AspNetTests.cs`
- `test/Paramore.Darker.Tests.AOT/Base/AOTTestClassBase.cs`
- `samples/SampleMinimalApi/Program.cs`
- `SampleMauiTestApp/MauiProgram.cs`

### Solution files
- `Darker.slnx`
- `Darker.Filter.slnf`

### Documentation
- `CLAUDE.md`
- Any README or docs referencing the old package name

## Additional Context

This aligns with the [V5 discussion #273](https://github.com/BrighterCommand/Darker/discussions/273) and follows the precedent set by Microsoft's own naming conventions for DI extension packages.
