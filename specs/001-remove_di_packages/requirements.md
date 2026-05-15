# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: #295

## Problem Statement

As a **library maintainer**, I would like to remove the `Paramore.Darker.SimpleInjector` and `Paramore.Darker.LightInject` integration packages, so that we reduce maintenance burden and consolidate on the standard .NET DI abstraction.

Microsoft.Extensions.DependencyInjection has become the standard DI abstraction in .NET. Most DI containers (including SimpleInjector and LightInject) now provide their own adapters for `IServiceCollection`/`IServiceProvider`. Maintaining separate Darker-specific integration packages for these containers adds maintenance cost with limited benefit, since users can achieve the same result through the MS DI abstraction layer.

This was identified as part of the [V5 discussion (#273)](https://github.com/BrighterCommand/Darker/discussions/273).

## Proposed Solution

Remove the two third-party DI integration packages from the solution entirely. Users who currently depend on these packages should migrate to `Paramore.Darker.Extensions.DependencyInjection` (see #296) and use their container's built-in MS DI adapter if they still want SimpleInjector or LightInject as the underlying container.

## Requirements

### Functional Requirements

- FR1: Remove the `Paramore.Darker.SimpleInjector` project (`src/Paramore.Darker.SimpleInjector/`) from the solution and codebase
- FR2: Remove the `Paramore.Darker.LightInject` project (`src/Paramore.Darker.LightInject/`) from the solution and codebase
- FR3: Remove any test projects associated with these packages (if they exist)
- FR4: Remove references to these packages from solution files (`Darker.slnx`, `Darker.Filter.slnf`)
- FR5: Update documentation (README.md, CLAUDE.md, agent instructions) to remove references to these packages
- FR6: Note the breaking change in release/changelog documentation directing users to migrate to `Paramore.Darker.Extensions.DependencyInjection`

### Non-functional Requirements

- NFR1: Existing tests for core Darker functionality must continue to pass
- NFR2: The build pipeline (GitHub Actions) must continue to work without modification beyond removing the deleted projects
- NFR3: No changes to the public API of any remaining packages

### Constraints and Assumptions

- **Constraint**: This is a breaking change for users of `Paramore.Darker.SimpleInjector` and `Paramore.Darker.LightInject`
- **Assumption**: Users can migrate to `Paramore.Darker.Extensions.DependencyInjection` (which is being introduced in #296) and use their container's MS DI adapter
- **Assumption**: No other packages in the solution depend on these two packages

### Out of Scope

- Creating the new `Paramore.Darker.Extensions.DependencyInjection` package (that is #296)
- Migrating existing users — migration guidance is documentation only
- Removing the `Paramore.Darker.AspNetCore` package (it uses MS DI and is retained)
- Changes to the core `Paramore.Darker` abstractions (`IQueryHandlerFactory`, `IQueryHandlerRegistry`, etc.)

## Acceptance Criteria

- AC1: The solution builds successfully without the SimpleInjector and LightInject projects
- AC2: All existing tests pass (excluding any removed tests for the deleted packages)
- AC3: The `Darker.slnx` and `Darker.Filter.slnf` files no longer reference the removed projects
- AC4: No source files for these packages remain in the repository
- AC5: Documentation no longer references these packages as available integrations
- AC6: The CI pipeline (`dotnet-core.yml`) builds and tests cleanly

## Additional Context

The two packages being removed each contain:
- A custom `IQueryHandlerFactory` implementation
- A custom `IQueryHandlerRegistry` implementation
- Extension methods for the `QueryProcessorBuilder`
- A `HandlerSettings` configuration class

These are container-specific implementations of the same abstractions. With MS DI as the standard, only one implementation is needed.
