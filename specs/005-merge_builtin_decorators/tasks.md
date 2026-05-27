# Tasks: Merge Builtin Decorators (Policies and QueryLogging) Into Core

**Spec**: 005-merge_builtin_decorators
**ADR**: [0011-merge-builtin-decorators.md](../../docs/adr/0011-merge-builtin-decorators.md)
**Requirements**: [requirements.md](requirements.md)
**Issue**: [#321](https://github.com/BrighterCommand/Darker/issues/321)

## Overview

This is a **purely structural change** — Beck's "Tidy First". Three side packages fold into core, the in-core layout aligns with Brighter (`Policies/{Attributes,Handlers}/`, `Logging/{Attributes,Handlers}/`, root-level `QueryHandlerAttribute*`), `SampleMauiTestApp/` relocates under `samples/`, `Paramore.Darker.Testing.Ports` folds into the test project's `Exported/` folder, and the combined test project splits into `Paramore.Darker.Core.Tests` + `Paramore.Darker.Extensions.Tests`. No new behaviour, no test-content changes — only `using`-statement updates inside tests where namespaces moved. The existing test suite (Core.Tests, Extensions.Tests, Tests.AOT) is the verification.

Use `/tidy-first <change>` for each task — these are structural moves, not behavioural changes.

## Pre-flight

- [x] **STRUCTURAL: First ADR commit on `merge-decorators`**
  - Branch `merge-decorators` is currently 0 commits ahead of `master` (per PROMPT.md).
  - First commit is `docs/adr/0011-merge-builtin-decorators.md` (already on Accepted status) plus the `specs/005-merge_builtin_decorators/` directory.
  - Commit message: `docs: add ADR 0011 — merge builtin decorators (#321)`
  - Verify: `git log --oneline merge-decorators ^master | head` shows the ADR commit.

## Tasks

### Step 1: Relocate existing-core `FallbackPolicy*` and `QueryHandlerAttribute*`

- [x] **STRUCTURAL: Move `QueryHandlerAttribute*` to root namespace; move `FallbackPolicy*` into `Policies/{Attributes,Handlers}/`**
  - **USE COMMAND**: `/tidy-first move QueryHandlerAttribute to root and FallbackPolicy into Policies feature-folder`
  - Move files:
    - `src/Paramore.Darker/Attributes/QueryHandlerAttribute.cs` → `src/Paramore.Darker/QueryHandlerAttribute.cs` (namespace `Paramore.Darker`)
    - `src/Paramore.Darker/Attributes/QueryHandlerAttributeAsync.cs` → `src/Paramore.Darker/QueryHandlerAttributeAsync.cs` (namespace `Paramore.Darker`)
    - `src/Paramore.Darker/Attributes/FallbackPolicyAttribute.cs` → `src/Paramore.Darker/Policies/Attributes/FallbackPolicyAttribute.cs` (namespace `Paramore.Darker.Policies.Attributes`)
    - `src/Paramore.Darker/Attributes/FallbackPolicyAttributeAsync.cs` → `src/Paramore.Darker/Policies/Attributes/FallbackPolicyAttributeAsync.cs` (namespace `Paramore.Darker.Policies.Attributes`)
    - `src/Paramore.Darker/Decorators/FallbackPolicyDecorator.cs` → `src/Paramore.Darker/Policies/Handlers/FallbackPolicyDecorator.cs` (namespace `Paramore.Darker.Policies.Handlers`)
    - `src/Paramore.Darker/Decorators/FallbackPolicyDecoratorAsync.cs` → `src/Paramore.Darker/Policies/Handlers/FallbackPolicyDecoratorAsync.cs` (namespace `Paramore.Darker.Policies.Handlers`)
  - Update `using`s per requirements FR8 for the in-core moves — **production-code consumers + side-package source files only** (test-file `using` updates all happen in Step 5, so the Step 1 → Step 5 boundary stays clean):
    - In core: `src/Paramore.Darker/Builder/QueryProcessorBuilder.cs`, `src/Paramore.Darker/PipelineBuilder.cs`
    - In `Paramore.Darker.Extensions.DependencyInjection`: `ServiceCollectionDecoratorRegistry.cs`
    - In still-standalone side packages (importing `Paramore.Darker.Attributes` for the base classes and `Paramore.Darker.Decorators` for `FallbackPolicyDecorator.CauseOfFallbackException`):
      - `src/Paramore.Darker.Policies/RetryableQueryAttribute.cs`, `RetryableQueryAttributeAsync.cs`
      - `src/Paramore.Darker.QueryLogging/QueryLoggingAttribute.cs`, `QueryLoggingAttributeAsync.cs`, `QueryLoggingDecorator.cs`, `QueryLoggingDecoratorAsync.cs`
  - At this point the core production assemblies + side-package assemblies all build green; test projects briefly have stale `using Paramore.Darker.Attributes;` / `using Paramore.Darker.Decorators;` lines that Step 5 sweeps.
  - Verify: `dotnet build src/Paramore.Darker/Paramore.Darker.csproj -c Release`, `dotnet build src/Paramore.Darker.Extensions.DependencyInjection/Paramore.Darker.Extensions.DependencyInjection.csproj -c Release`, `dotnet build src/Paramore.Darker.Policies/Paramore.Darker.Policies.csproj -c Release`, and `dotnet build src/Paramore.Darker.QueryLogging/Paramore.Darker.QueryLogging.csproj -c Release` all succeed. (Full-filter build is deferred to Step 5 after test-file usings catch up.)

### Step 2: Move Policies side-package sources into core

- [x] **STRUCTURAL: Move `Paramore.Darker.Policies/` sources into `src/Paramore.Darker/Policies/`**
  - **USE COMMAND**: `/tidy-first move Paramore.Darker.Policies sources into core Policies feature-folder`
  - Move files:
    - `src/Paramore.Darker.Policies/RetryableQueryAttribute.cs` → `src/Paramore.Darker/Policies/Attributes/RetryableQueryAttribute.cs` (namespace `Paramore.Darker.Policies.Attributes`)
    - `src/Paramore.Darker.Policies/RetryableQueryAttributeAsync.cs` → `src/Paramore.Darker/Policies/Attributes/RetryableQueryAttributeAsync.cs` (namespace `Paramore.Darker.Policies.Attributes`)
    - `src/Paramore.Darker.Policies/RetryableQueryDecorator.cs` → `src/Paramore.Darker/Policies/Handlers/RetryableQueryDecorator.cs` (namespace `Paramore.Darker.Policies.Handlers`)
    - `src/Paramore.Darker.Policies/RetryableQueryDecoratorAsync.cs` → `src/Paramore.Darker/Policies/Handlers/RetryableQueryDecoratorAsync.cs` (namespace `Paramore.Darker.Policies.Handlers`)
    - `src/Paramore.Darker.Policies/Constants.cs` → `src/Paramore.Darker/Policies/Constants.cs` (namespace `Paramore.Darker.Policies`)
    - `src/Paramore.Darker.Policies/QueryProcessorBuilderExtensions.cs` → `src/Paramore.Darker/Policies/QueryProcessorBuilderExtensions.cs` (namespace `Paramore.Darker.Policies`)
  - Side-package csproj `src/Paramore.Darker.Policies/Paramore.Darker.Policies.csproj` left on disk for now (deleted in Step 9).
  - Verify: `dotnet build src/Paramore.Darker/Paramore.Darker.csproj -c Release` succeeds (core builds with the merged Policies code).

### Step 3: Add `Newtonsoft.Json` to core csproj

- [x] **STRUCTURAL: Add `Newtonsoft.Json` as a direct dependency of `Paramore.Darker.csproj`**
  - **USE COMMAND**: `/tidy-first add Newtonsoft.Json direct dependency to Paramore.Darker.csproj`
  - Add `<PackageReference Include="Newtonsoft.Json" />` to `src/Paramore.Darker/Paramore.Darker.csproj` (no version attribute — CPM in `Directory.Packages.props` pins `13.0.4`).
  - Verify: `dotnet build src/Paramore.Darker/Paramore.Darker.csproj -c Release` succeeds.

### Step 4: Move QueryLogging side-package sources into core

- [x] **STRUCTURAL: Move `Paramore.Darker.QueryLogging/` sources into `src/Paramore.Darker/Logging/`**
  - **USE COMMAND**: `/tidy-first move Paramore.Darker.QueryLogging sources into core Logging feature-folder`
  - Move files:
    - `src/Paramore.Darker.QueryLogging/QueryLoggingAttribute.cs` → `src/Paramore.Darker/Logging/Attributes/QueryLoggingAttribute.cs` (namespace `Paramore.Darker.Logging.Attributes`)
    - `src/Paramore.Darker.QueryLogging/QueryLoggingAttributeAsync.cs` → `src/Paramore.Darker/Logging/Attributes/QueryLoggingAttributeAsync.cs` (namespace `Paramore.Darker.Logging.Attributes`)
    - `src/Paramore.Darker.QueryLogging/QueryLoggingDecorator.cs` → `src/Paramore.Darker/Logging/Handlers/QueryLoggingDecorator.cs` (namespace `Paramore.Darker.Logging.Handlers`)
    - `src/Paramore.Darker.QueryLogging/QueryLoggingDecoratorAsync.cs` → `src/Paramore.Darker/Logging/Handlers/QueryLoggingDecoratorAsync.cs` (namespace `Paramore.Darker.Logging.Handlers`)
    - `src/Paramore.Darker.QueryLogging/Constants.cs` → `src/Paramore.Darker/Logging/Constants.cs` (namespace `Paramore.Darker.Logging`)
    - `src/Paramore.Darker.QueryLogging/QueryProcessorBuilderExtensions.cs` → `src/Paramore.Darker/Logging/QueryProcessorBuilderExtensions.cs` (namespace `Paramore.Darker.Logging`)
  - `src/Paramore.Darker/Logging/ApplicationLogging.cs` (namespace `Paramore.Darker.Logging`) is **unchanged** — it pre-existed at this location alongside the new `Constants.cs` and `QueryProcessorBuilderExtensions.cs`.
  - Side-package csproj `src/Paramore.Darker.QueryLogging/Paramore.Darker.QueryLogging.csproj` left on disk for now (deleted in Step 9).
  - Verify: `dotnet build src/Paramore.Darker/Paramore.Darker.csproj -c Release` succeeds.

### Step 5: Update remaining `using` statements across DI extensions, samples, AOT, tests

- [x] **STRUCTURAL: Sweep `using`s for moved namespaces across all consumers**
  - **USE COMMAND**: `/tidy-first update using statements for Policies and Logging namespace moves across consumers`
  - Per requirements FR8 inventory, update `using`s in:
    - `Paramore.Darker.Extensions.DependencyInjection`: `PolicyDIExtensions.cs`, `QueryLoggingDIExtensions.cs`, `ServiceCollectionDecoratorRegistry.cs` (latter already touched in Step 1 — recheck).
    - `samples/SampleMinimalApi`: 4 files.
    - `SampleMauiTestApp/` (root-level — moved to `samples/` in Step 6): 4 files (`MauiProgram.cs`, `DarkerSettings.cs`, `QueryHandlers/GetPeopleQueryHandler.cs`, `QueryHandlers/GetPersonQueryHandler.cs`).
    - `test/Paramore.Darker.Tests` (root-level "When_*" files importing Policies/QueryLogging/Attributes/Decorators namespaces): 5 files.
    - `test/Paramore.Darker.Tests/Decorators`: 3 files (two `When_*` feature tests plus `FallbackPolicyTests.cs`).
    - `test/Paramore.Darker.Tests/Integrations`: 2 files.
    - `test/Paramore.Darker.Tests/TestDoubles`: 7 files.
    - `test/Paramore.Darker.Tests/` additional root-level files importing existing-core moving namespaces: 5 files (4 `When_*` files plus `PipelineBuilderExceptionTests.cs`, per FR8 line 158).
    - `test/Paramore.Darker.Tests.AOT/Base/AOTTestClassBase.cs`: 1 file.
  - Spot-checked unaffected at spec time (re-run the FR8 grep before this task — if new hits found, add them here): `src/Paramore.Darker.Testing/`, `test/Paramore.Darker.Benchmarks/`, `test/Paramore.Test.Helpers/`.
  - Verify: `dotnet build Darker.Filter.slnf -c Release` succeeds. `dotnet test Darker.Filter.slnf -c Release --no-build` passes. **On a MAUI-workload-capable machine, also run `dotnet build SampleMauiTestApp/SampleMauiTestApp.csproj -c Release`** to catch any typos in the MAUI sample's `using` updates (the filter excludes MAUI, so a typo would otherwise slip through to Step 9's full-`Darker.slnx` build).

### Step 6: Move `SampleMauiTestApp/` under `samples/`

- [x] **STRUCTURAL: Relocate `SampleMauiTestApp/` to `samples/SampleMauiTestApp/`**
  - **USE COMMAND**: `/tidy-first move SampleMauiTestApp under samples directory`
  - Move the entire folder: `SampleMauiTestApp/` → `samples/SampleMauiTestApp/`.
  - Update `Darker.slnx`: change the `Path` attribute on the `<Project>` element for SampleMauiTestApp. **Preserve verbatim** the `Type="Classic C#"` attribute and the nested `<Configuration>` mappings — a one-line replacement that drops the `Type` attribute will silently change IDE behaviour for the project.
  - Drop the side-package `<ProjectReference>` entries (to `Paramore.Darker.Policies` and `Paramore.Darker.QueryLogging`) from `samples/SampleMauiTestApp/SampleMauiTestApp.csproj`.
  - Verify: `Darker.slnx` opens cleanly in JetBrains Rider / VS (sanity check). `Darker.Filter.slnf` continues to build green on CI (MAUI sample is excluded from the filter — workload-only).

### Step 7: Merge `Paramore.Darker.Testing.Ports` into the test project's `Exported/` folder

- [x] **STRUCTURAL: Fold `Paramore.Darker.Testing.Ports` into `test/Paramore.Darker.Tests/Exported/`; flip local TestDoubles to `internal`; retarget AOT csproj with `<PrivateAssets>all</PrivateAssets>`**
  - **USE COMMAND**: `/tidy-first merge Paramore.Darker.Testing.Ports into Paramore.Darker.Tests Exported folder with visibility split`
  - Move the 5 source files from `test/Paramore.Darker.Testing.Ports/` to `test/Paramore.Darker.Tests/Exported/`:
    - `TestQueryA.cs`, `TestQueryB.cs`, `TestQueryC.cs`, `TestQueryHandler.cs`, `TestQueryHandlerAsync.cs`
  - Update their namespaces from `Paramore.Darker.Testing.Ports` **directly** to `Paramore.Darker.Core.Tests.Exported` (the post-Step-8 target — same files are touched again in Step 8). Keep them `public`.
  - Flip every type in `test/Paramore.Darker.Tests/TestDoubles/*.cs` from `public class` to `internal class`. Spec-time count: 12 source files containing 14 public class declarations (12 outer + 2 nested `Result` classes inside `AsyncTestQuery.cs` and `SyncTestQuery.cs`).
  - Update `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj`: replace the `<ProjectReference>` to `Paramore.Darker.Testing.Ports` with one to `..\Paramore.Darker.Tests\Paramore.Darker.Tests.csproj`, with `<PrivateAssets>all</PrivateAssets>` on the reference. (Step 8 will retarget the `Include` path to the renamed csproj.)
  - Drop the `<ProjectReference>` to `Paramore.Darker.Testing.Ports` from `test/Paramore.Darker.Tests/Paramore.Darker.Tests.csproj`.
  - Update all 9 `using Paramore.Darker.Testing.Ports;` statements to `using Paramore.Darker.Core.Tests.Exported;` (file list in ADR §7 inventory).
  - Verify: `dotnet build Darker.Filter.slnf -c Release` succeeds. Run `dotnet test test/Paramore.Darker.Tests.AOT/` and **confirm the test count matches AOT-only tests** (not AOT + Tests combined) — primary verification of the xunit-entry-assembly-only-discovery assumption; `<PrivateAssets>all</PrivateAssets>` is a belt-and-braces measure for transitive test-asset flow per ADR §10, not the load-bearing control. **If the count check fails** (xunit's discoverer turns out to scan Core.Tests' DLL after all), apply the §10 contingency: extract `Exported/` into a separate non-test `test/Paramore.Darker.Tests.Exported/Paramore.Darker.Tests.Exported.csproj` and have both Core.Tests and Tests.AOT `<ProjectReference>` it.
  - Confirm `Paramore.Darker.Tests` and `Paramore.Darker.Tests.AOT` pass — especially `When_AddHandlersFromAssemblies_*` and `QueryHandlerRegistryTests` (assembly-scan tests exercise the new visibility ring-fence). A regression here surfaces as `ConfigurationException("Registry already contains an entry...")` on duplicate handler binding — per ADR §10 "Why `internal`".

### Step 8: Split `Paramore.Darker.Tests` into `Paramore.Darker.Core.Tests` + `Paramore.Darker.Extensions.Tests`

- [x] **STRUCTURAL: Rename test project to `Paramore.Darker.Core.Tests`; create new `Paramore.Darker.Extensions.Tests`; move 4 `Integrations/` files**
  - **USE COMMAND**: `/tidy-first split Paramore.Darker.Tests into Core.Tests and Extensions.Tests mirroring Brighter`
  - Rename `test/Paramore.Darker.Tests/` → `test/Paramore.Darker.Core.Tests/`:
    - Folder name, csproj filename, `<AssemblyName>`, `<RootNamespace>` MSBuild properties.
    - `Exported/` and `TestDoubles/` sub-folders rename in place.
  - **Rewrite every `namespace Paramore.Darker.Tests` declaration** in the renamed project to `namespace Paramore.Darker.Core.Tests`. Spec-time count: 43 source files. Verify: `grep -rln 'namespace Paramore.Darker.Tests' test/Paramore.Darker.Core.Tests/` returns 0.
  - Verify no production `InternalsVisibleTo("Paramore.Darker.Tests")` attribute exists (`grep -rn InternalsVisibleTo src/`); if found, update to `"Paramore.Darker.Core.Tests"`.
  - Create `test/Paramore.Darker.Extensions.Tests/Paramore.Darker.Extensions.Tests.csproj` mirroring Brighter's `tests/Paramore.Brighter.Extensions.Tests/`. `<ProjectReference>` `Paramore.Darker.Extensions.DependencyInjection` (transitively pulls `Paramore.Darker`). Multi-target the same TFMs as Core.Tests. Default namespace `Paramore.Darker.Extensions.Tests`.
  - Move 4 files out of `test/Paramore.Darker.Core.Tests/Integrations/` into the new project (drop the `Integrations/` sub-folder — the project name carries the meaning):
    - `QueryProcessorIntegrationTests.cs`
    - `When_AddDefaultPolicies_called_should_register_policy_registry.cs`
    - `When_AddHandlersFromAssemblies_scans_assembly_should_register_both_sync_and_async_handlers.cs`
    - `When_AddJsonQueryLogging_called_should_register_serializer_settings.cs`
  - Rewrite their `namespace Paramore.Darker.Tests.Integrations` → `namespace Paramore.Darker.Extensions.Tests`.
  - Retarget `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj`'s `<ProjectReference>` (still has `<PrivateAssets>all</PrivateAssets>` from Step 7) to point at `..\Paramore.Darker.Core.Tests\Paramore.Darker.Core.Tests.csproj`.
  - Update `Darker.slnx` and `Darker.Filter.slnf`: drop `Paramore.Darker.Tests`, add `Paramore.Darker.Core.Tests` and `Paramore.Darker.Extensions.Tests`.
  - Consider adding inline comment `// IMPORTANT: ExportedTypes is load-bearing — see ADR 0011 §9-10` in `src/Paramore.Darker/QueryHandlerRegistry.cs` and `src/Paramore.Darker/QueryHandlerRegistryAsync.cs` per the ADR Risks-table mitigation ("during Step 8 implementation").
  - **Defensive grep for the two-step AOT csproj rewrite** (per ADR Negative Consequences — "easy to forget the second update"): `grep -n 'Paramore.Darker.Tests' test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj` should only show `Paramore.Darker.Core.Tests` `<ProjectReference>` lines (and the AOT csproj's own `Paramore.Darker.Tests.AOT` self-reference if any), never a stale unprefixed `Paramore.Darker.Tests` reference from Step 7.
  - Verify: `dotnet build Darker.Filter.slnf -c Release` succeeds. `dotnet test Darker.Filter.slnf -c Release --no-build` passes all three test projects (Core.Tests, Extensions.Tests, Tests.AOT).

### Step 9: Delete removed projects and empty folders; final solution-file cleanup

- [x] **STRUCTURAL: Delete `Paramore.Darker.Policies/`, `Paramore.Darker.QueryLogging/`, `Paramore.Darker.Testing.Ports/` folders + empty `Attributes/`/`Decorators/`; remove slnx/slnf entries; grep safety net**
  - **USE COMMAND**: `/tidy-first delete merged side-package folders and prune solution files`
  - Delete entire directories (folders + csproj + `bin/` + `obj/`):
    - `src/Paramore.Darker.Policies/`
    - `src/Paramore.Darker.QueryLogging/`
    - `test/Paramore.Darker.Testing.Ports/`
  - Delete the now-empty `src/Paramore.Darker/Attributes/` and `src/Paramore.Darker/Decorators/` folders.
  - Remove entries from `Darker.slnx` for the three deleted projects.
  - Remove entries from `Darker.Filter.slnf` (`projects` array): `src\\Paramore.Darker.Policies\\Paramore.Darker.Policies.csproj`, `src\\Paramore.Darker.QueryLogging\\Paramore.Darker.QueryLogging.csproj`, `test\\Paramore.Darker.Testing.Ports\\Paramore.Darker.Testing.Ports.csproj`.
  - **Grep safety net** (the authoritative check per ADR §5):
    - `grep -rln 'Paramore.Darker.Policies.csproj\|Paramore.Darker.QueryLogging.csproj\|Paramore.Darker.Testing.Ports.csproj' --include='*.csproj' .`
    - Remove the `<ProjectReference>` from every result. If the result set differs from ADR §5's enumeration (DI Extensions, SampleMinimalApi, SampleMauiTestApp post-move, the renamed Core.Tests, Tests.AOT), treat the grep as authoritative.
  - Verify: `dotnet build Darker.Filter.slnf -c Release` succeeds. On a workload-capable machine, `dotnet build Darker.slnx -c Release` succeeds (full solution with MAUI sample).

### Step 10: Run full test suite

- [ ] **STRUCTURAL: Run all three test projects and confirm green**
  - **USE COMMAND**: `/tidy-first verify full test suite passes after merge`
  - `dotnet test Darker.Filter.slnf -c Release --no-build` — Core.Tests + Extensions.Tests should pass.
  - `dotnet test test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj -c Release` — AOT project should pass on both `net8.0` and `net9.0`.
  - If AOT regressions surface against `Newtonsoft.Json` (reflection-heavy / trim-unfriendly paths), capture as a separate follow-up issue per the Constraints section in ADR §Forces — does not block this PR.

### Step 11: Update `<Description>` on core csproj

- [ ] **STRUCTURAL: Update `Paramore.Darker.csproj` `<Description>` to reflect the merged scope**
  - **USE COMMAND**: `/tidy-first update Paramore.Darker.csproj Description for merged scope`
  - Replace the current `<Description>Darker Query Processor</Description>` (or equivalent) in `src/Paramore.Darker/Paramore.Darker.csproj` with:
    > Darker is the query-side counterpart to Brighter, implementing the Query pattern (CQRS read-side) with a pipeline architecture for cross-cutting concerns including retry, fallback, and request logging.
  - Verify: `dotnet pack src/Paramore.Darker/Paramore.Darker.csproj -c Release -o /tmp/darker-pack` (or equivalent) produces a `.nupkg` whose embedded `.nuspec` shows the new description.

## Final validation

- [ ] **VERIFY: All acceptance criteria pass**
  - **AC1** — Build: `dotnet build Darker.Filter.slnf -c Release` succeeds.
  - **AC2** — Tests: `dotnet test Darker.Filter.slnf -c Release --no-build` passes (Core.Tests + Extensions.Tests).
  - **AC3** — AOT tests: `dotnet test test/Paramore.Darker.Tests.AOT/` passes on `net8.0` and `net9.0`; the test count is AOT-only (not AOT + Core.Tests).
  - **AC4** — Sample build: `dotnet run --project samples/SampleMinimalApi/SampleMinimalApi.csproj` starts cleanly; `GET /people` and `GET /people/{id}` return expected JSON.
  - **AC5** — Full solution: `dotnet build Darker.slnx -c Release` succeeds on a MAUI-workload-capable developer machine.
  - **AC6** — Source tree:
    - `src/Paramore.Darker.Policies/`, `src/Paramore.Darker.QueryLogging/`, `test/Paramore.Darker.Testing.Ports/` no longer exist.
    - `src/Paramore.Darker/Attributes/` and `src/Paramore.Darker/Decorators/` no longer exist.
    - `src/Paramore.Darker/Policies/{Attributes,Handlers}/` and `src/Paramore.Darker/Logging/{Attributes,Handlers}/` exist with expected files.
    - `src/Paramore.Darker/QueryHandlerAttribute.cs` and `QueryHandlerAttributeAsync.cs` at the root.
    - `samples/SampleMauiTestApp/` exists; `SampleMauiTestApp/` at repo root does not.
    - `test/Paramore.Darker.Core.Tests/Exported/` contains the 5 public test-double types; `test/Paramore.Darker.Core.Tests/TestDoubles/*.cs` are all `internal class`.
    - `test/Paramore.Darker.Extensions.Tests/` contains the 4 moved Integrations files.
  - **AC7** — Namespace sweep: `grep -rln 'namespace Paramore.Darker.Tests' test/Paramore.Darker.Core.Tests/` returns 0 results. `grep -rln 'Paramore.Darker.Policies.csproj\|Paramore.Darker.QueryLogging.csproj\|Paramore.Darker.Testing.Ports.csproj' --include='*.csproj' .` returns 0 results.
  - **AC8** — Solution files: `Darker.slnx` and `Darker.Filter.slnf` reference exactly the post-merge project set; no orphaned entries.
  - **AC9** — Core csproj `<Description>` matches the new wording from Step 11.

## Notes

- This is entirely structural — no TDD `/test-first` tasks because no new behaviour is introduced. The ADR explicitly states: "Tests do not drive this work because there is no new behaviour to test — the build green-light plus the existing test suite is the verification."
- Each step is its own commit (Tidy First). Reviewers can follow the sequencing in §Implementation Approach of the ADR.
- The grep safety net at Step 9 is authoritative — if it finds csprojs not enumerated in ADR §5, fix those too.
- The Step 7 `<PrivateAssets>all</PrivateAssets>` + xunit-entry-assembly-only-discoverer assumption is verified by the `dotnet test test/Paramore.Darker.Tests.AOT/` count check. If that check fails, apply the §10 contingency (extract `Exported/` into a separate non-test csproj) — do not skip the verification.
- AOT compatibility is **expected to be preserved, verified post-merge**. If `Newtonsoft.Json`'s reflection paths trigger AOT/trimming regressions, that becomes a separate follow-up issue per ADR §Constraints.
- Open follow-ups (out of scope for this spec, per ADR): rename `Paramore.Darker.Tests.AOT`; create `Paramore.Darker.Testing.Tests`; move `QueryProcessorBuilder.cs` out of core into `Paramore.Darker.Extensions.DependencyInjection` (ADR 10 §8 commitment that ADR 11 unblocks).
