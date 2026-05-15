# Tasks: Rename DI Package

**Spec**: 002-rename_di_package
**ADR**: [0007-rename-di-package.md](../../docs/adr/0007-rename-di-package.md)

## Overview

This is a **purely structural change** — renaming `Paramore.Darker.AspNetCore` to `Paramore.Darker.Extensions.DependencyInjection`. No behavioral changes are involved. The existing test suite validates that the rename preserves all existing behavior.

## Pre-flight

- [x] **Create feature branch** from master

## Tasks

### Task 1: Rename project directory and file

- [x] **STRUCTURAL: Rename the project from AspNetCore to Extensions.DependencyInjection**
  - Rename directory: `src/Paramore.Darker.AspNetCore` → `src/Paramore.Darker.Extensions.DependencyInjection`
  - Rename csproj: `Paramore.Darker.AspNetCore.csproj` → `Paramore.Darker.Extensions.DependencyInjection.csproj`
  - Update namespace declarations in all 9 `.cs` files in the project:
    - `DarkerContextBag.cs`
    - `DarkerOptions.cs`
    - `IDarkerHandlerBuilder.cs`
    - `ServiceCollectionDarkerHandlerBuilder.cs`
    - `ServiceCollectionDecoratorRegistry.cs`
    - `ServiceCollectionExtensions.cs`
    - `ServiceCollectionHandlerRegistry.cs`
    - `ServiceProviderHandlerDecoratorFactory.cs`
    - `ServiceProviderHandlerFactory.cs`
  - Verify: `dotnet build src/Paramore.Darker.Extensions.DependencyInjection/Paramore.Darker.Extensions.DependencyInjection.csproj`

### Task 2: Update solution files

- [x] **STRUCTURAL: Update solution files to reference the renamed project**
  - Update `Darker.slnx`: change project path from `src\Paramore.Darker.AspNetCore\Paramore.Darker.AspNetCore.csproj` to `src\Paramore.Darker.Extensions.DependencyInjection\Paramore.Darker.Extensions.DependencyInjection.csproj`
  - Update `Darker.Filter.slnf`: change project path accordingly
  - Verify: `dotnet build Darker.Filter.slnf -c Release`

### Task 3: Update consuming projects

- [x] **STRUCTURAL: Update all project references and using statements**
  - Update project references in:
    - `test/Paramore.Darker.Tests/Paramore.Darker.Tests.csproj`
    - `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj`
    - `samples/SampleMinimalApi/SampleMinimalApi.csproj`
    - `SampleMauiTestApp/SampleMauiTestApp.csproj`
  - Update `using Paramore.Darker.AspNetCore` → `using Paramore.Darker.Extensions.DependencyInjection` in:
    - `test/Paramore.Darker.Tests/Integrations/AspNetTests.cs`
    - `test/Paramore.Darker.Tests.AOT/Base/AOTTestClassBase.cs`
    - `samples/SampleMinimalApi/Program.cs`
    - `SampleMauiTestApp/MauiProgram.cs`
  - Verify: `dotnet test Darker.Filter.slnf -c Release --no-build`

### Task 4: Update documentation

- [x] **STRUCTURAL: Update documentation to reference new package name**
  - Update `README.md`: package name and NuGet link
  - Update `CLAUDE.md`: project description in Architecture section
  - Update `.agent_instructions/project_structure.md`: project listing
  - Note: Historical ADRs (0003, 0004, 0006) and completed specs (001) are NOT updated — they document decisions made at the time

### Task 5: Final validation

- [x] **VERIFY: All acceptance criteria pass**
  - AC1: `dotnet build Darker.Filter.slnf -c Release` succeeds
  - AC2: `dotnet test Darker.Filter.slnf -c Release` passes
  - AC3: No remaining references to `Paramore.Darker.AspNetCore` in source code (excluding historical ADRs, completed specs, and git history)
  - AC4: Sample app runs: `dotnet run --project samples/SampleMinimalApi/SampleMinimalApi.csproj`
  - AC5: MAUI test app csproj references the renamed project

## Notes

- This is entirely structural — no TDD tasks because no behavior changes
- Existing tests validate that the rename preserves all behavior (AC2)
- Historical documents (prior ADRs, completed specs) retain the old name as accurate records of past decisions
- Follow Tidy First: this should be a single structural commit, separate from any future behavioral changes
