# Tasks: Swap Newtonsoft.Json for System.Text.Json in the Query Logging Decorator

**Spec**: 006-json_serializer
**ADR**: [0012-json-serializer-swap.md](../../docs/adr/0012-json-serializer-swap.md)
**Requirements**: [requirements.md](requirements.md)
**Issue**: [#294](https://github.com/BrighterCommand/Darker/issues/294)

## Overview

This work has two intertwined strands:

1. **Behavioural swap.** Replace `Newtonsoft.Json` with `System.Text.Json` in the query logging decorator, introduce the static `QueryLoggingJsonOptions` (mirroring Brighter's `JsonSerialisationOptions`), change the `AddJsonQueryLogging` / `JsonQueryLogging` callback shape to `Action<JsonSerializerOptions>`, remove the DI singleton registration, and suppress `IL2026` / `IL3050` at the decorator's call site with documented justification. Default options carry `ReferenceHandler.IgnoreCycles` (FR3).

2. **Test infrastructure upgrade (FR12).** Upgrade `xunit` 2.x → `xunit.v3` across the four xunit-referencing test projects so that `IAssemblyFixture<LoggerCaptureFixture>` becomes available (FR10's pinned log-capture mechanism). This is structural prep with a known API-break inventory; it ships in the same PR as the behavioural swap because FR10 depends on it.

Use `/tidy-first <change>` for structural-only tasks (xunit upgrade, package add/remove, file moves with no behaviour change). Use `/test-first <behaviour>` for every behavioural task — write the test, **STOP for IDE approval**, then implement. Each TEST + IMPLEMENT task below names the exact `/test-first` invocation; do not write tests manually.

## Pre-flight

- [x] **STRUCTURAL: First ADR commit on `feature/json-serializer`** — done in commit `87e123e` (ADR 0012 + full spec dir staged with explicit paths; `specs/006-json_serializer/README.md` also included to keep the spec dir complete; `git log --oneline master..feature/json-serializer` shows it as the only commit; tree clean except untracked `docs/.DS_Store`).
  - At the time this task is run, `feature/json-serializer` is 0 commits ahead of `master` (verify with `git log --oneline master..feature/json-serializer` returns empty). All spec artefacts are currently untracked.
  - Stage and commit the **post-design-review** ADR (status `Accepted` at `docs/adr/0012-json-serializer-swap.md`, with the Decision-step amendments from the design review applied) plus the complete spec directory. Enumerate explicitly:
    - `docs/adr/0012-json-serializer-swap.md`
    - `specs/006-json_serializer/requirements.md`
    - `specs/006-json_serializer/tasks.md` (this file)
    - `specs/006-json_serializer/review-requirements.md`
    - `specs/006-json_serializer/review-design.md`
    - `specs/006-json_serializer/review-tasks.md`
    - `specs/006-json_serializer/.adr-list`
    - `specs/006-json_serializer/.issue-number`
    - `specs/006-json_serializer/.requirements-approved`
    - `specs/006-json_serializer/.design-approved`
    - `specs/.current-spec` (modified to point at `006-json_serializer`)
  - Use `git add` with explicit paths (not `git add .` or `git add -A`) so the commit's scope is auditable.
  - Commit message: `docs: add ADR 0012 — swap Newtonsoft.Json for System.Text.Json in query logging (#294)`
  - Verify: `git log --oneline master..feature/json-serializer` shows the new commit as the first (and only, at this point) commit on the branch. `git status` is clean.

## Tasks

### Step 1: Upgrade `xunit` 2.x → `xunit.v3` across the four test projects (FR12)

This step is **structural-only** — the test code's behaviour is unchanged, only the framework reference, namespace usings, and one piece of reflection are touched. The behavioural-test work in later steps assumes `IAssemblyFixture<T>` is available, which is what this step unlocks.

- [x] **STRUCTURAL: Replace `xunit` 2.9.3 pin with `xunit.v3` in `Directory.Packages.props`** — Chosen: `xunit.v3 3.2.2` (highest stable from `dotnet package search`, no pre/rc/alpha/beta). `xunit.analyzers` bump decision: **not bumped** — `dotnet restore Darker.Filter.slnf` succeeded with 1.27.0, no `NU1605`/`NU1107`/v3-compat warning.
  - **USE COMMAND**: `/tidy-first replace xunit 2.x pin with xunit.v3 in central package management`
  - **Pin the `xunit.v3` version at task-execution time** (not at spec-authoring time — the spec was written 2026-06-01; by the time the task runs the latest stable may differ): run `dotnet package search xunit.v3 --exact-match --source https://api.nuget.org/v3/index.json` (or `dotnet nuget list versions xunit.v3 --source https://api.nuget.org/v3/index.json` with the CLI in use), pick the highest stable version without `-pre`/`-rc`/`-alpha`/`-beta` suffix, and **record the chosen version in this checkbox before commit** (e.g. "Chosen: `xunit.v3 1.2.3`").
  - Replace `<PackageVersion Include="xunit" Version="2.9.3" />` with `<PackageVersion Include="xunit.v3" Version="<recorded version>" />` in `Directory.Packages.props`.
  - **No rename** for `xunit.runner.visualstudio` (already 3.1.5 — supports v3).
  - **`xunit.analyzers` bump criterion**: leave the existing `xunit.analyzers` pin in place. Bump only if `dotnet restore Darker.Filter.slnf` reports an analyzer-vs-xunit.v3 incompatibility error (`NU1605`, `NU1107`, or `xunit.analyzers` warns of an unsupported runtime). "Required" = `restore` fails or the analyzer surfaces a v3-compat warning; nothing else. Record the bump decision in this checkbox.
  - Verify: `dotnet restore Darker.Filter.slnf` succeeds with no missing-package errors; no other package id changes in `Directory.Packages.props`.

- [x] **STRUCTURAL: Repoint the four xunit-referencing test csprojs to `xunit.v3`** — all four updated (Core.Tests, Extensions.Tests, Tests.AOT, Test.Helpers); Benchmarks untouched; `dotnet restore Darker.Filter.slnf` clean.
  - **USE COMMAND**: `/tidy-first repoint test csprojs from xunit to xunit.v3`
  - Update `<PackageReference Include="xunit" />` to `<PackageReference Include="xunit.v3" />` in exactly:
    - `test/Paramore.Darker.Core.Tests/Paramore.Darker.Core.Tests.csproj`
    - `test/Paramore.Darker.Extensions.Tests/Paramore.Darker.Extensions.Tests.csproj`
    - `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj`
    - `test/Paramore.Test.Helpers/Paramore.Test.Helpers.csproj`
  - `test/Paramore.Darker.Benchmarks/Paramore.Darker.Benchmarks.csproj` is **not** touched (no xunit reference).
  - Verify: `dotnet restore Darker.Filter.slnf` succeeds with no missing-package errors.

- [x] **STRUCTURAL: Drop `using Xunit.Abstractions;` (v3 moves `ITestOutputHelper` into `Xunit`)** — 4 pure-`ITestOutputHelper` files updated here (AOTQueryProcessorTests, AOTTestClassBase, CoreTestOutputHelper, ICoreTestOutputHelper); the 2 `ITest`-bearing files (`TestClassBase.cs`, `ITestClassBase.cs`) had their using swap folded into the `IXunitTest` rework sub-task below (their `ITest` reference is inseparable from it). Build green.
  - **USE COMMAND**: `/tidy-first drop Xunit.Abstractions usings after xunit.v3 upgrade`
  - Per FR12 item 1, remove `using Xunit.Abstractions;` and ensure each file has `using Xunit;` in exactly these 6 files:
    - `test/Paramore.Test.Helpers/TestOutput/ICoreTestOutputHelper.cs`
    - `test/Paramore.Test.Helpers/TestOutput/CoreTestOutputHelper.cs`
    - `test/Paramore.Test.Helpers/Base/ITestClassBase.cs`
    - `test/Paramore.Test.Helpers/Base/TestClassBase.cs`
    - `test/Paramore.Darker.Tests.AOT/Base/AOTTestClassBase.cs`
    - `test/Paramore.Darker.Tests.AOT/QueryProcessor/AOTQueryProcessorTests.cs`
  - Verify: `dotnet build Darker.Filter.slnf -c Release` succeeds (no `CS0234` for `Xunit.Abstractions`).

- [x] **STRUCTURAL: Update `IAsyncLifetime` return types to `ValueTask` (if any usages exist)** — no usages (`grep -rn IAsyncLifetime test/` → empty); vacuously satisfied.
  - **USE COMMAND**: `/tidy-first update IAsyncLifetime return types to ValueTask for xunit.v3`
  - Per FR12 item 2: grep for `IAsyncLifetime` across `test/**`; for each implementer, change `InitializeAsync()` / `DisposeAsync()` return types from `Task` to `ValueTask`.
  - If grep returns no hits, mark this task complete with the note "no usages — task vacuously satisfied".
  - Verify: `dotnet build Darker.Filter.slnf -c Release` succeeds.

- [x] **TEST + IMPLEMENT: Replace `TestOutputHelper` private-`test`-field reflection in `TestClassBase.cs` (FR12 item 6)** — **Option (a)** chosen and pinned: `TestContext.Current.Test` is available inside the running test, so `XunitTest` returns a non-null `Xunit.v3.IXunitTest` and `TestQualifiedName` uses `IXunitTest.TestDisplayName` (the v2 `.DisplayName` member is gone). Test approved in IDE (RED→GREEN); private-field reflection + its `IL2075` suppression removed; new test passes on net8.0 & net9.0.
  - **USE COMMAND**: `/test-first when TestClassBase XunitTest accessed under xunit v3 should return non null IXunitTest from TestContext`
  - Test location: `test/Paramore.Test.Helpers/Tests/` (create if it does not exist — this is a behavioural pin on `TestClassBase`'s public surface)
  - Test file: `When_TestClassBase_XunitTest_accessed_under_xunit_v3_should_return_non_null_IXunitTest.cs`
  - Test should verify:
    - A concrete test class deriving from `TestClassBase<T>` is instantiated under xunit.v3.
    - Its `XunitTest` property returns a non-null `IXunitTest` (option (a) — the preferred path).
    - `TestQualifiedName` returns the test method name as produced by `TestContext.Current.Test`, not the type-name fallback.
    - Justification: the existing v2 reflection access (`testOutputHelper.GetType().GetField("test", BindingFlags.Instance | BindingFlags.NonPublic)`) returns `null` under xunit.v3 because the private field's name/shape changed; without the rework, `XunitTest` silently becomes `null` and `TestQualifiedName` falls back to the type name — a behaviour change masquerading as a `/tidy-first` move. This is **behavioural**, so it gets `/test-first`.
    - **Fallback path (option (b))**: if at implementation time it is discovered that `TestContext.Current.Test` is not available in the surrounding execution context (e.g. the calling code is outside a test method), explicitly opt for option (b) per FR12 item 6: accept `null` from `XunitTest` and let `TestQualifiedName` drop to `typeof(T).GetLoggerCategoryName()`. **If choosing option (b), the test above changes**: assert `XunitTest` is `null` and `TestQualifiedName` equals the type-name form, with a one-line `// FR12 item 6 option (b): TestContext.Current.Test not available; accepting null fallback` comment at the property. Pick one option at implementation time and pin the test to the chosen behaviour.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - In `test/Paramore.Test.Helpers/Base/TestClassBase.cs`, replace the `testOutputHelper.GetType().GetField("test", BindingFlags.Instance | BindingFlags.NonPublic)` access (around lines 107–110) with `TestContext.Current.Test`. Update the cast/type on the `XunitTest` property (around line 49) from v2 `ITest` to v3 `IXunitTest`.
    - If option (b) is chosen, leave the property null-coalescing through to the type-name fallback and pin the trade-off with a one-line comment as described in the test.
  - Verify: `dotnet test test/Paramore.Darker.Tests.AOT/ -c Release --no-build` passes; the new behavioural test passes; no other test in the helpers test project regresses.

- [x] **STRUCTURAL: Full test-suite green on xunit.v3 (no behaviour-change baseline) + FR12 items 3/4/5 audit** — `dotnet build Darker.Filter.slnf -c Release` succeeds; `dotnet test` green on net8.0 & net9.0 (Core 69, Extensions 8, AOT 6, Test.Helpers 1 — the +1 is the new FR12-item-6 test; no other count delta). Audit re-run: item 3 = 58 public test classes (was 57; +1 from the new test class — left `public`, migration out of scope); item 4 = 0 `[Theory]`/`[InlineData]` (vacuous); item 5 = 0 `Xunit.Sdk` references (vacuous).
  - **USE COMMAND**: `/tidy-first verify full test-suite green on xunit.v3 and audit FR12 items 3 4 5 as no action required`
  - Verify: `dotnet build Darker.Filter.slnf -c Release` succeeds. `dotnet test Darker.Filter.slnf -c Release --no-build` passes with the same test count as on `master` (modulo the xunit.v3 discovery changes — investigate any count delta before continuing).
  - **FR12 items 3, 4, 5 audit** — explicitly mark each as no-action-required for this PR, with current evidence (verified 2026-06-01 against `master`-equivalent state; re-run before commit to confirm):
    - **FR12 item 3 — `[Fact]` / `[Theory]` method visibility convention (`internal` over `public`)**: 57 public test classes exist across `test/` (`grep -rcE "public (sealed |partial )?class" test/ --include="*.cs"` → 57). Requirements FR12 explicitly state "`public` continues to work" and label this a convention shift, not a hard break. **Decision**: leave test classes `public` for this PR; visibility migration is out of scope and can be a follow-up tidy if desired.
    - **FR12 item 4 — `[Theory]` + `[InlineData]` semantics + fixture types unchanged**: zero `[Theory]` or `[InlineData]` usage in the entire test tree (`grep -rn "\[Theory\]\|\[InlineData\]" test/ --include="*.cs"` → empty). Nothing to migrate; the semantics-unchanged statement is vacuous in this repo.
    - **FR12 item 5 — `Xunit.Sdk` types referenced by analysers/runners**: zero `Xunit.Sdk` references (`grep -rn "Xunit\.Sdk" test/` → empty). No custom analyser/runner extensions exist. Vacuous no-op.
  - The audit is recorded here (not in a separate task) because items 3/4/5 produce no code change. Re-running the three greps at task-execution time confirms the audit is still valid before commit.

### Step 2: Add `System.Text.Json` package and `QueryLoggingJsonOptions` (FR2, FR3, FR6, FR14)

- [x] **STRUCTURAL: Add `System.Text.Json` `PackageReference` and `PackageVersion` (FR6)** — `System.Text.Json 10.0.8` pinned in CPM (aligned with `Microsoft.Extensions.*` family) and referenced in `Paramore.Darker.csproj`; builds on `netstandard2.0;net8.0;net9.0`.
  - **USE COMMAND**: `/tidy-first add System.Text.Json direct dependency to Paramore.Darker.csproj`
  - Add `<PackageVersion Include="System.Text.Json" Version="10.0.8" />` to `Directory.Packages.props`, aligned with the existing `Microsoft.Extensions.*` 10.0.8 family.
  - Add `<PackageReference Include="System.Text.Json" />` (no version attribute — CPM) to `src/Paramore.Darker/Paramore.Darker.csproj`. Apply uniformly across all TFMs (`netstandard2.0;net8.0;net9.0`).
  - Verify: `dotnet build src/Paramore.Darker/Paramore.Darker.csproj -c Release` succeeds for all TFMs.

- [x] **TEST + IMPLEMENT: `QueryLoggingJsonOptions.Options` default is non-null and has `ReferenceHandler.IgnoreCycles` (FR3, FR14)** — test approved (RED→GREEN); created `src/Paramore.Darker/Logging/QueryLoggingJsonOptions.cs` (default instance with `IgnoreCycles`, no `Serialize` in class-init). Added `[CollectionDefinition("QueryLoggingJsonOptions", DisableParallelization = true)]`. Setter left as auto-property here; null-guard driven by the next test.
  - **USE COMMAND**: `/test-first when QueryLoggingJsonOptions accessed without configuration should expose default options with ReferenceHandler IgnoreCycles`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `Logging/When_QueryLoggingJsonOptions_accessed_without_configuration_should_expose_default_options_with_ReferenceHandler_IgnoreCycles.cs`
  - Test should verify:
    - In a fresh process, `QueryLoggingJsonOptions.Options` is not null.
    - `QueryLoggingJsonOptions.Options.ReferenceHandler` equals `ReferenceHandler.IgnoreCycles`.
    - Class-init does NOT lock the options (assert by setting any settable property, e.g. `QueryLoggingJsonOptions.Options.MaxDepth = 32;`, and observing no throw — this verifies FR14 too).
    - Test runs inside a `[Collection(...)]` declared `DisableParallelization = true` and a try/finally save-and-restore (per C5) so it does not leak the `MaxDepth` mutation into other tests.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Create `src/Paramore.Darker/Logging/QueryLoggingJsonOptions.cs` with the shape pinned in ADR §"Key Components" (`public static class`, backing field initialised to `new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles }`, settable `Options` property).
    - No call to `JsonSerializer.Serialize` in the class initialiser (FR14 — class-init must not lock).

- [x] **TEST + IMPLEMENT: `QueryLoggingJsonOptions.Options` setter throws `ArgumentNullException` on `null` (FR2)** — test approved (RED→GREEN); replaced the auto-property with backing field + `value ?? throw new ArgumentNullException(nameof(value))`. `ParamName == "value"`; prior instance survives a failed set.
  - **USE COMMAND**: `/test-first when QueryLoggingJsonOptions Options setter receives null should throw ArgumentNullException`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `Logging/When_QueryLoggingJsonOptions_Options_setter_receives_null_should_throw_ArgumentNullException.cs`
  - Test should verify:
    - `QueryLoggingJsonOptions.Options = null;` throws `ArgumentNullException`.
    - Exception's `ParamName` is `"value"`.
    - After the throw, `QueryLoggingJsonOptions.Options` retains its prior non-null value (no partial-state corruption).
    - Test save-and-restores `Options` via try/finally so failure does not leak.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - In `src/Paramore.Darker/Logging/QueryLoggingJsonOptions.cs`, the `Options` setter throws `new ArgumentNullException(nameof(value))` when assigned `null`.

- [x] **TEST + IMPLEMENT: Direct assignment to `QueryLoggingJsonOptions.Options` replaces the instance and drops `IgnoreCycles` (ADR Decision step 7)** — GREEN-on-arrival contract guard (no production code, per spec): `ShouldBeSameAs(fresh)` + `ReferenceHandler.ShouldBeNull()`. Protects against a future defensive setter that auto-applies `IgnoreCycles`.
  - **USE COMMAND**: `/test-first when QueryLoggingJsonOptions Options directly assigned a new instance should return that instance and drop ReferenceHandler default`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `Logging/When_QueryLoggingJsonOptions_Options_directly_assigned_should_replace_instance_and_drop_IgnoreCycles_default.cs`
  - Test should verify:
    - Save the prior `Options` reference in try/finally.
    - Assign `var fresh = new JsonSerializerOptions(); QueryLoggingJsonOptions.Options = fresh;` — assignment succeeds (no throw).
    - `ReferenceEquals(QueryLoggingJsonOptions.Options, fresh)` is `true` — the setter replaces the instance, does not merge or defensively copy.
    - `QueryLoggingJsonOptions.Options.ReferenceHandler` is `null` (STJ default) — NOT `ReferenceHandler.IgnoreCycles`. This pins the supported-but-lossy contract: direct assignment drops the FR3 default exactly as ADR Decision step 7 documents.
    - Pin the contract: this is the "you own all the defaults" path; consumers must re-apply `IgnoreCycles` themselves if they want it. The test exists to prevent a future "defensive" setter refactor (that auto-applies `IgnoreCycles` to assigned instances) from going un-noticed — the test would fail if such a refactor lands.
    - Test runs in the same `[Collection(...)]` as the FR2 null-guard test (both mutate `Options`) with `DisableParallelization = true`.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - No new production code — the setter behaviour is the trivial `_options = value ?? throw …` from the FR2 task. This test pins the contract against future "helpful" setter refactors.

### Step 3: Add `LoggerCaptureFixture` infrastructure (FR10 mechanism)

- [ ] **STRUCTURAL: Add `LoggerCaptureFixture` + `CapturingLoggerProvider` for FR10 / FR11 log-capture**
  - **USE COMMAND**: `/tidy-first add LoggerCaptureFixture and CapturingLoggerProvider for query logging tests`
  - Add `test/Paramore.Darker.Core.Tests/Logging/LoggerCaptureFixture.cs` implementing `IAssemblyFixture<LoggerCaptureFixture>` per FR10:
    - Constructor saves `var previous = ApplicationLogging.LoggerFactory;` and replaces it with `new LoggerFactory(new[] { new CapturingLoggerProvider(buffer) })`.
    - `Dispose()` restores `ApplicationLogging.LoggerFactory = previous;`.
    - Exposes `Clear()` and `IReadOnlyList<CapturedLogEntry> CapturedLogs` (or thread-local equivalent) so individual tests assert in isolation.
  - Add `CapturingLoggerProvider` + `CapturingLogger` + `CapturedLogEntry` record alongside the fixture. `CapturedLogEntry` retains:
    - `LogLevel`
    - The **message template** (the `{OriginalFormat}` value from the state KVP collection — required by Steps 4/5 with-fallback tests to pin the runtime-concatenation pattern per FR9)
    - The **rendered message** (state.ToString() — what a structured sink sees)
    - The **structured argument KVP collection** (the full `IReadOnlyList<KeyValuePair<string, object>>` from `ILogger.Log`'s `state` parameter) — required to pull `{QueryName}` and `{Query}` individually
    - The `Exception` (if any)
  - **Install-before-touch guard (FR10)**: the fixture must expose a `CapturedLogs.ShouldNotBeEmpty()` precondition helper that tests call **before** asserting on log content. Empty captured logs indicate the install ran after a `QueryLoggingDecorator<,>` closed generic had already cached its `static readonly Logger` field against the default `ApplicationLogging.LoggerFactory` (a no-provider factory — see `src/Paramore.Darker/Logging/ApplicationLogging.cs:7`), and tests would silently pass against a no-op logger. The precondition surfaces this hazard as a test failure rather than a false positive.
  - Add an `Extensions.Tests` mirror: `test/Paramore.Darker.Extensions.Tests/Logging/LoggerCaptureFixture.cs` and supporting types. The two fixtures live in disjoint assemblies and serve disjoint closed generics per FR10's cross-assembly discipline.
  - Verify: `dotnet build Darker.Filter.slnf -c Release` succeeds.

- [ ] **STRUCTURAL: Add disjoint test query types `CoreLoggingTestQuery` and `ExtensionsLoggingTestQuery` (FR10 discipline)**
  - **USE COMMAND**: `/tidy-first add disjoint logging test query types per FR10 cross-assembly discipline`
  - Add `test/Paramore.Darker.Core.Tests/TestDoubles/CoreLoggingTestQuery.cs` — minimal `internal sealed class CoreLoggingTestQuery : IQuery<CoreLoggingTestQuery.Result>` with a couple of public properties (`Guid Id`, `string Name`) so STJ output is observable; nested `Result` class.
  - Add a matching `CoreLoggingTestQueryHandler` (async + sync as needed) in `test/Paramore.Darker.Core.Tests/TestDoubles/`.
  - Add `test/Paramore.Darker.Extensions.Tests/TestDoubles/ExtensionsLoggingTestQuery.cs` and handler — same shape, distinct namespace/type.
  - Verify: `dotnet build Darker.Filter.slnf -c Release` succeeds. The two closed generics `QueryLoggingDecorator<CoreLoggingTestQuery,…>` and `QueryLoggingDecorator<ExtensionsLoggingTestQuery,…>` are now compileable and serve as disjoint cache cells per FR10.

### Step 4: Rewrite the sync query logging decorator (FR1, FR8, FR9, FR13)

- [ ] **TEST + IMPLEMENT: Sync decorator serialises the query with `QueryLoggingJsonOptions.Options` (FR1, FR9)**
  - **USE COMMAND**: `/test-first when sync logging decorator executes should log query body as System Text Json output`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `Logging/When_sync_logging_decorator_executes_should_log_query_body_as_System_Text_Json_output.cs`
  - Test should verify:
    - Test class takes `IAssemblyFixture<LoggerCaptureFixture>`; `[Fact]` calls `fixture.Clear()` at the start.
    - **Install-before-touch precondition** (Step 3 fixture invariant): after executing the query, assert `fixture.CapturedLogs.ShouldNotBeEmpty()` BEFORE asserting on log content. An empty buffer means the fixture's `LoggerFactory` install ran after the closed generic's `static readonly Logger` field had already cached the default no-provider factory — the install-before-touch ordering broke and the test would silently pass against a no-op logger.
    - Arrange: `QueryLoggingJsonOptions.Options.WriteIndented = false;` in a try/finally that restores the prior value (per C5).
    - Build a `QueryProcessor` (real `QueryHandlerRegistry`, `SimpleHandlerFactory`, `SimpleHandlerDecoratorFactory`, `InMemoryDecoratorRegistry`, `InMemoryQueryContextFactory`) wired for `CoreLoggingTestQuery` with the `[QueryLogging]` attribute on the handler.
    - Act: call `Execute(new CoreLoggingTestQuery(...))`.
    - Assert the captured `LogInformation` entries on the start template `"Executing query {QueryName}: {Query}"` (per FR9 sync-start) — the `{QueryName}` arg equals `nameof(CoreLoggingTestQuery)`, the `{Query}` arg equals `JsonSerializer.Serialize(theQueryInstance, QueryLoggingJsonOptions.Options)`.
    - Assert the completion template `"Execution of query {QueryName} completed in {Elapsed}ms"` with `{QueryName}` matching.
    - The decorator's constructor takes **no** serialiser parameter.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - In `src/Paramore.Darker/Logging/Handlers/QueryLoggingDecorator.cs`:
      - Remove the constructor parameter (FR8) and the `using Newtonsoft.Json;` (FR5).
      - Replace `Serialize<T>` body with `JsonSerializer.Serialize(value, QueryLoggingJsonOptions.Options)`.
      - Delete the `ConfigurationException("No serializer settings are configured…")` path (FR8).
      - Preserve the existing start/completion log templates verbatim per FR9 (do not refactor the `" (with fallback)"` runtime concatenation at line 42).
    - Add the FR13 `UnconditionalSuppressMessage` pair (`IL2026`, `IL3050`) on the `Serialize<T>` method with the justifications pinned in FR13 / the ADR.
    - **Caller-propagation contingency (ADR Decision step 3)**: if AOT publish in Step 9 surfaces `IL2026` / `IL3050` warnings at the *caller* of `Serialize<T>` (i.e. inside `Execute<TQuery>`), expand the suppressions to `Execute<TQuery>` with the same `Justification`. This is implementation-time discovery, not a spec violation — the FR13 allow-list expands to match. Do not pre-emptively add the attributes; only add them if the analyser warns.

- [ ] **TEST + IMPLEMENT: Sync decorator emits the with-fallback completion template when fallback fired (FR9)**
  - **USE COMMAND**: `/test-first when sync logging decorator completes after fallback should append with fallback suffix`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `Logging/When_sync_logging_decorator_completes_after_fallback_should_append_with_fallback_suffix.cs`
  - Test should verify:
    - Test class uses `LoggerCaptureFixture` via `IAssemblyFixture<…>`.
    - Pipeline composed so the decorator's `Execute` runs with `withFallback == " (with fallback)"` (build pipeline + handler that triggers the fallback path).
    - **Assert the message TEMPLATE, not the rendered message** — pull `{OriginalFormat}` from the captured log entry's state KVP collection (the `IReadOnlyList<KeyValuePair<string, object>>` that `ILogger.Log` receives) and assert it equals the literal `"Execution of query {QueryName} completed in {Elapsed}ms (with fallback)"`. This pins the runtime-concatenation pattern (FR9): a future refactor that replaces `+ withFallback` with a structured placeholder `{Fallback}` would produce the same *rendered* message but a different `{OriginalFormat}` — the assertion catches that refactor and fails. Asserting only the rendered message would silently accept a structured-placeholder refactor that FR9 declares out of scope.
    - `CapturedLogEntry` (from Step 3) must expose the `{OriginalFormat}` string for this assertion to be possible — verify it does before writing the test.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - No new production code beyond Step 4's first task — the with-fallback path already exists; this test pins FR9's preservation of the runtime-concatenated suffix (asserted against `{OriginalFormat}`) so the subsequent file rewrite does not refactor it away.

### Step 5: Rewrite the async query logging decorator (FR1, FR8, FR9, FR13)

- [ ] **TEST + IMPLEMENT: Async decorator serialises the query with `QueryLoggingJsonOptions.Options` (FR1, FR9)**
  - **USE COMMAND**: `/test-first when async logging decorator executes should log query body as System Text Json output`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `Logging/When_async_logging_decorator_executes_should_log_query_body_as_System_Text_Json_output.cs`
  - Test should verify:
    - Same arrangement as the sync variant but uses `QueryHandlerRegistryAsync` + `ExecuteAsync` against an async handler decorated with `[QueryLogging]` for `CoreLoggingTestQuery`.
    - Captured templates are the **async** forms: start `"Executing async query {QueryName}: {Query}"` (FR9 async-start) and completion `"Async execution of query {QueryName} completed in {Elapsed}ms"` (FR9 async-completion).
    - `{Query}` arg equals `JsonSerializer.Serialize(theQueryInstance, QueryLoggingJsonOptions.Options)`.
    - Constructor takes no serialiser parameter.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - In `src/Paramore.Darker/Logging/Handlers/QueryLoggingDecoratorAsync.cs`:
      - Mirror the sync rewrite — drop constructor parameter, drop `using Newtonsoft.Json;`, drop `ConfigurationException` path, change `Serialize<T>` body to STJ + `QueryLoggingJsonOptions.Options`, add the FR13 `UnconditionalSuppressMessage` pair.
      - Preserve async templates and the `" (with fallback)"` runtime-concatenation behaviour.
      - **Caller-propagation contingency (ADR Decision step 3)**: if AOT publish in Step 9 surfaces `IL2026` / `IL3050` warnings at `ExecuteAsync<TQuery>`, expand the suppressions to `ExecuteAsync<TQuery>` with the same `Justification` — same rule as the sync decorator.

- [ ] **TEST + IMPLEMENT: Async decorator emits the with-fallback completion template when fallback fired (FR9)**
  - **USE COMMAND**: `/test-first when async logging decorator completes after fallback should append with fallback suffix`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `Logging/When_async_logging_decorator_completes_after_fallback_should_append_with_fallback_suffix.cs`
  - Test should verify:
    - Async equivalent of Step 4's fallback test — same `{OriginalFormat}` assertion discipline.
    - Pull `{OriginalFormat}` from the captured entry's state KVP collection and assert it equals the literal `"Async execution of query {QueryName} completed in {Elapsed}ms (with fallback)"` (FR9 async-completion-with-fallback). Asserting against the message template (not the rendered message) is the load-bearing assertion — see Step 4's with-fallback test for the rationale.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - No new production code beyond Step 5's first task.

### Step 6: Update extension methods — callback signature, single call-site, no DI singleton (FR4, FR5, FR8)

- [ ] **TEST + IMPLEMENT: `AddJsonQueryLogging` callback mutates `QueryLoggingJsonOptions.Options` (FR4) — rewrite of the existing extensions test**
  - **USE COMMAND**: `/test-first when AddJsonQueryLogging called should configure JsonSerializerOptions on QueryLoggingJsonOptions`
  - Test location: `test/Paramore.Darker.Extensions.Tests`
  - Test file: `Logging/When_AddJsonQueryLogging_called_should_configure_json_options.cs` (renamed/rewritten from the existing `When_AddJsonQueryLogging_called_should_register_serializer_settings.cs` per FR10)
  - Test should verify:
    - Calling `services.AddDarker().AddJsonQueryLogging(o => o.WriteIndented = true);` causes `QueryLoggingJsonOptions.Options.WriteIndented` to equal `true` after the call returns.
    - The test save-and-restores `QueryLoggingJsonOptions.Options` in try/finally to keep test isolation per C5.
    - A second `[Fact]` is the rewritten smoke test: handler decorated with `[QueryLogging]` for `ExtensionsLoggingTestQuery` executes successfully, captured `{Query}` log argument equals `JsonSerializer.Serialize(query, QueryLoggingJsonOptions.Options)` (i.e. STJ output, not Newtonsoft output). Test uses the Extensions-side `LoggerCaptureFixture`.
    - No assertion remains that the DI container resolves a `JsonSerializerSettings` singleton (FR8 removed the registration).
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - **Delete** the old file `test/Paramore.Darker.Extensions.Tests/When_AddJsonQueryLogging_called_should_register_serializer_settings.cs` (the file replaces it; the renames are a deletion + new file, not a `git mv` in-place edit, so the new file lands at `Logging/When_AddJsonQueryLogging_called_should_configure_json_options.cs`).
    - In `src/Paramore.Darker/Logging/QueryProcessorBuilderExtensions.cs`:
      - Change the canonical method signature to `TBuilder AddJsonQueryLogging<TBuilder>(this TBuilder builder, Action<JsonSerializerOptions> configure = null) where TBuilder : IQueryProcessorExtensionBuilder` (FR4 canonical site).
      - Body: `configure?.Invoke(QueryLoggingJsonOptions.Options);` then register `QueryLoggingDecorator<,>` and `QueryLoggingDecoratorAsync<,>` with the builder.
      - Change the `JsonQueryLogging(IBuildTheQueryProcessor, Action<JsonSerializerOptions>?)` method to forward to the canonical generic (cast to `QueryProcessorBuilder`).
      - Remove `using Newtonsoft.Json;` and any `JsonSerializerSettings` references.
    - In `src/Paramore.Darker.Extensions.DependencyInjection/QueryLoggingDIExtensions.cs`:
      - Change the method signature on `AddJsonQueryLogging(this IDarkerHandlerBuilder builder, Action<JsonSerializerOptions> configure = null)`.
      - Body: forward to `Paramore.Darker.Logging.QueryProcessorBuilderExtensions.AddJsonQueryLogging<IDarkerHandlerBuilder>(builder, configure)`. No decorator registration here; no `services.AddSingleton(settings)` (FR8).
      - Remove `using Newtonsoft.Json;` and any `JsonSerializerSettings` references.

- [ ] **TEST + IMPLEMENT: `JsonQueryLogging(IBuildTheQueryProcessor, …)` throws `NotSupportedException` for custom builders (ADR Decision step 5)**
  - **USE COMMAND**: `/test-first when JsonQueryLogging called on custom IBuildTheQueryProcessor implementation should throw NotSupportedException`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `Logging/When_JsonQueryLogging_called_on_custom_builder_should_throw_NotSupportedException.cs`
  - Test should verify:
    - Define a minimal test double `CustomQueryProcessorBuilder : IBuildTheQueryProcessor` in `test/Paramore.Darker.Core.Tests/TestDoubles/` whose only member is a stub `IQueryProcessor Build()` returning `null!`. This double is **not** a `QueryProcessorBuilder`.
    - Calling `customBuilder.JsonQueryLogging(o => o.WriteIndented = true);` throws `NotSupportedException`.
    - The exception message references `QueryProcessorBuilder` (so consumers understand the constraint) — matches the precedent at `Policies/QueryProcessorBuilderExtensions.cs:11-14`.
    - **This is a documented limitation of the builder surface** (ADR Decision step 5): the cast is by design, not a bug. The test exists so a future refactor that "fixes" the cast by making `JsonQueryLogging` accept any builder doesn't silently change the surface contract.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - In `src/Paramore.Darker/Logging/QueryProcessorBuilderExtensions.cs`, the `JsonQueryLogging(this IBuildTheQueryProcessor builder, Action<JsonSerializerOptions> configure = null)` body casts to `QueryProcessorBuilder` (the concrete type) and throws `NotSupportedException` if the cast fails, then forwards to the canonical generic `AddJsonQueryLogging<QueryProcessorBuilder>(...)`. Match the precedent in `Policies/QueryProcessorBuilderExtensions.cs:11-14` (same throw shape, same wording style).

### Step 7: Rewrite the existing core decorator test and delete the obsolete `ConfigurationException` test (FR10)

- [ ] **TEST + IMPLEMENT: Core decorator test rewrite — assert STJ output via captured log (FR10)**
  - **USE COMMAND**: `/test-first when logging decorator executes should use QueryLoggingJsonOptions to serialise body`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `Logging/When_logging_decorator_executes_should_use_json_options.cs` (renamed/rewritten from the existing `When_logging_decorator_executes_should_use_injected_serializer_settings.cs` per FR10)
  - Test should verify:
    - Arrange: `QueryLoggingJsonOptions.Options.WriteIndented = false;` (mutate the existing default in place — preserves `ReferenceHandler.IgnoreCycles` per the FR10 / C6 recommended path).
    - Decorator constructor takes **no** serialiser parameter (FR8).
    - Capture the `LogInformation` invocation via `LoggerCaptureFixture` and assert the `{Query}` arg equals the expected `System.Text.Json.JsonSerializer.Serialize(query, QueryLoggingJsonOptions.Options)` output for `CoreLoggingTestQuery`.
    - Behavioural strengthening over the previous test (which only asserted `result.Value`) — review finding #7 closed.
    - Test save-and-restores `QueryLoggingJsonOptions.Options.WriteIndented` in try/finally.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - **Delete** the previous test file `test/Paramore.Darker.Core.Tests/When_logging_decorator_executes_should_use_injected_serializer_settings.cs` as part of landing the rewrite.
    - No new production code — the decorator changes were landed in Steps 4 & 5; this task pins the FR10 rewrite.

- [ ] **STRUCTURAL: Delete the obsolete `ConfigurationException` decorator test (FR10)**
  - **USE COMMAND**: `/tidy-first delete obsolete ConfigurationException decorator test after FR8 removal`
  - Delete `test/Paramore.Darker.Core.Tests/When_logging_decorator_executes_without_settings_should_throw_ConfigurationException.cs` per FR10.
  - The `ConfigurationException` *type* is retained — it is still thrown elsewhere (`PipelineBuilder.cs`, `QueryHandlerRegistry.cs`, `RetryableQueryDecorator.cs`, `Policies/QueryProcessorBuilderExtensions.cs`). Do not delete the type.
  - Verify: `dotnet test test/Paramore.Darker.Core.Tests/ -c Release` succeeds and references to `ConfigurationException` in other production files still compile.

### Step 8: Remove `Newtonsoft.Json` from `Paramore.Darker` and central package management (FR5)

This step can only run **after** Steps 4–7 have removed every `using Newtonsoft.Json;` and `JsonSerializerSettings` reference from production code and tests. If the build fails after dropping the package, a `using` was missed — fix the using rather than re-adding the package.

- [ ] **STRUCTURAL: Drop `Newtonsoft.Json` from `Paramore.Darker.csproj` and `Directory.Packages.props` (FR5)**
  - **USE COMMAND**: `/tidy-first remove Newtonsoft.Json from Paramore.Darker dependencies`
  - Remove `<PackageReference Include="Newtonsoft.Json" />` from `src/Paramore.Darker/Paramore.Darker.csproj`.
  - Remove `<PackageVersion Include="Newtonsoft.Json" Version="13.0.4" />` from `Directory.Packages.props`.
  - Verify:
    - `dotnet build Darker.Filter.slnf -c Release` succeeds on all TFMs.
    - `dotnet list src/Paramore.Darker/Paramore.Darker.csproj package --include-transitive` shows **no** `Newtonsoft.Json` entry for any TFM (AC1).
    - `dotnet list samples/SampleMinimalApi/SampleMinimalApi.csproj package --include-transitive` shows **no** `Newtonsoft.Json` entry on either `net8.0` or `net9.0` (AC1).

### Step 9: AOT coverage with content assertions (FR11, AC4)

- [ ] **STRUCTURAL: Enable AOT publish on the AOT test csproj and AOT-compat on core (prerequisite for the FR11 tests below)**
  - **USE COMMAND**: `/tidy-first enable PublishAot and IsAotCompatible so AOT analyser actually runs against Paramore.Darker.Logging`
  - **Critical**: today `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj` does **not** set `<PublishAot>true</PublishAot>` — `dotnet publish` produces a normal IL publish and the AOT analyser never runs. Without this task, the FR11 tests below and the AC4 warning enforcement are vacuous.
  - In `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj` add:
    - `<PublishAot>true</PublishAot>`
    - `<IsAotCompatible>true</IsAotCompatible>` (project-level acknowledgement; surfaces consumer-side AOT warnings at build time, not only at publish time)
    - `<TrimMode>full</TrimMode>` (so the trim analyser also runs — AC4's allow-list covers both IL2xxx and IL3xxx, which means trim warnings as well as AOT warnings)
  - In `src/Paramore.Darker/Paramore.Darker.csproj` add `<IsAotCompatible>true</IsAotCompatible>` (so AOT/trim warnings under `src/Paramore.Darker/Logging/` surface at build time of the core library, not just at AOT publish time of the test project — this lets the FR13 suppressions catch their warnings at the same site they're declared).
  - Verify:
    - `dotnet build src/Paramore.Darker/Paramore.Darker.csproj -c Release` succeeds on all TFMs with at most the FR13 allow-listed warnings (`IL2026` / `IL3050` on `QueryLoggingDecorator<,>.Serialize` and `QueryLoggingDecoratorAsync<,>.Serialize`, possibly also on their callers per Decision step 3 caller-propagation note).
    - `dotnet publish test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj -c Release -f net8.0` succeeds and emits `ILC` (IL compiler) output — confirming AOT is actually engaged. Same for `-f net9.0`. If publish does NOT emit ILC output, `<PublishAot>` did not take effect — investigate before continuing.

- [ ] **TEST + IMPLEMENT: AOT — property-bearing `[QueryLogging]` handler emits exact source-generated JSON (FR11 case 1)**
  - **USE COMMAND**: `/test-first when AOT published query with QueryLogging executes should log expected source generated JSON for property bearing query`
  - Test location: `test/Paramore.Darker.Tests.AOT`
  - Test file: `Logging/LoggingQueryHandlerAOTTests.cs` (contains both FR11 subtests)
  - Test should verify:
    - Defines `record AotLoggedQuery(Guid Id, string Name) : IQuery<AotLoggedQuery.Result>` with nested `Result`, plus a `[QueryLogging]`-decorated handler.
    - Defines `[JsonSerializable(typeof(AotLoggedQuery))]`-decorated `internal partial class AotTestJsonContext : JsonSerializerContext { }`.
    - Arrange: install the source-generated resolver: `QueryLoggingJsonOptions.Options.TypeInfoResolver = AotTestJsonContext.Default;`; install AOT-compatible log capture (the existing `LoggerCaptureFixture` mechanism if it runs under AOT; otherwise an AOT-safe equivalent — verify at design time).
    - Act: `Execute` / `ExecuteAsync` the query with a known `Guid` and `Name` value.
    - Assert: the captured `{Query}` arg equals the **exact** expected string `{"Id":"<lowercase-hyphenated-Guid>","Name":"<name>"}` (STJ defaults Guid format to `D`). Confirms FR11 case 1.
    - Save-and-restore `TypeInfoResolver` and any other `Options` mutations in try/finally.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `Logging/LoggingQueryHandlerAOTTests.cs` (or split into two files if cleaner — the spec is agnostic).
    - Add the source-generated context type.
    - `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj` has `<PublishAot>true</PublishAot>` set in the previous task — verify this is still in place. The decorator's FR13 suppressions cover the `IL2026` / `IL3050` warnings on the `Serialize<T>` methods (and possibly their `Execute<TQuery>` / `ExecuteAsync<TQuery>` callers per Decision step 3 caller-propagation contingency); **no other** `IL2xxx` / `IL3xxx` warning under `src/Paramore.Darker/Logging/` is permitted per AC4.

- [ ] **TEST + IMPLEMENT: AOT — cycle-bearing `[QueryLogging]` handler does not throw (FR11 case 2)**
  - **USE COMMAND**: `/test-first when AOT published query with QueryLogging executes cycle bearing query should not throw because of IgnoreCycles default`
  - Test location: `test/Paramore.Darker.Tests.AOT`
  - Test file: `Logging/LoggingQueryHandlerAOTTests.cs` (second `[Fact]` in the same file, or a sibling file)
  - Test should verify:
    - Defines a query whose result/parameter graph contains a `Parent { Child[] }` / `Child { Parent }` cycle.
    - Adds the cycle-bearing type to `AotTestJsonContext` via `[JsonSerializable]`.
    - Acts via `Execute` / `ExecuteAsync` and asserts no exception is thrown — this verifies that the FR3 `ReferenceHandler.IgnoreCycles` default applies under AOT as it does under JIT.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add the cycle-bearing query + handler to the AOT test fixture.
    - No new production code — verifying the FR3 default already implemented in Step 2.

- [ ] **STRUCTURAL: Add AOT-warning enforcement check to verification routine (AC4)**
  - **USE COMMAND**: `/tidy-first verify AOT publish emits no IL2xxx or IL3xxx warnings outside FR13 allow-list`
  - Manually run on the design-time machine (depends on `<PublishAot>true</PublishAot>` having been set in the Step 9 prerequisite task above):
    - `dotnet publish test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj -c Release -f net8.0`
    - `dotnet publish test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj -c Release -f net9.0`
  - For each: confirm any `IL2xxx` / `IL3xxx` warning emitted from compilation units under `src/Paramore.Darker/Logging/` falls into the FR13 allow-list:
    - **Always allowed**: `IL2026` and `IL3050` on `QueryLoggingDecorator<,>.Serialize` and `QueryLoggingDecoratorAsync<,>.Serialize`.
    - **Conditionally allowed via caller-propagation expansion** (per ADR Decision step 3): `IL2026` and `IL3050` on `QueryLoggingDecorator<,>.Execute` / `QueryLoggingDecoratorAsync<,>.ExecuteAsync` IF — and only if — the analyser surfaces them at those caller sites. The ADR explicitly classifies this as "implementation-time discovery, not a defect in this ADR". When discovered, expand the `UnconditionalSuppressMessage` attributes onto the calling methods with the same `Justification`, then mark this task complete.
    - **Anything else** under `src/Paramore.Darker/Logging/` is a FAIL — debug (likely a missing `UnconditionalSuppressMessage` site, a new BCL warning category, or a regression in the suppression placement).
  - Verify: both publish commands succeed; the published binaries run the FR11 AOT tests to green when invoked; the allow-list is empirically pinned (record in this checkbox the exact list of `[file:line]` sites carrying `UnconditionalSuppressMessage` after the task completes).

### Step 10: FR14 lock-after-use ordering test (AC3)

- [ ] **STRUCTURAL: Add `OrderingTestQuery` test double in a dedicated xUnit collection**
  - **USE COMMAND**: `/tidy-first add OrderingTestQuery test double for FR14 lock after use ordering test`
  - Add `test/Paramore.Darker.Core.Tests/TestDoubles/OrderingTestQuery.cs` containing the exact shape pinned by AC3: `public sealed class OrderingTestQuery : IQuery<OrderingTestQuery.Result> { public string Marker { get; init; } = "x"; public sealed class Result { } }` (plus the matching `OrderingTestQueryHandler`).
  - Add a `[CollectionDefinition("QueryLoggingJsonOptionsOrdering", DisableParallelization = true)]` declaration in a small file so the FR14 ordering test can pin its collection.
  - No other test in `Paramore.Darker.Core.Tests` references `OrderingTestQuery` — its closed generic is reserved for the ordering test per AC3 test-isolation requirement #2.

- [ ] **TEST + IMPLEMENT: FR14 — `QueryLoggingJsonOptions.Options` locks after first query and rejects subsequent mutation (AC3)**
  - **USE COMMAND**: `/test-first when QueryLoggingJsonOptions Options mutated after first query executes should throw InvalidOperationException`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `Logging/When_QueryLoggingJsonOptions_Options_mutated_after_first_query_executes_should_throw_InvalidOperationException.cs`
  - Test should verify (single `[Fact]`, sequential, per AC3 test-isolation requirement #1):
    - Step 1: assert `QueryLoggingJsonOptions.Options.MaxDepth = 32;` succeeds (no throw) — pre-bootstrap property setter is callable per AC3 step 1 framing.
    - Step 2: call `AddJsonQueryLogging(o => o.WriteIndented = true);` — succeeds.
    - Step 3: build a `QueryProcessor` (real registry + factories) wired with the `[QueryLogging]`-decorated `OrderingTestQueryHandler`. Capture `{Query}` via `LoggerCaptureFixture`. Execute one query. Assert captured `{Query}` equals the indented form `"{\n  \"Marker\": \"x\"\n}"` (normalised line endings — STJ emits `\n` regardless of platform).
    - Step 4: call `AddJsonQueryLogging(o => o.WriteIndented = false);` (callback body **must mutate** a settable property — empty callbacks would not exercise the lock). Assert this throws `InvalidOperationException` — surfaced unmodified per OOS12.
    - The `[Fact]` is decorated `[Collection("QueryLoggingJsonOptionsOrdering")]` from the previous structural task. No other test mutates `QueryLoggingJsonOptions.Options` after this collection runs.
    - Save-and-restore as much state as is meaningful (notably, the lock itself is irreversible — see AC3's "process-scope" note; the test design accepts this).
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - No new production code — FR14 is an invariant verified by this test against work already landed in Step 2.

### Step 11: Sample app verification (AC5)

- [ ] **STRUCTURAL: Verify sample app still demonstrates query logging end-to-end (AC5)**
  - **USE COMMAND**: `/tidy-first verify sample app produces System Text Json log lines for GET people endpoints`
  - `dotnet build samples/SampleMinimalApi/SampleMinimalApi.csproj -c Release` succeeds.
  - `dotnet run --project samples/SampleMinimalApi/SampleMinimalApi.csproj` starts.
  - `GET http://localhost:5000/people` produces an Information-level log line `"Executing async query GetPeopleQuery: {…}"` followed by `"Async execution of query GetPeopleQuery completed in {Elapsed}ms"`. `{Query}` body is `{}` for the parameterless `GetPeopleQuery` (verified shape in `samples/SampleMinimalApi/QueryHandlers/GetPeopleQueryHandler.cs:8`).
  - `GET http://localhost:5000/people/{id}` for an **unknown** id triggers the fallback path on `GetPersonNameQuery` (handler `GetPersonQueryHandler`); the completion log line is the with-fallback variant `"Async execution of query GetPersonNameQuery completed in {Elapsed}ms (with fallback)"`.
  - No `Newtonsoft.Json` appears in `dotnet list samples/SampleMinimalApi/SampleMinimalApi.csproj package --include-transitive` for either `net8.0` or `net9.0` (AC1 cross-check).

### Step 12: Migration guidance for V5 release notes (DoD artefact)

This step is **documentation authoring** — it is neither a code refactor (so `/tidy-first` doesn't apply) nor a behavioural change (so `/test-first` doesn't apply). It ships an auditable artefact in the spec directory so the DoD's release-notes requirement is satisfied within the PR, not punted to release-tagging.

- [ ] **DOC: Draft V5 migration entry covering serialiser swap (DoD)**
  - **No `/tidy-first` or `/test-first` skill** — straight documentation drafting.
  - **Landing location** (pinned, not "park somewhere"): `specs/006-json_serializer/release-notes-draft.md`. The draft is committed as part of the PR; at release-tagging time it is copy-pasted (or referenced) into the GitHub release notes. Having a committed file makes the DoD requirement auditable now rather than at tagging time.
  - Cover (each as a heading or bullet in the draft):
    - Callback type change (`Action<JsonSerializerSettings>` → `Action<JsonSerializerOptions>`).
    - Decorator constructor change (no serialiser parameter).
    - DI singleton removal (no `services.AddSingleton<JsonSerializerSettings>(...)`).
    - `Newtonsoft.Json` removed as a `Paramore.Darker` dependency.
    - Recommended migration snippet (preserves `ReferenceHandler.IgnoreCycles`) — copy from requirements DoD lines 277–281.
    - Direct-assignment migration snippet (warning: drops `IgnoreCycles` unless re-applied) — copy from requirements DoD lines 283–289.
    - "How to swap serialiser entirely" → custom decorator (per ADR Alternative 1 dismissal).
    - AOT note covering `IL2026` / `IL3050` suppression policy + `TypeInfoResolver` opt-in path.
    - Known limitation — parallel `WebApplicationFactory` integration tests; recommend `[CollectionDefinition("DarkerHostBootstrap", DisableParallelization = true)]` per C6.
    - **Builder-surface limitation** (ADR Decision step 5, per Step 6 `NotSupportedException` test): the `IBuildTheQueryProcessor.JsonQueryLogging(...)` overload only works for the in-box `QueryProcessorBuilder`; custom `IBuildTheQueryProcessor` implementations throw `NotSupportedException`. Consumers using a custom builder must use the DI extension (`IDarkerHandlerBuilder.AddJsonQueryLogging(...)`) instead, or call the canonical `AddJsonQueryLogging<TBuilder>(...)` generic directly.
    - **Do not** list the xunit v2 → v3 upgrade as a consumer-facing break — `Paramore.Darker.Testing` has no xunit dependency, so the upgrade is invisible to consumers.
  - Verify: the file `specs/006-json_serializer/release-notes-draft.md` exists, has at least one bullet per DoD item above, and is staged for commit. Step 13's final-validation task re-checks this.

### Step 13: Final cross-cutting validation (AC1–AC5, DoD)

- [ ] **STRUCTURAL: Cross-cutting validation against all acceptance criteria**
  - **USE COMMAND**: `/tidy-first run full acceptance validation for ADR 0012`
  - Run, in this order, and confirm each passes:
    - `dotnet build Darker.Filter.slnf -c Release` (AC1 — `net8.0` + `net9.0`).
    - `dotnet test Darker.Filter.slnf -c Release --no-build` (AC2 — full suite green; AC3 — rewritten extension test passes; AC3 — FR14 ordering test passes; AC3 — rewritten core decorator test passes).
    - `dotnet publish test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj -c Release -f net8.0` and `… -f net9.0` (AC4 — succeeds with no `IL2xxx` / `IL3xxx` warnings under `src/Paramore.Darker/Logging/` outside the FR13 allow-list).
    - Run the published AOT binary — both FR11 subtests pass (AC4).
    - `dotnet list src/Paramore.Darker/Paramore.Darker.csproj package --include-transitive` — no `Newtonsoft.Json` for any TFM (AC1).
    - `dotnet list samples/SampleMinimalApi/SampleMinimalApi.csproj package --include-transitive` — no `Newtonsoft.Json` on `net8.0` or `net9.0` (AC1).
    - `Directory.Packages.props` — `System.Text.Json 10.0.8` pinned, `xunit.v3` pinned, **no** `xunit` 2.x, **no** `Newtonsoft.Json` (AC1).
    - The four FR12 csprojs reference `xunit.v3`; the benchmarks csproj is unchanged (AC1).
    - Sample app smoke test rerun (AC5).
    - `specs/006-json_serializer/release-notes-draft.md` exists and is committed; each DoD bullet from Step 12 has at least one corresponding entry in the file. This makes the DoD release-notes requirement auditable in this PR (DoD).
  - Any failure here loops back to the relevant step above; do not partially mark this task complete.

## Risk Mitigations

- **xunit.v3 API breaks under-counted.** Step 1 enumerates the 6 files for the `Xunit.Abstractions` rename and the reflection rework in `TestClassBase.cs:107-110`, but FR12 item 2 (`IAsyncLifetime` → `ValueTask`) is handled defensively by a grep-before-edit so a missed implementer surfaces as a compile error, not a runtime miss.
- **Cached `static readonly Logger` per closed generic leaks across assemblies.** Mitigated by FR10's disjoint test-query types (Step 3) and the process-isolation assumption (one process per test assembly). If a future runner config changes that, the disjoint closed generics remain the safety net.
- **`JsonSerializerOptions` lock-after-first-use is irreversible.** Step 10's ordering test runs once, sequentially, in a `DisableParallelization = true` collection, with a reserved closed generic — so the lock never leaks across tests in the same assembly. The Extensions-side fixture uses a different closed generic and is unaffected.
- **AOT publish surfaces a new `IL2xxx` / `IL3xxx` warning category.** AC4's categorical rule (any `IL2xxx` / `IL3xxx` under `src/Paramore.Darker/Logging/` outside the FR13 allow-list is a FAIL) flags this on the next build. Mitigation: add the new code to the allow-list **only** with a spec amendment — bare `<NoWarn>` is explicitly disallowed.
- **Test-isolation regression on `QueryLoggingJsonOptions.Options`.** Every mutating test save-and-restores per C5; Step 10's ordering test owns the irreversible lock; AC3 documents the cross-test constraint.
- **Migration friction for consumers customising Newtonsoft via `StringEnumConverter` / `DateTime` formatting.** Mitigated by the migration entry in Step 12 (C2 differences are documented in DoD).

## Dependencies

- Step 1 (xunit.v3) must complete before Step 3 (FR10 fixture depends on `IAssemblyFixture<T>`).
- Step 2 (add `System.Text.Json` + `QueryLoggingJsonOptions`) must complete before Steps 4–5 (decorator rewrites reference it).
- Step 3 (capture fixture + disjoint queries) must complete before Steps 4–7 (behavioural tests use it).
- Steps 4–7 must complete before Step 8 (drop Newtonsoft) — every `using Newtonsoft.Json;` must be gone before the package is removed.
- Step 8 must complete before Step 11 (sample-app verification depends on the dependency removal landing).
- **Step 9's `<PublishAot>true</PublishAot>` prerequisite sub-task must complete BEFORE the two AOT TEST + IMPLEMENT tasks and the AOT-warning enforcement task** — without it the AOT analyser is not engaged and the FR11 / AC4 verifications are vacuous (finding #1 of `review-tasks.md`).
- Step 9 AOT tests depend on Step 8 having succeeded (no transient Newtonsoft surface).
- Step 10's ordering test must run in its own xUnit collection — sequencing it after Step 9 keeps the lock-after-use mutation contained.
- Step 12 produces `specs/006-json_serializer/release-notes-draft.md` as a committed artefact; Step 13's final-validation explicitly checks for it.
- Step 13 is the gate before considering the spec done.
