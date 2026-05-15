# 7. Rename DI Integration Package from AspNetCore to Extensions.DependencyInjection

Date: 2026-05-15

## Status

Accepted

## Context

**Parent Requirement**: [specs/002-rename_di_package/requirements.md](../../specs/002-rename_di_package/requirements.md)

**Scope**: This ADR addresses the naming of the DI integration package and the structural changes required to rename it.

The package `Paramore.Darker.AspNetCore` provides integration with `Microsoft.Extensions.DependencyInjection`. Despite its name, it has **no dependency on ASP.NET Core** — it only depends on the `Microsoft.Extensions.DependencyInjection` abstractions package, which is framework-agnostic.

The current name misleads developers into thinking ASP.NET Core is required to use Darker with Microsoft's DI container. This discourages adoption in non-web scenarios: console apps, background workers, MAUI apps, and other hosts that use `Microsoft.Extensions.DependencyInjection` directly.

The forces at play:

- **Correctness**: The package name should reflect its actual dependencies, not imply phantom ones
- **Discoverability**: Developers searching for "Darker DI" should find the package without being confused by "AspNetCore" in the name
- **Convention**: Microsoft's own ecosystem uses the `*.Extensions.DependencyInjection` naming convention for DI integration packages (e.g. `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Logging`)
- **Breaking change**: Renaming a NuGet package is a breaking change for existing consumers — they must update package references and `using` statements

## Decision

Rename the project and package from `Paramore.Darker.AspNetCore` to `Paramore.Darker.Extensions.DependencyInjection`.

This is a **purely structural change** — no behavioral changes are involved. The public API surface remains identical:

- `AddDarker()` extension method on `IServiceCollection`
- `IDarkerHandlerBuilder` interface
- `DarkerOptions` configuration class
- All handler/decorator factory implementations

### Affected Roles and Responsibilities

The renamed package contains types fulfilling two roles:

1. **Interfacer** — `ServiceCollectionExtensions` bridges the gap between Darker's core abstractions and `Microsoft.Extensions.DependencyInjection`. Its responsibility is *doing*: registering Darker's services with the DI container.

2. **Service Provider** — `ServiceProviderHandlerFactory` and `ServiceProviderHandlerDecoratorFactory` implement `IQueryHandlerFactory` and `IQueryHandlerDecoratorFactory` respectively. Their responsibility is *doing*: resolving handler and decorator instances from `IServiceProvider`.

3. **Information Holder / Structurer** — `ServiceCollectionHandlerRegistry`, `ServiceCollectionDecoratorRegistry`, and `ServiceCollectionDarkerHandlerBuilder` hold registration state and structure the builder pattern. Their responsibilities are *knowing* (what's registered) and *deciding* (how to wire up registrations).

None of these roles change — only the namespace that contains them changes.

### Implementation Approach

This is a single structural change following Tidy First principles. The steps are:

1. **Rename project directory**: `src/Paramore.Darker.AspNetCore` → `src/Paramore.Darker.Extensions.DependencyInjection`
2. **Rename `.csproj` file** to match the new directory name
3. **Update root namespace** in the `.csproj` to `Paramore.Darker.Extensions.DependencyInjection`
4. **Update `namespace` declarations** in all `.cs` files within the project
5. **Update `using` statements** across the solution that reference the old namespace
6. **Update project references** in dependent `.csproj` files
7. **Update solution files** (`.slnx`, `.slnf`) to reference the new project path
8. **Update documentation** (`CLAUDE.md`, samples, README) to reference the new name

### Technology Choices

No new dependencies or technologies are introduced. The package continues to depend solely on:

- `Paramore.Darker` (core library)
- `Microsoft.Extensions.DependencyInjection` (the actual DI abstractions)

## Consequences

### Positive

- Package name accurately describes its scope — no false implication of ASP.NET Core dependency
- Follows the established Microsoft naming convention for DI extension packages
- Encourages adoption in non-web scenarios (console apps, workers, MAUI)
- Aligns with the V5 direction discussed in #273

### Negative

- Breaking change for existing consumers: they must update NuGet references and `using` statements
- Existing blog posts, tutorials, and Stack Overflow answers referencing `Paramore.Darker.AspNetCore` become stale

### Risks and Mitigations

- **Risk**: Existing users fail to discover the renamed package
  - **Mitigation**: Publish a final version of `Paramore.Darker.AspNetCore` that depends on the new package and marks itself as deprecated (out of scope for this PR, handled at release time per NFR2)
- **Risk**: Some file references are missed during rename
  - **Mitigation**: Acceptance criteria AC3 requires no remaining references to the old name in source code; the build and test suite (AC1, AC2) validate correctness

## Alternatives Considered

1. **Keep the current name**: Rejected because it actively misleads developers about the package's dependencies and scope.

2. **Create a new package alongside the old one**: Rejected because maintaining two packages with identical functionality adds unnecessary complexity. A clean rename with a deprecation notice on the old package is simpler.

3. **Use `Paramore.Darker.DependencyInjection`** (without `Extensions`): Rejected because it doesn't follow the `*.Extensions.*` convention established by Microsoft's ecosystem packages.

## References

- Requirements: [specs/002-rename_di_package/requirements.md](../../specs/002-rename_di_package/requirements.md)
- Related ADRs: [0006-remove-third-party-di-packages.md](0006-remove-third-party-di-packages.md)
- V5 Discussion: https://github.com/BrighterCommand/Darker/discussions/273
- Issue: https://github.com/BrighterCommand/Darker/issues/296
