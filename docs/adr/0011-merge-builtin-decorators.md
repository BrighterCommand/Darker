# 11. Merge Builtin Decorators (Policies and QueryLogging) Into Core

Date: 2026-05-26

## Status

Accepted

## Context

**Parent Requirement**: [specs/005-merge_builtin_decorators/requirements.md](../../specs/005-merge_builtin_decorators/requirements.md)

**Scope**: This ADR realigns Darker's **division of responsibilities across assemblies** with Brighter's, using Brighter as the model for what belongs in a side assembly versus what belongs in core. It covers (a) folding `Paramore.Darker.Policies` and `Paramore.Darker.QueryLogging` into core, (b) folding `Paramore.Darker.Testing.Ports` into the test project, (c) the internal-folder/namespace restructure (`Policies/{Attributes,Handlers}`, `Logging/{Attributes,Handlers}`, `QueryHandlerAttribute*` at the root, deletion of the root-level `Attributes/`/`Decorators/` folders), (d) the rename of `QueryLogging` folder/namespace to `Logging`, (e) the addition of `Newtonsoft.Json` as a direct dependency of core, (f) the relocation of `SampleMauiTestApp/` to `samples/`, (g) the `public`/`internal` visibility split on test doubles plus the formalisation of `Assembly.ExportedTypes` as load-bearing, (h) **splitting the test project into `Paramore.Darker.Core.Tests` and `Paramore.Darker.Extensions.Tests` to mirror Brighter's one-test-assembly-per-production-assembly pattern**, and (i) the inventory-driven update of every consuming `using` statement and `ProjectReference`. It does **not** change decorator behaviour, attribute semantics, type names, method signatures, the DI extension method surface, the implementation of either `QueryHandlerRegistry` class, or the `Paramore.Darker.Testing` production assembly (which stays as a Brighter-equivalent test-helpers library shipping `FakeQueryProcessor`).

### Problem

Brighter has converged on a principle for how to divide responsibilities across assemblies: **most features belong in core**, and a side assembly is justified only when it provides a concrete implementation of a *generic interface defined in core* — for example, `Paramore.Brighter.Kafka` is a concrete `IAmAMessageProducer`/`IAmAMessageConsumer`, `Paramore.Brighter.Outbox.MsSql` is a concrete inbox/outbox provider. Those side assemblies pull in transport- or store-specific dependencies that core consumers shouldn't have to take, and they slot into a generic interface in core, which keeps the dependency direction one-way.

Brighter has also converged on a layout convention inside each assembly: feature-folders (`Policies/`, `Logging/`) each with `Attributes/` and `Handlers/` sub-folders, the base `RequestHandlerAttribute.cs` at the root namespace, and `ApplicationLogging.cs` at the feature-folder root alongside the `Attributes/Handlers/` split. `Polly` and `Newtonsoft.Json` are direct dependencies of `Paramore.Brighter.csproj`. The test layout follows a *primary-subject* grouping: `Paramore.Brighter.Core.Tests`, `Paramore.Brighter.Extensions.Tests`, `Paramore.Brighter.Testing.Tests` (note Brighter's `Core.Tests` in practice `<ProjectReference>`s multiple production assemblies including the DI Extensions — the partition is by *what the test primarily exercises*, not by exclusive csproj reference).

Darker — the query-side sibling — has no message-broker concerns, no gateways, no `*Boxes`. The only thing in Darker that fits Brighter's "concrete implementation of a generic interface in core" pattern is **IoC container integration** (`Paramore.Darker.Extensions.DependencyInjection`). Applying Brighter's principle, the expected end-state for Darker is:

| Layer        | Brighter                                                                                     | Darker (target)                                                              |
|--------------|----------------------------------------------------------------------------------------------|------------------------------------------------------------------------------|
| Core         | `Paramore.Brighter` (+ `Policies/`, `Logging/`, decorators, attributes)                       | `Paramore.Darker` (+ `Policies/`, `Logging/`, decorators, attributes)        |
| IoC adapter  | `Paramore.Brighter.Extensions.DependencyInjection`                                            | `Paramore.Darker.Extensions.DependencyInjection`                             |
| Test helpers | `Paramore.Brighter.Testing` (ships `SpyCommandProcessor`)                                     | `Paramore.Darker.Testing` (ships `FakeQueryProcessor`)                       |
| Tests        | `Paramore.Brighter.Core.Tests`, `Paramore.Brighter.Extensions.Tests`, `Paramore.Brighter.Testing.Tests` | `Paramore.Darker.Core.Tests`, `Paramore.Darker.Extensions.Tests` (Testing.Tests deferred to a follow-up) |

Darker's actual layout diverges in five places. None of them have a "generic interface in core" justification; each came from incidental history rather than principle:

1. **`Paramore.Darker.Policies` is a side assembly that shouldn't be.** It pulls in `Polly` and ships `RetryableQuery*`. There is no generic-interface contract here. Every consumer of Darker pulls it via the DI extensions anyway. Belongs in core.

2. **`Paramore.Darker.QueryLogging` is a side assembly that shouldn't be.** Same story — pulls in `Newtonsoft.Json`, ships `QueryLogging*`, has no generic-interface contract.

3. **`Paramore.Darker.Testing.Ports` is a side assembly with no Brighter equivalent.** Its only purpose is to provide public test doubles to the main test project and the AOT test project for assembly-scanning tests. It exists to manage visibility, not to package functionality. Belongs as an `Exported/` sub-folder of the test project with a `public`/`internal` split on test doubles (relying on `Assembly.ExportedTypes` to ring-fence what assembly-scanning tests see).

4. **`Paramore.Darker.Tests` is one test project covering two production assemblies.** Brighter has separate `Paramore.Brighter.Core.Tests` and `Paramore.Brighter.Extensions.Tests` (Brighter's `Core.Tests` does in fact `<ProjectReference>` multiple production assemblies including the DI Extensions, so the convention is "group tests by primary subject under test", not "exclusively reference one production assembly"). Darker's single combined project doesn't signal which production-assembly contract a given test is primarily about. Split.

5. **In-core layout drift.** `src/Paramore.Darker/Attributes/` and `src/Paramore.Darker/Decorators/` are root-level folders that don't match Brighter's per-feature `Policies/{Attributes,Handlers}` and `Logging/{Attributes,Handlers}` layout. `QueryHandlerAttribute*` is in a sub-folder when Brighter's `RequestHandlerAttribute.cs` is at the root namespace. `Logging/ApplicationLogging.cs` already exists as a sibling at the parent namespace, mirroring Brighter — that one's fine and stays.

ADR 10 already made core directly depend on `Polly` (for `IPolicyRegistry<string>` on the `IQueryContext` interface) and signposted this merge as the follow-up. The `merge-decorators` branch is the place to do it.

### Forces

- **Brighter alignment.** Brighter is the more actively developed sibling and has set the convention. A user reading both source trees should see the same shape on both sides. After this ADR, Darker's `Policies/{Attributes,Handlers}/`, `Logging/{Attributes,Handlers}/`, and root-level base attributes will all match Brighter node-for-node.
- **Dependency story already simplified.** `Polly` is direct in core after ADR 10. `Newtonsoft.Json` is the only material new direct dependency for downstream consumers — and they were already pulling it transitively via `Paramore.Darker.QueryLogging` whenever they used the logging decorator.
- **Maintenance overhead.** Two side csproj files, two extra NuGet packages to publish per release, two extra `Description` fields to keep current. The split adds friction without adding modularity — `Paramore.Darker.Extensions.DependencyInjection` project-references both side packages, so a consumer who wires up DI gets both regardless.
- **V5 is the breaking-version window.** ADR 10 already commits V5 to breaking `IQueryProcessor`, `IQueryContext`, and the builder surface. Amortising a folder-and-namespace rename in the same major version is cheaper than doing it later, and waiting would mean Darker drifts further from Brighter.
- **Existing core types are already out of step with Brighter.** Leaving `Paramore.Darker.Attributes` and `Paramore.Darker.Decorators` in place while introducing `Paramore.Darker.Policies.{Attributes,Handlers}` would be self-contradictory — the very next reviewer would ask "why does some FallbackPolicy stuff live in `Attributes/` and the rest in `Policies/Attributes/`?" Do the whole move at once.
- **MAUI sample disorganisation.** `SampleMauiTestApp/` sits at the repository root rather than under `samples/`, where the other sample lives. The `Darker.slnx` solution organises it virtually under a `/samples/` folder but the file system doesn't. Moving the physical folder into `samples/` cleans this up at the moment we're touching it for the namespace rename anyway.
- **Namespace clarity vs. type-name clarity.** A bare `Logging.LoggingAttribute` would collide visually with `Microsoft.Extensions.Logging` and confuse IDE auto-import. Keeping `QueryLogging` on type names (e.g. `QueryLoggingAttribute`) while moving them under a `Logging` namespace mirrors Brighter's `RequestLoggingAttribute`-under-`Paramore.Brighter.Logging.Attributes` pattern and avoids the collision.

### Constraints

- **Type names, method signatures, DI extension names, and decorator behaviour must not change.** Only locations (assembly, folder, namespace) change.
- **Central package management** stays — versions live in `Directory.Packages.props`, not in csproj entries.
- **Multi-target frameworks unchanged:** `netstandard2.0;net8.0;net9.0` for core.
- **AOT compatibility expected to be preserved**, verified by running `Paramore.Darker.Tests.AOT` post-merge on `net8.0` and `net9.0`. If `Newtonsoft.Json`'s reflection-heavy paths trip AOT/trimming after the merge, that regression is tracked and fixed under a separate follow-up issue rather than blocking this ADR.
- **MAUI sample stays out of `Darker.Filter.slnf`** (CI does not have the MAUI workload), but must compile inside `Darker.slnx` for developers who do.

## Decision

Restructure `src/Paramore.Darker/` to mirror Brighter's layout exactly, by (a) moving the side packages' sources into core under `Policies/{Attributes,Handlers}/` and `Logging/{Attributes,Handlers}/`, (b) relocating the existing in-core `FallbackPolicy*` types into the same `Policies/{Attributes,Handlers}/` folders so the new layout is internally consistent, (c) moving `QueryHandlerAttribute*` to the root namespace to mirror Brighter's `RequestHandlerAttribute`, (d) deleting the now-empty `Attributes/` and `Decorators/` folders, (e) adding `Newtonsoft.Json` as a direct dependency of core, (f) deleting the side projects from disk and from both solution files, (g) moving `SampleMauiTestApp/` to `samples/SampleMauiTestApp/` and updating its source files and project references accordingly, and (h) updating every internal `using` statement per the canonical inventory in requirements FR8. Ship this as a V5 breaking namespace change.

### Architecture Overview

```
BEFORE                                            AFTER
------                                            -----
src/Paramore.Darker/                              src/Paramore.Darker/
  Attributes/                                       QueryHandlerAttribute.cs       (ns Paramore.Darker)
    QueryHandlerAttribute.cs                        QueryHandlerAttributeAsync.cs  (ns Paramore.Darker)
    QueryHandlerAttributeAsync.cs                   Policies/
    FallbackPolicyAttribute.cs                        Constants.cs                            (new)
    FallbackPolicyAttributeAsync.cs                   QueryProcessorBuilderExtensions.cs      (new)
  Decorators/                                         Attributes/
    FallbackPolicyDecorator.cs                          RetryableQueryAttribute.cs            (new)
    FallbackPolicyDecoratorAsync.cs                     RetryableQueryAttributeAsync.cs       (new)
  Logging/                                              FallbackPolicyAttribute.cs            (moved)
    ApplicationLogging.cs                               FallbackPolicyAttributeAsync.cs       (moved)
  (no Policies folder)                                Handlers/
                                                        RetryableQueryDecorator.cs            (new)
src/Paramore.Darker.Policies/                           RetryableQueryDecoratorAsync.cs       (new)
  Constants.cs                                          FallbackPolicyDecorator.cs            (moved)
  QueryProcessorBuilderExtensions.cs                    FallbackPolicyDecoratorAsync.cs       (moved)
  RetryableQueryAttribute.cs                        Logging/
  RetryableQueryAttributeAsync.cs                     ApplicationLogging.cs                   (kept)
  RetryableQueryDecorator.cs                          Constants.cs                            (new)
  RetryableQueryDecoratorAsync.cs                     QueryProcessorBuilderExtensions.cs      (new)
                                                      Attributes/
src/Paramore.Darker.QueryLogging/                       QueryLoggingAttribute.cs              (new)
  Constants.cs                                          QueryLoggingAttributeAsync.cs         (new)
  QueryProcessorBuilderExtensions.cs                  Handlers/
  QueryLoggingAttribute.cs                              QueryLoggingDecorator.cs              (new)
  QueryLoggingAttributeAsync.cs                         QueryLoggingDecoratorAsync.cs         (new)
  QueryLoggingDecorator.cs
  QueryLoggingDecoratorAsync.cs                   (folders src/Paramore.Darker/Attributes/ and
                                                   src/Paramore.Darker/Decorators/ deleted; side
                                                   project folders deleted entirely)

SampleMauiTestApp/                                samples/SampleMauiTestApp/
  ...                                               ... (same contents, new path)

samples/SampleMinimalApi/                         samples/SampleMinimalApi/
  ...                                               ... (unchanged path; only `using`s update)

test/Paramore.Darker.Testing.Ports/               (folder deleted)
  TestQueryA.cs                                   test/Paramore.Darker.Core.Tests/Exported/
  TestQueryB.cs                              ->     TestQueryA.cs      (public, ns Paramore.Darker.Core.Tests.Exported)
  TestQueryC.cs                                     TestQueryB.cs      (public)
  TestQueryHandler.cs                               TestQueryC.cs      (public)
  TestQueryHandlerAsync.cs                          TestQueryHandler.cs      (public)
                                                    TestQueryHandlerAsync.cs (public)

test/Paramore.Darker.Tests/TestDoubles/           test/Paramore.Darker.Tests/TestDoubles/
  (12 files, 14 public class decls)          ->     (same 12 files, flipped to internal class)
  (12 outer + 2 nested Result classes)              (14 internal class declarations)
```

Package graph collapses from three publishable assemblies to one for these concerns:

```
BEFORE                                            AFTER
------                                            -----
Paramore.Darker.Extensions.DI                     Paramore.Darker.Extensions.DI
  -> Paramore.Darker                                -> Paramore.Darker
       (+ Polly direct dep from ADR 10)                 (+ Polly direct dep, unchanged from ADR 10)
  -> Paramore.Darker.Policies                          (+ Newtonsoft.Json direct dep, new)
       -> Paramore.Darker
       -> Polly
  -> Paramore.Darker.QueryLogging
       -> Paramore.Darker
       -> Newtonsoft.Json
```

### Roles and Responsibilities

This is a structural change in Beck's "Tidy First" sense — it rearranges code without changing behaviour. The roles and responsibilities established by earlier ADRs (especially ADR 2 on attribute-driven decorators and ADR 10 on `IQueryContext.Policies`) are unchanged. For clarity, the *holders* of each role move as follows:

| Role (stereotype)                                       | Type(s)                                                                       | Old location (V4)                                                                  | New location (V5)                                              |
|---------------------------------------------------------|-------------------------------------------------------------------------------|------------------------------------------------------------------------------------|----------------------------------------------------------------|
| Decorator attribute base (structurer of pipeline metadata) | `QueryHandlerAttribute`, `QueryHandlerAttributeAsync`                         | `src/Paramore.Darker/Attributes/`, ns `Paramore.Darker.Attributes`                  | `src/Paramore.Darker/` (root), ns `Paramore.Darker`            |
| Policy attribute — retryable (info holder + structurer) | `RetryableQueryAttribute`, `RetryableQueryAttributeAsync`                     | `src/Paramore.Darker.Policies/` (side package), ns `Paramore.Darker.Policies`      | `src/Paramore.Darker/Policies/Attributes/`, ns `Paramore.Darker.Policies.Attributes` |
| Policy attribute — fallback (info holder + structurer)  | `FallbackPolicyAttribute`, `FallbackPolicyAttributeAsync`                     | `src/Paramore.Darker/Attributes/`, ns `Paramore.Darker.Attributes`                  | `src/Paramore.Darker/Policies/Attributes/`, ns `Paramore.Darker.Policies.Attributes` |
| Policy decorator — retryable (service provider, applies Polly) | `RetryableQueryDecorator`, `RetryableQueryDecoratorAsync`             | `src/Paramore.Darker.Policies/` (side package), ns `Paramore.Darker.Policies`      | `src/Paramore.Darker/Policies/Handlers/`, ns `Paramore.Darker.Policies.Handlers` |
| Policy decorator — fallback (service provider, handles failures) | `FallbackPolicyDecorator`, `FallbackPolicyDecoratorAsync`           | `src/Paramore.Darker/Decorators/`, ns `Paramore.Darker.Decorators`                  | `src/Paramore.Darker/Policies/Handlers/`, ns `Paramore.Darker.Policies.Handlers` |
| Policy builder entry (controller for fluent config)     | `QueryProcessorBuilderExtensions` (Policies), `Constants` (Policies)          | `src/Paramore.Darker.Policies/` (side package), ns `Paramore.Darker.Policies`      | `src/Paramore.Darker/Policies/`, ns `Paramore.Darker.Policies` |
| Logging attribute (info holder + structurer)            | `QueryLoggingAttribute`, `QueryLoggingAttributeAsync`                         | `src/Paramore.Darker.QueryLogging/` (side package), ns `Paramore.Darker.QueryLogging` | `src/Paramore.Darker/Logging/Attributes/`, ns `Paramore.Darker.Logging.Attributes` |
| Logging decorator (service provider, serialises and logs) | `QueryLoggingDecorator`, `QueryLoggingDecoratorAsync`                       | `src/Paramore.Darker.QueryLogging/` (side package), ns `Paramore.Darker.QueryLogging` | `src/Paramore.Darker/Logging/Handlers/`, ns `Paramore.Darker.Logging.Handlers` |
| Logging builder entry (controller for fluent config)    | `QueryProcessorBuilderExtensions` (Logging), `Constants` (Logging)            | `src/Paramore.Darker.QueryLogging/` (side package), ns `Paramore.Darker.QueryLogging` | `src/Paramore.Darker/Logging/`, ns `Paramore.Darker.Logging` (alongside the pre-existing `ApplicationLogging.cs`) |
| Logging — application (info holder, exposes `LoggerFactory`) | `ApplicationLogging`                                                     | `src/Paramore.Darker/Logging/`, ns `Paramore.Darker.Logging`                        | **unchanged** — pre-existing, lives at the same path with the same namespace. Acknowledged so the colocation is intentional, not accidental. |

DI-side entry points (`AddPolicies`, `AddDefaultPolicies`, `AddJsonQueryLogging`) stay in `Paramore.Darker.Extensions.DependencyInjection` and keep their roles as DI controllers; only their `using`s change.

### Key Design Decisions

#### 1. Folder and namespace layout mirrors Brighter — including for existing in-core types

Each feature lives in its own sub-folder under `src/Paramore.Darker/` with the same `Attributes` / `Handlers` split Brighter uses:

```
src/Paramore.Darker/Policies/Attributes/   ns Paramore.Darker.Policies.Attributes
src/Paramore.Darker/Policies/Handlers/     ns Paramore.Darker.Policies.Handlers
src/Paramore.Darker/Logging/Attributes/    ns Paramore.Darker.Logging.Attributes
src/Paramore.Darker/Logging/Handlers/      ns Paramore.Darker.Logging.Handlers
```

Parent-namespace utilities (each feature's `Constants.cs` and `QueryProcessorBuilderExtensions.cs`) sit at the folder root:

```
src/Paramore.Darker/Policies/Constants.cs                       ns Paramore.Darker.Policies
src/Paramore.Darker/Policies/QueryProcessorBuilderExtensions.cs ns Paramore.Darker.Policies
src/Paramore.Darker/Logging/Constants.cs                        ns Paramore.Darker.Logging
src/Paramore.Darker/Logging/QueryProcessorBuilderExtensions.cs  ns Paramore.Darker.Logging
src/Paramore.Darker/Logging/ApplicationLogging.cs               ns Paramore.Darker.Logging  (pre-existing)
```

This matches Brighter's choice to put `ApplicationLogging.cs` directly under `src/Paramore.Brighter/Logging/` rather than forcing it into a sub-namespace. Darker already does the same thing; the new `Constants.cs` and `QueryProcessorBuilderExtensions.cs` files simply join it.

The base attribute classes go to the root, mirroring Brighter:

```
src/Paramore.Darker/QueryHandlerAttribute.cs       ns Paramore.Darker
src/Paramore.Darker/QueryHandlerAttributeAsync.cs  ns Paramore.Darker
```

This mirrors `src/Paramore.Brighter/RequestHandlerAttribute.cs` (namespace `Paramore.Brighter`). After the move, the root-level `src/Paramore.Darker/Attributes/` and `src/Paramore.Darker/Decorators/` folders are empty and are deleted.

#### 2. `FallbackPolicy*` types relocate into the new `Policies/` folder

The current location of `FallbackPolicyAttribute*` (in `src/Paramore.Darker/Attributes/`, namespace `Paramore.Darker.Attributes`) and `FallbackPolicyDecorator*` (in `src/Paramore.Darker/Decorators/`, namespace `Paramore.Darker.Decorators`) is a pre-side-package artefact. Brighter has its `FallbackPolicyAttribute*` and `FallbackPolicyHandler*` inside `Policies/Attributes/` and `Policies/Handlers/`. Darker mirrors that exactly:

- `FallbackPolicyAttribute.cs`, `FallbackPolicyAttributeAsync.cs` → `src/Paramore.Darker/Policies/Attributes/`, namespace `Paramore.Darker.Policies.Attributes`.
- `FallbackPolicyDecorator.cs`, `FallbackPolicyDecoratorAsync.cs` → `src/Paramore.Darker/Policies/Handlers/`, namespace `Paramore.Darker.Policies.Handlers`.

Type names are preserved (`FallbackPolicyAttribute`, not `FallbackAttribute`).

#### 3. Rename `QueryLogging` folder/namespace to `Logging`; keep `QueryLogging` on type names

The folder goes from `QueryLogging` (side package) to `Logging` (in core) so Darker's namespace tree matches Brighter's tree node-for-node. The *type names* keep their `QueryLogging` prefix for three reasons:

- **Brighter precedent**: Brighter's type names are `RequestLoggingAttribute`, `RequestLoggingHandler`, etc. — the *request kind* prefixes the role. Darker's request kind is "Query", so `QueryLoggingAttribute` is the direct analogue. Keeping this naming convention *is* mirroring Brighter, not a departure from it.
- **Collision avoidance**: a bare `LoggingAttribute` / `LoggingDecorator` would compete with `Microsoft.Extensions.Logging.*` and other `Logging*` types in IDE auto-import lists.
- **Namespace already qualifies it**: at `Paramore.Darker.Logging.Attributes.QueryLoggingAttribute`, the fully-qualified type is unambiguous, and the `Logging` namespace segment is safely contextualised by its parent.

#### 4. Core csproj gains `Newtonsoft.Json`; `Polly` is already there

`Paramore.Darker.csproj` adds one new `PackageReference`:

```xml
<PackageReference Include="Newtonsoft.Json" />
```

`Polly` is already referenced by core today (added during ADR 10's `IPolicyRegistry<string>` work).

No version attributes — central package management already pins `Newtonsoft.Json` (`13.0.4`) and `Polly` (`8.6.6`) in `Directory.Packages.props`.

The `Description` is updated. Proposed wording, for review:

> Darker is the query-side counterpart to Brighter, implementing the Query pattern (CQRS read-side) with a pipeline architecture for cross-cutting concerns including retry, fallback, and request logging.

Replaces the current single-phrase `Darker Query Processor`. Brighter uses a longer two-sentence form on its own csproj; this proposal is one sentence and matches Brighter's tone of "explains what the package actually does."

#### 5. Side projects and project references removed in lock-step

The two csproj directories are deleted entirely from disk:

- `src/Paramore.Darker.Policies/` (including `Constants.cs`, `QueryProcessorBuilderExtensions.cs`, the four `RetryableQuery*.cs` files, the csproj, and the `bin/` / `obj/` artefacts).
- `src/Paramore.Darker.QueryLogging/` (including `Constants.cs`, `QueryProcessorBuilderExtensions.cs`, the four `QueryLogging*.cs` files, the csproj, and the `bin/` / `obj/` artefacts).

Solution files lose their entries:

- `Darker.slnx` — drop the two `<Project Path=...>` entries under `/src/`; also update the `samples/` folder entry for `SampleMauiTestApp` to point at `samples/SampleMauiTestApp/SampleMauiTestApp.csproj` (was `SampleMauiTestApp/SampleMauiTestApp.csproj`).
- `Darker.Filter.slnf` — drop `src\\Paramore.Darker.Policies\\Paramore.Darker.Policies.csproj` and `src\\Paramore.Darker.QueryLogging\\Paramore.Darker.QueryLogging.csproj` from the `projects` array.

Every downstream `<ProjectReference>` to the two side projects is removed. Spec-time locations (from the requirements FR7 inventory plus `grep -rln 'Paramore.Darker.Policies.csproj\|Paramore.Darker.QueryLogging.csproj' --include="*.csproj" .`):

- `src/Paramore.Darker.Extensions.DependencyInjection/Paramore.Darker.Extensions.DependencyInjection.csproj` — drop both references.
- `samples/SampleMinimalApi/SampleMinimalApi.csproj` — drop both references.
- `samples/SampleMauiTestApp/SampleMauiTestApp.csproj` (post-move path) — drop both references.
- `test/Paramore.Darker.Tests/Paramore.Darker.Tests.csproj` — drop both references.
- `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj` — drop both references.

At task-time, re-run the grep and treat the result as authoritative — see Implementation Approach Step 9 for the safety-net instruction.

#### 6. SampleMauiTestApp moves under `samples/`

The MAUI sample currently sits at `SampleMauiTestApp/` at the repository root, with `Darker.slnx` virtually placing it under a `/samples/` folder. Move the physical folder to `samples/SampleMauiTestApp/`, fix the `Darker.slnx` path, and update its source files for the namespace changes. The sample stays *out* of `Darker.Filter.slnf` (CI does not install MAUI workloads), but the full-solution `Darker.slnx` build remains green for developers who do have the workload.

When updating `Darker.slnx`, only the `Path` attribute on the `<Project>` element changes. The `Type="Classic C#"` attribute and the nested `<Configuration>` mappings must be preserved verbatim — a one-line replacement that drops the `Type` attribute will silently change the IDE behaviour for the project.

Source files in `samples/SampleMauiTestApp/` that need `using`-statement updates: `MauiProgram.cs`, `DarkerSettings.cs`, `QueryHandlers/GetPeopleQueryHandler.cs`, `QueryHandlers/GetPersonQueryHandler.cs`. (Identical inventory to the `samples/SampleMinimalApi/` sample plus `MauiProgram.cs` in place of `Program.cs`.)

#### 7. `using` statements updated everywhere — canonical inventory in requirements FR8

The full file-by-file list of `using`-statement updates is in `requirements.md` FR8. It is the canonical inventory and is what the task list will drive from. Summary:

| Area                                                 | Files                                                                                                 |
|------------------------------------------------------|-------------------------------------------------------------------------------------------------------|
| Core (internal references to moving types)           | `src/Paramore.Darker/Builder/QueryProcessorBuilder.cs`, `src/Paramore.Darker/PipelineBuilder.cs`      |
| Side-package sources (import `Paramore.Darker.Attributes`/`Decorators`, which move in Step 1) | `src/Paramore.Darker.Policies/RetryableQueryAttribute.cs`, `RetryableQueryAttributeAsync.cs`; `src/Paramore.Darker.QueryLogging/QueryLoggingAttribute.cs`, `QueryLoggingAttributeAsync.cs`, `QueryLoggingDecorator.cs`, `QueryLoggingDecoratorAsync.cs` |
| `Paramore.Darker.Extensions.DependencyInjection`     | `PolicyDIExtensions.cs`, `QueryLoggingDIExtensions.cs`, `ServiceCollectionDecoratorRegistry.cs`       |
| `samples/SampleMinimalApi`                           | 4 files                                                                                               |
| `samples/SampleMauiTestApp` (post-move)              | 4 files                                                                                               |
| `test/Paramore.Darker.Tests` (root-level files)      | 5 files (the `When_*` feature tests touching policies or logging directly)                            |
| `test/Paramore.Darker.Tests/Decorators`              | 3 files (one feature test plus `FallbackPolicyTests.cs`)                                              |
| `test/Paramore.Darker.Tests/Integrations`            | 2 files                                                                                               |
| `test/Paramore.Darker.Tests/TestDoubles`             | 7 files                                                                                               |
| `test/Paramore.Darker.Tests/` (root-level "When_*" files importing existing-core moving namespaces) | 4 additional files (handler/attribute scenarios)                                                       |
| `test/Paramore.Darker.Tests.AOT`                     | 1 file (`Base/AOTTestClassBase.cs`)                                                                   |
| Testing.Ports `using` updates (move directly to `Paramore.Darker.Core.Tests.Exported`, per §10 + §11) | 9 files: `QueryHandlerRegistryTests.cs`, `FakeQueryProcessorTests.cs`, `QueryProcessorTests.cs`, `QueryProcessorAsyncTests.cs`, `When_QueryProcessorBuilder_builds_processor_should_configure_both_sync_and_async.cs`, `Integrations/QueryProcessorIntegrationTests.cs`, `Integrations/When_AddHandlersFromAssemblies_scans_assembly_should_register_both_sync_and_async_handlers.cs`, `Paramore.Darker.Tests.AOT/Base/AOTTestClassBase.cs`, `Paramore.Darker.Tests.AOT/QueryProcessor/AOTQueryProcessorTests.cs` |
| **Spot-checked unaffected** by Policies/QueryLogging/Attributes/Decorators moves (no current import of any of those moving namespaces) | `src/Paramore.Darker.Testing/`, `test/Paramore.Darker.Benchmarks/`, `test/Paramore.Test.Helpers/` |

#### 8. Decorator behaviour, attribute semantics, and DI surface unchanged

The decorator pipeline machinery — attribute discovery, step ordering, decorator factory resolution, exception handling, `IQueryContext.Policies` access, `JsonSerializerSettings` constructor injection — is left exactly as ADR 10 established it. No test in `Paramore.Darker.Tests` should need a behavioural assertion change. Only `using` statements in test files change, and only for the renamed/relocated namespaces.

(Note on the requirements doc: NFR1 in the previous draft of `requirements.md` claimed namespaces were preserved — which contradicted FR1/FR2. The current `requirements.md` has fixed this; NFR1 now says "test logic does not change" without the contradictory parenthetical. This ADR is consistent with the corrected NFR1.)

#### 9. `QueryHandlerRegistry` already scans public-only via `Assembly.ExportedTypes` — this becomes load-bearing

Both `QueryHandlerRegistry.RegisterFromAssemblies` and `QueryHandlerRegistryAsync.RegisterFromAssemblies` already do:

```csharp
from t in assemblies.SelectMany(a => a.ExportedTypes)
```

`Assembly.ExportedTypes` returns only public types ("the public types defined in this assembly that are visible outside the assembly"). No code change is needed in either registry class for the visibility-based filtering described in §10 to work. This ADR formalises that behaviour as load-bearing: any future refactor that switches from `ExportedTypes` to `GetTypes()` would silently re-register all the now-`internal` test doubles and break the assembly-scanning tests.

#### 10. Merge `Paramore.Darker.Testing.Ports` into the test project's `Exported/` folder

`test/Paramore.Darker.Testing.Ports/` is a 5-file project whose only purpose is to provide *public* test doubles (`TestQueryA`, `TestQueryB`, `TestQueryC`, `TestQueryHandler`, `TestQueryHandlerAsync`) shared between the main test project and the AOT test project for assembly-scanning tests. It is the lightest possible csproj — one `<ProjectReference>` to core, no third-party dependencies — and exists purely as a namespace/visibility scope.

Fold it into the main test project as a sub-folder:

- Move the 5 `.cs` files to `test/Paramore.Darker.Tests/Exported/` (the folder is renamed in Step 8 to `test/Paramore.Darker.Core.Tests/Exported/`).
- Change namespaces from `Paramore.Darker.Testing.Ports` to the post-Step-8 namespace `Paramore.Darker.Core.Tests.Exported` (tracking the renamed assembly — see §11). The 9 `using Paramore.Darker.Testing.Ports;` statements (see §7 inventory row) update directly to `using Paramore.Darker.Core.Tests.Exported;` rather than via an intermediate `Paramore.Darker.Tests.Exported`.
- Keep them `public` — they're exactly the types `AddHandlersFromAssemblies` should find when scanning the test assembly.
- Make every type in `test/Paramore.Darker.Tests/TestDoubles/*.cs` `internal class` (currently `public class`). They're used by direct reference within the test assembly, so `internal` visibility is sufficient and prevents `Assembly.ExportedTypes` from returning them when tests scan the test assembly.
- Update the AOT project's `<ProjectReference>` from `Paramore.Darker.Testing.Ports` to `test/Paramore.Darker.Tests` (and again to `test/Paramore.Darker.Core.Tests` after Step 8). Declare the reference with `<PrivateAssets>all</PrivateAssets>` so the reference's xunit/test-discoverer assets do not flow into the AOT project as transitive build assets. **Important caveat**: `<PrivateAssets>all</PrivateAssets>` controls *transitive* flow to consumers of the AOT project — it does **not**, on its own, prevent `Paramore.Darker.Core.Tests.dll` from being copied to the AOT project's `bin/` output directory. The reason xunit does not double-discover Core.Tests' tests under the AOT runner is that xunit's discoverer scans only the *entry assembly* of the test host (`Paramore.Darker.Tests.AOT.dll`), not every assembly in the bin folder. `<PrivateAssets>all</PrivateAssets>` is therefore a belt-and-braces measure for transitive test-asset flow, not the load-bearing mechanism preventing double-discovery; the entry-assembly-only discoverer behaviour is. The AOT project consumes only the public `Exported/` types; the now-`internal` local TestDoubles are correctly invisible to it.

  If the Step 7 `dotnet test test/Paramore.Darker.Tests.AOT/` count verification reveals double-discovery (i.e. the entry-assembly-only assumption fails on this combination of xunit + AOT host), the immediate fallback is to **extract `Exported/` into a separate non-test csproj** (`test/Paramore.Darker.Tests.Exported/Paramore.Darker.Tests.Exported.csproj`, no xunit/`IsTestProject` reference) and have both `Paramore.Darker.Core.Tests` and `Paramore.Darker.Tests.AOT` `<ProjectReference>` *that* csproj. This restores the clean separation `Paramore.Darker.Testing.Ports` provided, at the cost of one extra csproj. It is no longer deferred to a follow-up — it is the documented contingency action for the implementer if the count check fails.
- Delete `test/Paramore.Darker.Testing.Ports/` (folder + csproj + bin/obj) and remove its entries from `Darker.slnx` and `Darker.Filter.slnf`.

Why merge: removing a near-empty csproj reduces project count and per-release CI overhead; co-locating the public test doubles with the test project that uses them makes their purpose obvious to a reader; the `Exported/` folder name makes "this is the API surface tests should scan" visible from the source tree; the AOT project's awkward separate reference goes away in favour of a direct reference to the test project.

Why `internal` for local TestDoubles: the assembly-scanning tests (`When_AddHandlersFromAssemblies_scans_assembly_should_register_both_sync_and_async_handlers.cs`, `QueryHandlerRegistryTests.cs`) call `AddHandlersFromAssemblies(typeof(TestQueryHandler).Assembly)` — after the merge, `typeof(TestQueryHandler).Assembly` *is* the main test assembly. Without the visibility split, the scan would pick up the 12 local TestDoubles in addition to the 5 Exported handlers — and several of those TestDoubles register handlers for the *same* query type (e.g. `LoggingQueryHandler`, `RetryableQueryHandler`, `SyncHandlerWithFallback`, `SyncHandlerWithAsyncAttribute`, and `ContextCapturingHandler` are all `QueryHandler<SyncTestQuery, SyncTestQuery.Result>`). `QueryHandlerRegistry.RegisterFromAssemblies` enforces handler-per-query uniqueness and throws `ConfigurationException("Registry already contains an entry...")` on the second registration. The scan would therefore fail outright at handler discovery time, not silently inflate a count. Making local TestDoubles `internal` is the cleanest way to ring-fence what the scan sees, and it relies on `RegisterFromAssemblies`'s existing use of `ExportedTypes` (per §9).

#### 11. Split `Paramore.Darker.Tests` into `Paramore.Darker.Core.Tests` and `Paramore.Darker.Extensions.Tests`

Brighter splits its tests by *primary subject under test*: `Paramore.Brighter.Core.Tests` (primarily tests the core), `Paramore.Brighter.Extensions.Tests` (primarily tests the DI extensions), `Paramore.Brighter.Testing.Tests` (primarily tests the `Testing` library). In practice Brighter's `Core.Tests` `<ProjectReference>`s six production projects including the DI Extensions, so the partition is conventional and signalled by file-organisation rather than enforced by csproj boundaries. The point of the split is to make "this test is primarily about X" legible at the test-project level rather than buried in folder structure.

Darker conflates `Paramore.Brighter.Core.Tests`-equivalent and `Paramore.Brighter.Extensions.Tests`-equivalent tests into a single `Paramore.Darker.Tests` project. Splitting them recovers Brighter's primary-subject signal.

The mechanics:

- **Rename** `test/Paramore.Darker.Tests/` to `test/Paramore.Darker.Core.Tests/`:
  - Folder name, csproj filename, `<AssemblyName>` MSBuild property, `<RootNamespace>` MSBuild property — all rename.
  - **Rewrite every `namespace Paramore.Darker.Tests` declaration in the renamed project's source files** to `namespace Paramore.Darker.Core.Tests`. This affects 43 source files at spec time (including the `Exported/` and `TestDoubles/` sub-folders, which become `Paramore.Darker.Core.Tests.Exported` and `Paramore.Darker.Core.Tests.TestDoubles`). Verify with `grep -rln 'namespace Paramore.Darker.Tests' test/Paramore.Darker.Core.Tests/` returning 0 after the sweep.
  - The full rename mirrors Brighter's `Paramore.Brighter.Core.Tests/**/*.cs` namespace convention (verified — Brighter's namespaces consistently track the assembly name).
- **Create** a new `test/Paramore.Darker.Extensions.Tests/Paramore.Darker.Extensions.Tests.csproj`. Mirrors `tests/Paramore.Brighter.Extensions.Tests/`. Project-references `Paramore.Darker.Extensions.DependencyInjection` (and transitively `Paramore.Darker`). Multi-targets the same TFMs as the existing test project. Source files use namespace `Paramore.Darker.Extensions.Tests`.
- **Move** the four files currently in `test/Paramore.Darker.Tests/Integrations/` into the new project (drop the `Integrations/` sub-folder — the project name carries the meaning):
  - `QueryProcessorIntegrationTests.cs`
  - `When_AddDefaultPolicies_called_should_register_policy_registry.cs`
  - `When_AddHandlersFromAssemblies_scans_assembly_should_register_both_sync_and_async_handlers.cs`
  - `When_AddJsonQueryLogging_called_should_register_serializer_settings.cs`
- Rewrite their `namespace Paramore.Darker.Tests.Integrations` declarations to `namespace Paramore.Darker.Extensions.Tests`.
- **Re-target** `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj`'s `<ProjectReference>` to the post-rename `Paramore.Darker.Core.Tests` (where the `Exported/` folder lives) with `<PrivateAssets>all</PrivateAssets>` — see §10 and the AOT row of the Risks table.
- Update `Darker.slnx` and `Darker.Filter.slnf` for the rename + the new project.

The AOT test project (`Paramore.Darker.Tests.AOT`) is **not renamed** in this spec. It's a special-purpose AOT harness rather than a per-production-assembly test project; renaming it (e.g. to `Paramore.Darker.Extensions.Tests.AOT` since it exercises `AddDarker`) is a defensible follow-up but adds risk to this PR and isn't required by the principle. Likewise, creating `Paramore.Darker.Testing.Tests` to fully mirror Brighter's third test project is left as a follow-up — `Paramore.Darker.Testing` (one file, `FakeQueryProcessor`) is currently not covered by dedicated tests and adding that test project is its own piece of work.

The four moved files have no awkward cross-cutting dependencies on the other tests in the project — they're already grouped under `Integrations/` because they only test the DI surface. The four `using` updates they need (for the moving namespaces from FR1/FR2/FR3 and the Testing.Ports → Exported rename) are still required, just in their new home.

### Technology Choices

- **No new third-party dependencies.** `Polly` is already a direct dependency of core. `Newtonsoft.Json` is promoted from a transitive (via the QueryLogging side package, when consumers used logging) to a direct dependency of core. The closure for a consumer that already used `AddJsonQueryLogging` is unchanged; a consumer who never used logging now picks up `Newtonsoft.Json` transitively but does not have to call any of its APIs.
- **Central package management retained.** All new `PackageReference` entries in core go in without a version attribute; versions stay in `Directory.Packages.props`.

### Implementation Approach

The change is purely structural, so it follows Beck's "Tidy First" workflow: structural commits first, no behavioural mixing. Suggested sequencing — the task phase will firm this up:

1. **Move existing-core `FallbackPolicy*` and `QueryHandlerAttribute*`** into their new homes (`Policies/{Attributes,Handlers}/` and the root respectively). In the same step, update every consumer of the moving namespaces so the build stays green:
   - In core: `src/Paramore.Darker/Builder/QueryProcessorBuilder.cs`, `src/Paramore.Darker/PipelineBuilder.cs`.
   - In `Paramore.Darker.Extensions.DependencyInjection`: `ServiceCollectionDecoratorRegistry.cs`.
   - **In the still-standalone side packages** (they currently `using Paramore.Darker.Attributes;` for the `QueryHandlerAttribute*` base classes and `using Paramore.Darker.Decorators;` for `FallbackPolicyDecorator.CauseOfFallbackException`):
     - `src/Paramore.Darker.Policies/RetryableQueryAttribute.cs`, `RetryableQueryAttributeAsync.cs`
     - `src/Paramore.Darker.QueryLogging/QueryLoggingAttribute.cs`, `QueryLoggingAttributeAsync.cs`, `QueryLoggingDecorator.cs`, `QueryLoggingDecoratorAsync.cs`
   - All test files that import `Paramore.Darker.Attributes` or `Paramore.Darker.Decorators` per requirements FR8.

   Build the full filter and confirm green before moving on. (Goal: the existing `Attributes/` and `Decorators/` folders become empty; the new `Policies/Attributes/` and `Policies/Handlers/` exist with only the FallbackPolicy types in them; `QueryHandlerAttribute*` lives at the root namespace; the side packages still build because their `using`s now point at the new locations.)
2. **Move the Policies side-package sources** into `src/Paramore.Darker/Policies/{Attributes,Handlers}/` (joining the FallbackPolicy types) and add the new `Polly`-bearing `Constants.cs` and `QueryProcessorBuilderExtensions.cs` files under `src/Paramore.Darker/Policies/`. Build core in isolation.
3. **Add `Newtonsoft.Json`** to `Paramore.Darker.csproj`. Build.
4. **Move the QueryLogging side-package sources** into `src/Paramore.Darker/Logging/{Attributes,Handlers}/` (alongside the existing `ApplicationLogging.cs`). Build.
5. **Update remaining internal `using`s** across `Paramore.Darker.Extensions.DependencyInjection`, `samples/SampleMinimalApi`, the AOT tests, and the bulk of `Paramore.Darker.Tests`. Build the full filter. (Spot-checked unaffected at spec time — confirmed by re-running the FR8 grep before this step: `src/Paramore.Darker.Testing/`, `test/Paramore.Darker.Benchmarks/`, `test/Paramore.Test.Helpers/`. If the grep finds new hits during the task phase, they get added here.)
6. **Move `SampleMauiTestApp/` to `samples/SampleMauiTestApp/`** and update its `using`s + drop its side-package `<ProjectReference>` entries.
7. **Merge `Paramore.Darker.Testing.Ports` into the test project's `Exported/` folder** (per §10):
   - Move the 5 source files from `test/Paramore.Darker.Testing.Ports/` to `test/Paramore.Darker.Tests/Exported/`. Update their namespace from `Paramore.Darker.Testing.Ports` *directly* to the post-Step-8 target `Paramore.Darker.Core.Tests.Exported` (no intermediate `Paramore.Darker.Tests.Exported` — the file moves and the namespace lands at its final value in one edit, since Step 8 immediately follows and the same files will be touched again). Keep them `public`.
   - Flip every type in `test/Paramore.Darker.Tests/TestDoubles/*.cs` from `public class` to `internal class`. Verify no test fixture in another assembly depends on them (none should — they're not exposed across the assembly boundary today either, per FR12's rationale).
   - In `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj`, replace the `<ProjectReference>` to `Paramore.Darker.Testing.Ports` with one to `..\Paramore.Darker.Tests\Paramore.Darker.Tests.csproj`, with `<PrivateAssets>all</PrivateAssets>` set on the reference (so xunit doesn't double-discover the referenced project's tests under the AOT runner). Step 8 retargets the `Include` path to the post-rename csproj.
   - In `test/Paramore.Darker.Tests/Paramore.Darker.Tests.csproj`, drop the existing `<ProjectReference>` to `Paramore.Darker.Testing.Ports`.
   - Update all 9 `using Paramore.Darker.Testing.Ports;` statements to `using Paramore.Darker.Core.Tests.Exported;` (file list in §7 inventory). The non-test-project source files (the AOT project's two source files — `Base/AOTTestClassBase.cs` and `QueryProcessor/AOTQueryProcessorTests.cs`) compile fine against the new namespace immediately; the in-test-project source files briefly have a namespace that points "ahead" of Step 8 — that's fine, the test project still builds because the `Exported/` files use the new namespace and the using statements match. (Sample handlers in `samples/SampleMinimalApi/` and `samples/SampleMauiTestApp/` do **not** import `Paramore.Darker.Testing.Ports` — verified by `grep -rln 'using Paramore.Darker.Testing.Ports' samples/ SampleMauiTestApp/` returning zero hits — so they need no changes in this step.)
   - Build the full filter. **Run** `dotnet test test/Paramore.Darker.Tests.AOT/` and confirm the test count matches AOT-only tests (not AOT + Tests combined). This is the `PrivateAssets` verification.
   - Confirm `Paramore.Darker.Tests` and `Paramore.Darker.Tests.AOT` pass — especially the `When_AddHandlersFromAssemblies_*` and `QueryHandlerRegistryTests` assembly-scan tests, which now exercise the new visibility ring-fence.
8. **Split the test project into `Paramore.Darker.Core.Tests` + `Paramore.Darker.Extensions.Tests`** (per §11):
   - Rename `test/Paramore.Darker.Tests/` to `test/Paramore.Darker.Core.Tests/` (folder name, csproj filename, `<AssemblyName>`, `<RootNamespace>`). The `Exported/` and `TestDoubles/` folders rename in place.
   - **Rewrite every `namespace Paramore.Darker.Tests` declaration** in the renamed project to `namespace Paramore.Darker.Core.Tests` (43 source files at spec time, including `TestDoubles/`). The `Exported/` files were already updated to `Paramore.Darker.Core.Tests.Exported` in Step 7. Verify: `grep -rln 'namespace Paramore.Darker.Tests' test/Paramore.Darker.Core.Tests/` returns 0.
   - Verify no production `InternalsVisibleTo("Paramore.Darker.Tests")` attribute exists (`grep -rn InternalsVisibleTo src/`); update it to `"Paramore.Darker.Core.Tests"` if one is found.
   - Create `test/Paramore.Darker.Extensions.Tests/Paramore.Darker.Extensions.Tests.csproj` mirroring Brighter's `tests/Paramore.Brighter.Extensions.Tests/`. Project-references `Paramore.Darker.Extensions.DependencyInjection` (and transitively `Paramore.Darker`). Multi-targets the same TFMs as Core.Tests. Default namespace `Paramore.Darker.Extensions.Tests`.
   - Move the four `Integrations/` files (`QueryProcessorIntegrationTests.cs`, `When_AddDefaultPolicies_*.cs`, `When_AddHandlersFromAssemblies_*.cs`, `When_AddJsonQueryLogging_*.cs`) into the new project, dropping the `Integrations/` sub-folder. Rewrite their `namespace Paramore.Darker.Tests.Integrations` declarations to `namespace Paramore.Darker.Extensions.Tests`.
   - Update `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj` so its `<ProjectReference>` (set in Step 7, with `PrivateAssets`) now points at `..\Paramore.Darker.Core.Tests\Paramore.Darker.Core.Tests.csproj`.
   - Update `Darker.slnx` and `Darker.Filter.slnf` to drop `Paramore.Darker.Tests` and add the two new projects (`Paramore.Darker.Core.Tests`, `Paramore.Darker.Extensions.Tests`).
   - Build the full filter and run the tests. All three test projects (Core.Tests, Extensions.Tests, Tests.AOT) should be green.
9. **Delete the removed projects** (`Paramore.Darker.Policies/`, `Paramore.Darker.QueryLogging/`, `Paramore.Darker.Testing.Ports/`) — folders + csproj + bin/obj — and **remove their entries from `Darker.slnx` and `Darker.Filter.slnf`** along with the now-empty `src/Paramore.Darker/Attributes/` and `src/Paramore.Darker/Decorators/` folders. Before declaring this step done, **re-run** `grep -rln 'Paramore.Darker.Policies.csproj\|Paramore.Darker.QueryLogging.csproj\|Paramore.Darker.Testing.Ports.csproj' --include='*.csproj' .` and remove the `<ProjectReference>` from every result; if the result set differs from §5's enumeration, treat the grep as authoritative. Build the full filter. Build `Darker.slnx` (full solution) for the MAUI side on a workload-capable machine.
10. **Run tests** — `Paramore.Darker.Core.Tests`, `Paramore.Darker.Extensions.Tests`, and `Paramore.Darker.Tests.AOT` should pass without behavioural changes.
11. **Update `Description`** on `Paramore.Darker.csproj` to the proposed wording in §4.

Tests do not drive this work because there is no new behaviour to test — the build green-light plus the existing test suite is the verification. The task phase should call out the spot-check assertions on new fully-qualified type names from the requirements' acceptance criteria.

## Consequences

### Positive

- Darker's package, folder, and namespace layout matches Brighter's node-for-node, lowering the cognitive cost of switching between the two libraries.
- One fewer project reference for the typical Darker consumer (`Paramore.Darker.Extensions.DependencyInjection` plus `Paramore.Darker` — done).
- Two fewer NuGet packages to publish, version-bump, and document on each release.
- The `Attributes` / `Handlers` split inside each feature folder makes the pipeline's two-layer architecture (declaration via attribute, behaviour via handler) immediately visible from the source tree.
- Unblocks the `QueryProcessorBuilder` relocation that ADR 10 §8 deferred until after #321. The actual move of `QueryProcessorBuilder.cs` out of core into `Paramore.Darker.Extensions.DependencyInjection` is left to a follow-up ADR — this ADR only makes that follow-up possible by ending the side packages' need to cast `builder as QueryProcessorBuilder` from outside core.
- Cleans up the long-standing inconsistency where some `Policies` types lived in `src/Paramore.Darker.Policies/` and others (`FallbackPolicy*`) lived in `src/Paramore.Darker/{Attributes,Decorators}/`.
- `SampleMauiTestApp/` now sits alongside `SampleMinimalApi/` under `samples/`, matching where the solution already organised it virtually.
- `Paramore.Darker.Testing.Ports` (a 5-file project whose only purpose was to provide public test doubles to the main test project and the AOT test project) is folded into `Paramore.Darker.Core.Tests/Exported/`. One fewer csproj to maintain, and the `Exported/` folder name plus the `internal`-vs-`public` visibility split on test doubles makes "what is auto-discoverable by `AddHandlersFromAssemblies` from outside this assembly" obvious from the source tree.
- Darker's assembly layout now matches Brighter's principle ("most things in core; side assemblies only for generic-interface implementations") node-for-node. The expected production set is `Paramore.Darker` + `Paramore.Darker.Extensions.DependencyInjection` + `Paramore.Darker.Testing`, and the expected test set is `Paramore.Darker.Core.Tests` + `Paramore.Darker.Extensions.Tests`. A new contributor familiar with Brighter can now reason about Darker's layout by analogy.

### Negative

- V5 breaking change at the namespace level. Two distinct consumer groups are affected:
  - Consumers who use the side packages will need to drop `using Paramore.Darker.Policies;` / `using Paramore.Darker.QueryLogging;` in favour of the new `.Attributes` / `.Handlers` sub-namespaces.
  - **Every** Darker consumer (not just side-package users) will need to update imports of `Paramore.Darker.Attributes` and `Paramore.Darker.Decorators`, because `[FallbackPolicy]` and the `[QueryHandler]` base attribute live in those namespaces today and they move. A consumer who never used policies-or-logging but did use `[FallbackPolicy]` is still affected.
  - Fix is mechanical (replace per the mapping table in `requirements.md`) but the blast radius is wider than "side-package users only".
  - Attribute *discovery* in `PipelineBuilder` resolves attributes by type reference, not by namespace string, so DI assembly scanning (`AddHandlersFromAssemblies`) is not affected — this is a source-compat break, not a runtime metadata change.
- `Paramore.Darker` package gains a direct dependency on `Newtonsoft.Json`, which a future ADR might want to swap for `System.Text.Json`. The swap will be slightly easier (one package) but slightly more visible (in core).
- Larger diff than the original "just merge the side packages" scope: this ADR also moves `FallbackPolicy*` and `QueryHandlerAttribute*` and relocates the MAUI sample. Task phase needs to plan the sequencing so reviewers can follow it.
- `Paramore.Darker.Logging` namespace now hosts both the `ApplicationLogging` shared infrastructure helper and the `QueryProcessorBuilderExtensions`/`Constants` from the merged logging decorator. They cohabit because Brighter does the same; readers unfamiliar with Brighter may be briefly surprised.
- **Test-project split (FR14 / §11) has a large blast radius.** Rewriting every `namespace Paramore.Darker.Tests` declaration in the renamed project affects ~43 source files. Solution files (`Darker.slnx`, `Darker.Filter.slnf`) gain/lose three entries (drop `Paramore.Darker.Tests`, add `Paramore.Darker.Core.Tests` and `Paramore.Darker.Extensions.Tests`). The AOT csproj's `<ProjectReference>` is rewritten twice (once at end of Step 7 to `Paramore.Darker.Tests`, once at Step 8 to `Paramore.Darker.Core.Tests`) — easy to forget the second update. Total diff churn for the test layer alone is ~150 files; reviewers should be ready for the sequencing to matter.

### Risks and Mitigations

| Risk                                                                                                | Mitigation                                                                                                                                                                                                                                                                                                                  |
|-----------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Build breaks subtly because a `using` was missed (often in less-touched test fixtures or samples)   | Requirements FR8 is the canonical, file-by-file inventory; CI builds `Darker.Filter.slnf` and the AOT project; `samples/SampleMinimalApi` build is part of acceptance; the MAUI sample build is part of acceptance on workload-capable developer machines.                                                                  |
| Third-party consumers' NuGet restore breaks when V5 ships and the side packages stop being published | V5 changelog and migration notes call out the package-name change; the namespace mapping table from `requirements.md` forms the basis of the migration guide.                                                                                                                                                              |
| ASP.NET Core or DI registration ordering changes accidentally due to moved files                    | The DI extensions stay in `Paramore.Darker.Extensions.DependencyInjection` and only their `using`s change; behaviour is covered by existing integration tests (`When_AddDefaultPolicies_*`, `When_AddJsonQueryLogging_*`).                                                                                                  |
| AOT test fails because `Newtonsoft.Json` is reflection-heavy and not trim-friendly out of the box   | `Paramore.Darker.Tests.AOT` already exercises `AddJsonQueryLogging` via its base class (`AOTTestClassBase.BuildServiceProvider`), so any AOT/trim regression surfaces in CI as soon as the merge lands. Per the Constraints section, AOT compatibility is not asserted unconditionally — it is verified post-merge. If the AOT test fails, the regression is captured as a separate follow-up issue (likely requiring pre-configured `JsonSerializerSettings` in `AOTTestClassBase.BuildServiceProvider`); the failure does not block this ADR. |
| Diff for the rename is large and review-fatiguing                                                   | Sequence the work as the steps in §Implementation Approach (each commit is one structural move). The task phase will firm this into a tasks.md.                                                                                                                                                                            |
| `Darker.slnx` MAUI path mismatch after move breaks IDE solution loading                              | Verify `Darker.slnx` opens cleanly in JetBrains Rider / VS after the path update; the task phase includes an explicit step to load the solution as a sanity check.                                                                                                                                                          |
| Two `QueryProcessorBuilderExtensions` files (one in `Policies/`, one in `Logging/`) with the same type name in different namespaces could confuse readers | Same pattern as the current side-package layout (both side packages had a `QueryProcessorBuilderExtensions` already); no behaviour change. The namespace disambiguates them.                                                                                                                                                |
| A future refactor of `QueryHandlerRegistry.RegisterFromAssemblies` switching from `Assembly.ExportedTypes` to `GetTypes()` would re-register the now-`internal` local TestDoubles, causing `RegisterFromAssemblies` to throw `ConfigurationException` on the duplicate `SyncTestQuery`/`AsyncTestQuery` handlers (see §10) and breaking `When_AddHandlersFromAssemblies_*` and `QueryHandlerRegistryTests` at handler-discovery time | §9 documents this as load-bearing; the `Exported/` folder name documents the same contract for human readers. Any future PR touching that scan should be reviewed against this ADR. Consider adding an inline `// IMPORTANT: ExportedTypes is load-bearing — see ADR 0011 §9-10` comment in both registries during Step 8 implementation. |
| AOT project referencing the main test project (`Paramore.Darker.Core.Tests`) could cause xunit to double-discover Core.Tests' tests under the AOT runner | The load-bearing mechanism is xunit's discoverer scanning only the entry assembly (`Paramore.Darker.Tests.AOT.dll`) of the test host, not every DLL in the bin folder. `<PrivateAssets>all</PrivateAssets>` on the AOT csproj's `<ProjectReference>` (Step 7) is a belt-and-braces measure that stops the reference's xunit/test-discoverer build assets flowing *transitively* through the AOT project — it does **not** prevent `Paramore.Darker.Core.Tests.dll` from being copied to the AOT project's `bin/` output, so it is not the primary control. The primary verification is Step 7's `dotnet test test/Paramore.Darker.Tests.AOT/` count check (AOT-only count, not AOT + Core.Tests combined). If the count check fails, the immediate contingency is to extract `Exported/` into a separate non-test `test/Paramore.Darker.Tests.Exported/Paramore.Darker.Tests.Exported.csproj` and have both Core.Tests and Tests.AOT `<ProjectReference>` that csproj — documented in §10 as the contingency action (no longer deferred to a follow-up). |

## Alternatives Considered

### 1. Keep the side packages

Reject. The side packages add maintenance overhead without modularity benefit — in practice every Darker consumer pulls both via the DI extensions. ADR 10 has already put `Polly` in core, so the strongest argument for keeping `Paramore.Darker.Policies` separate (avoid forcing Polly on consumers who don't want it) no longer applies. And Brighter's choice — Policies and Logging in core — is the precedent we're aligning to.

### 2. Merge the side packages but don't relocate existing in-core types

Move only the side-package sources, leaving `FallbackPolicy*` in `src/Paramore.Darker/Attributes/`/`Decorators/` and `QueryHandlerAttribute*` in `src/Paramore.Darker/Attributes/`. Reject. The merge's whole point is "mirror Brighter exactly"; leaving in-core types in non-Brighter-shaped folders means we'd still be inconsistent immediately after the merge. A reviewer would rightly ask why some `FallbackPolicy` stuff is in `Attributes/` and the rest in `Policies/Attributes/`. Do the whole move at once.

### 3. Merge without restructuring (flat namespaces inside core)

Move the files under `src/Paramore.Darker/Policies/` and `src/Paramore.Darker/QueryLogging/` but keep the flat `Paramore.Darker.Policies` / `Paramore.Darker.QueryLogging` namespaces. Reject. The whole point of mirroring Brighter is to make the two source trees navigable with the same mental map. The `Attributes` / `Handlers` split is a meaningful organising principle — it separates pipeline *declaration* (attributes) from pipeline *behaviour* (handlers). Brighter has it; Darker should too. And once we're breaking V5 anyway, paying the namespace-rename cost twice (once at merge, again at split) would be worse than paying it once.

### 4. Provide source-compat shims (e.g. type-forwarders)

Publish empty `Paramore.Darker.Policies` and `Paramore.Darker.QueryLogging` packages in V5 that contain only `[TypeForwardedTo]` attributes pointing at the merged core. Reject. V5 is the breaking-version window; documented migration is cheaper than shipping six more zombie packages. Type forwarders also don't help with `using` statements — they only redirect resolved type references at runtime — so consumers still have to recompile and update their imports.

### 5. Rename `QueryLogging` types to `Logging` for full symmetry with Brighter's folder

Rename `QueryLoggingAttribute` → `LoggingAttribute`, `QueryLoggingDecorator` → `LoggingDecorator`, `AddJsonQueryLogging` → `AddJsonLogging`, etc. Reject. (a) It collides at the type-name level with `Microsoft.Extensions.Logging.*` and would surface confusing IDE auto-import suggestions; (b) Brighter itself does *not* do this — Brighter's types are `RequestLoggingAttribute`, prefixed with the request kind. Darker's request kind is "Query", so `QueryLoggingAttribute` is the faithful mirror, not the departure.

### 6. Keep `QueryLogging` folder/namespace name (and `Logging` only on type names)

Rename nothing — leave the folder and namespace as `QueryLogging` and only mirror Brighter's `Policies/{Attributes,Handlers}` split. Reject. The whole tree should mirror Brighter, not just half of it. The collision argument against renaming *type names* does not apply to the namespace (`Paramore.Darker.Logging.*` is unambiguous in context).

### 7. Move `SampleMauiTestApp/` in a separate PR

Defer the MAUI sample move so this PR stays "only about the namespace merge." Reject. The MAUI sample's `using` statements and `<ProjectReference>`s break under the merge anyway, so we have to touch it. Moving its directory location while we're already in the file is cheaper than two passes. The only argument for deferring would be diff-size; the §Implementation Approach sequencing keeps the MAUI move as its own commit so diff size remains manageable.

## References

- Requirements: [specs/005-merge_builtin_decorators/requirements.md](../../specs/005-merge_builtin_decorators/requirements.md)
- Linked Issue: #321 (Merge `Paramore.Darker.Policies` and `Paramore.Darker.QueryLogging` into `Paramore.Darker`)
- Predecessor ADR: [docs/adr/0010-pass-query-context.md](0010-pass-query-context.md) — established the direct Polly dependency in core and called this merge out as a follow-up
- Brighter layout: `../Brighter/src/Paramore.Brighter/Policies/{Attributes,Handlers}/`, `../Brighter/src/Paramore.Brighter/Logging/{Attributes,Handlers}/`, and `../Brighter/src/Paramore.Brighter/RequestHandlerAttribute.cs` at the root namespace.
- Brighter core csproj: `../Brighter/src/Paramore.Brighter/Paramore.Brighter.csproj` (direct refs to `Polly`, `Newtonsoft.Json`, `Microsoft.Extensions.Logging`)
- Design principles: [.agent_instructions/design_principles.md](../../.agent_instructions/design_principles.md) — particularly "Tidy First" separation of structural and behavioural changes
- Review that drove this revision: [specs/005-merge_builtin_decorators/review-design.md](../../specs/005-merge_builtin_decorators/review-design.md)
