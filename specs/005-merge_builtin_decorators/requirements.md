# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: #321

## Problem Statement

As a Darker maintainer, I would like Darker's **division of responsibilities across assemblies** to be aligned with Brighter's, so that the two libraries can be reasoned about as one system rather than two diverging trees.

Brighter (the more actively-developed sibling) has converged on a principle: **most features belong in the core assembly**. A side assembly is justified only when it provides a concrete implementation of a *generic interface* defined in core — for example, Brighter's `Gateways/*` (transport-specific implementations of `IAmAMessageProducer`/`IAmAMessageConsumer`) and `*Boxes` (broker-specific `Inbox`/`Outbox` providers). Those are the right shape for side assemblies because they pull in transport-specific dependencies that core consumers shouldn't have to take. They also slot into a generic interface in core, which keeps the dependency direction one-way.

Applying that principle to Darker:

- Darker has **no message-broker concerns**, no gateways, no `*Boxes` — it's a query-side library. The only thing in Darker that fits the "concrete implementation of a generic interface defined in core" pattern is **IoC container integration** (`Paramore.Darker.Extensions.DependencyInjection`), which adapts core's `IQueryHandlerFactory` / `IQueryHandlerDecoratorFactory` to `Microsoft.Extensions.DependencyInjection`. That assembly stays.
- A test-helpers library (`Paramore.Darker.Testing`, currently shipping `FakeQueryProcessor`) is a third production assembly that mirrors `Paramore.Brighter.Testing` (which ships `SpyCommandProcessor`). It stays.
- Everything else — policy decorators, request-logging decorators, the `FallbackPolicy` attribute, the `QueryHandlerAttribute` base class — has no generic-interface story justifying a side assembly. They should live in core.

The expected end-state for Darker's source layout is therefore:

- **`Paramore.Darker`** (core) — the framework itself, including all built-in decorators (`Policies/`, `Logging/`), all attribute base classes, and all cross-cutting concerns.
- **`Paramore.Darker.Extensions.DependencyInjection`** — the IoC adapter.
- **`Paramore.Darker.Testing`** — the test-helpers library (`FakeQueryProcessor`).

Paired with a test assembly per *primary subject under test* (mirroring how Brighter actually groups its tests — Brighter's `Core.Tests` references multiple production projects, so the partition is "what is this test primarily about", not "which single production assembly does this csproj reference"):

- **`Paramore.Darker.Core.Tests`** — covers `Paramore.Darker` as primary subject (renamed from the existing `Paramore.Darker.Tests`, mirroring Brighter's `Paramore.Brighter.Core.Tests`).
- **`Paramore.Darker.Extensions.Tests`** — covers `Paramore.Darker.Extensions.DependencyInjection` as primary subject (new project, mirroring Brighter's `Paramore.Brighter.Extensions.Tests`).
- (Future: `Paramore.Darker.Testing.Tests` to mirror Brighter's `Paramore.Brighter.Testing.Tests` — out of scope here.)

Today, Darker's actual layout diverges from this principle in five places:

1. `Paramore.Darker.Policies` — a side assembly that adds a `Polly` dependency and ships retry/fallback decorators. There is no generic-interface contract here; it's a built-in decorator that every Darker consumer pulls via the DI extensions anyway. Belongs in core.
2. `Paramore.Darker.QueryLogging` — a side assembly that adds a `Newtonsoft.Json` dependency and ships the request-logging decorator. Same story. Belongs in core.
3. `Paramore.Darker.Testing.Ports` — a tiny project whose only purpose is to provide *public* test doubles to the main and AOT test projects for assembly-scanning tests. It's not a Brighter-equivalent assembly; it's an internal arrangement. Belongs as an `Exported/` sub-folder inside the test project that uses it, with a `public`/`internal` visibility split on test doubles.
4. **`Paramore.Darker.Tests` is not split.** A single test project covers both `Paramore.Darker` and `Paramore.Darker.Extensions.DependencyInjection`. Brighter has separate `Paramore.Brighter.Core.Tests` and `Paramore.Brighter.Extensions.Tests` — though note Brighter's `Core.Tests` does in fact `<ProjectReference>` multiple production projects including the DI Extensions, so the convention is "group tests by primary subject under test", not "exclusively reference one production assembly". Splitting Darker the same way gives readers a clear file-organisation signal about which production-assembly contract a given test is primarily asserting against.
5. **In-core layout drift.** `src/Paramore.Darker/Attributes/` and `src/Paramore.Darker/Decorators/` are root-level folders that don't match Brighter's per-feature `Policies/{Attributes,Handlers}` and `Logging/{Attributes,Handlers}` layout. `QueryHandlerAttribute*` is in a sub-folder when Brighter's equivalent `RequestHandlerAttribute` is at the root namespace. The `Logging` namespace already exists (`ApplicationLogging.cs`) but is underpopulated. These are physical-layout details, but together they make Darker's source tree harder to navigate for anyone who knows Brighter.

This V5 work aligns all five at once. The path also clears for issue #320 (`Pass QueryContext into QueryProcessor`), which requires `IPolicyRegistry<string>` to be a constructor parameter on `QueryProcessor` and therefore that core depends directly on `Polly`.

## Proposed Solution

1. **Merge the two side packages into core**: move the source trees of `Paramore.Darker.Policies` and `Paramore.Darker.QueryLogging` into `src/Paramore.Darker/`, deleting the side projects.
2. **Restructure folder and namespace layout to mirror Brighter**:
   - Split each feature folder into `Attributes/` and `Handlers/` sub-folders/sub-namespaces.
   - Rename the `QueryLogging` folder/namespace to `Logging` (folder mirror only — type names keep their `QueryLogging` prefix to mirror Brighter's `RequestLogging*` convention and avoid `Microsoft.Extensions.Logging` collisions).
   - Relocate the existing in-core `FallbackPolicy*` attribute and decorator out of the root-level `Attributes/` and `Decorators/` folders and into the new `Policies/Attributes/` and `Policies/Handlers/` folders.
   - Relocate the `QueryHandlerAttribute` / `QueryHandlerAttributeAsync` base classes from `src/Paramore.Darker/Attributes/` to the root of `src/Paramore.Darker/` (namespace `Paramore.Darker`), mirroring Brighter's `RequestHandlerAttribute.cs` at the root of `src/Paramore.Brighter/`.
   - Once empty, delete the root-level `src/Paramore.Darker/Attributes/` and `src/Paramore.Darker/Decorators/` folders entirely.
3. **Move `SampleMauiTestApp` under `samples/`**: relocate from `SampleMauiTestApp/` (repository root) to `samples/SampleMauiTestApp/`, so the samples folder picks up both samples consistently and `Darker.slnx` references both via the `samples/` folder. The MAUI sample stays *out* of `Darker.Filter.slnf` (CI cannot build MAUI workloads), but its source must be updated so the full-solution `Darker.slnx` build remains green for developers who do have the MAUI workload.
4. **Fold `Paramore.Darker.Testing.Ports` into the test project's `Exported/` folder** with a `public`/`internal` split on test doubles (FR11–FR13 below).
5. **Split `Paramore.Darker.Tests` into `Paramore.Darker.Core.Tests` and `Paramore.Darker.Extensions.Tests`** — one test assembly per production assembly, mirroring Brighter (FR14 below).
6. **Add `Newtonsoft.Json` as a direct dependency of `Paramore.Darker`** (Polly is already direct). Versions stay in `Directory.Packages.props`.
7. **Update every internal `using` statement and `ProjectReference`** that points at either side package or at the moved core namespaces, so the solution and filter both build green.

From a consumer's perspective:

- A reference to `Paramore.Darker` is sufficient to use `[QueryLogging]`, `[RetryableQuery]`, `[FallbackPolicy]`, `AddDefaultPolicies()`, `AddPolicies(...)`, and `AddJsonQueryLogging(...)`.
- The `Paramore.Darker.Policies` and `Paramore.Darker.QueryLogging` NuGet packages are no longer published.
- `using` statements break and must be updated per the mapping table below (V5 is permitted to break the API).

### Namespace Mapping

| V4 Namespace                    | V5 Namespace                              | Types affected                                                          |
|---------------------------------|-------------------------------------------|--------------------------------------------------------------------------|
| `Paramore.Darker.Policies` (side package) | `Paramore.Darker.Policies`             | `Constants`, `QueryProcessorBuilderExtensions` (parent-level utilities)  |
| `Paramore.Darker.Policies` (side package) | `Paramore.Darker.Policies.Attributes`  | `RetryableQueryAttribute`, `RetryableQueryAttributeAsync`                |
| `Paramore.Darker.Policies` (side package) | `Paramore.Darker.Policies.Handlers`    | `RetryableQueryDecorator`, `RetryableQueryDecoratorAsync`                |
| `Paramore.Darker.QueryLogging` (side package) | `Paramore.Darker.Logging`           | `Constants`, `QueryProcessorBuilderExtensions` (parent-level utilities; sit alongside the existing `ApplicationLogging.cs`) |
| `Paramore.Darker.QueryLogging` (side package) | `Paramore.Darker.Logging.Attributes`| `QueryLoggingAttribute`, `QueryLoggingAttributeAsync`                    |
| `Paramore.Darker.QueryLogging` (side package) | `Paramore.Darker.Logging.Handlers`  | `QueryLoggingDecorator`, `QueryLoggingDecoratorAsync`                    |
| `Paramore.Darker.Attributes` (existing core) | `Paramore.Darker`                    | `QueryHandlerAttribute`, `QueryHandlerAttributeAsync` (base classes, mirror Brighter's `RequestHandlerAttribute` at the root namespace) |
| `Paramore.Darker.Attributes` (existing core) | `Paramore.Darker.Policies.Attributes`| `FallbackPolicyAttribute`, `FallbackPolicyAttributeAsync`                 |
| `Paramore.Darker.Decorators` (existing core) | `Paramore.Darker.Policies.Handlers`  | `FallbackPolicyDecorator`, `FallbackPolicyDecoratorAsync`                 |
| `Paramore.Darker.Logging` (existing core, contains `ApplicationLogging`) | `Paramore.Darker.Logging` | `ApplicationLogging` — unchanged location; just acknowledging it pre-exists so the new `Constants`/`QueryProcessorBuilderExtensions` will sit beside it.

Type names (`QueryLoggingAttribute`, `RetryableQueryAttribute`, `QueryLoggingDecorator`, `FallbackPolicyAttribute`, `FallbackPolicyDecorator`, `QueryHandlerAttribute`, etc.) and the DI extension method names (`AddJsonQueryLogging`, `AddPolicies`, `AddDefaultPolicies`) do **not** change — only the namespaces and folder paths do.

The `QueryLogging` prefix on type names is preserved deliberately:

- **It mirrors Brighter.** Brighter's equivalents are `RequestLoggingAttribute`, `RequestLoggingHandler`, etc. — the *request kind* prefixes the role. Darker's request kind is "Query", so `QueryLoggingAttribute` is the direct analogue of Brighter's `RequestLoggingAttribute`. Keeping this naming convention is part of "mirror Brighter", not a departure from it.
- **It avoids name collisions.** `Logging` is a heavily overloaded word in .NET (`Microsoft.Extensions.Logging`, `Microsoft.Extensions.Logging.ILogger`, `ILoggerFactory`, project-local `Logging` helpers, etc.). A bare `LoggingAttribute` / `LoggingDecorator` would be ambiguous to readers and to IDE auto-imports. `QueryLoggingAttribute` is unambiguous — it can only mean one thing in a Darker pipeline.
- **The folder/namespace name (`Logging`) is contextualised by its parent (`Paramore.Darker.Logging.*`)**, so the collision concern does not apply at the namespace level the way it does at the type level.

## Requirements

### Functional Requirements

- **FR1 — Merge Policies sources into core, split by role, and absorb existing in-core `FallbackPolicy*` types.** The end state is:
  - `src/Paramore.Darker/Policies/Attributes/` (namespace `Paramore.Darker.Policies.Attributes`) contains:
    - `RetryableQueryAttribute.cs`, `RetryableQueryAttributeAsync.cs` (moved from the side package)
    - `FallbackPolicyAttribute.cs`, `FallbackPolicyAttributeAsync.cs` (moved from existing `src/Paramore.Darker/Attributes/`)
  - `src/Paramore.Darker/Policies/Handlers/` (namespace `Paramore.Darker.Policies.Handlers`) contains:
    - `RetryableQueryDecorator.cs`, `RetryableQueryDecoratorAsync.cs` (moved from the side package)
    - `FallbackPolicyDecorator.cs`, `FallbackPolicyDecoratorAsync.cs` (moved from existing `src/Paramore.Darker/Decorators/`)
  - `src/Paramore.Darker/Policies/` (namespace `Paramore.Darker.Policies`) contains `Constants.cs`, `QueryProcessorBuilderExtensions.cs` (moved from the side package).

- **FR2 — Merge QueryLogging sources into core, split by role, and rename folder/namespace to `Logging`.** The end state is:
  - `src/Paramore.Darker/Logging/Attributes/` (namespace `Paramore.Darker.Logging.Attributes`) — `QueryLoggingAttribute.cs`, `QueryLoggingAttributeAsync.cs`.
  - `src/Paramore.Darker/Logging/Handlers/` (namespace `Paramore.Darker.Logging.Handlers`) — `QueryLoggingDecorator.cs`, `QueryLoggingDecoratorAsync.cs`.
  - `src/Paramore.Darker/Logging/` (namespace `Paramore.Darker.Logging`) — `Constants.cs`, `QueryProcessorBuilderExtensions.cs`, sitting alongside the pre-existing `ApplicationLogging.cs` (which keeps its current namespace `Paramore.Darker.Logging`).

- **FR3 — Relocate `QueryHandlerAttribute*` base classes to the root.** The current `src/Paramore.Darker/Attributes/QueryHandlerAttribute.cs` and `QueryHandlerAttributeAsync.cs` move to `src/Paramore.Darker/QueryHandlerAttribute.cs` and `QueryHandlerAttributeAsync.cs`, with namespace `Paramore.Darker` (root). This mirrors Brighter's `RequestHandlerAttribute.cs` (namespace `Paramore.Brighter`).

- **FR4 — Delete the now-empty `Attributes/` and `Decorators/` folders.** After FR1 and FR3 complete, `src/Paramore.Darker/Attributes/` and `src/Paramore.Darker/Decorators/` are empty and must be removed.

- **FR5 — Move `SampleMauiTestApp` under `samples/`.** Move the project directory from `SampleMauiTestApp/` to `samples/SampleMauiTestApp/`. Update `Darker.slnx` so the project path reflects the new location. Update all source files in the moved sample so they compile against the new namespaces (FR8 inventory). The MAUI sample remains excluded from `Darker.Filter.slnf` (CI does not build MAUI workloads), but the full-solution build (`Darker.slnx`) must remain green for developers with the MAUI workload installed.

- **FR6 — Add `Newtonsoft.Json` as a direct dependency of `Paramore.Darker`.** `Paramore.Darker.csproj` declares both `Polly` (already present) and `Newtonsoft.Json` as direct `PackageReference`s; versions remain centrally managed via `Directory.Packages.props`.

- **FR7 — Remove the side projects.** `src/Paramore.Darker.Policies/` and `src/Paramore.Darker.QueryLogging/` are deleted from disk (including their `bin/` and `obj/` artefacts). The corresponding entries are removed from `Darker.slnx` and `Darker.Filter.slnf`. Every remaining `<ProjectReference>` that points at either side project is removed. The list of csproj files holding such references at spec time is:
  - `src/Paramore.Darker.Extensions.DependencyInjection/Paramore.Darker.Extensions.DependencyInjection.csproj`
  - `samples/SampleMinimalApi/SampleMinimalApi.csproj`
  - `samples/SampleMauiTestApp/SampleMauiTestApp.csproj` (post-FR5 path)
  - `test/Paramore.Darker.Tests/Paramore.Darker.Tests.csproj` (post-FR14 path: `test/Paramore.Darker.Core.Tests/Paramore.Darker.Core.Tests.csproj`, plus the new `test/Paramore.Darker.Extensions.Tests/Paramore.Darker.Extensions.Tests.csproj`)
  - `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj`

  At task-time, re-run `grep -rln 'Paramore.Darker.Policies.csproj\|Paramore.Darker.QueryLogging.csproj' --include='*.csproj' .` and treat the result as authoritative — if it differs from this list, the grep wins. (Two review rounds turned up missed csproj entries in this inventory; the grep is the safety net.)

- **FR8 — Update all internal `using` statements.** The canonical list of files that import one of `Paramore.Darker.Policies`, `Paramore.Darker.QueryLogging`, `Paramore.Darker.Attributes`, or `Paramore.Darker.Decorators` (and so need updating) is:

  *Source projects:*
  - `src/Paramore.Darker/Builder/QueryProcessorBuilder.cs` (uses `FallbackPolicyAttribute*` → new namespace)
  - `src/Paramore.Darker/PipelineBuilder.cs` (uses `QueryHandlerAttribute*` → root namespace)
  - `src/Paramore.Darker.Extensions.DependencyInjection/PolicyDIExtensions.cs`
  - `src/Paramore.Darker.Extensions.DependencyInjection/QueryLoggingDIExtensions.cs`
  - `src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionDecoratorRegistry.cs` (uses `QueryHandlerAttribute*`)

  *Samples:*
  - `samples/SampleMinimalApi/DarkerSettings.cs`
  - `samples/SampleMinimalApi/Program.cs`
  - `samples/SampleMinimalApi/QueryHandlers/GetPeopleQueryHandler.cs`
  - `samples/SampleMinimalApi/QueryHandlers/GetPersonQueryHandler.cs`
  - `samples/SampleMauiTestApp/MauiProgram.cs` (post-FR5 path)
  - `samples/SampleMauiTestApp/DarkerSettings.cs` (post-FR5 path)
  - `samples/SampleMauiTestApp/QueryHandlers/GetPeopleQueryHandler.cs` (post-FR5 path)
  - `samples/SampleMauiTestApp/QueryHandlers/GetPersonQueryHandler.cs` (post-FR5 path)

  *Note on test project paths:* all `test/Paramore.Darker.Tests/...` paths below are the **pre-FR14** locations. After FR14 these files live under `test/Paramore.Darker.Core.Tests/...` (renamed) — except the four `Integrations/` files, which move to `test/Paramore.Darker.Extensions.Tests/`. The task list will perform the rename + split in a separate step from the `using` updates.

  *Tests — files importing the side namespaces (Policies/QueryLogging):*
  - `test/Paramore.Darker.Tests.AOT/Base/AOTTestClassBase.cs`
  - `test/Paramore.Darker.Tests/Decorators/When_query_logging_attribute_used_on_sync_and_async_handlers_should_apply_correct_decorator.cs`
  - `test/Paramore.Darker.Tests/Decorators/When_retryable_query_attribute_used_on_sync_and_async_handlers_should_apply_correct_decorator.cs`
  - `test/Paramore.Darker.Tests/Integrations/When_AddDefaultPolicies_called_should_register_policy_registry.cs` (moves to Extensions.Tests per FR14)
  - `test/Paramore.Darker.Tests/Integrations/When_AddJsonQueryLogging_called_should_register_serializer_settings.cs` (moves to Extensions.Tests per FR14)
  - `test/Paramore.Darker.Tests/TestDoubles/AsyncRetryableQueryHandler.cs`
  - `test/Paramore.Darker.Tests/TestDoubles/LoggingQueryHandler.cs`
  - `test/Paramore.Darker.Tests/TestDoubles/RetryableQueryHandler.cs`
  - `test/Paramore.Darker.Tests/When_logging_decorator_executes_should_use_injected_serializer_settings.cs`
  - `test/Paramore.Darker.Tests/When_logging_decorator_executes_without_settings_should_throw_ConfigurationException.cs`
  - `test/Paramore.Darker.Tests/When_policy_decorator_executes_without_policies_should_throw_ConfigurationException.cs`
  - `test/Paramore.Darker.Tests/When_query_processor_built_with_policy_registry_should_set_policies_on_context.cs`
  - `test/Paramore.Darker.Tests/When_retryable_decorator_executes_should_read_policy_from_context_policies.cs`

  *Tests — files importing existing-core moving namespaces (Attributes/Decorators):*
  - `test/Paramore.Darker.Tests/Decorators/FallbackPolicyTests.cs`
  - `test/Paramore.Darker.Tests/PipelineBuilderExceptionTests.cs`
  - `test/Paramore.Darker.Tests/TestDoubles/AsyncHandlerWithFallback.cs`
  - `test/Paramore.Darker.Tests/TestDoubles/AsyncHandlerWithSyncAttribute.cs`
  - `test/Paramore.Darker.Tests/TestDoubles/SyncHandlerWithAsyncAttribute.cs`
  - `test/Paramore.Darker.Tests/TestDoubles/SyncHandlerWithFallback.cs`
  - `test/Paramore.Darker.Tests/When_async_query_executed_should_resolve_from_async_registry_and_build_async_decorator_chain.cs`
  - `test/Paramore.Darker.Tests/When_query_processor_has_both_sync_and_async_handlers_should_dispatch_to_correct_pipeline.cs`
  - `test/Paramore.Darker.Tests/When_sync_attribute_on_async_handler_should_throw_configuration_exception.cs`
  - `test/Paramore.Darker.Tests/When_sync_query_executed_should_resolve_from_sync_registry_and_build_sync_decorator_chain.cs`

  *Files importing `Paramore.Darker.Testing.Ports` (need updating to `Paramore.Darker.Core.Tests.Exported` per FR13):*
  - `test/Paramore.Darker.Tests/QueryHandlerRegistryTests.cs`
  - `test/Paramore.Darker.Tests/FakeQueryProcessorTests.cs`
  - `test/Paramore.Darker.Tests/QueryProcessorTests.cs`
  - `test/Paramore.Darker.Tests/QueryProcessorAsyncTests.cs`
  - `test/Paramore.Darker.Tests/When_QueryProcessorBuilder_builds_processor_should_configure_both_sync_and_async.cs`
  - `test/Paramore.Darker.Tests/Integrations/QueryProcessorIntegrationTests.cs` (moves to Extensions.Tests per FR14)
  - `test/Paramore.Darker.Tests/Integrations/When_AddHandlersFromAssemblies_scans_assembly_should_register_both_sync_and_async_handlers.cs` (moves to Extensions.Tests per FR14)
  - `test/Paramore.Darker.Tests.AOT/Base/AOTTestClassBase.cs`
  - `test/Paramore.Darker.Tests.AOT/QueryProcessor/AOTQueryProcessorTests.cs`

  *Projects spot-checked and confirmed unaffected by the Policies/QueryLogging/Attributes/Decorators moves (no current imports of any moving namespace):*
  - `src/Paramore.Darker.Testing/`
  - `test/Paramore.Darker.Benchmarks/`
  - `test/Paramore.Test.Helpers/`

- **FR9 — Preserve type names and signatures.** Public type names (`QueryLoggingAttribute`, `RetryableQueryAttribute`, `FallbackPolicyAttribute`, `QueryHandlerAttribute`, the four decorator classes, plus async siblings) and DI extension method names (`AddJsonQueryLogging`, `AddPolicies`, `AddDefaultPolicies`) are unchanged. Only the namespaces are reorganised per the mapping above.

- **FR10 — Preserve the configured pipeline.** Decorator resolution from attributes, step ordering, and the `IPolicyRegistry<string>` / `JsonSerializerSettings` configuration entry points all behave exactly as before.

- **FR11 — Merge `Paramore.Darker.Testing.Ports` into the renamed test project's `Exported/` folder.** The five source files in `test/Paramore.Darker.Testing.Ports/` (`TestQueryA.cs`, `TestQueryB.cs`, `TestQueryC.cs`, `TestQueryHandler.cs`, `TestQueryHandlerAsync.cs`) move to `test/Paramore.Darker.Core.Tests/Exported/` (post-FR14 path). They stay `public` (they're the only handlers the assembly-scanning tests should see). Their namespace changes from `Paramore.Darker.Testing.Ports` to `Paramore.Darker.Core.Tests.Exported` (i.e. tracks the renamed assembly per FR14). The `test/Paramore.Darker.Testing.Ports/` directory (csproj + sources + `bin/`/`obj/`) is deleted; its entry is removed from `Darker.slnx` and `Darker.Filter.slnf`.

- **FR12 — Make local `TestDoubles` types `internal`.** Every type in `test/Paramore.Darker.Tests/TestDoubles/*.cs` flips from `public class` to `internal class` (at spec time: 12 source files containing 14 public class declarations — 12 outer classes plus 2 nested `Result` classes inside `AsyncTestQuery.cs` and `SyncTestQuery.cs`). This prevents them appearing in `Assembly.ExportedTypes` — the property `QueryHandlerRegistry.RegisterFromAssemblies` already uses — so they will not be auto-registered by tests that scan the test assembly. (No code change is needed in `QueryHandlerRegistry.cs` or `QueryHandlerRegistryAsync.cs`; the existing `ExportedTypes`-based scan is already public-only. This requirement formalises that behaviour as load-bearing for the visibility split.)

- **FR13 — Update the AOT test project's reference.** `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj` currently `<ProjectReference>`s `Paramore.Darker.Testing.Ports`. It changes to `<ProjectReference>` the (post-FR14-rename) `test/Paramore.Darker.Core.Tests/Paramore.Darker.Core.Tests.csproj`, since that is where the `Exported/` folder lives. The reference is declared with `<PrivateAssets>all</PrivateAssets>` to stop the reference's xunit/test-discoverer build assets from flowing transitively through the AOT project. Note: `<PrivateAssets>all</PrivateAssets>` does not prevent `Paramore.Darker.Core.Tests.dll` from being copied to the AOT project's `bin/` output — the load-bearing mechanism against xunit double-discovering Core.Tests' tests under the AOT runner is xunit's discoverer scanning only the entry assembly (`Paramore.Darker.Tests.AOT.dll`), not every DLL in the output directory. The Step 7 build check (run `dotnet test test/Paramore.Darker.Tests.AOT/` and confirm the test count matches AOT-only tests, not AOT + Core.Tests combined) is the primary verification. If the verification fails, the contingency action (documented in ADR §10) is to extract `Exported/` into a separate non-test `test/Paramore.Darker.Tests.Exported/Paramore.Darker.Tests.Exported.csproj` and have both Core.Tests and Tests.AOT `<ProjectReference>` it. All `using Paramore.Darker.Testing.Ports;` statements across the repo (9 files at spec time, listed in FR8) update to `using Paramore.Darker.Core.Tests.Exported;`.

- **FR14 — Split the main test project into one assembly per production assembly.** Mirroring Brighter's `Paramore.Brighter.Core.Tests` / `Paramore.Brighter.Extensions.Tests` naming (Brighter's actual `Core.Tests` references multiple production projects including DI Extensions — the convention is "group tests by primary subject under test", not "exclusively reference one production assembly"). We adopt the same grouping:

  - **Rename** `test/Paramore.Darker.Tests/` → `test/Paramore.Darker.Core.Tests/` (folder name, csproj filename, `AssemblyName`, `RootNamespace`). Then **rewrite every `namespace Paramore.Darker.Tests` declaration** in the renamed project's source files to `namespace Paramore.Darker.Core.Tests` (43 source files at spec time, including the `Exported/` and `TestDoubles/` sub-folders — verify with `grep -rln 'namespace Paramore.Darker.Tests' test/Paramore.Darker.Core.Tests/` returning 0 after the rewrite). The renamed assembly holds the tests that exercise `Paramore.Darker` (the core assembly) — decorators, pipeline, registries, attribute discovery, `FallbackPolicy`, `QueryHandler*`, `QueryLogging`, `RetryableQuery`. The `Exported/` folder (post-FR11) lives here with namespace `Paramore.Darker.Core.Tests.Exported`.
  - **Create** `test/Paramore.Darker.Extensions.Tests/Paramore.Darker.Extensions.Tests.csproj` as a new test project, targeting `net8.0;net9.0` (or whatever the existing test project targets). This holds the tests that exercise `Paramore.Darker.Extensions.DependencyInjection` — the four files currently in `test/Paramore.Darker.Tests/Integrations/`:
    - `QueryProcessorIntegrationTests.cs`
    - `When_AddDefaultPolicies_called_should_register_policy_registry.cs`
    - `When_AddHandlersFromAssemblies_scans_assembly_should_register_both_sync_and_async_handlers.cs`
    - `When_AddJsonQueryLogging_called_should_register_serializer_settings.cs`
  - Move those four files to the new project. Update their `using`s if needed.
  - Update `Darker.slnx` and `Darker.Filter.slnf` to reflect the rename + new project.
  - Update `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj` per FR13 (reference the post-rename `Paramore.Darker.Core.Tests`).
  - `Paramore.Darker.Tests.AOT` is **not renamed** in this spec (it's a special-purpose AOT harness; renaming it to `Paramore.Darker.Extensions.Tests.AOT` or similar is left as a possible follow-up).
  - `Paramore.Darker.Testing.Tests` is **not created** in this spec (would mirror `Paramore.Brighter.Testing.Tests`); deferred to a follow-up. The `Paramore.Darker.Testing` assembly stays as-is.

### Non-functional Requirements

- **NFR1 — No behavioural regressions.** Existing tests in `Paramore.Darker.Tests` and `Paramore.Darker.Tests.AOT` continue to pass after their `using` statements are updated to the new namespaces. Test *logic* (Arrange/Act/Assert) does not change.
- **NFR2 — AOT compatibility.** `Paramore.Darker.Tests.AOT` continues to build and pass on `net8.0` and `net9.0`. The AOT base class already exercises `AddJsonQueryLogging` and `AddDefaultPolicies`, so the merged core's reflection-heavy paths (Newtonsoft.Json serialisation, attribute discovery) are covered.
- **NFR3 — Multi-target support unchanged.** `Paramore.Darker` continues to target `netstandard2.0;net8.0;net9.0`.
- **NFR4 — Central package management preserved.** New `PackageReference` entries in core do not introduce hard-coded versions; versions stay in `Directory.Packages.props`.
- **NFR5 — Package metadata accurate.** The `Description` on `Paramore.Darker.csproj` reflects that the package now includes the policy and logging decorators (proposed wording belongs in the ADR for review).

### Constraints and Assumptions

- This is a **V5 breaking change**: package layout AND namespaces change. The `Paramore.Darker.Policies` and `Paramore.Darker.QueryLogging` NuGet packages cease to publish, and consumer `using` statements break. Consumers upgrading from V4 to V5 reference `Paramore.Darker` only and update their imports per the mapping table above.
- **Type names and method signatures are preserved.** Only namespaces and physical locations are reorganised; type/method/attribute names do not change.
- We follow Brighter's folder and namespace layout precisely: `src/Paramore.Darker/Policies/{Attributes,Handlers}`, `src/Paramore.Darker/Logging/{Attributes,Handlers}`, with `QueryHandlerAttribute*` at the root (mirroring `RequestHandlerAttribute` at the root of `src/Paramore.Brighter/`). Top-level helpers (`Constants`, `QueryProcessorBuilderExtensions`) sit at the parent namespace, matching how Brighter places `ApplicationLogging.cs` directly under `Logging/` (a placement that Darker also already uses for its own `ApplicationLogging.cs`).
- `Polly` is already a direct dependency of core today (added during ADR 10's `IPolicyRegistry<string>` work). `Newtonsoft.Json` is the only new direct package reference; downstream consumers already pulled it transitively when they used `Paramore.Darker.QueryLogging`.
- The MAUI sample is intentionally excluded from `Darker.Filter.slnf` (CI cannot install MAUI workloads); only the full-solution `Darker.slnx` build verifies the MAUI source compiles. This is the status quo and is preserved.

### Out of Scope

- Changing `[RetryableQuery]`, `[QueryLogging]`, or `[FallbackPolicy]` semantics or any other decorator behaviour.
- Updating the `QueryProcessor` constructor or `IQueryContext` interface — those are issue #320 and have already been delivered on the `merge-decorators` branch's parent commits.
- Migrating away from `Newtonsoft.Json` or `Polly` to alternatives.
- Adding new policies, decorators, or features.
- Documentation rewrites beyond what is needed to reflect the new package and namespace layout (a brief V5 migration note in the changelog is in scope; full doc rewrites are not).
- Source-package compatibility shims (e.g. empty `Paramore.Darker.Policies` / `Paramore.Darker.QueryLogging` packages that type-forward). Not planned for V5.
- Changing the implementation of `QueryHandlerRegistry.RegisterFromAssemblies` or `QueryHandlerRegistryAsync.RegisterFromAssemblies`. Both already use `Assembly.ExportedTypes`, which returns public types only — exactly the filter the Testing.Ports merge depends on. FR12 formalises this existing behaviour as load-bearing; no `.cs` change to either registry class is required.

## Acceptance Criteria

- `dotnet build Darker.Filter.slnf -c Release` succeeds with both side projects removed and no project references to them remaining.
- `dotnet build Darker.slnx -c Release` (full solution including the relocated `samples/SampleMauiTestApp`) succeeds on a machine with the MAUI workload installed.
- `dotnet test Darker.Filter.slnf -c Release --no-build` passes for `Paramore.Darker.Tests` and `Paramore.Darker.Tests.AOT` after FR8's `using` statements are updated to the new namespaces. No test logic changes.
- A consumer can use `[QueryLogging]`, `[RetryableQuery]`, `[FallbackPolicy]`, `[QueryHandler]`, `AddDefaultPolicies()`, `AddPolicies(IPolicyRegistry<string>)`, and `AddJsonQueryLogging(...)` after referencing only `Paramore.Darker` and `Paramore.Darker.Extensions.DependencyInjection`, with imports drawn from the V5 namespaces (`Paramore.Darker`, `Paramore.Darker.Policies.Attributes`, `Paramore.Darker.Policies.Handlers`, `Paramore.Darker.Logging.Attributes`, `Paramore.Darker.Logging.Handlers`).
- `Darker.slnx` and `Darker.Filter.slnf` no longer list the removed projects; `Darker.slnx` shows `samples/SampleMauiTestApp/SampleMauiTestApp.csproj` (new path) and not `SampleMauiTestApp/SampleMauiTestApp.csproj` (old root-level path).
- The `samples/SampleMinimalApi` project builds and runs against the merged core, with logging and policy decorators wired up via the existing DI extensions (its imports updated to the new namespaces).
- The relocated `samples/SampleMauiTestApp` project builds (where the MAUI workload is available) with its imports updated to the new namespaces and its old `<ProjectReference>` entries removed.
- `src/Paramore.Darker/Attributes/` and `src/Paramore.Darker/Decorators/` no longer exist.
- `test/Paramore.Darker.Testing.Ports/` no longer exists; the five test-double types live at `test/Paramore.Darker.Core.Tests/Exported/` under namespace `Paramore.Darker.Core.Tests.Exported` and remain `public`.
- Every type in `test/Paramore.Darker.Core.Tests/TestDoubles/*.cs` is `internal`. The `Paramore.Darker.Core.Tests` assembly scan via `Assembly.ExportedTypes` returns exactly the five `Exported/` types (and any other public test fixtures that may exist), not the 12 local TestDoubles.
- `grep -rln 'namespace Paramore.Darker.Tests' test/Paramore.Darker.Core.Tests/` returns zero results — every `namespace` declaration in the renamed test project uses the new prefix.
- `dotnet test test/Paramore.Darker.Tests.AOT/` reports a test count consistent with AOT-only tests; the Core.Tests test count is not duplicated under the AOT runner (verifies the `<PrivateAssets>all</PrivateAssets>` directive per FR13).
- `test/Paramore.Darker.Tests/` no longer exists; its sources have been split between `test/Paramore.Darker.Core.Tests/` (Core-focused tests, including the `Exported/` and `TestDoubles/` folders) and `test/Paramore.Darker.Extensions.Tests/` (the four files that exercise `Paramore.Darker.Extensions.DependencyInjection` from the former `Integrations/` folder).
- `Paramore.Darker.Extensions.Tests` has a `<ProjectReference>` to `Paramore.Darker.Extensions.DependencyInjection` and tests its DI registration surface.
- `Darker.slnx` and `Darker.Filter.slnf` list `Paramore.Darker.Core.Tests` and `Paramore.Darker.Extensions.Tests` and no longer list `Paramore.Darker.Tests`.
- Spot check on the new layout confirms:
  - `Paramore.Darker.Policies.Attributes.RetryableQueryAttribute` exists in `Paramore.Darker.dll`.
  - `Paramore.Darker.Policies.Attributes.FallbackPolicyAttribute` exists in `Paramore.Darker.dll`.
  - `Paramore.Darker.Policies.Handlers.RetryableQueryDecorator` exists in `Paramore.Darker.dll`.
  - `Paramore.Darker.Policies.Handlers.FallbackPolicyDecorator` exists in `Paramore.Darker.dll`.
  - `Paramore.Darker.Logging.Attributes.QueryLoggingAttribute` exists in `Paramore.Darker.dll`.
  - `Paramore.Darker.Logging.Handlers.QueryLoggingDecorator` exists in `Paramore.Darker.dll`.
  - `Paramore.Darker.QueryHandlerAttribute` exists at the root namespace.
  - The V4 type names (`Paramore.Darker.Policies.RetryableQueryAttribute`, `Paramore.Darker.QueryLogging.QueryLoggingAttribute`, `Paramore.Darker.Attributes.FallbackPolicyAttribute`, `Paramore.Darker.Attributes.QueryHandlerAttribute`, `Paramore.Darker.Decorators.FallbackPolicyDecorator`) no longer exist in the V5 build.

## Additional Context

- **Brighter reference layout** (`../Brighter/src/Paramore.Brighter/`):
  - `RequestHandlerAttribute.cs` at the root, namespace `Paramore.Brighter`.
  - `Policies/Attributes/` — `FallbackPolicyAttribute`, `UsePolicyAttribute`, `TimeoutPolicyAttribute`, `UseResiliencePipelineAttribute`, plus async siblings.
  - `Policies/Handlers/` — `ExceptionPolicyHandler`, `FallbackPolicyHandler`, `TimeoutPolicyHandler`, `ResilienceExceptionPolicyHandler`, plus async siblings.
  - `Logging/Attributes/` — `RequestLoggingAttribute`, `RequestLoggingAsyncAttribute`.
  - `Logging/Handlers/` — `RequestLoggingHandler`, `RequestLoggingHandlerAsync`.
  - `Logging/ApplicationLogging.cs` — sits at parent namespace alongside the `Attributes/`/`Handlers/` folders.
  - Core csproj declares `Polly`, `Newtonsoft.Json`, `Microsoft.Extensions.Logging`, `OpenTelemetry`, `NJsonSchema` as direct `PackageReference`s.

- **Current Darker shape (V4)**:
  - Side projects: `Paramore.Darker.Policies` (ships `RetryableQuery*`, depends on `Polly`), `Paramore.Darker.QueryLogging` (ships `QueryLogging*`, depends on `Newtonsoft.Json`).
  - In-core, non-Brighter-shaped folders: `src/Paramore.Darker/Attributes/` (holds `QueryHandlerAttribute*` and `FallbackPolicyAttribute*`), `src/Paramore.Darker/Decorators/` (holds `FallbackPolicyDecorator*`), `src/Paramore.Darker/Logging/` (holds only `ApplicationLogging.cs` today).
  - `Paramore.Darker.Extensions.DependencyInjection` project-references both side projects and exposes `AddPolicies`, `AddDefaultPolicies`, `AddJsonQueryLogging`.

- **Branch**: `merge-decorators` (already created; ADR 10's work is on parent commits per the `git log` shown when the spec was created).
- **Next ADR**: `docs/adr/0011-merge-builtin-decorators.md`.
