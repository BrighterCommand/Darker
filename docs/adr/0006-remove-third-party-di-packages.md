# 6. Remove Third-Party DI Container Integration Packages

Date: 2026-05-15

## Status

Accepted

## Context

**Parent Requirement**: [specs/001-remove_di_packages/requirements.md](../../specs/001-remove_di_packages/requirements.md)

**Scope**: This ADR addresses whether to continue maintaining `Paramore.Darker.SimpleInjector` and `Paramore.Darker.LightInject`, and the approach for their removal.

### The Problem

Darker currently ships three DI container integration packages:

1. `Paramore.Darker.AspNetCore` — Microsoft.Extensions.DependencyInjection
2. `Paramore.Darker.SimpleInjector` — SimpleInjector
3. `Paramore.Darker.LightInject` — LightInject

Each integration package implements the same roles defined in [ADR 0004](0004-factory-registry-abstractions.md):
- **Service provider** role: `IQueryHandlerFactory` + `IQueryHandlerDecoratorFactory`
- **Information holder** role: `IQueryHandlerRegistry` (via `QueryHandlerRegistry` base) + `IQueryHandlerDecoratorRegistry`
- **Interfacer** role: Builder extension methods for fluent configuration

The SimpleInjector and LightInject packages are thin adapters — each is ~60 lines of code — that delegate `Create()` calls to their respective container's `GetInstance()` method.

### Forces

1. **Maintenance burden**: Each package requires its own NuGet dependency updates, compatibility testing, and CI configuration. Two packages with near-identical code doubles this cost.

2. **MS DI as the standard**: Since .NET Core 1.0, `Microsoft.Extensions.DependencyInjection` has become the standard DI abstraction. Both SimpleInjector and LightInject now provide their own MS DI adapters (`SimpleInjector.Integration.ServiceCollection` and `LightInject.Microsoft.DependencyInjection`), meaning users can use these containers through the MS DI abstraction without Darker-specific packages.

3. **Declining usage**: The .NET ecosystem has converged on MS DI. Maintaining separate packages for containers that already integrate with MS DI provides diminishing value.

4. **Architectural soundness**: ADR 0004's factory/registry abstractions remain correct — the core should not depend on any DI container. The question is whether Darker needs to ship container-specific adapter packages when the containers themselves provide MS DI adapters.

5. **V5 milestone**: This removal is part of the broader V5 initiative ([Discussion #273](https://github.com/BrighterCommand/Darker/discussions/273)) that simplifies the library's surface area.

## Decision

Remove `Paramore.Darker.SimpleInjector` and `Paramore.Darker.LightInject` from the solution and codebase. Retain `Paramore.Darker.AspNetCore` as the sole DI integration package, using MS DI as the standard integration point.

### What Changes

1. **Delete source projects**:
   - `src/Paramore.Darker.SimpleInjector/` (4 files: factory, registry, settings, builder extensions)
   - `src/Paramore.Darker.LightInject/` (4 files: factory, registry, settings, builder extensions)

2. **Update solution files**:
   - Remove project references from `Darker.slnx`
   - Remove project references from `Darker.Filter.slnf`

3. **Update documentation**:
   - Remove references from `README.md`, `CLAUDE.md`, and `.agent_instructions/` files
   - Add migration guidance noting that users should use `Paramore.Darker.Extensions.DependencyInjection` (see #296) with their container's MS DI adapter

4. **Remove NuGet dependencies** from `Directory.Packages.props`:
   - `SimpleInjector`
   - `LightInject`

### What Does Not Change

- The core abstractions (`IQueryHandlerFactory`, `IQueryHandlerRegistry`, `IQueryHandlerDecoratorFactory`, `IQueryHandlerDecoratorRegistry`) remain unchanged — they are part of the core `Paramore.Darker` package and are not affected
- `Paramore.Darker.AspNetCore` is retained
- The `QueryProcessorBuilder` fluent API is retained
- Users can still implement the factory/registry interfaces directly for custom DI containers if needed (the DI-friendly framework pattern from ADR 0004 is preserved)

### Migration Path for Users

Users of the removed packages have two migration options:

1. **Use MS DI directly**: Switch to `Paramore.Darker.Extensions.DependencyInjection` (#296) and use `services.AddDarker()`. If they still want SimpleInjector or LightInject as the underlying container, use the container's own MS DI adapter.

2. **Implement the abstractions**: Since the core factory/registry interfaces remain, advanced users can implement `IQueryHandlerFactory` and `IQueryHandlerDecoratorFactory` directly for their container — the same pattern the removed packages used.

## Consequences

### Positive

- **Reduced maintenance**: Two fewer packages to version, test, and publish
- **Simpler solution**: Fewer projects in the solution, fewer NuGet dependencies
- **Clearer guidance**: One recommended way to integrate DI (MS DI), following the principle "there should be one -- and preferably only one -- obvious way to do it"
- **No loss of extensibility**: The core abstractions remain, so users can still integrate any container via the factory/registry pattern

### Negative

- **Breaking change**: Existing users of `Paramore.Darker.SimpleInjector` or `Paramore.Darker.LightInject` must migrate
- **Reduced discoverability**: Users of these containers won't find a ready-made Darker package on NuGet — they must understand the MS DI adapter approach

### Risks and Mitigations

- **Risk**: Users are unable to migrate because they depend on container-specific features exposed through the Darker integration.
  - **Mitigation**: The removed packages don't expose any container-specific features — they are pure adapters. The same functionality is achievable through MS DI adapters or direct implementation of the factory/registry interfaces.

- **Risk**: Users don't notice the breaking change until runtime.
  - **Mitigation**: The packages will no longer exist on NuGet for the V5 version range, causing a compile-time error when upgrading. Release notes and changelog will document the migration path.

## Alternatives Considered

### Alternative 1: Deprecate but Keep

Mark the packages as deprecated and stop active development, but keep them in the solution.

**Rejected because**: Deprecated packages still require CI maintenance, dependency updates for security patches, and cause confusion about which integration to use. A clean removal is simpler.

### Alternative 2: Move to Community-Maintained Packages

Transfer the packages to separate repositories or community maintainers.

**Rejected because**: The packages are ~60 lines of trivial adapter code each. The overhead of maintaining separate repositories exceeds the value. Users who need these adapters can implement the interfaces directly in less code than it takes to add a NuGet dependency.

### Alternative 3: Keep All Packages

Continue maintaining all three DI integration packages.

**Rejected because**: This maintains the status quo with its maintenance burden and contradicts the V5 simplification goals. The .NET ecosystem has moved to MS DI as the standard; Darker should follow.

## References

- Requirements: [specs/001-remove_di_packages/requirements.md](../../specs/001-remove_di_packages/requirements.md)
- Related ADRs:
  - [ADR 0004: Factory and Registry Abstractions](0004-factory-registry-abstractions.md) — Defines the abstractions that the removed packages implement; these abstractions are retained
  - [ADR 0003: Fluent Builder](0003-fluent-builder-for-query-processor.md) — Builder API is retained
- Related Issues:
  - [#295: Remove SimpleInjector and LightInject integration packages](https://github.com/BrighterCommand/Darker/issues/295)
  - [#296: Add Extensions.DependencyInjection package](https://github.com/BrighterCommand/Darker/issues/296)
  - [Discussion #273: V5 simplification](https://github.com/BrighterCommand/Darker/discussions/273)
