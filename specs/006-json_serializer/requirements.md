# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: #294
**Linked Discussion**: [V5 discussion #273](https://github.com/BrighterCommand/Darker/discussions/273)

## Problem Statement

As a Darker consumer, I would like the built-in query logging decorator to serialise queries with `System.Text.Json` instead of `Newtonsoft.Json`, so that:

- I don't pull a third-party JSON library transitively into every application that takes a dependency on `Paramore.Darker`. Post-ADR 0011 the logging decorator lives in core, so `Newtonsoft.Json` is now a *direct* dependency of every Darker consumer — this is a regression for anyone who has otherwise removed `Newtonsoft.Json` from their dependency graph.
- I get the performance and reduced-allocation characteristics of `System.Text.Json` on the per-query hot path that runs on every logged execution.
- My logging configuration follows the same JSON conventions as the rest of the .NET ecosystem (e.g. `Microsoft.Extensions.Logging`'s `JsonConsoleFormatter`, ASP.NET Core's response serialisation).

As a Darker maintainer, I would like the JSON serialisation approach to **mirror Brighter exactly**, so that the two libraries stay reasonable-as-one-system per the ADR 0011 alignment work.

> **Brighter parity** (verified during this revision): Brighter's `RequestLoggingHandler` calls `JsonSerializer.Serialize(request, JsonSerialisationOptions.Options)` directly, where `JsonSerialisationOptions` is a `public static class` with a settable `Options` property (mutable global, not DI-injected, no interface). There is no `IJsonSerializer` / `IRequestSerializer` plug-in interface in Brighter. Brighter migrated to `System.Text.Json` in PR [#1470](https://github.com/BrighterCommand/Brighter/pull/1470) (2021). Darker mirrors this design rather than diverging.

## Proposed Solution

From a consumer's perspective:

1. **Default serialiser is `System.Text.Json`.** The logging decorator calls `System.Text.Json.JsonSerializer.Serialize(query, QueryLoggingJsonOptions.Options)` directly.
2. **Configuration via a static options class.** `QueryLoggingJsonOptions` (new public static class in `src/Paramore.Darker/Logging/`) exposes a settable `Options` property of type `JsonSerializerOptions`, mirroring Brighter's `JsonSerialisationOptions.Options`. The **recommended** configuration path is the `AddJsonQueryLogging(o => …)` callback, which mutates the default instance in place and preserves the `ReferenceHandler.IgnoreCycles` default (FR3). Direct assignment (`QueryLoggingJsonOptions.Options = new JsonSerializerOptions { … }`) is supported but loses the default `IgnoreCycles` setting unless the consumer re-applies it. Configuration is expected at application startup only, before any query is handled (see C6).
3. **`AddJsonQueryLogging()` callback exposes `JsonSerializerOptions`.** Both the DI extension `AddJsonQueryLogging(Action<JsonSerializerOptions> configure = null)` and the builder extension `JsonQueryLogging(Action<JsonSerializerOptions> configure = null)` accept an optional callback that mutates `QueryLoggingJsonOptions.Options`. Callback type changes from `Action<JsonSerializerSettings>` to `Action<JsonSerializerOptions>`.
4. **`Newtonsoft.Json` is no longer a dependency of `Paramore.Darker`.** The `PackageReference` is removed from `src/Paramore.Darker/Paramore.Darker.csproj`, the `using Newtonsoft.Json` and `JsonSerializerSettings` references are removed from `src/Paramore.Darker.Extensions.DependencyInjection/QueryLoggingDIExtensions.cs`, and the `PackageVersion` is removed from `Directory.Packages.props`.
5. **`System.Text.Json` is added as a direct `PackageReference` of `Paramore.Darker` for all target frameworks.** Required because `src/Paramore.Darker/Paramore.Darker.csproj` targets `netstandard2.0;net8.0;net9.0` and `System.Text.Json` is OOB on `netstandard2.0`. (`net8.0`/`net9.0` resolve the assembly from the shared framework regardless of the `PackageReference`.)
6. **Public extension method names are preserved.** `AddJsonQueryLogging()` and `JsonQueryLogging()` keep their names so the registration call site does not change — only the type of the configuration delegate changes.
7. **Log message templates are preserved.** The decorator continues to emit the same `"Executing query …"` / `"Executing async query …"` start templates and `"Execution of query …"` / `"Async execution of query …"` completion templates. Only the JSON *body* embedded in `{Query}` may differ in formatting because the serialiser is different.

The pluggable `IQueryLoggingSerializer` interface explored in v1 issue #294 lists pluggability as a *consideration*, not a requirement. This requirement **removes it from scope** Brighter does not offer such an interface; consumers who need radically different serialisation already have the decorator-pattern escape hatch — write a custom decorator and skip `AddJsonQueryLogging()`).

## Requirements

### Functional Requirements

- **FR1 — Default serialiser is `System.Text.Json`.** The sync decorator `QueryLoggingDecorator<TQuery, TResult>` and async decorator `QueryLoggingDecoratorAsync<TQuery, TResult>` (both in `src/Paramore.Darker/Logging/Handlers/`) call `System.Text.Json.JsonSerializer.Serialize(query, QueryLoggingJsonOptions.Options)` to produce the `{Query}` log argument. No `Newtonsoft.Json` types are referenced.

- **FR2 — Static `QueryLoggingJsonOptions` class.** A new `public static class QueryLoggingJsonOptions` in `src/Paramore.Darker/Logging/` exposes a `public static JsonSerializerOptions Options { get; set; }` property. The static class mirrors the shape of Brighter's `JsonSerialisationOptions`. The decorators reference `QueryLoggingJsonOptions.Options` at call time (not captured in a field at construction) — this is safe because the startup-only contract (C6) forbids mutation after the first query executes. The setter throws `ArgumentNullException` if assigned `null`, to fail fast rather than letting a later `JsonSerializer.Serialize(query, null)` throw a less-clear `NullReferenceException`.

- **FR3 — Default `QueryLoggingJsonOptions.Options` configures `ReferenceHandler.IgnoreCycles`.** The default `JsonSerializerOptions` instance assigned at class-init has `ReferenceHandler = ReferenceHandler.IgnoreCycles` set so that EF Core-backed query objects with navigation-property cycles do not throw `JsonException` on the logging hot path. All other properties stay at `System.Text.Json` defaults (PascalCase, no indentation, `MaxDepth = 64`).

- **FR4 — Configuration callback exposes `JsonSerializerOptions`.** Public method signatures (mirror the current delegation chain):
  - **Canonical implementation**: `TBuilder AddJsonQueryLogging<TBuilder>(this TBuilder builder, Action<JsonSerializerOptions> configure = null) where TBuilder : IQueryProcessorExtensionBuilder` — in `Paramore.Darker.Logging.QueryProcessorBuilderExtensions`. This is the sole site where `configure?.Invoke(QueryLoggingJsonOptions.Options)` is called, and where `builder.RegisterDecorator(typeof(QueryLoggingDecorator<,>))` / `typeof(QueryLoggingDecoratorAsync<,>)` are registered.
  - **Delegates to canonical**: `IBuildTheQueryProcessor JsonQueryLogging(this IBuildTheQueryProcessor builder, Action<JsonSerializerOptions> configure = null)` — in the same class; casts `builder` to `QueryProcessorBuilder` and forwards to the canonical implementation.
  - **Delegates to canonical**: `IDarkerHandlerBuilder AddJsonQueryLogging(this IDarkerHandlerBuilder builder, Action<JsonSerializerOptions> configure = null)` — in `Paramore.Darker.Extensions.DependencyInjection.QueryLoggingDIExtensions`; forwards to `QueryProcessorBuilderExtensions.AddJsonQueryLogging<IDarkerHandlerBuilder>(builder, configure)`. This forwarder no longer registers a DI singleton for the serialiser (see FR8); it only forwards.
  
  This single-call-site discipline guarantees the callback runs exactly once per consumer call, regardless of which surface they used. Calling any of the three overloads more than once is permitted but discouraged (C6) — each call invokes the callback against the same static options.

- **FR5 — Remove `Newtonsoft.Json` from production code and package graph.**
  - Delete `using Newtonsoft.Json;` and all `JsonSerializerSettings` / `JsonConvert.SerializeObject` references from:
    - `src/Paramore.Darker/Logging/Handlers/QueryLoggingDecorator.cs`
    - `src/Paramore.Darker/Logging/Handlers/QueryLoggingDecoratorAsync.cs`
    - `src/Paramore.Darker/Logging/QueryProcessorBuilderExtensions.cs`
    - `src/Paramore.Darker.Extensions.DependencyInjection/QueryLoggingDIExtensions.cs`
  - Remove `<PackageReference Include="Newtonsoft.Json" />` from `src/Paramore.Darker/Paramore.Darker.csproj`.
  - Remove `<PackageVersion Include="Newtonsoft.Json" Version="13.0.4" />` from `Directory.Packages.props` (after FR9 rewrites the two test files that currently `using Newtonsoft.Json`, no project requires the pin).

- **FR6 — Add `System.Text.Json` `PackageReference` to `Paramore.Darker`.**
  - Add `<PackageReference Include="System.Text.Json" />` to `src/Paramore.Darker/Paramore.Darker.csproj`. This is required for the `netstandard2.0` target framework; `net8.0`/`net9.0` already provide the assembly via the shared framework but the `PackageReference` is uniform across TFMs for simplicity.
  - Add `<PackageVersion Include="System.Text.Json" Version="10.0.8" />` to `Directory.Packages.props` — aligned with the `Microsoft.Extensions.Logging` / `Microsoft.Extensions.DependencyInjection` 10.0.8 family already pinned in the file (`Directory.Packages.props:9–11`). If the M.E.* family is updated in a separate PR, the `System.Text.Json` pin is updated to match in that same PR.

- **FR7 — Public API surface preserved at the method-name level.**
  - `AddJsonQueryLogging` keeps its name on both the DI extensions class and the builder-extensions class.
  - `JsonQueryLogging` keeps its name on the builder-extensions class.
  - `QueryLoggingDecorator<TQuery, TResult>` and `QueryLoggingDecoratorAsync<TQuery, TResult>` keep their names and namespace (`Paramore.Darker.Logging.Handlers`).

- **FR8 — Decorator constructor change; remove `ConfigurationException` path.**
  - Decorator constructors no longer take a `JsonSerializerSettings` parameter (or any serialiser parameter). They reference `QueryLoggingJsonOptions.Options` directly inside `Execute` / `ExecuteAsync`.
  - The `Serialize<T>` private method's `ConfigurationException("No serializer settings are configured…")` path is removed — `QueryLoggingJsonOptions.Options` is initialised at class-init (FR3) and the setter is null-guarded (FR2), so the value is always non-null at the call site. There is no longer a "not configured" state to throw against.
  - Confirmed side-effect: the `IDarkerHandlerBuilder.Services.AddSingleton(settings)` call in `QueryLoggingDIExtensions.AddJsonQueryLogging` is removed (no DI singleton needed; options are static).

- **FR9 — Log message templates are unchanged.** The decorator continues to emit the templates below at `Information`. Note that the completion templates exist in **two distinct forms** because the `" (with fallback)"` suffix is a runtime `string` concatenated onto the template at the call site (`QueryLoggingDecorator.cs:42`, `QueryLoggingDecoratorAsync.cs:47`) — not a structured-logging placeholder. Structured sinks (Serilog, Elasticsearch) therefore see two separate message templates per decorator, and FR10's log-capture tests must match against the form actually produced.
  - Sync start: `"Executing query {QueryName}: {Query}"` (file ref: `src/Paramore.Darker/Logging/Handlers/QueryLoggingDecorator.cs:34`).
  - Async start: `"Executing async query {QueryName}: {Query}"` (file ref: `src/Paramore.Darker/Logging/Handlers/QueryLoggingDecoratorAsync.cs:39`).
  - Sync completion, no fallback: `"Execution of query {QueryName} completed in {Elapsed}ms"` (file ref: `src/Paramore.Darker/Logging/Handlers/QueryLoggingDecorator.cs:42` with `withFallback == ""`).
  - Sync completion, with fallback: `"Execution of query {QueryName} completed in {Elapsed}ms (with fallback)"` (same file/line with `withFallback == " (with fallback)"`).
  - Async completion, no fallback: `"Async execution of query {QueryName} completed in {Elapsed}ms"` (file ref: `src/Paramore.Darker/Logging/Handlers/QueryLoggingDecoratorAsync.cs:47` with `withFallback == ""`).
  - Async completion, with fallback: `"Async execution of query {QueryName} completed in {Elapsed}ms (with fallback)"` (same file/line with `withFallback == " (with fallback)"`).
  
  Only the body of `{Query}` is permitted to differ in formatting between Newtonsoft and STJ output. The runtime-concatenation pattern itself is preserved as-is — refactoring it to a structured placeholder is out of scope.

- **FR10 — Rewrite the two existing serialiser tests and delete the third "no-settings" test.**
  - `test/Paramore.Darker.Core.Tests/When_logging_decorator_executes_should_use_injected_serializer_settings.cs` — rename file and class to `When_logging_decorator_executes_should_use_json_options` (or similar). Rewrite arrange block to use the callback path: `QueryLoggingJsonOptions.Options.WriteIndented = false;` (mutate the existing default in place, preserving `ReferenceHandler.IgnoreCycles`). Decorator constructor no longer takes a parameter. Add a behavioural assertion: capture the `ILogger.LogInformation` call (see capture mechanism below) and assert the `{Query}` argument equals the expected `System.Text.Json` output for the test query. This addresses review finding #7 — current test only asserts `result.Value`, which gives the serialiser swap no real coverage.
  - `test/Paramore.Darker.Extensions.Tests/When_AddJsonQueryLogging_called_should_register_serializer_settings.cs` — rename file and class to `When_AddJsonQueryLogging_called_should_configure_json_options`. Rewrite both `[Fact]`s. The first now asserts that calling `AddJsonQueryLogging(o => o.WriteIndented = true)` actually mutates `QueryLoggingJsonOptions.Options.WriteIndented` to `true` (the DI singleton registration is gone per FR8). The second remains an end-to-end smoke test — handler with `[QueryLogging]` executes successfully — but the assertion strengthens to verify the captured log argument is a `System.Text.Json` output (not a `Newtonsoft.Json` one).
  - **Delete** `test/Paramore.Darker.Core.Tests/When_logging_decorator_executes_without_settings_should_throw_ConfigurationException.cs`. This test asserts `Should.Throw<ConfigurationException>(...)` against the "no serializer settings configured" path that FR8 removes. After FR8 the decorator constructor takes no serialiser parameter and the `ConfigurationException` throw site is gone, so this test has nothing to assert against. The file is deleted, not rewritten. The `ConfigurationException` *type* itself is retained — it is thrown elsewhere in production code (`PipelineBuilder.cs`, `QueryHandlerRegistry.cs`, `RetryableQueryDecorator.cs`, `Policies/QueryProcessorBuilderExtensions.cs`).
  - **Log-capture mechanism (pinned)**: the decorator caches its `ILogger` in a `static readonly` field initialised from `ApplicationLogging.LoggerFactory` at first use per closed generic (verified in `src/Paramore.Darker/Logging/Handlers/QueryLoggingDecorator.cs:13`). The cache is not invalidated when `ApplicationLogging.LoggerFactory` is reassigned. Therefore tests **must** install a capturing `ILoggerFactory` *before* any `QueryLoggingDecorator<,>` closed generic is touched. The required mechanism is an xUnit v3 `IAssemblyFixture<LoggerCaptureFixture>` (available after the FR12 upgrade) that:
    1. In its constructor: stores `var previous = ApplicationLogging.LoggerFactory;`, then assigns `ApplicationLogging.LoggerFactory = new LoggerFactory(new[] { new CapturingLoggerProvider(buffer) });`.
    2. In `Dispose()`: restores `ApplicationLogging.LoggerFactory = previous;`.
    3. Exposes a per-test `Clear()` method or a thread-local buffer so individual tests assert in isolation.
    
    Tests that need to assert log content take a constructor dependency on the fixture, call `fixture.Clear()` at the start of the `[Fact]`, exercise the decorator, then assert against `fixture.CapturedLogs`. `Mock<ILogger>` is explicitly **not** an acceptable substitute here — the cached static field means a per-test mock cannot be injected — and Moq is "last resort" per CLAUDE.md anyway.
  - **Cross-assembly closed-generic discipline**: `Paramore.Darker.Core.Tests` and `Paramore.Darker.Extensions.Tests` both exercise the logging decorator. They **must** use disjoint test-query types (e.g. `CoreLoggingTestQuery` in `Paramore.Darker.Core.Tests/TestDoubles/`, `ExtensionsLoggingTestQuery` in `Paramore.Darker.Extensions.Tests/TestDoubles/`) so the closed generics `QueryLoggingDecorator<CoreLoggingTestQuery, …>` vs `QueryLoggingDecorator<ExtensionsLoggingTestQuery, …>` cache disjoint `static readonly Logger` fields. This prevents one assembly's cached `LoggerFactory` from leaking into the other assembly's tests in the corner case where both run in a single `dotnet test` process.
  - **Process-isolation assumption**: `dotnet test` is assumed to fork one process per test assembly (default for `vstest`). This assumption holds for the CI workflow (`.github/workflows/dotnet-core.yml`). If a future runner configuration changes this assumption, the disjoint-closed-generic discipline above remains the safety net.
  - **Test isolation (cross-references C5)**: because `QueryLoggingJsonOptions.Options` is a mutable global, tests that mutate it must save-and-restore the prior value (try/finally per `[Fact]`, or via the same `LoggerCaptureFixture` if scope allows). Two tests that mutate the static in parallel xUnit runs WILL interfere; tests in the same fixture run sequentially.

- **FR11 — Add a `[QueryLogging]` AOT test with content assertions, using a source-generated `JsonSerializerContext`.** A new test in `test/Paramore.Darker.Tests.AOT/` (e.g. `LoggingQueryHandlerAOTTests.cs`) defines two AOT-publishable test queries and exercises them under AOT publish (`net8.0` and `net9.0`). The `JsonSerializerContext` is included for **runtime trim-safety** — under AOT publish the trimmer may strip property metadata for any type not statically reachable, which would cause the decorator's `JsonSerializer.Serialize` call to emit incomplete JSON and fail the content assertion at runtime. A source-generated `TypeInfoResolver` keeps the test query types' metadata intact at runtime. (Note: the `IL2026`/`IL3050` analyser warnings at the decorator's call site fire **regardless** of whether the consumer supplies a source-generated context — those warnings are suppressed unconditionally in Darker per FR13, not avoided by the test's choice of resolver.) The test arranges:
  ```csharp
  [JsonSerializable(typeof(AotLoggedQuery))]
  [JsonSerializable(typeof(AotCyclicQuery))]
  internal partial class AotTestJsonContext : JsonSerializerContext { }
  ```
  and installs it before exercising the decorator:
  ```csharp
  QueryLoggingJsonOptions.Options.TypeInfoResolver = AotTestJsonContext.Default;
  ```
  Test cases:
  1. **Property-bearing query**: `record AotLoggedQuery(Guid Id, string Name) : IQuery<AotLoggedQuery.Result>` whose handler is `[QueryLogging]`-decorated. The test asserts the captured `{Query}` log argument equals the exact string `{"Id":"<Guid in D format>","Name":"<Name>"}` (e.g. `{"Id":"d3b07384-d113-4ec3-8c5f-3b13d2ab0ad9","Name":"Alice"}`). `System.Text.Json` defaults to `"D"` format for `Guid` (hyphenated lowercase). This verifies the serialiser path actually runs under AOT and produces correct JSON for a non-trivial type.
  2. **Cycle-bearing query**: a query type whose result graph holds a parent-child cycle (`Parent { Child[] }`, `Child { Parent }`), whose handler is `[QueryLogging]`-decorated. The test asserts execution completes without throwing — i.e. the FR3 `ReferenceHandler.IgnoreCycles` default applies under AOT just as under JIT.
  
  Today's AOT test base calls `.AddJsonQueryLogging()` but no test query carries `[QueryLogging]`, so the serialiser path is not actually exercised under AOT — this FR closes that gap (review finding #6). The source-generated context demonstrates the **runtime trim-safe** pattern that NFR2 commits to: consumer query types whose metadata is preserved (by source-gen, `[DynamicallyAccessedMembers]`, or trimmer roots) serialise correctly under AOT. Consumer types without such preservation may produce stripped JSON at runtime — that limitation is OOS11.

- **FR12 — Upgrade `xunit` to `xunit.v3` across all test projects.** v3 of `xunit` (`xunit.v3`) ships `IAssemblyFixture<T>`, which FR10 depends on. The repo currently pins `xunit 2.9.3` (`Directory.Packages.props:19`) but already pins `xunit.runner.visualstudio 3.1.5` (`Directory.Packages.props:24`) — a known mixed transition configuration. Required changes:
  - `Directory.Packages.props`: replace the `xunit` (2.9.3) pin with `xunit.v3` (latest stable at design time — verify against NuGet). **No rename needed for `xunit.runner.visualstudio`**: the existing 3.1.5 pin already supports xunit.v3 — `xunit.v3.runner.visualstudio` does **not** exist as a separately-named NuGet package. **No rename needed for `xunit.analyzers`** either — the existing 1.27.0 pin is xunit.v3-compatible; verify at design time and bump the version if required, but the package id stays. Net effect: one package id change (`xunit → xunit.v3`) plus version bumps as needed.
  - Update the `xunit` `PackageReference` to `xunit.v3` in **exactly the following 4 test csprojs** (the complete inventory of files that reference `xunit` today, verified via `grep -l 'PackageReference Include="xunit'`):
    - `test/Paramore.Darker.Core.Tests/Paramore.Darker.Core.Tests.csproj`
    - `test/Paramore.Darker.Extensions.Tests/Paramore.Darker.Extensions.Tests.csproj`
    - `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj`
    - `test/Paramore.Test.Helpers/Paramore.Test.Helpers.csproj`
    
    `test/Paramore.Darker.Benchmarks/Paramore.Darker.Benchmarks.csproj` does **not** reference xunit (it is a BenchmarkDotNet project) and is therefore out of scope for FR12.
  - **API-break inventory** (must be addressed in the migration, not deferred):
    1. **`Xunit.Abstractions` namespace removed.** `ITestOutputHelper` and friends moved to the `Xunit` namespace in v3. Affected files in this repo (verified): `test/Paramore.Test.Helpers/TestOutput/ICoreTestOutputHelper.cs`, `test/Paramore.Test.Helpers/TestOutput/CoreTestOutputHelper.cs`, `test/Paramore.Test.Helpers/Base/ITestClassBase.cs`, `test/Paramore.Test.Helpers/Base/TestClassBase.cs`, `test/Paramore.Darker.Tests.AOT/Base/AOTTestClassBase.cs`, `test/Paramore.Darker.Tests.AOT/QueryProcessor/AOTQueryProcessorTests.cs` (6 files). Each must drop the `using Xunit.Abstractions;` and rely on `using Xunit;` for `ITestOutputHelper`.
    2. **`IAsyncLifetime` signature change.** v3 returns `ValueTask` instead of `Task`. Any current usage must update its `InitializeAsync`/`DisposeAsync` return types.
    3. **`[Fact]`/`[Theory]` method visibility convention.** v3 recommends `internal` over `public` for test methods (test discovery is unaffected by visibility but the convention shifted). This is a convention change, not a hard break — `public` continues to work.
    4. **`[Theory]` + `[InlineData]` semantics unchanged**; `IClassFixture<T>` and `ICollectionFixture<T>` unchanged.
    5. **`Xunit.Sdk` types referenced by analysers/runners changed**; any custom analyser or runner extension in this repo would need review. None present today (verified by grep across `test/**`).
    6. **xunit v2 `TestOutputHelper` private-`test`-field reflection.** `test/Paramore.Test.Helpers/Base/TestClassBase.cs:107-110` calls `testOutputHelper.GetType().GetField("test", BindingFlags.Instance | BindingFlags.NonPublic)` to fish out v2's private `test` field; the result is cast to `Xunit.Abstractions.ITest` and exposed via the `XunitTest` property (line 49), which `TestQualifiedName` (line 52) uses to label log output. This is a string-based reflection access, **invisible to namespace-only `using` greps**. Under xunit.v3 the `TestOutputHelper` implementation is a different concrete type and the private `test` field does not exist with the same name/shape, so `GetField(...)` will return `null` at runtime and `XunitTest` will silently become `null` — `TestQualifiedName` will then fall back to `typeof(T).GetLoggerCategoryName()` for every test, producing a behavioural regression in AOT-test log naming, not a compile-time error. **Required design-time decision**: either (a) replace the reflection with xunit.v3's `TestContext.Current.Test` (and update the `ITest` cast to the v3 equivalent — `IXunitTest`), or (b) accept the `null` fallback and document the test-naming regression in the AOT test suite as the trade-off. Option (a) is preferred; option (b) is acceptable only if `TestContext.Current.Test` is not available in the test execution context for some reason.
  - This is a **scope expansion vs. the original JSON-serialiser swap**, accepted because it unblocks FR10's pinned mechanism. The change touches every test project in the repo, not just the two named in FR9/FR10. The scope expansion is explicit; the spec owns it rather than hand-waving to a "separate xunit upgrade" issue.

- **FR13 — Suppress unavoidable AOT warnings in the decorator with a justified allow-list.** `System.Text.Json.JsonSerializer.Serialize<T>(T, JsonSerializerOptions)` is annotated `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` in .NET 8+. The warning fires **at the decorator's call site**, regardless of what `JsonSerializerOptions` (with or without a source-generated `TypeInfoResolver`) the consumer passes. The decorator therefore suppresses the call-site warnings with an `UnconditionalSuppressMessage` attribute pair on the `Serialize` method. **Pinned signature** (minimal change vs. FR8 — `Serialize` stays instance-method and generic, only the body changes):
  ```csharp
  [UnconditionalSuppressMessage(
      "Trimming", "IL2026:RequiresUnreferencedCodeAttribute",
      Justification = "Consumers supply their own JsonSerializerOptions. AOT/trim-safe usage is documented as the consumer responsibility (NFR2). Source-gen TypeInfoResolver is the supported escape hatch.")]
  [UnconditionalSuppressMessage(
      "AOT", "IL3050:RequiresDynamicCodeAttribute",
      Justification = "Same as IL2026 — call site is unavoidable without erasing the public Serialize API. Consumers supply source-gen TypeInfoResolver for full AOT safety.")]
  private string Serialize<T>(T value) =>
      JsonSerializer.Serialize(value, QueryLoggingJsonOptions.Options);
  ```
  Chosen `JsonSerializer.Serialize` overload: the closed-generic `Serialize<T>(T, JsonSerializerOptions)` overload (matches the current code's shape and is the conventional pick). The `Serialize` method is duplicated as-shown on both `QueryLoggingDecorator<TQuery, TResult>` and `QueryLoggingDecoratorAsync<TQuery, TResult>` — two methods on two types, not an overload set. Both suppressions are added to a project-level allow-list (in `Paramore.Darker.csproj` or a `GlobalSuppressions.cs`) with the same justification, and are listed in the AC4 allow-list. No other AOT warnings under `src/Paramore.Darker/Logging/` are accepted — see AC4.

- **FR14 — `QueryLoggingJsonOptions.Options` first-access ordering.** The static initialiser of `QueryLoggingJsonOptions` MUST NOT trigger `JsonSerializerOptions` first-use locking. Concretely: the class-init code (FR3) assigns the default instance and configures `ReferenceHandler.IgnoreCycles`, but does NOT invoke `JsonSerializer.Serialize(...)` or otherwise cause the options to lock. The first access that causes the lock is the decorator's `Execute`/`ExecuteAsync` invocation — i.e. after all DI bootstrap and `AddJsonQueryLogging(configure)` calls have run. This guarantees consumers can mutate options at startup without hitting `InvalidOperationException`. The startup-only contract in C6 is the *operational* expectation; FR14 is its *implementation* invariant.

### Non-functional Requirements

- **NFR1 — `System.Text.Json` is an external dependency, but a small and well-bounded one.** `System.Text.Json` ships as a NuGet package; the `Paramore.Darker` package gains it as a direct dependency. On `net8.0`/`net9.0` it is provided by the shared framework and adds no runtime cost. On `netstandard2.0` it pulls `System.Memory`, `System.Text.Encodings.Web`, and a handful of small ancillary packages. This is judged an acceptable trade vs. the `Newtonsoft.Json` direct dependency it replaces (smaller transitive graph, ecosystem alignment, AOT story).

- **NFR2 — AOT-publishable, with documented limitations.** The default `JsonSerializer.Serialize(query, QueryLoggingJsonOptions.Options)` call path **publishes and runs** under AOT (`net8.0` and `net9.0`) — verified by FR11. However, two BCL-annotated warnings (`IL2026`, `IL3050`) are emitted at the decorator's call site, regardless of what `JsonSerializerOptions` the consumer supplies, because the analyser cannot prove the runtime type's metadata is statically reachable. These warnings are suppressed in Darker with explicit `UnconditionalSuppressMessage` attributes (FR13). The trade-offs are:
  - **Runtime behaviour**: AOT consumers using arbitrary query types get the same reflection-based serialiser fallback as JIT consumers. This works but may fail at runtime if the trimmer has removed properties from a query type that has no `[DynamicallyAccessedMembers]` annotation and isn't preserved otherwise.
  - **Trim-safety (out of scope)**: Darker does not commit to full trim-safety. Consumers running `PublishTrimmed=true` may see their query types' properties stripped if not preserved.
  - **Supported AOT-safe usage**: consumers assign a source-generated `JsonSerializerOptions` (built from a `JsonSerializerContext`) to `QueryLoggingJsonOptions.Options`. FR11 demonstrates this pattern. This requires no code change in Darker.

> *Removed in v2*: previous NFR3 ("per-query overhead is no worse than the current Newtonsoft path; no formal benchmark required") is dropped. It was unmeasurable without a benchmark, and adding a benchmark adds work for low information value (the STJ-vs-Newtonsoft perf delta on logging hot paths is well-established and in STJ's favour). If perf regressions are observed in practice, raise a new issue.

### Constraints and Assumptions

- **C1 — V5 breaking change is permitted.** This issue is labelled `Breaking Change` and is scoped to V5. The configuration callback type changes (`JsonSerializerSettings` -> `JsonSerializerOptions`), the decorator constructor parameter is removed, the DI singleton registration for `JsonSerializerSettings` is removed, and consumers whose code reads or constructs `JsonSerializerSettings` will need to migrate. Migration guidance is included in the GitHub release notes for V5.

- **C2 — JSON body formatting may differ.** `System.Text.Json` (STJ) and `Newtonsoft.Json` produce different default outputs for the same input. Documented differences:
  - **Property casing**: both default to PascalCase (matches property names as written). No change expected.
  - **Whitespace**: both default to non-indented. No change expected.
  - **Null handling**: both serialise nulls by default. No change expected.
  - **Enum representation**: both default to the numeric value as a JSON number. **However**, consumers who customised Newtonsoft with `StringEnumConverter` (commonly applied globally via `JsonConvert.DefaultSettings` or per-`JsonSerializerSettings`) must migrate to STJ's `JsonStringEnumConverter`. Naming-policy defaults differ between the two converters (Newtonsoft's `StringEnumConverter` writes member names as-declared; STJ's `JsonStringEnumConverter` also defaults to as-declared but applies any configured `JsonNamingPolicy`). Verify the migrated output if your queries include enum properties.
  - **`DateTime` formatting**: STJ default is **shortest round-trippable ISO 8601** — trailing-zero subsecond digits are trimmed, and the fractional component is omitted entirely if zero. UTC `DateTime`s end in `Z`; offsets end in `+HH:mm`. Example: `2026-05-28T10:30:00Z` (no `.0000000`), `2026-05-28T10:30:00.123Z` (no trailing zeros). Newtonsoft default is `yyyy-MM-ddTHH:mm:ss[.fffffff]` with kind-dependent suffix: `Z` for `Utc`, `+HH:mm` for `Local`, **no suffix** for `Unspecified`. The shortest-vs-fixed-precision difference produces observably different log lines. `DateTimeOffset` is similarly affected (STJ emits shortest, Newtonsoft uses `yyyy-MM-ddTHH:mm:ss.fffffffzzz`).
  - **Dictionary key casing**: STJ has a separate `DictionaryKeyPolicy` (default: as-declared) that Newtonsoft does not surface; behaviour is equivalent under defaults but converges differently if a `PropertyNamingPolicy` is configured.
  - **`decimal` precision**: subtle differences possible (STJ writes the shortest round-trippable representation; Newtonsoft preserves more trailing zeros). Acceptable for log output.
  - **Reference cycles**: STJ defaults THROW (`JsonException`) on cycles. FR3 mitigates by setting `ReferenceHandler.IgnoreCycles` in the default `QueryLoggingJsonOptions.Options`. Newtonsoft by default also throws without `ReferenceLoopHandling.Ignore` configured.
  - **`MaxDepth`**: STJ default is 64 (throws beyond); Newtonsoft default is 128. Queries that produce nesting deeper than 64 will fail under defaults — consumers must raise `QueryLoggingJsonOptions.Options.MaxDepth` via the `AddJsonQueryLogging` callback.

- **C3 — Built on top of ADR 0011's layout.** This work targets the post-ADR-0011 layout: the logging decorator lives in `src/Paramore.Darker/Logging/Handlers/`, the builder extensions live in `src/Paramore.Darker/Logging/QueryProcessorBuilderExtensions.cs`, and the DI extension lives in `src/Paramore.Darker.Extensions.DependencyInjection/QueryLoggingDIExtensions.cs`. ADR 0011 has merged.

- **C4 — Mirror Brighter.** Verified during this revision: Brighter's `RequestLoggingHandler` uses `JsonSerializer.Serialize(request, JsonSerialisationOptions.Options)` with a static-class options pattern, no interface. Darker's `QueryLoggingJsonOptions.Options` mirrors this exactly. The Darker class is named `QueryLoggingJsonOptions` (not just `JsonOptions`) to disambiguate from any future Darker-side JSON options for non-logging concerns.

- **C5 — Test isolation around the mutable static.** Because `QueryLoggingJsonOptions.Options` is a mutable global, any test that mutates it must restore the prior value on completion. This is the same testing constraint Brighter has against `JsonSerialisationOptions.Options`. FR10 calls this out explicitly.

- **C6 — Startup-only configuration contract.** `QueryLoggingJsonOptions.Options` is contractually intended to be configured at application startup, *before any query is handled*. Concurrent mutation during query execution is **undefined behaviour** — Darker provides no thread-safety guarantee on the static read-during-write path. This matches Brighter's implicit contract on `JsonSerialisationOptions.Options`.
  - **Recommended configuration path**: the `AddJsonQueryLogging(o => …)` callback. It mutates the default instance in place and preserves the `ReferenceHandler.IgnoreCycles` default (FR3). It can be called multiple times at startup; each call invokes the callback once, accumulating mutations onto the same instance.
  - **Permitted but discouraged**: direct assignment to `QueryLoggingJsonOptions.Options`. This replaces the default instance entirely, silently dropping `ReferenceHandler.IgnoreCycles` (and any other defaults set by FR3) unless the consumer re-applies them. The setter throws `ArgumentNullException` on `null` assignment (FR2).
  - **Locked-options behaviour**: `JsonSerializerOptions` self-locks on first use. If a consumer mutates options *after* the first query has been logged, the next mutation throws `InvalidOperationException`. The startup-only contract avoids this — no FR/AC test covers it.
  - **Programmer-error cases (out of scope to defend against)**: assigning `null`, mutating after first use, mutating from multiple threads concurrently. These produce undefined behaviour or clear runtime exceptions; Darker does not add defensive code beyond the null-guard in FR2.
  - **Known limitation — parallel integration tests**: consumer test suites that build multiple `WebApplicationFactory<TStartup>` hosts in parallel (xUnit's default collection-parallelism) will call `AddJsonQueryLogging(o => …)` concurrently against the same static. This is the "concurrent mutation" case C6 declares undefined. Consumers running parallel integration tests must serialise host construction — e.g. via `[CollectionDefinition("DarkerHostBootstrap", DisableParallelization = true)]` on the relevant test collection. The DoD release notes call this out explicitly so consumers are not surprised.

- **A1 — `ReferenceHandler.IgnoreCycles` is the right default for logging.** Logging-time serialisation that throws on cycles will mask the underlying query failure with a serialisation failure — a bad outcome for diagnostics. Silently dropping the cycle is preferable for log output. Consumers who disagree assign their own options.

- **A2 — Existing tests do not depend on exact serialised string content.** Confirmed: both existing tests assert on `result.Value` (the query result), not on the log output string. A serialiser swap does not break the existing assertions; FR10 *strengthens* the assertions during the rewrite rather than just preserving them.

### Out of Scope

- **OOS1 — Pluggable `IQueryLoggingSerializer` interface.** Removed from scope per the v2 revision. Users who need radically different serialisation write a custom decorator (decorator pattern is the architecture) and skip `AddJsonQueryLogging()`.

- **OOS2 — Logging template changes.** The log message templates (`"Executing query …"` etc.) are not changed. Anyone who wants a different shape writes their own decorator.

- **OOS3 — Logging level / sink configuration.** The decorator continues to log at `Information` level via `Microsoft.Extensions.Logging.ILogger`. No new logging level configuration, no log filtering, no per-query log enable/disable.

- **OOS4 — Removing the logging decorator from core.** The decorator stays in core (per ADR 0011). This issue only swaps the serialiser; it does not re-debate the assembly placement.

- **OOS5 — A source-generated `JsonSerializerContext` shipped by Darker.** Darker does not ship a `[JsonSerializable]`-decorated context for arbitrary user query types (impossible — Darker doesn't know the user's query types at compile time). AOT consumers who need source generation supply their own context via `QueryLoggingJsonOptions.Options.TypeInfoResolver`.

- **OOS6 — Backwards-compatibility shim that preserves the `JsonSerializerSettings` overload.** No `[Obsolete]` overload accepting `JsonSerializerSettings`. V5 is permitted to break; carrying the overload would require keeping `Newtonsoft.Json`.

- **OOS7 — Renaming `AddJsonQueryLogging`.** Issue #294 proposes "Update the package name from `AddJsonQueryLogging()` if appropriate, or keep it" — the requirement is **keep it**. The method name describes *what the user gets* (JSON-based query logging) not *which library implements it*.

- **OOS8 — `Paramore.Darker.QueryLogging.Newtonsoft` adapter package.** No adapter package is created. Users who want Newtonsoft serialisation write a ~10-line custom decorator.

- **OOS9 — Dropping `netstandard2.0`.** The core csproj keeps targeting `netstandard2.0;net8.0;net9.0`. Adding `System.Text.Json` as a `PackageReference` (FR6) is the chosen solution rather than dropping the TFM.

- **OOS10 — Performance benchmark.** Per the dropped NFR3, no BenchmarkDotNet comparison is added. If perf regressions are observed in practice, raise a new issue.

- **OOS11 — Full trim-safety.** Darker commits to AOT-*publishability* (NFR2) but not to full trim-safety. Consumers running `PublishTrimmed=true` who use arbitrary query types without `[DynamicallyAccessedMembers]` annotations or a source-generated `JsonSerializerContext` may see properties stripped from log output. Resolving this for arbitrary user types is impossible without source generation; FR11 demonstrates the supported AOT-safe pattern but does not extend it to general trim-safety.

- **OOS12 — Lock-after-use defensive coding.** If a consumer mutates `QueryLoggingJsonOptions.Options` after the first query has been processed (violating C6 and FR14), the resulting `InvalidOperationException` is surfaced unmodified. Darker does not wrap, catch, or translate this exception, nor does it implement a defensive copy-on-write to avoid it.

- **OOS13 — Verification of the parallel `WebApplicationFactory` failure mode.** C6 documents the parallel-integration-test limitation and prescribes `[CollectionDefinition(…, DisableParallelization = true)]` as the consumer-side mitigation. Darker does not ship a test fixture that reproduces the failure mode (i.e. that asserts a deterministic `InvalidOperationException` or race-related behaviour when two `WebApplicationFactory<TStartup>` hosts are constructed concurrently). The failure mode is described in the release notes; consumer reproductions and any further hardening are handled as follow-up issues. Rationale: building a robust race-reproducing fixture inside Darker's test suite is high-cost and low-value relative to the existing documentation-plus-mitigation contract — the failure surface lives in consumer code, not in Darker's.

## Acceptance Criteria

### How we'll know this is working correctly

1. **Build & dependency graph.**
   - `dotnet build Darker.Filter.slnf -c Release` succeeds on `net8.0` and `net9.0`.
   - `dotnet build Darker.slnx -c Release` succeeds for developers with the MAUI workload installed.
   - `Paramore.Darker.csproj` contains `<PackageReference Include="System.Text.Json" />` and does **not** contain `<PackageReference Include="Newtonsoft.Json" />`.
   - `Directory.Packages.props` contains `<PackageVersion Include="System.Text.Json" Version="10.0.8" />` and does **not** contain any `<PackageVersion Include="Newtonsoft.Json" ... />` entry.
   - `Newtonsoft.Json` does **not** appear anywhere in `dotnet list samples/SampleMinimalApi/SampleMinimalApi.csproj package --include-transitive` output for either `net8.0` or `net9.0`.
   - `Newtonsoft.Json` does **not** appear anywhere in `dotnet list src/Paramore.Darker/Paramore.Darker.csproj package --include-transitive` output for any TFM.
   - `Directory.Packages.props` pins `xunit.v3` and does **not** pin `xunit` 2.x. `xunit.runner.visualstudio` remains at 3.1.5 (or later) — its package id is unchanged. `xunit.analyzers` retains its existing package id, with a version bump permitted if required for xunit.v3 compatibility (verify at design time).
   - The 4 xunit-referencing test csprojs enumerated in FR12 (`test/Paramore.Darker.Core.Tests/Paramore.Darker.Core.Tests.csproj`, `test/Paramore.Darker.Extensions.Tests/Paramore.Darker.Extensions.Tests.csproj`, `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj`, `test/Paramore.Test.Helpers/Paramore.Test.Helpers.csproj`) reference `xunit.v3` (not `xunit`). `test/Paramore.Darker.Benchmarks/Paramore.Darker.Benchmarks.csproj` is unaffected (no xunit reference).

2. **Existing test suite still green.**
   - All tests in `Paramore.Darker.Core.Tests` and `Paramore.Darker.Extensions.Tests` pass on `net8.0` and `net9.0`.
   - The two tests covering serialiser injection are renamed, rewritten per FR10, and pass.

3. **New tests pass.**
   - The rewritten `When_AddJsonQueryLogging_called_should_configure_json_options` test asserts that the callback's mutation of `JsonSerializerOptions` is observable on `QueryLoggingJsonOptions.Options` after `AddJsonQueryLogging()` returns.
   - The rewritten `When_logging_decorator_executes_should_use_json_options` test captures the `ILogger.LogInformation` call and asserts the `{Query}` argument is the `System.Text.Json` serialisation of the test query.
   - The new FR11 AOT test (`LoggingQueryHandlerAOTTests` or similar) publishes with `PublishAot=true` on `net8.0` and `net9.0` and runs to green. Both subtests pass: (a) the property-bearing query test asserts the captured `{Query}` log argument equals the exact source-generated JSON (`{"Id":"<lowercase hyphenated Guid>","Name":"<Name>"}`); (b) the cycle-bearing query test asserts `Execute`/`ExecuteAsync` returns without throwing.
   - A new ordering test (FR14 verification) confirms that, in a fresh process, the following sequence holds inside a **single `[Fact]`**:
     1. Read `QueryLoggingJsonOptions.Options` — the options must **not** be locked at this point (assert by mutating any settable property, e.g. `QueryLoggingJsonOptions.Options.MaxDepth = 32;`, and observing no throw). This is an in-place property mutation on the existing instance (which preserves the FR3 defaults), distinct from C6's "direct assignment" path which replaces the instance entirely and drops defaults. FR14's lock-after-use invariant only constrains `Serialize` calls, so a pre-bootstrap property setter is safely callable; FR14's "after all DI bootstrap" canonical-sequence framing describes the consumer pattern, not this AC's ordering proof.
     2. Call `AddJsonQueryLogging(o => o.WriteIndented = true)` — the mutation must succeed (no throw).
     3. Execute one query through a `QueryProcessor` wired with a `[QueryLogging]`-decorated handler whose query type is the pinned `OrderingTestQuery` (see Test isolation below). The decorator's `Serialize<TQuery>` call locks the options. `OrderingTestQuery` must carry **at least one public property** so STJ's indenter has something to wrap — pinned shape: `public sealed class OrderingTestQuery : IQuery<OrderingTestQuery.Result> { public string Marker { get; init; } = "x"; public sealed class Result { } }`. Capture the `{Query}` log argument via the `LoggerCaptureFixture` and assert it equals the indented form `"{\n  \"Marker\": \"x\"\n}"` (use a normalised line-ending comparison — STJ emits `\n` regardless of platform). Empty-shape query types serialise as compact `{}` even under `WriteIndented = true` and are therefore not usable here.
     4. Call `AddJsonQueryLogging(o => o.WriteIndented = false)` — **the callback body must mutate a settable property** (empty callbacks do not exercise the lock and will not throw). This call must throw `InvalidOperationException` (lock-after-use), surfaced unmodified to the caller.
     - **Test isolation requirements** (because `JsonSerializerOptions` self-locks at process scope and the lock is irreversible):
       1. The `[Fact]` lives in a dedicated xUnit collection declared `[CollectionDefinition("QueryLoggingJsonOptionsOrdering", DisableParallelization = true)]`. A single `[Fact]` performs steps 1–4 in sequence — this avoids any cross-test ordering question.
       2. The handler/query used in step 3 is a **distinct test-query type** `OrderingTestQuery` (in `test/Paramore.Darker.Core.Tests/TestDoubles/OrderingTestQuery.cs`) that no other test in the assembly uses. This pins a unique closed generic `QueryLoggingDecorator<OrderingTestQuery, …>` whose cached `static readonly Logger` field is initialised exactly once, under this collection's `LoggerCaptureFixture` install — eliminating the cross-test cached-Logger race FR10's "disjoint test-query types" discipline addresses between assemblies but not within one.
       3. No other test in the same assembly may mutate `QueryLoggingJsonOptions.Options` after this collection has executed — the affected-files list in Additional Context documents the constraint.
     - **Alternative considered, not chosen**: spawning a fresh process per assertion via a small launcher. Rejected as over-engineering for a single ordering invariant; the single-`[Fact]` sequential approach above is sufficient.

4. **AOT publish succeeds on both TFMs.**
   - `dotnet publish test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj -c Release -f net8.0` succeeds.
   - `dotnet publish test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj -c Release -f net9.0` succeeds.
   - The published binary, when run, executes both FR11 AOT tests to green (property-bearing query asserts JSON content; cycle-bearing query asserts no-throw).
   - **AOT warning policy** — any analyser warning in the `IL3xxx` family (AOT-analysis) or `IL2xxx` family (trim-analysis) emitted from compilation units under `src/Paramore.Darker/Logging/` during AOT publish is a **FAIL**, with the following explicit allow-list:
     - **`IL2026` on `QueryLoggingDecorator<,>.Serialize` and `QueryLoggingDecoratorAsync<,>.Serialize`** (two methods on two types, not an overload set) — suppressed via `UnconditionalSuppressMessage` per FR13. Justification documented in the attribute and in NFR2.
     - **`IL3050` on `QueryLoggingDecorator<,>.Serialize` and `QueryLoggingDecoratorAsync<,>.Serialize`** (two methods on two types, not an overload set) — suppressed via `UnconditionalSuppressMessage` per FR13. Justification documented in the attribute and in NFR2.
     
     No other suppressions are permitted without amending this spec / a follow-up ADR. Suppressions are added via `UnconditionalSuppressMessage` (preferred) or `GlobalSuppressions.cs` (acceptable), with a `Justification` argument; bare `<NoWarn>` in csprojs is **not** acceptable because it hides warnings repo-wide rather than locally.
   - Warnings emitted from `System.Text.Json` source paths on user-supplied query types are acknowledged as user responsibility (NFR2 / OOS5) and do not block this AC. FR11's AOT test uses a source-generated `JsonSerializerContext` for **runtime trim-safety** (so the test query types' property metadata survives trimming and the content assertion's expected JSON matches what the decorator emits at runtime) — not for analyser-warning suppression, which is handled at the decorator's call site by FR13 regardless of the consumer's `JsonSerializerOptions` choice.

5. **Sample app still demonstrates query logging.**
   - `samples/SampleMinimalApi` builds and runs.
   - A `GET /people` request produces an `Information`-level log line of the form `"Executing async query GetPeopleQuery: {…}"` followed by `"Async execution of query GetPeopleQuery completed in {Elapsed}ms"`. (Note: the endpoint uses `ExecuteAsync`, and the query type is `GetPeopleQuery` — verified in `samples/SampleMinimalApi/QueryHandlers/GetPeopleQueryHandler.cs` and `samples/SampleMinimalApi/Program.cs:16`.)
   - The `{Query}` body in the log line is valid `System.Text.Json` output for `GetPeopleQuery` (currently a parameter-less `sealed class` with no public properties, so `{}` — verified in `samples/SampleMinimalApi/QueryHandlers/GetPeopleQueryHandler.cs:8`).
   - A `GET /people/{id:int}` request for an unknown id (exercising the `FallbackPolicyAttributeAsync` path on `GetPersonQueryHandler` in `samples/SampleMinimalApi/QueryHandlers/GetPersonQueryHandler.cs`) produces the with-fallback completion variant of the template: `"Async execution of query GetPersonNameQuery completed in {Elapsed}ms (with fallback)"` — confirming FR9's runtime-concatenated suffix continues to flow through. The query type is `GetPersonNameQuery` (the file is named `GetPersonQueryHandler.cs` but the query class is `GetPersonNameQuery`).

### Definition of done

- All functional requirements (FR1–FR14) implemented and covered by tests.
- All non-functional requirements (NFR1–NFR2) satisfied (verified via AC4 and FR11).
- The GitHub release notes for V5 include a migration entry covering:
  - Callback type change (`JsonSerializerSettings` -> `JsonSerializerOptions`)
  - Decorator constructor change (no serialiser parameter)
  - DI singleton removal (no `services.AddSingleton<JsonSerializerSettings>(...)` is performed by `AddJsonQueryLogging`)
  - `Newtonsoft.Json` dependency removal from `Paramore.Darker`
  - **Recommended migration** (preserves the `ReferenceHandler.IgnoreCycles` default):
    ```csharp
    services.AddDarker()
            .AddJsonQueryLogging(o => { o.WriteIndented = true; /* mutate as needed */ });
    ```
  - **Direct-assignment migration** (warning: drops `IgnoreCycles` unless re-applied):
    ```csharp
    QueryLoggingJsonOptions.Options = new JsonSerializerOptions
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles, // re-apply default
        WriteIndented = true,
    };
    ```
  - How to swap serialiser entirely: write a custom decorator (no interface to implement).
  - **AOT note**: `JsonSerializer.Serialize` on the decorator's hot path emits `IL2026`/`IL3050` warnings that Darker suppresses (FR13). Consumers running `PublishAot=true` who want a fully type-safe path supply a source-generated `JsonSerializerContext` via `QueryLoggingJsonOptions.Options.TypeInfoResolver`. Reflection-based fallback is supported but trim-unsafe.
  - **Known limitation — parallel integration tests**: consumer test suites that build multiple `WebApplicationFactory<TStartup>` hosts in parallel will race on the static `QueryLoggingJsonOptions.Options`. Serialise host construction with `[CollectionDefinition("DarkerHostBootstrap", DisableParallelization = true)]` on the relevant test collection. (See C6.)
  - (No `CHANGELOG.md` exists in the repo, so release notes is the canonical channel.)
  - **Note**: the xunit v2 → v3 upgrade (FR12) is an internal test-infrastructure change. `Paramore.Darker.Testing` (the shipped test-helper assembly) has no xunit dependency today and gains none under this work, so the upgrade is **not** a consumer-facing breaking change and does **not** appear in the release notes as one.
- Issue #294 closed.

> **Process note**: a new ADR will accompany this work per the `Requirements -> Design` workflow (CLAUDE.md). The ADR is created in the design phase under `docs/adr/`, numbered as the next available per repository convention. The ADR is not itself a requirement of this spec (i.e. its merge is governed by the spec workflow, not by this Definition of Done).

## Additional Context

- **Origin**: Proposed by @SebastianRobbins in V5 discussion #273; endorsed by maintainer.
- **Relationship to ADR 0011** ("Merge Builtin Decorators"): ADR 0011 moved the logging decorator into core, which made `Newtonsoft.Json` a direct dependency of every Darker consumer. That elevated the cost of the dependency and made this issue more pressing.
- **Brighter parity verified**: Brighter's `RequestLoggingHandler` (`src/Paramore.Brighter/Logging/Handlers/RequestLoggingHandler.cs`) calls `JsonSerializer.Serialize(request, JsonSerialisationOptions.Options)`. `JsonSerialisationOptions` (`src/Paramore.Brighter/JsonConverters/JsonSerialisationOptions.cs`) is a `public static class` with a settable `Options` property. No `IJsonSerializer` interface exists in Brighter. Brighter's migration happened in PR [#1470](https://github.com/BrighterCommand/Brighter/pull/1470) (2021).
- **Existing source files affected** (post-ADR-0011 layout):
  - `src/Paramore.Darker/Logging/Handlers/QueryLoggingDecorator.cs` — rewrite to call `JsonSerializer.Serialize(query, QueryLoggingJsonOptions.Options)`, drop ctor parameter and `ConfigurationException` path, add `UnconditionalSuppressMessage` for IL2026/IL3050 (FR13).
  - `src/Paramore.Darker/Logging/Handlers/QueryLoggingDecoratorAsync.cs` — same rewrite.
  - `src/Paramore.Darker/Logging/QueryProcessorBuilderExtensions.cs` — change callback type to `Action<JsonSerializerOptions>`, invoke against `QueryLoggingJsonOptions.Options`.
  - `src/Paramore.Darker.Extensions.DependencyInjection/QueryLoggingDIExtensions.cs` — change callback type, remove `services.AddSingleton(settings)`, drop `using Newtonsoft.Json`.
  - `src/Paramore.Darker/Paramore.Darker.csproj` — drop `Newtonsoft.Json` ref, add `System.Text.Json` ref.
  - `Directory.Packages.props` — drop `Newtonsoft.Json` pin, add `System.Text.Json` pin, swap `xunit` family for `xunit.v3` family (FR12).
  - The 4 test csprojs enumerated in FR12 (`test/Paramore.Darker.Core.Tests/Paramore.Darker.Core.Tests.csproj`, `test/Paramore.Darker.Extensions.Tests/Paramore.Darker.Extensions.Tests.csproj`, `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj`, `test/Paramore.Test.Helpers/Paramore.Test.Helpers.csproj`) — update `PackageReference` from `xunit` to `xunit.v3` (FR12). `test/Paramore.Darker.Benchmarks/Paramore.Darker.Benchmarks.csproj` is unaffected (no xunit reference).
  - `test/Paramore.Darker.Core.Tests/When_logging_decorator_executes_should_use_injected_serializer_settings.cs` — rename + rewrite per FR10.
  - `test/Paramore.Darker.Extensions.Tests/When_AddJsonQueryLogging_called_should_register_serializer_settings.cs` — rename + rewrite per FR10.
  - `test/Paramore.Darker.Core.Tests/When_logging_decorator_executes_without_settings_should_throw_ConfigurationException.cs` — **delete** per FR10 (the `ConfigurationException` throw site this test exercises is removed by FR8; the `ConfigurationException` type itself stays in `src/Paramore.Darker/Exceptions/ConfigurationException.cs` because it is used elsewhere).
  - `test/Paramore.Test.Helpers/TestOutput/ICoreTestOutputHelper.cs`, `test/Paramore.Test.Helpers/TestOutput/CoreTestOutputHelper.cs`, `test/Paramore.Test.Helpers/Base/ITestClassBase.cs`, `test/Paramore.Test.Helpers/Base/TestClassBase.cs`, `test/Paramore.Darker.Tests.AOT/Base/AOTTestClassBase.cs`, `test/Paramore.Darker.Tests.AOT/QueryProcessor/AOTQueryProcessorTests.cs` — remove `using Xunit.Abstractions;` per FR12 item 1 (the `ITestOutputHelper` symbol moves to the `Xunit` namespace under `xunit.v3`).
  - `test/Paramore.Test.Helpers/Base/TestClassBase.cs` (lines 49 and 107-110 specifically) — additional rework per FR12 item 6: replace the `GetField("test", BindingFlags.Instance | BindingFlags.NonPublic)` reflection with `TestContext.Current.Test` (or accept the `null` fallback per FR12 item 6's option (b)). The `ITest` cast at line 49 may need to change to `IXunitTest` under v3.
- **New source files expected** (final names decided at design):
  - `src/Paramore.Darker/Logging/QueryLoggingJsonOptions.cs` — static class, mirrors Brighter's `JsonSerialisationOptions`.
  - `test/Paramore.Darker.Tests.AOT/LoggingQueryHandlerAOTTests.cs` (or similar) — AOT coverage for the `[QueryLogging]` path per FR11, including the `AotTestJsonContext : JsonSerializerContext` source-generated context.
  - `test/Paramore.Darker.Core.Tests/TestDoubles/CoreLoggingTestQuery.cs` and `test/Paramore.Darker.Extensions.Tests/TestDoubles/ExtensionsLoggingTestQuery.cs` — disjoint test query types per FR10's cross-assembly closed-generic discipline.
  - `test/Paramore.Darker.Core.Tests/TestDoubles/OrderingTestQuery.cs` — distinct test-query type reserved for the AC3 FR14 ordering test, ensuring its `QueryLoggingDecorator<OrderingTestQuery, …>` closed-generic Logger field is initialised exactly once under the `QueryLoggingJsonOptionsOrdering` collection (per AC3 test-isolation requirement #2).
  - `test/Paramore.Darker.Core.Tests/LoggerCaptureFixture.cs` (and Extensions equivalent) — `IAssemblyFixture<LoggerCaptureFixture>` implementation per FR10.
