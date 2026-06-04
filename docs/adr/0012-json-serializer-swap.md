# 12. Swap Newtonsoft.Json for System.Text.Json in the Query Logging Decorator

Date: 2026-05-29

## Status

Accepted

## Context

**Parent Requirement**: [specs/006-json_serializer/requirements.md](../../specs/006-json_serializer/requirements.md)

**Scope**: This ADR covers the **serialiser swap** in the query logging decorator: removing `Newtonsoft.Json` as a direct dependency of `Paramore.Darker`, replacing it with `System.Text.Json`, and choosing the **configuration shape** that consumers use to customise the serialiser. It explicitly addresses (a) the choice between a static mutable-global options class, a DI-registered singleton, and a pluggable serialiser interface; (b) the configuration-callback contract on the public extension methods; (c) the default `JsonSerializerOptions` content; (d) the AOT analyser-warning policy at the decorator's call site; and (e) the startup-only configuration contract that makes the mutable-global pattern safe.

It does **not** cover:
- The test-infrastructure upgrade from `xunit` 2.x to `xunit.v3` (FR12). That upgrade is in scope for the *requirement* because it unblocks the FR10 log-capture mechanism (`IAssemblyFixture<T>`), but the test framework choice is an internal test-project decision, not a consumer-facing architectural one. If a separate ADR is warranted, it can be raised as `0013-xunit-v3-upgrade.md`.
- The relocation/structure of the logging decorator inside core. ADR 0011 already settled that (`src/Paramore.Darker/Logging/Handlers/`).
- Whether the logging decorator should live in core at all. ADR 0011 settled that.

### Problem

Post-ADR-0011, the query logging decorator lives in core (`src/Paramore.Darker/Logging/`). This made `Newtonsoft.Json` a **direct** dependency of every consumer of `Paramore.Darker` — a regression for anyone who has otherwise removed `Newtonsoft.Json` from their dependency graph. The codebase needs a JSON serialiser for the `{Query}` log argument, but the choice is no longer optional-per-package; it's mandatory for every consumer.

Three forces interact:

1. **Dependency-graph cleanliness.** `Newtonsoft.Json` is widely-used and self-contained (no transitive package dependencies of its own at 13.x). The actual cost is the assembly itself plus its place as a direct dependency of every Darker consumer post-ADR-0011 — see Consequences for the honest accounting of the `netstandard2.0` package-surface trade. The .NET ecosystem has converged on `System.Text.Json` for new code — `Microsoft.Extensions.Logging.Console`'s `JsonConsoleFormatter`, ASP.NET Core's response serialisation, `Microsoft.Extensions.Configuration.Json` (indirectly), etc. Carrying `Newtonsoft.Json` as a direct dependency of core is increasingly out-of-step with the ecosystem.

2. **Brighter alignment.** ADR 0011 was an alignment ADR — Darker now matches Brighter's per-feature folder layout and assembly partition. Brighter swapped its `RequestLoggingHandler` from `Newtonsoft.Json` to `System.Text.Json` in 2021 (PR [#1470](https://github.com/BrighterCommand/Brighter/pull/1470)) using a `public static class JsonSerialisationOptions` with a settable `Options` property. There is no `IJsonSerializer` / `IRequestSerializer` interface in Brighter. Darker drifting from that pattern would re-introduce divergence in the very place ADR 0011 just closed.

3. **AOT-publishability.** Darker's AOT test project (`Paramore.Darker.Tests.AOT`) builds under `PublishAot=true` on `net8.0` and `net9.0`. `JsonSerializer.Serialize<T>(T, JsonSerializerOptions)` is annotated `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` in .NET 8+, so any call to it from core emits `IL2026` / `IL3050` warnings — which a strict AOT-warning policy would treat as a build failure. The decorator has no statically-known query type (it's generic over `TQuery`) so the warnings are unavoidable at the call site, regardless of which serialiser-options shape is chosen.

The architectural question is therefore not "which serialiser?" — `System.Text.Json` is the obvious pick — but **"what configuration shape does the decorator use to call the serialiser, and how does that shape interact with DI lifetimes, AOT, and Brighter parity?"**

### Forces

- **Brighter parity.** Brighter's `RequestLoggingHandler` uses a `public static class JsonSerialisationOptions` with a settable `Options` property. No `IJsonSerializer` interface. Darker mirroring this means a user reading both source trees sees the same pattern in the same place. Diverging means the next reviewer asks "why does Darker have an `IQueryLoggingSerializer` interface when Brighter has none?" — and the answer would need to be principled, not historical.

- **DI lifetime hazards.** A `JsonSerializerOptions` registered as a DI singleton would need to interact with `IDarkerHandlerBuilder.Services.AddSingleton(...)`. But the decorator is resolved per-query (`PipelineBuilder` calls `IQueryHandlerDecoratorFactory` on every `Execute`/`ExecuteAsync`), and the factory contract doesn't pre-define how serialiser settings flow into a decorator instance. A static global sidesteps the question entirely — there's no DI plumbing to get wrong.

- **`JsonSerializerOptions` self-locks on first use as a failure signal, NOT a safety net.** `JsonSerializerOptions` self-locks on first use; mutations after that throw `InvalidOperationException`. This catches *post-startup mutation* in a *single-host* process — useful as a diagnostic signal, but it is **not** a safety net for the mutable-global pattern at large. It does **not** catch: (a) two consumer hosts (e.g. parallel `WebApplicationFactory<>` instances in an integration-test suite) bootstrapping concurrently and racing on `AddJsonQueryLogging(o => …)`; (b) torn cross-thread reads if a consumer reassigns `Options` while another thread is mid-`Serialize`; (c) silent process-wide effect when feature-flag-driven runtime code mutates options after bootstrap. The startup-only contract (Decision step 8) is the actual safety story; the self-lock is a useful diagnostic for one specific failure mode out of several. A defensive process-wide lock on the global is considered overkill for the documented usage pattern — see Decision step 8 for the explicit trade-off.

- **Pluggable serialiser interface adds complexity for an unclear consumer.** Issue #294 lists pluggability as a *consideration*, not a requirement. The decorator-pattern escape hatch already exists — a consumer who wants Newtonsoft, MessagePack, or anything else writes a custom decorator and skips `AddJsonQueryLogging()`. Adding an `IQueryLoggingSerializer` interface would: (a) introduce a public type with no Brighter equivalent; (b) require either a DI registration path or a constructor-parameter wiring; (c) impose a per-call virtual dispatch on the logging hot path; (d) need versioning across V5+ of Darker. Trade-off doesn't favour the interface.

- **AOT call-site warning is unavoidable.** `JsonSerializer.Serialize<T>(T, JsonSerializerOptions)` is annotated regardless of which `JsonSerializerOptions` instance is passed. The warning fires at the call site in the decorator. Options:
  - **Suppress at the call site with `UnconditionalSuppressMessage`.** Localised, justified, documented.
  - **Propagate `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` to public methods.** Forces every consumer of `AddJsonQueryLogging` to suppress or annotate, virally. Bad ergonomics.
  - **Add `<NoWarn>IL2026;IL3050</NoWarn>` to the csproj.** Hides the warnings repo-wide rather than at the specific known-safe call site. Bad signal-to-noise — a *new* future call site would be silently ignored.
  - The suppress-at-call-site option is the only one that's both localised and honest about what's safe and what isn't.

- **Reference cycles on EF Core-backed queries.** Query objects sourced from EF entities frequently have navigation-property cycles (parent ↔ child). `System.Text.Json`'s default behaviour is to **throw** `JsonException` on cycles. A serialisation throw on the logging path masks the underlying query result with a logging failure — a bad diagnostic outcome. The default must accommodate cycles. `ReferenceHandler.IgnoreCycles` silently emits `null` at the cycle point, which is the right semantic for log output.

- **Default-instance vs replacement semantics.** Two ergonomic paths exist for consumers:
  - Mutate the existing `Options` instance via a callback: `o => o.WriteIndented = true`. Preserves any defaults Darker set (notably `ReferenceHandler.IgnoreCycles`).
  - Replace the `Options` instance entirely: `QueryLoggingJsonOptions.Options = new JsonSerializerOptions { WriteIndented = true }`. Silently drops `ReferenceHandler.IgnoreCycles` unless the consumer re-applies it.
  
  Both are useful — the callback for "I want Darker's defaults plus my tweaks", direct assignment for "I want full control". Supporting both is cheap at the property level (one settable property + one callback hook), but the direct-assignment path carries a real consumer-side cost: it silently drops `ReferenceHandler.IgnoreCycles` (and any other future class-init defaults) unless the consumer explicitly re-applies them. The only safety net is the release-notes migration entry — there is no compile-time guard, no setter-side warning, no audit when an assigned `Options` lacks `IgnoreCycles`. We accept this regression risk because Brighter parity and the operational simplicity of "your object, your defaults" outweigh adding a defensive setter that re-applies the cycle handler. The cost is real but bounded — a single documented bullet in the migration notes.

### Constraints

- **Mirror Brighter on naming and shape.** `public static class QueryLoggingJsonOptions` with `public static JsonSerializerOptions Options { get; set; }` — same shape as Brighter's `JsonSerialisationOptions`. The Darker class is named `QueryLoggingJsonOptions` (not just `JsonOptions`) because Darker may grow future JSON options for non-logging concerns; the name disambiguates.

- **Public method names preserved.** `AddJsonQueryLogging()` (DI extension) and `JsonQueryLogging()` (builder extension) keep their names. Only the callback delegate type changes (`Action<JsonSerializerSettings>` → `Action<JsonSerializerOptions>`).

- **`netstandard2.0` target retained.** Core targets `netstandard2.0;net8.0;net9.0`. `System.Text.Json` ships out-of-band on `netstandard2.0` (NuGet `PackageReference` required) and via the shared framework on `net8.0`/`net9.0`. A single `PackageReference` in `Paramore.Darker.csproj` covers all TFMs uniformly.

- **No backwards-compatibility shim.** V5 is the breaking-version window; consumers migrate via release notes. No `[Obsolete]` overload accepting `JsonSerializerSettings` (that would force `Newtonsoft.Json` to stay).

- **Decorator stays in core.** ADR 0011 settled placement; this ADR does not re-debate it.

- **Log message templates unchanged.** Only the JSON body inside `{Query}` may differ in formatting; the surrounding `"Executing query …"` / `"Execution of query …"` templates stay byte-identical. The runtime-concatenated `" (with fallback)"` suffix stays as-is — refactoring it to a structured placeholder is a separate concern.

## Decision

Replace `Newtonsoft.Json` with `System.Text.Json` in the query logging decorator, configured via a **mutable-global static class** that mirrors Brighter's `JsonSerialisationOptions` exactly.

The full decision:

1. **Add `public static class QueryLoggingJsonOptions`** at `src/Paramore.Darker/Logging/QueryLoggingJsonOptions.cs` with a single member: `public static JsonSerializerOptions Options { get; set; }`, initialised at class-init to a new `JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles }`. The setter throws `ArgumentNullException` on `null` assignment. **Brighter divergence on default content**: the *shape* (static class, settable property, null-guard) mirrors Brighter's `JsonSerialisationOptions` exactly; the *default content* diverges by adding `ReferenceHandler.IgnoreCycles`. Brighter's default does not set this — its request types are not typically EF-Core entity graphs with cycles. Darker adds it because query objects sourced from EF-Core navigation properties commonly contain parent↔child cycles, and a serialisation throw on the logging hot path masks the underlying query result with a logging failure (per Forces). Parity is on the *surface*, not on the defaults — flagged explicitly so future maintainers reconcile any Brighter-side default changes against this divergence.

2. **Rewrite `QueryLoggingDecorator<TQuery, TResult>` and `QueryLoggingDecoratorAsync<TQuery, TResult>`** to serialise the query via `System.Text.Json` at the `Serialize<T>` private method. The decorator constructors no longer take a serialiser parameter. The `ConfigurationException("No serializer settings are configured…")` throw site is removed — `Options` is never `null` at the call site (class-init guarantees it; setter null-guard preserves it).

   **Implementation-time correction (runtime-type serialisation).** The pipeline closes the decorator over `IQuery<TResult>`, not the concrete query type (`PipelineBuilder.cs:214` does `MakeGenericType(typeof(IQuery<TResult>), typeof(TResult))`, because `PipelineBuilder<TResult>` is generic only over `TResult` and has no concrete-query type parameter). `System.Text.Json`'s generic `JsonSerializer.Serialize<T>(value, options)` resolves the `JsonTypeInfo` from `typeof(T)` — which here is the bare marker interface `IQuery<TResult>` — and would emit `{}` for every query. `Newtonsoft.Json` masked this by reflecting over the runtime object. The correct call is therefore the **runtime-type overload**: `JsonSerializer.Serialize(value, value.GetType(), QueryLoggingJsonOptions.Options)`. This matches Newtonsoft's observable behaviour, composes with FR11's source-generated `JsonSerializerContext` (the runtime type is the one registered via `[JsonSerializable]`), and leaves the FR13 suppressions on the same `Serialize<T>` method. This supersedes the one-argument `Serialize(value, Options)` form sketched in the Key Components snippet below.

3. **Suppress `IL2026` and `IL3050` at the `Serialize<T>` method with `UnconditionalSuppressMessage`** attributes carrying explicit justifications referencing NFR2 and the "consumer responsibility" contract for AOT-safe `JsonSerializerOptions` content. The suppressions live on the method, not on the csproj. **Caller-propagation risk**: because `Serialize<T>` takes an unconstrained generic `T`, the analyser may also surface `IL2026` / `IL3050` at the *caller* of `Serialize<T>` (i.e. inside `Execute<TQuery>` / `ExecuteAsync<TQuery>`) — BCL trim/AOT annotations can propagate up unconstrained generic call chains. AC4's allow-list (FR13 in requirements) names only the two `Serialize` methods today; if the AOT publish surfaces caller-site warnings, the allow-list expands to include the calling methods, with the same justification. The expansion is implementation-time discovery, not a defect in this ADR. The principle stays: suppressions live at known-safe sites with explicit justification, never via repo-wide `<NoWarn>`.

   **Implementation-time correction (AOT verification harness — 2026-06-02).** The AOT-publishability claims this ADR makes (Context force 3; Consequences › Positive › "AOT publish succeeds…"; the trim-safety risk) were originally to be verified by the existing `Paramore.Darker.Tests.AOT` **xunit test project** published under `PublishAot=true`. Two facts surfaced during Step 9 that make that harness both unworkable and unnecessary:

   1. **The test framework needn't be AOT-compiled — only the product code.** What FR11 / AC4 actually require is that *a consumer of `Paramore.Darker`* publishes cleanly under native AOT (no `IL2xxx` / `IL3xxx` outside the FR13 allow-list) and exhibits the correct runtime behaviour (source-generated JSON; `IgnoreCycles` default). Nothing requires that consumer to be an xunit host.
   2. **The xunit.v3 host cannot be AOT-published here.** FR12's xunit 2.x → v3 upgrade turned the test projects into executables. `Paramore.Darker.Tests.AOT` references two of them (`Paramore.Darker.Core.Tests`, `Paramore.Test.Helpers`) purely for shared types; a self-contained AOT executable cannot reference non-self-contained executables (`NETSDK1150`), and AOT-publishing a reflection-driven test runner is itself fraught.

   **Amended decision.** The AOT verification harness is a **plain console application** (`OutputType=Exe`, `PublishAot=true`, `TrimMode=full`, `net8.0;net9.0`) that references only the product libraries (`Paramore.Darker` + Extensions.DI + Testing), defines its own minimal query/handler doubles plus a source-generated `JsonSerializerContext`, runs the two FR11 scenarios, and exits non-zero on mismatch. `dotnet publish -c Release -f <tfm> -r <rid>` followed by executing the published binary is the AC4 proof (locally and in CI). This drops the two test-project references (resolving `NETSDK1150`) and removes all xunit / `Test.Helpers` / `Core.Tests.Exported` usage from the harness.

   **Consequent structural changes.** (a) `Paramore.Test.Helpers`, referenced only by the former AOT test host, loses its sole consumer and is removed **in full** — including `TestClassBase` / `ITestClassBase` and the FR12-item-6 meta-test (`When_TestClassBase_XunitTest_accessed_under_xunit_v3…`). The meta-test is **dropped, not relocated**: it pinned the xunit.v3 reflection rework of `TestClassBase` (FR12 item 6's log-naming hazard), whose only consumer was the deleted AOT host. With the harness no longer using `TestClassBase`, that hazard cannot occur and the test would otherwise exercise deleted code; FR12 item 6 is thereby satisfied vacuously. No acceptance criterion (AC1–AC5) depends on it. (b) `<IsAotCompatible>true</IsAotCompatible>` stays on `src/Paramore.Darker/Paramore.Darker.csproj` so the FR13 suppressions are validated at the library's own build. Enabling it also surfaces ~28 **pre-existing** trim/AOT warnings in the reflection pipeline (`PipelineBuilder`, `QueryHandlerRegistry*`) — all **outside** `src/Paramore.Darker/Logging/`, all warnings-only (build succeeds), and therefore all outside AC4's scope. They are recorded as a known AOT limitation in the release notes and **not** silenced with `<NoWarn>`.

   This supersedes the "xunit AOT test project" framing wherever it appears in the task breakdown (Step 9) and final validation (Step 13). It changes no product-code decision in this ADR — the decorator, suppressions, options class, and callback contract are unaffected.

4. **Change the configuration callback type** on `AddJsonQueryLogging` (DI) and `JsonQueryLogging` (builder) from `Action<JsonSerializerSettings>` to `Action<JsonSerializerOptions>`. The callback (when supplied) is invoked once with `QueryLoggingJsonOptions.Options` as its argument — mutating the existing default instance in place.

5. **Single call-site discipline for the callback.** The canonical implementation lives in `Paramore.Darker.Logging.QueryProcessorBuilderExtensions.AddJsonQueryLogging<TBuilder>(...)` with the generic constraint `where TBuilder : IQueryProcessorExtensionBuilder`. The DI extension `Paramore.Darker.Extensions.DependencyInjection.QueryLoggingDIExtensions.AddJsonQueryLogging(IDarkerHandlerBuilder, ...)` forwards directly — `IDarkerHandlerBuilder` implements `IQueryProcessorExtensionBuilder` (verified: `src/Paramore.Darker.Extensions.DependencyInjection/IDarkerHandlerBuilder.cs:8`). The legacy builder method `JsonQueryLogging(IBuildTheQueryProcessor, ...)` forwards via the existing precedent in `Policies/QueryProcessorBuilderExtensions.cs:11-14`: **cast `IBuildTheQueryProcessor` to the concrete `QueryProcessorBuilder` and throw `NotSupportedException` if the consumer supplied a custom `IBuildTheQueryProcessor` implementation**. This is a real, documented limitation — consumers using a custom builder type cannot use the builder-surface JSON-logging entry point. The single-call-site guarantee holds for the in-box surfaces (DI + concrete `QueryProcessorBuilder`); custom builders fall outside this guarantee per the documented limitation.

6. **Remove the DI singleton registration of serialiser settings.** `QueryLoggingDIExtensions.AddJsonQueryLogging` no longer calls `IDarkerHandlerBuilder.Services.AddSingleton(settings)`. There is no DI registration for `JsonSerializerOptions` at all — consumers configure it via the callback or by direct static assignment.

7. **Direct assignment to `QueryLoggingJsonOptions.Options` is supported** but documented as the "you own all the defaults" path. Direct assignment drops `ReferenceHandler.IgnoreCycles` (and any future FR3 defaults) unless the consumer re-applies them. The release notes call this out.

8. **Startup-only configuration contract.** `QueryLoggingJsonOptions.Options` is contractually intended to be configured at application startup, *before any query is handled*. Violating this contract surfaces one of three concrete failure modes — none defended against, all observable:
   - **(a)** A second mutator after the first `Serialize` call hits `InvalidOperationException` — `JsonSerializerOptions` self-locks on first use (the common single-host case).
   - **(b)** Racing reference-assignments to `Options` produce a torn cross-thread read on the decorator side. CLR-level reference-assignment is atomic per the spec, but cross-thread visibility is unordered without a memory barrier, so a `Serialize` call mid-race may observe either the old or new instance.
   - **(c)** Two parallel host bootstraps (e.g. xUnit's default-parallel `WebApplicationFactory<>` integration suites) race on `AddJsonQueryLogging(o => …)`; the loser's callback completes against a now-locked instance and throws `InvalidOperationException` mid-startup. Consumers running such suites must serialise host construction with `[CollectionDefinition(..., DisableParallelization = true)]` on the relevant collection.
   
   Darker provides no thread-safety guarantee on the static read-during-write path, does not catch or wrap any of the above, and considers a defensive process-wide lock on the global to be overkill for the documented usage pattern. A lock would add per-`Serialize` contention on the logging hot path in exchange for protecting against a contract violation; the existing self-lock at least signals (a), (b) is bounded to single-instance reference semantics, and (c) has a known consumer-side mitigation. This matches Brighter's implicit contract on `JsonSerialisationOptions.Options` — both libraries make the same trade.

9. **Add `System.Text.Json` as a direct `PackageReference` of `Paramore.Darker`** for all TFMs uniformly (required by `netstandard2.0`; redundant-but-harmless on `net8.0`/`net9.0`). Pin the version in `Directory.Packages.props` aligned to the `Microsoft.Extensions.*` 10.0.8 family already present (`System.Text.Json` 10.0.8).

10. **Remove `Newtonsoft.Json` as a direct dependency of `Paramore.Darker`.** Drop the `PackageReference` from `src/Paramore.Darker/Paramore.Darker.csproj` and the `PackageVersion` from `Directory.Packages.props`.

### Architecture Overview

The role decomposition is intentionally minimal — the static-global pattern is, by design, a small surface:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│   ROLE: Information holder (static mutable global)                      │
│   ┌──────────────────────────────────────────────────────────────┐     │
│   │   QueryLoggingJsonOptions                                    │     │
│   │   ─────────────────────────                                  │     │
│   │   • Knows: the JsonSerializerOptions instance to use         │     │
│   │   • Decides: nothing — it's a holder                         │     │
│   │   • Does: null-guards on set                                 │     │
│   │                                                              │     │
│   │   Default value: new JsonSerializerOptions {                 │     │
│   │       ReferenceHandler = ReferenceHandler.IgnoreCycles       │     │
│   │   }                                                          │     │
│   └──────────────────────────────────────────────────────────────┘     │
│                                ▲                                        │
│                                │ reads (every query)                    │
│                                │                                        │
│   ROLE: Service provider (per-query decorator)                          │
│   ┌──────────────────────────────────────────────────────────────┐     │
│   │   QueryLoggingDecorator<TQuery, TResult>                     │     │
│   │   QueryLoggingDecoratorAsync<TQuery, TResult>                │     │
│   │   ─────────────────────────────────────────                  │     │
│   │   • Knows: its inner handler, the cached static Logger       │     │
│   │   • Decides: when to log, what to log                        │     │
│   │   • Does: serialise query body to JSON, log start/complete   │     │
│   │                                                              │     │
│   │   private string Serialize<T>(T value) =>                    │     │
│   │       JsonSerializer.Serialize(value,                        │     │
│   │           QueryLoggingJsonOptions.Options);                  │     │
│   └──────────────────────────────────────────────────────────────┘     │
│                                ▲                                        │
│                                │ writes (once at startup)               │
│                                │                                        │
│   ROLE: Coordinator (extension methods, single call site)               │
│   ┌──────────────────────────────────────────────────────────────┐     │
│   │   QueryProcessorBuilderExtensions                            │     │
│   │   .AddJsonQueryLogging<TBuilder>(                            │     │
│   │       this TBuilder, Action<JsonSerializerOptions>?)         │     │
│   │   ─────────────────────────────────────────────              │     │
│   │   • Knows: how to register the decorator                     │     │
│   │   • Decides: when to invoke the callback                     │     │
│   │   • Does: invoke callback against                            │     │
│   │     QueryLoggingJsonOptions.Options,                         │     │
│   │     register decorator types                                 │     │
│   │                                                              │     │
│   │   Other surfaces forward here:                               │     │
│   │     • JsonQueryLogging(IBuildTheQueryProcessor, …)           │     │
│   │     • AddJsonQueryLogging(IDarkerHandlerBuilder, …)          │     │
│   └──────────────────────────────────────────────────────────────┘     │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

      Lifetime / temporal ordering (the "startup-only" contract):

         Application startup                Query execution
         ────────────────────                ──────────────────
         class-init: Options =               decorator reads Options
           defaults                          JsonSerializer.Serialize
              │                              locks Options on first call
              ▼                                       │
         AddJsonQueryLogging(o => …)                  ▼
           callback mutates Options          (any subsequent mutation
              │                               throws InvalidOperationException
              ▼                               — surfaced unmodified)
         (more callbacks / direct
          assignment ok)
              │
              ▼
        ─────────── first query ──────────────►  (locked from here)
```

The arrows above are deliberate:
- The static `QueryLoggingJsonOptions` is **read-only after first query** (enforced by `JsonSerializerOptions`'s self-lock).
- The decorator's dependency on options is via **late binding** (it reads the static each call, not at construction). This allows the callback in step 4 of the lifecycle to mutate options after the decorator type has been registered but before any query has been executed — the canonical bootstrap pattern.
- The coordinator role is **the only writer** in the normal flow. Direct assignment to `QueryLoggingJsonOptions.Options` is supported but classified as an alternative path.

### Key Components

#### `QueryLoggingJsonOptions` (new)

`src/Paramore.Darker/Logging/QueryLoggingJsonOptions.cs`. Static class. Single role: information holder for the global `JsonSerializerOptions`. Its null-guarded setter is the only behaviour it has — everything else is data.

```csharp
public static class QueryLoggingJsonOptions
{
    private static JsonSerializerOptions _options = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public static JsonSerializerOptions Options
    {
        get => _options;
        set => _options = value ?? throw new ArgumentNullException(nameof(value));
    }
}
```

#### `QueryLoggingDecorator<,>` / `QueryLoggingDecoratorAsync<,>` (modified)

Existing types under `src/Paramore.Darker/Logging/Handlers/`. Constructor parameter removed. `Serialize<T>` body changes to call `JsonSerializer.Serialize(value, QueryLoggingJsonOptions.Options)`. `UnconditionalSuppressMessage` attributes for `IL2026` / `IL3050` added to the `Serialize<T>` method, with `Justification` arguments referencing the consumer-responsibility contract.

#### `QueryProcessorBuilderExtensions` (modified, becomes canonical site)

Existing class under `src/Paramore.Darker/Logging/`. The `AddJsonQueryLogging<TBuilder>(...)` overload (generic-constrained `where TBuilder : IQueryProcessorExtensionBuilder`) becomes the single site that (a) invokes the consumer's `Action<JsonSerializerOptions>` callback against `QueryLoggingJsonOptions.Options`, and (b) registers `QueryLoggingDecorator<,>` and `QueryLoggingDecoratorAsync<,>` with the builder. The `JsonQueryLogging(IBuildTheQueryProcessor, …)` overload in the same class casts to concrete `QueryProcessorBuilder` and throws `NotSupportedException` if the cast fails (matching the precedent in `Policies/QueryProcessorBuilderExtensions.cs:11-14`). Custom `IBuildTheQueryProcessor` implementations are not supported through the builder surface — this is the documented limitation called out in Decision step 5.

#### `QueryLoggingDIExtensions` (modified, becomes a forwarder)

Existing class under `src/Paramore.Darker.Extensions.DependencyInjection/`. The `AddJsonQueryLogging(IDarkerHandlerBuilder, ...)` method becomes a thin forwarder to `QueryProcessorBuilderExtensions.AddJsonQueryLogging<IDarkerHandlerBuilder>(builder, configure)`. The `IDarkerHandlerBuilder.Services.AddSingleton(settings)` call is removed entirely.

### Technology Choices

| Choice                                  | Rationale                                                                                                                                                  |
|-----------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `System.Text.Json` (vs. `Newtonsoft.Json`) | Ecosystem alignment; smaller transitive surface on `netstandard2.0`; first-class AOT story (with documented limitations); Brighter parity.                  |
| `System.Text.Json` 10.0.8                | Aligns with `Microsoft.Extensions.Logging` / `Microsoft.Extensions.DependencyInjection` 10.0.8 family already pinned in `Directory.Packages.props`.        |
| `ReferenceHandler.IgnoreCycles` default  | Logging-time serialisation that throws on cycles masks the query result with a serialisation failure — bad diagnostic outcome. Silent drop is better for logs. |
| Static class, mutable global             | Brighter parity. No DI lifetime question. Late-bound read by the decorator means callback-after-registration just works.                                    |
| `Action<JsonSerializerOptions>` callback | Mirrors `Newtonsoft.Json`'s `Action<JsonSerializerSettings>` shape exactly; minimum-friction migration for consumers; supports defaults-plus-tweaks pattern. |
| `UnconditionalSuppressMessage` at call site | Localised to the known-safe call; documents the trade-off in code (`Justification`); doesn't hide future warnings elsewhere in the codebase.              |
| No `IQueryLoggingSerializer` interface   | Brighter has none; pluggability has the decorator-pattern escape hatch; interface adds public surface and per-call virtual dispatch for no clear consumer. |

### Implementation Approach

The work splits cleanly along Tidy First lines (see [refactor/tidy-first](../../.claude/commands/refactor/tidy-first.md)):

**Structural (no behaviour change):**
- None. The serialiser swap is inherently behavioural — the JSON body inside `{Query}` will format differently for some inputs (see Consequences). There's no purely-structural prep that makes the swap safer.

**Behavioural:**
1. Add `QueryLoggingJsonOptions` static class (new file, isolated from existing code; no callers yet).
2. Add `System.Text.Json` `PackageReference` to `Paramore.Darker.csproj` and `PackageVersion` to `Directory.Packages.props`.
3. Rewrite `QueryLoggingDecorator.cs` / `QueryLoggingDecoratorAsync.cs` to use `QueryLoggingJsonOptions.Options` + `JsonSerializer.Serialize`, add `UnconditionalSuppressMessage` on `Serialize<T>`, drop constructor parameter, drop `ConfigurationException` path.
4. Change callback delegate types and forwarding in `QueryProcessorBuilderExtensions.cs` and `QueryLoggingDIExtensions.cs`. Remove the `AddSingleton(settings)` call.
5. Delete `Newtonsoft.Json` `PackageReference` from `Paramore.Darker.csproj` and `PackageVersion` from `Directory.Packages.props`.
6. Update tests (per FR10 in the requirements).
7. Add the AOT test with the `JsonSerializerContext` source-generation pattern (per FR11).

The order matters: steps 1–4 leave the code in a compileable state at each cut (the rewrites in step 3 happen *after* the new options class exists in step 1). Step 5 is the dependency removal; it can only happen after no production source references `Newtonsoft` types. Step 6 (the test rewrites) is gated by step 5 *only* for the two specific tests that currently `using Newtonsoft.Json`; the other test changes can land independently.

## Consequences

### Positive

- **`Newtonsoft.Json` is no longer in the dependency graph of `Paramore.Darker`.** Consumers who'd otherwise eliminated `Newtonsoft.Json` from their app stop pulling it back in via the logging decorator. Direct removal from `Paramore.Darker.csproj` and `Directory.Packages.props`; verified via `dotnet list package --include-transitive` (AC1).
- **Brighter parity restored at the JSON-options layer.** A user reading both repos sees the same static-class-with-mutable-global pattern, same null-guard, same default-instance-with-callback ergonomics. No "why are these two different" question.
- **AOT publish succeeds with a documented trim-safety boundary.** `System.Text.Json` has a documented AOT-publish-safe path (source-generated `JsonSerializerContext`). Darker publishes under `PublishAot=true` on `net8.0` and `net9.0` with `IL2026` / `IL3050` suppressed via `UnconditionalSuppressMessage` on the decorator's `Serialize<T>` method (and possibly its callers per Decision step 3's caller-propagation note), each with an explicit `Justification` — not via repo-wide `<NoWarn>`. **Trim-safety remains the consumer's responsibility** per OOS11 / NFR2: consumers running `PublishTrimmed=true` whose query types lack `[DynamicallyAccessedMembers]` annotations or a source-generated `JsonSerializerContext` may see properties stripped from log output at runtime. The opt-in path is `QueryLoggingJsonOptions.Options.TypeInfoResolver = MyContext.Default;` — Darker improves AOT *publishability* and gives consumers a clean opt-in to full AOT-safety; it does not silently guarantee runtime trim-safety for arbitrary query types.
- **Cycle handling no longer surprises EF Core consumers.** `ReferenceHandler.IgnoreCycles` is the default; entity-backed query objects with navigation cycles serialise to log output without exception.
- **Ecosystem alignment on the dependency graph (the *direction* is the win, not the size).** Honest accounting: on `netstandard2.0` the swap *increases* the transitive package surface — `Newtonsoft.Json` 13.x declares no package dependencies of its own, while `System.Text.Json` 10.x pulls `System.Memory`, `System.Text.Encodings.Web`, `System.Buffers`, `System.Threading.Tasks.Extensions`, and friends. On `net8.0` / `net9.0` the shared framework absorbs the cost entirely (assembly is in-box). The trade is qualitative: Darker's deps now match the rest of the ecosystem's defaults, the AOT story is supported first-class on the in-box TFMs, and the `netstandard2.0` surface growth is bounded to well-known OOB packages that most consumer apps already pull through other Microsoft.* references. The size argument cuts the other way on `netstandard2.0`; the *direction* argument is the real win.
- **DI lifetime question disappears.** No `JsonSerializerOptions` singleton registration to mismatch with `IQueryProcessor`'s scoped lifetime. The static is process-global by construction.

### Negative

- **JSON body formatting differs for some inputs.** `DateTime`, `DateTimeOffset`, `decimal`, and `enum` are the well-known differences (see C2 in requirements). For most query types — primitives, strings, `Guid` — the output is identical. For consumers whose queries embed `DateTime` properties, log lines change shape (shortest-round-trippable vs fixed-precision). This is acceptable for log output (information loss is zero either way) but consumers running log-content monitors or dashboards keyed on `DateTime` format will see breakage.
- **`MaxDepth` default drops from 128 (Newtonsoft) to 64 (STJ).** Queries that produce nesting deeper than 64 will fail under STJ defaults where they would have passed under Newtonsoft. Consumers must raise `QueryLoggingJsonOptions.Options.MaxDepth` if they hit this. Documented in the release notes.
- **Mutable global is a known smell.** A static settable property crosses the DI-purity boundary and complicates testing. The mitigations are (a) Brighter precedent — this is the established pattern in the sibling library; (b) `JsonSerializerOptions`'s self-lock-after-first-use enforces the startup-only contract at runtime; (c) tests that mutate the global save-and-restore via `try`/`finally` (per FR10 / C5).
- **No per-app or per-test isolation.** Two `WebApplicationFactory<TStartup>` hosts in the same process share `QueryLoggingJsonOptions.Options`. Consumer test suites running parallel integration hosts must `DisableParallelization` on the relevant collection (per C6 in requirements). Documented in release notes.
- **Pluggable-serialiser scenarios require a custom decorator.** A consumer who wants MessagePack, Protobuf, or anything non-JSON in the `{Query}` log argument writes a ~10-line custom decorator that wraps their own serialiser, and skips `AddJsonQueryLogging()`. The decorator-pattern escape hatch is the supported path.
- **Migration cost for consumers.** Callback type changes (`Action<JsonSerializerSettings>` → `Action<JsonSerializerOptions>`). Consumers who construct `JsonSerializerSettings` in test setup or in their startup migrate to `JsonSerializerOptions`. The mapping is reasonably mechanical (property names differ — `ContractResolver` → `PropertyNamingPolicy`, etc.) but it's still a hand-edit. V5 breaking-version window absorbs this.

### Risks and Mitigations

- **Risk: A consumer's `JsonSerializerSettings`-based test fixture silently breaks.** The build will fail (delegate type mismatch on the callback), so this is a *compile-time* risk, not a *runtime* risk. The compile error is the mitigation.
- **Risk: A consumer relies on Newtonsoft's `DateTime` format in log monitors.** Mitigation: documented in release notes (C2). No code-level mitigation possible — the format change is intrinsic to STJ.
- **Risk: An AOT consumer with a non-source-generated `JsonSerializerOptions` sees stripped properties at runtime.** Mitigation: NFR2 documents this; FR11 demonstrates the source-generated `JsonSerializerContext` pattern; OOS11 explicitly disclaims full trim-safety. AOT publish succeeds (the warnings are suppressed); runtime output may be incomplete for unpreserved types. Consumer fixes by supplying a context.
- **Risk: Test suite races on `QueryLoggingJsonOptions.Options`.** Mitigation: FR10's `IAssemblyFixture<LoggerCaptureFixture>` pattern (xunit.v3 from FR12); cross-assembly disjoint test-query types; per-test save-and-restore on the static. AC3 pins a `[CollectionDefinition(..., DisableParallelization = true)]` for the lock-after-use ordering test.
- **Risk: `IL2026` / `IL3050` suppressions become stale (e.g. BCL adds a new warning category and we miss it).** Mitigation: AC4's *categorical* rule — "any `IL3xxx` or `IL2xxx` warning under `src/Paramore.Darker/Logging/`" is a CI failure, with the explicit allow-list. A new warning category would surface in CI on the next build.
- **Risk: `System.Text.Json` 10.0.8 drifts from the `Microsoft.Extensions.*` family.** Mitigation: pinning is centralised in `Directory.Packages.props`; the version is bumped in the same PR as any future `Microsoft.Extensions.*` bump.
- **Risk: Future changes to the class-init default content of `QueryLoggingJsonOptions.Options` are silent behaviour changes.** A V6+ maintainer wanting to add a `JsonConverter`, raise `MaxDepth`, or set a `PropertyNamingPolicy` cannot do so without (a) silently altering callback-path consumers' log output, and (b) being a no-op for direct-assignment consumers (who built their own `Options` instance). There is no compile-time signal at either site. Mitigation: treat the class-init defaults as part of the public API surface, governed by semver — material default changes belong in a major release with a release-notes migration entry mirroring the V5 entry shape. Forward-versioning of the parity surface is otherwise unguarded; this risk is inherited from Brighter's parity pattern by design.

## Alternatives Considered

### 1. Pluggable `IQueryLoggingSerializer` interface

Define a `public interface IQueryLoggingSerializer { string Serialize<T>(T value); }`. The decorator depends on it via constructor. The DI extension registers a default implementation (`SystemTextJsonQueryLoggingSerializer`); consumers can register their own to swap serialiser.

**Why not chosen:**
- Brighter has no such interface — divergence at the very alignment we're trying to keep tight.
- The decorator pattern is already the escape hatch for "swap serialiser entirely" — a custom decorator is ~10 lines for a sync-only quick-and-dirty case, closer to ~50 lines for a production-quality sync+async pair (cached `static readonly Logger`, fallback-bag handling, sync and async start/complete templates). Modest cost, but not zero — the interface alternative would not eliminate it for non-JSON serialisers (a `MessagePack` consumer still authors the decorator pair).
- Adds public API surface (`IQueryLoggingSerializer` becomes versioned) for an unclear consumer.
- Per-call virtual dispatch on a logging hot path.
- The factory plumbing for a "decorator that depends on an interface that is DI-registered" is messier than the static-global path — Darker's `IQueryHandlerDecoratorFactory` doesn't have a clean way to pass the interface into the decorator's constructor without either pulling DI into the factory or pulling the factory into DI.

### 2. DI-registered `JsonSerializerOptions` singleton

`AddJsonQueryLogging` registers `services.AddSingleton(new JsonSerializerOptions { … })` (already in the v4 code). The decorator's constructor takes `JsonSerializerOptions options`, resolved per-decorator-instantiation.

**Why not chosen** (load-bearing reason first):
- **`SimpleHandlerDecoratorFactory` consumers have no `IServiceProvider`.** The decorator factory `IQueryHandlerDecoratorFactory` is constructed per-pipeline. A consumer using `SimpleHandlerDecoratorFactory` (delegate-based, no DI in the loop) cannot resolve a singleton — there is no container to ask. The static global works for this case; the DI-singleton alternative does not. This is the genuine architectural reason.
- **Brighter doesn't do this; it uses the static-global pattern.**
- **Asymmetric surfaces.** `IDarkerHandlerBuilder.Services` is the DI hook; the builder-extension path (`JsonQueryLogging(IBuildTheQueryProcessor, …)`) has no `Services` collection. A DI-only registration mechanism cannot serve both surfaces uniformly. The asymmetry is not by itself a knockdown — the codebase already accepts equivalent asymmetry in `Policies/QueryProcessorBuilderExtensions.cs` (builder-surface writes to `QueryProcessorBuilder.PolicyRegistry`, DI-surface writes to `IServiceCollection`) — but combined with the `SimpleHandlerDecoratorFactory` point it eliminates the singleton path entirely.
- **Adds a constructor parameter** to the decorator; FR8 explicitly removes the constructor parameter, simplifying the per-query allocation path.

### 3. Per-decorator `JsonSerializerOptions` (no shared state)

Each `AddJsonQueryLogging` call captures its own `JsonSerializerOptions` instance in a closure, passes it via the decorator factory. No global state.

**Why not chosen:**
- Brighter uses a global; divergence.
- The decorator's `Serialize<T>` method needs the options somehow. Without DI, without a global, without a constructor parameter, the only remaining mechanism is a static-per-closed-generic field (`QueryLoggingDecorator<TQuery, TResult>.Options`). That's *more* hidden state, not less — and worse, the per-closed-generic shape means the configuration would be impossible to set for closed generics that haven't yet been instantiated.
- The "no shared state" property is illusory anyway — the cached `static readonly Logger` already gives the decorator process-scope state. Fighting only the options field is inconsistent.

### 4. Use the existing `Newtonsoft.Json` static-options pattern (do nothing)

Keep `Newtonsoft.Json`; expose its `JsonSerializer.Settings` directly.

**Why not chosen:** This is the status quo and is what the requirement is designed to change. `Newtonsoft.Json` carries the regression noted in the Problem section: post-ADR-0011 it's a direct dependency of every Darker consumer. Ecosystem direction is `System.Text.Json`.

### 5. Drop `netstandard2.0` instead of adding `System.Text.Json` `PackageReference`

`System.Text.Json` is in-box on `net6.0+`. Dropping `netstandard2.0` would remove the need for an explicit `PackageReference`.

**Why not chosen:** OOS9 explicitly keeps `netstandard2.0`. Consumers on `net472` (still common in some enterprise estates) consume `Paramore.Darker` via the `netstandard2.0` target. Dropping that target is a much larger break than the serialiser swap, and is out of scope for this work.

### 6. Wrap `JsonSerializer.Serialize` in `[RequiresUnreferencedCode]` on the public API

Propagate the AOT-warning attributes outward instead of suppressing them. Every consumer of `AddJsonQueryLogging` would then have to suppress the warning at their call site, or annotate their query types.

**Why not chosen:** The warning is unavoidable at the decorator's call site regardless of consumer behaviour. Propagating it makes every consumer pay an annotation cost for a runtime behaviour that, in practice, is fine for the overwhelming majority of query types (any type whose properties are statically reachable from a root). The `UnconditionalSuppressMessage` at the call site with explicit justification is more honest: "we have inspected this site; the consumer-responsibility contract documents the trim-safety trade-off; the warning is suppressed locally with a paper trail."

## References

- Requirements: [specs/006-json_serializer/requirements.md](../../specs/006-json_serializer/requirements.md)
- Related ADRs:
  - [0011 Merge Builtin Decorators](./0011-merge-builtin-decorators.md) — established that the logging decorator lives in core, which made `Newtonsoft.Json` a direct dependency and motivated this ADR.
  - [0010 Pass Query Context](./0010-pass-query-context.md) — added `Polly` as a direct core dependency, set the precedent for "direct dependency in core is acceptable when it serves a core concern".
- External references:
  - [Brighter PR #1470](https://github.com/BrighterCommand/Brighter/pull/1470) — Brighter's 2021 swap of `Newtonsoft.Json` for `System.Text.Json` in `RequestLoggingHandler`.
  - [Brighter `JsonSerialisationOptions`](https://github.com/BrighterCommand/Brighter/blob/main/src/Paramore.Brighter/JsonConverters/JsonSerialisationOptions.cs) — the static-class options pattern this ADR mirrors.
  - [Issue #294](https://github.com/BrighterCommand/Darker/issues/294) — original issue proposing the swap.
  - [V5 discussion #273](https://github.com/BrighterCommand/Darker/discussions/273) — design discussion that scoped the work.
  - [`System.Text.Json` AOT documentation](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation) — the source-generated `JsonSerializerContext` pattern referenced by FR11 and NFR2.
  - [`UnconditionalSuppressMessage` documentation](https://learn.microsoft.com/dotnet/api/system.diagnostics.codeanalysis.unconditionalsuppressmessageattribute) — the attribute used in FR13 for the IL2026/IL3050 suppression.
