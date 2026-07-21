# Ralph Tasks: 014-Caching-Decorator

> Auto-generated from the approved design for unattended TDD execution.
> Each task is self-contained with all context a fresh Claude session needs.

## Spec Context

- **Spec**: 014-Caching-Decorator
- **Requirements**: specs/014-Caching-Decorator/requirements.md
- **ADRs**: docs/adr/0021-caching-decorator-architecture.md

## Tasks

- [x] **Add Microsoft.Extensions.Caching.Hybrid to Directory.Packages.props (prerequisite)**
  - **Behavior**: Central Package Management gains a pinned `Microsoft.Extensions.Caching.Hybrid` version so the new caching package and its tests can reference it. The package is not currently referenced anywhere in the repo. Pin the **concrete latest stable 9.x** patch — not a preview, not 10.x — to match the repo's net8.0/net9.0 targeting.
  - **Test file**: _none — pure scaffolding (Central Package Management edit)._
  - **Test should verify**:
    - No behavioral test. Correctness is proven by a clean restore/build of the solution filter.
  - **Implementation files**:
    - `Directory.Packages.props` - first resolve the concrete newest stable 9.x version (e.g. `dotnet package search Microsoft.Extensions.Caching.Hybrid --take 20` or nuget.org), then add `<PackageVersion Include="Microsoft.Extensions.Caching.Hybrid" Version="9.0.N" />` with that exact resolved version (NOT a `9.0.x` token — a literal `x` is an invalid NuGet version and will break restore) to the existing `<ItemGroup>` of `PackageVersion` entries.
  - **RALPH-VERIFY**: `dotnet build Darker.Filter.slnf -c Release`
  - **References**: requirements.md (FR7, Targeting NFR, Constraints — "not yet referenced anywhere in the repo"); ADR 0021 (Implementation Approach step 1, Technology Choices); `Directory.Packages.props` (existing CPM layout)

- [ ] **Create the Paramore.Darker.Caching project (net8.0;net9.0)**
  - **Behavior**: A new source project `Paramore.Darker.Caching` exists, targeting **net8.0 and net9.0 only** (deliberately NOT netstandard2.0), referencing the Darker core project and `Microsoft.Extensions.Caching.Hybrid` — and **NOT** OpenTelemetry or any metrics package. It is added to `Darker.slnx` and `Darker.Filter.slnf` so it builds with the rest of the solution.
  - **Test file**: _none — pure scaffolding (new csproj + solution wiring)._
  - **Test should verify**:
    - No behavioral test. Correctness is proven by the project compiling and appearing in the filtered solution build.
  - **Implementation files**:
    - `src/Paramore.Darker.Caching/Paramore.Darker.Caching.csproj` - `<TargetFrameworks>net8.0;net9.0</TargetFrameworks>`, `<Nullable>enable</Nullable>`, `<ProjectReference Include="..\Paramore.Darker\Paramore.Darker.csproj" />`, `<PackageReference Include="Microsoft.Extensions.Caching.Hybrid" />` (version comes from CPM). Model on `src/Paramore.Darker.Validation/Paramore.Darker.Validation.csproj` but without netstandard2.0.
    - `Darker.slnx` - add the new project.
    - `Darker.Filter.slnf` - add `src\\Paramore.Darker.Caching\\Paramore.Darker.Caching.csproj` to the projects array.
  - **RALPH-VERIFY**: `dotnet build Darker.Filter.slnf -c Release`
  - **References**: requirements.md (FR7, Targeting NFR, Resolved Decision 5); ADR 0021 (Key Components — "Package — `Paramore.Darker.Caching` … depends on … NOT on OpenTelemetry"); `src/Paramore.Darker.Validation/Paramore.Darker.Validation.csproj` (csproj template); `Darker.Filter.slnf` (existing project list)

- [ ] **Create the Paramore.Darker.Caching.Tests project**
  - **Behavior**: A new test project `Paramore.Darker.Caching.Tests` exists (mirroring the validation feature's test-project layout), targeting net8.0 and net9.0, referencing `Paramore.Darker.Caching`, the DI extensions project, xunit.v3, and Shouldly. It is added to `Darker.Filter.slnf`. A single trivial passing test proves the project runs under `dotnet test`.
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_test_project_bootstrapped_should_run.cs`
  - **Test should verify**:
    - A `[Fact]` named `When_test_project_bootstrapped_should_run` asserts `true.ShouldBeTrue()` so the runner and project wiring are proven.
  - **Implementation files**:
    - `test/Paramore.Darker.Caching.Tests/Paramore.Darker.Caching.Tests.csproj` - `<TargetFrameworks>net8.0;net9.0</TargetFrameworks>`, references to `src/Paramore.Darker.Caching`, `src/Paramore.Darker.Extensions.DependencyInjection`, and the standard test packages (model on `test/Paramore.Darker.Validation.Tests/Paramore.Darker.Validation.Tests.csproj`).
    - `test/Paramore.Darker.Caching.Tests/When_test_project_bootstrapped_should_run.cs` - the trivial fact.
    - `Darker.Filter.slnf` - add `test\\Paramore.Darker.Caching.Tests\\Paramore.Darker.Caching.Tests.csproj`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_test_project_bootstrapped_should_run"`
  - **References**: requirements.md (test-project placement mirrors validation); `test/Paramore.Darker.Validation.Tests/Paramore.Darker.Validation.Tests.csproj` (template); `Darker.Filter.slnf`

- [ ] **Add cache semantic-convention constants to core DarkerSemanticConventions**
  - **Behavior**: The core `DarkerSemanticConventions` static class exposes the three cache constants next to `MeterName` / `QueryDurationAllowedTags`: `CacheOutcome = "paramore.darker.cache.outcome"` (span-attribute / counter-dimension key, value `"hit"`/`"miss"`), `CacheRequestsMetricName = "paramore.darker.cache.requests"` (counter instrument name), and a low-cardinality allowed-tag set `CacheRequestsAllowedTags = { QueryType, CacheOutcome }` (a `FrozenSet<string>` on net8.0+, `HashSet<string>` otherwise — mirroring `QueryDurationAllowedTags`).
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_reading_cache_semantic_conventions_should_expose_cache_names_and_tags.cs`
  - **Test should verify**:
    - `DarkerSemanticConventions.CacheOutcome` equals `"paramore.darker.cache.outcome"`.
    - `DarkerSemanticConventions.CacheRequestsMetricName` equals `"paramore.darker.cache.requests"`.
    - `CacheRequestsAllowedTags` contains exactly `QueryType` and `CacheOutcome` (and excludes high-cardinality keys like `QueryId`).
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/DarkerSemanticConventions.cs` - add the three constants/sets in the "Meter / metric names" and "Per-instrument allowed-tag sets" regions, following the existing `#if NET8_0_OR_GREATER` FrozenSet pattern used by `QueryDurationAllowedTags`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_reading_cache_semantic_conventions_should_expose_cache_names_and_tags"`
  - **References**: requirements.md (FR10); ADR 0021 (Key Components — "Core — `Paramore.Darker` (`DarkerSemanticConventions`) additions"); `src/Paramore.Darker/Observability/DarkerSemanticConventions.cs` (~line 96+, existing `MeterName` / `QueryDurationAllowedTags`); `test/Paramore.Darker.Core.Tests/When_reading_metric_semantic_conventions_should_expose_meter_and_metric_names.cs` (existing convention-test pattern)

- [ ] **DefaultCacheKeyGenerator produces the deterministic default key from query type + invariant JSON**
  - **Behavior**: Introduce the `ICacheKeyGenerator` role (`string GenerateKey(object query)`, operating on the **runtime object** — never `typeof(TQuery)`), its default implementation `DefaultCacheKeyGenerator`, and the marker interface `IAmCacheable { string CacheKey { get; } }`. For a query that does **not** implement `IAmCacheable`, `GenerateKey` returns `query.GetType().FullName + "|" + <deterministic, culture-invariant JSON of the query's public readable properties>`. The JSON body is stable across runs: properties ordered by name (ordinal), `CultureInfo.InvariantCulture` formatting, explicit `null` properties. Worked example (FR5): a `GetUser(int UserId)` query with `UserId = 42` yields a key ending in `|{"UserId":42}`; `UserId = 43` yields a distinct body.
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_generating_default_key_should_be_deterministic_and_distinct_per_input.cs`
  - **Test should verify**:
    - For a test `GetUser` record/class with `UserId = 42`, the key equals `typeof(GetUser).FullName + "|{\"UserId\":42}"` (assert the `|{"UserId":42}` body exactly and that the key starts with the type's `FullName`).
    - `GetUser(43)` produces a **different** key (distinct body `|{"UserId":43}`).
    - Two **different query types with identical property shape and values** (e.g. `GetUser(int Id)` and `GetOrder(int Id)`, both `Id = 42`) produce **different** keys (FR5 "distinct queries … do not collide" — proven by the `Type.FullName` prefix, not merely implied).
    - Calling `GenerateKey` twice on equal inputs yields the identical string (determinism across invocations).
  - **Implementation files**:
    - `src/Paramore.Darker.Caching/ICacheKeyGenerator.cs` - `string GenerateKey(object query)`.
    - `src/Paramore.Darker.Caching/DefaultCacheKeyGenerator.cs` - reflect on `query.GetType()`; serialize public readable properties with `System.Text.Json` using a fixed ordinal property ordering and `CultureInfo.InvariantCulture`, emitting explicit nulls.
    - `src/Paramore.Darker.Caching/IAmCacheable.cs` - `public interface IAmCacheable { string CacheKey { get; } }` (namespace `Paramore.Darker.Caching`).
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_generating_default_key_should_be_deterministic_and_distinct_per_input"`
  - **References**: requirements.md (FR4, FR5); ADR 0021 (Key Components — `ICacheKeyGenerator` / `DefaultCacheKeyGenerator`, and Risks — "cache-key logic reflects on `typeof(TQuery)` … instead of the runtime object")

- [ ] **DefaultCacheKeyGenerator uses IAmCacheable.CacheKey when the query implements it**
  - **Behavior**: When the runtime query object implements `IAmCacheable`, `DefaultCacheKeyGenerator.GenerateKey` returns the query's `CacheKey` verbatim instead of the default type+JSON strategy. Example (FR5): a query with `CacheKey => $"GetUser-{UserId}"` and `UserId = 42` yields `"GetUser-42"`.
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_query_is_cacheable_should_use_its_cache_key.cs`
  - **Test should verify**:
    - A test query implementing `IAmCacheable` with `CacheKey => $"GetUser-{UserId}"` and `UserId = 42` produces exactly `"GetUser-42"` (not the type+JSON form).
    - The returned key does not contain the type `FullName` or a `|` separator (proving the default strategy was bypassed).
  - **Implementation files**:
    - `src/Paramore.Darker.Caching/DefaultCacheKeyGenerator.cs` - branch: `if (query is IAmCacheable c) return c.CacheKey;` before the default strategy.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_query_is_cacheable_should_use_its_cache_key"`
  - **References**: requirements.md (FR4, FR5); ADR 0021 (Key Components — `DefaultCacheKeyGenerator`, "`query is IAmCacheable c` → return `c.CacheKey`")

- [ ] **DefaultCacheKeyGenerator fails fast on a null/empty/whitespace IAmCacheable.CacheKey**
  - **Behavior**: If a query implements `IAmCacheable` but its `CacheKey` returns `null`, empty, or whitespace **at runtime**, `GenerateKey` throws `Paramore.Darker.Exceptions.ConfigurationException` rather than caching under an empty/colliding key. (The non-nullable annotation on `CacheKey` is compile-time only.)
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_cacheable_key_is_null_or_whitespace_should_throw_configuration_exception.cs`
  - **Test should verify**:
    - A query whose `CacheKey` returns `null` causes `GenerateKey` to throw `ConfigurationException`.
    - A query whose `CacheKey` returns `""` throws `ConfigurationException`.
    - A query whose `CacheKey` returns `"   "` (whitespace) throws `ConfigurationException`.
  - **Implementation files**:
    - `src/Paramore.Darker.Caching/DefaultCacheKeyGenerator.cs` - after reading `c.CacheKey`, `if (string.IsNullOrWhiteSpace(key)) throw new ConfigurationException(...)`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_cacheable_key_is_null_or_whitespace_should_throw_configuration_exception"`
  - **References**: requirements.md (FR4, Acceptance Criteria); ADR 0021 (Key Components — `IAmCacheable` "A runtime `null`/empty/whitespace value fails fast with `ConfigurationException`"); `src/Paramore.Darker/Exceptions/` (existing `ConfigurationException`)

- [ ] **Async caching decorator + AddCaching: cache hit skips the handler, miss runs it once and populates (minimal vertical slice)**
  - **Behavior**: Introduce the smallest async caching pipeline that has a passing end-to-end test — **only** the constructs this test exercises (no `CacheOutcome` enum, no `CachingOptions` type yet; those arrive in the later tasks that first use them). This slice creates: `CacheableQueryAttributeAsync(int step, int expirationSeconds)` deriving from `QueryHandlerAttributeAsync` (`GetDecoratorType()` returns the concrete `typeof(CacheableQueryDecoratorAsync<,>)`, `GetAttributeParams()` returns `new object[] { expirationSeconds }`); the concrete `CacheableQueryDecoratorAsync<TQuery,TResult>` (constructor takes `IServiceProvider`); and a parameterless `AddCaching(this IDarkerHandlerBuilder)` extension. The decorator resolves `HybridCache` from the service provider, delegates key computation to `ICacheKeyGenerator` on the **runtime** query object, and calls `HybridCache.GetOrCreateAsync(key, factory, options)` where the factory sets a local `bool ran = true` and invokes `next`. On a **miss** the factory runs (`ran == true`) and the result is stored; on a **hit** the factory never runs and the cached result is returned without invoking `next`. (The span-attribute write and the `CacheOutcome` enum are added later; here the `ran` flag exists but its outcome is not yet recorded.) **This must be proven end-to-end through a real `QueryProcessor`** with a `[CacheableQueryAsync]`-annotated handler and a registered Microsoft `HybridCache` — a directly-instantiated decorator exercises a resolution path the pipeline never uses (see the runtime-type trap) and can go false-green.
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_cached_query_executed_twice_should_run_handler_once_and_serve_hit.cs`
  - **Test should verify**:
    - Build a real `ServiceProvider`: `AddDarker().AddAsyncHandlers(...)/AddHandlersFromAssemblies(...).AddCaching()`, register Microsoft `HybridCache` via `services.AddHybridCache()`, and register a singleton handler-execution recorder (call-count) injected into the handler — modelled on `test/Paramore.Darker.Validation.FluentValidation.Tests/When_validated_query_executed_through_processor_should_validate.cs`.
    - First `ExecuteAsync(query)` returns the handler result and the recorder shows the handler ran exactly once (a miss populated the cache).
    - Second `ExecuteAsync(query)` with the same key returns the same result and the recorder count is **still 1** (a hit — the handler was not re-run).
  - **Implementation files**:
    - `src/Paramore.Darker.Caching/CacheableQueryAttributeAsync.cs` - `(int step, int expirationSeconds)`, step-first; `GetDecoratorType()` → `typeof(CacheableQueryDecoratorAsync<,>)`; `GetAttributeParams()` → `new object[] { expirationSeconds }`.
    - `src/Paramore.Darker.Caching/CacheableQueryDecoratorAsync.cs` - concrete `IQueryHandlerDecoratorAsync<TQuery,TResult>`; resolve `HybridCache` + `ICacheKeyGenerator` from the injected `IServiceProvider`; `GetOrCreateAsync` with the query passed as state to avoid per-call closure allocation.
    - `src/Paramore.Darker.Caching/CachingDarkerBuilderExtensions.cs` - parameterless `AddCaching(this IDarkerHandlerBuilder)`: `builder.RegisterDecorator(typeof(CacheableQueryDecoratorAsync<,>))` and register `DefaultCacheKeyGenerator` as `ICacheKeyGenerator` on `builder.Services`. (The `Action<CachingOptions>` overload is added in the later opt-in task.)
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_cached_query_executed_twice_should_run_handler_once_and_serve_hit"`
  - **References**: requirements.md (FR1, FR3, FR7, FR8 async, Acceptance Criteria); ADR 0021 (Architecture Overview diagram, Decision, Implementation Approach steps 2–4, "End-to-end pipeline tests are mandatory"); `src/Paramore.Darker/PipelineBuilder.cs:404` (decorators closed over `IQuery<TResult>`); `src/Paramore.Darker.Validation.FluentValidation/FluentValidationDarkerBuilderExtensions.cs` (DI registration template); `src/Paramore.Darker/Logging/QueryProcessorBuilderExtensions.cs` (`RegisterDecorator` usage); `src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionDarkerHandlerBuilder.cs` (`RegisterDecorator`); `test/Paramore.Darker.Validation.FluentValidation.Tests/When_validated_query_executed_through_processor_should_validate.cs` (real-processor test template)

- [ ] **Expiry maps to HybridCacheEntryOptions.Expiration and the handler re-runs after it elapses**
  - **Behavior**: `InitializeFromAttributeParams(new object[] { expirationSeconds })` reads `expirationSeconds` and maps it to `HybridCacheEntryOptions.Expiration = TimeSpan.FromSeconds(expirationSeconds)`, passed on every `GetOrCreateAsync` call. `LocalCacheExpiration` (L1) is left to HybridCache's default and is not set in v1. After the configured expiry elapses, a subsequent execution re-runs the handler (cache entry expired).
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_cache_entry_expires_should_rerun_handler.cs`
  - **Test should verify**:
    - Through a real `QueryProcessor` with a short `expirationSeconds` (e.g. 1), a first call runs the handler (recorder count 1), an immediate second call is a hit (count still 1), and after waiting slightly longer than the expiry a third call re-runs the handler (count 2).
    - (If the loop environment disallows real waiting, force expiry by advancing the HybridCache's clock / using a 1-second expiry with a short real delay; keep the delay minimal.)
  - **Implementation files**:
    - `src/Paramore.Darker.Caching/CacheableQueryDecoratorAsync.cs` - store the mapped `HybridCacheEntryOptions` from `InitializeFromAttributeParams` and pass it to `GetOrCreateAsync`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_cache_entry_expires_should_rerun_handler"`
  - **References**: requirements.md (FR2, Acceptance Criteria — "after the configured expiry elapses, the handler re-runs"); ADR 0021 (Technology Choices — `HybridCacheEntryOptions.Expiration`, "`LocalCacheExpiration` … not set in v1"); `src/Paramore.Darker/PipelineBuilder.cs:263` (`InitializeFromAttributeParams` at build time)

- [ ] **Non-positive expirationSeconds fails fast at pipeline build**
  - **Behavior**: A `[CacheableQueryAsync(step, expirationSeconds)]` with `expirationSeconds <= 0` throws `Paramore.Darker.Exceptions.ConfigurationException` inside `InitializeFromAttributeParams` — which `PipelineBuilder` invokes while **building** the pipeline (before any handler runs). There is no silent default and no caching with an undefined lifetime.
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_expiration_seconds_not_positive_should_throw_at_pipeline_build.cs`
  - **Test should verify**:
    - Through a real `QueryProcessor`, executing a handler annotated with `expirationSeconds: 0` throws `ConfigurationException`, and the handler-execution recorder shows the handler never ran (failure is at build, before dispatch).
    - A negative value (e.g. `-5`) likewise throws `ConfigurationException`.
  - **Implementation files**:
    - `src/Paramore.Darker.Caching/CacheableQueryDecoratorAsync.cs` - in `InitializeFromAttributeParams`, `if (expirationSeconds <= 0) throw new ConfigurationException(...)` before mapping to `Expiration`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_expiration_seconds_not_positive_should_throw_at_pipeline_build"`
  - **References**: requirements.md (FR2, Acceptance Criteria); ADR 0021 (Forces — "the correct, honest place to validate `expirationSeconds > 0` and fail fast"); `src/Paramore.Darker/PipelineBuilder.cs:263`

- [ ] **Missing HybridCache registration fails fast (FR12)**
  - **Behavior**: If a query is annotated `[CacheableQueryAsync]` and `AddCaching()` is called, but **no `HybridCache` is registered in DI**, the decorator throws `Paramore.Darker.Exceptions.ConfigurationException` (naming the registration required) rather than silently bypassing the cache.
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_hybrid_cache_not_registered_should_throw_configuration_exception.cs`
  - **Test should verify**:
    - Build a real `QueryProcessor` with `AddCaching()` but **without** `services.AddHybridCache()`; executing the cacheable query throws `ConfigurationException`.
    - The exception message references the missing `HybridCache` registration.
  - **Implementation files**:
    - `src/Paramore.Darker.Caching/CacheableQueryDecoratorAsync.cs` - `var cache = serviceProvider.GetService<HybridCache>() ?? throw new ConfigurationException("...register a HybridCache...");`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_hybrid_cache_not_registered_should_throw_configuration_exception"`
  - **References**: requirements.md (FR12); ADR 0021 (Forces — "Fail fast (FR12)", Architecture Overview diagram — `?? throw new ConfigurationException`)

- [ ] **IAmCacheable query caches under its own key end-to-end**
  - **Behavior**: A `[CacheableQueryAsync]` handler whose query implements `IAmCacheable` caches under the query's `CacheKey` (not the default strategy) when driven through a real `QueryProcessor`, and two calls with the same `CacheKey` serve a hit. This proves the runtime `query is IAmCacheable` path works through the pipeline (where `TQuery` is `IQuery<TResult>`).
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_cacheable_query_executed_through_processor_should_cache_under_its_key.cs`
  - **Test should verify**:
    - Two executions of an `IAmCacheable` query with the same `CacheKey` run the handler once (recorder count 1 after the second call).
    - Two `IAmCacheable` queries whose `CacheKey`s differ each run the handler (distinct entries — recorder count 2), proving the key drives entry identity.
  - **Implementation files**:
    - _No new implementation — exercises the existing decorator + `DefaultCacheKeyGenerator` end-to-end. Add only the test double query/handler._
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_cacheable_query_executed_through_processor_should_cache_under_its_key"`
  - **References**: requirements.md (FR4, FR5, Acceptance Criteria); ADR 0021 ("End-to-end pipeline tests are mandatory", runtime-type risk); `src/Paramore.Darker/PipelineBuilder.cs:404`

- [ ] **Null/empty runtime CacheKey fails fast end-to-end through the pipeline**
  - **Behavior**: When a `[CacheableQueryAsync]` query implements `IAmCacheable` but its `CacheKey` returns null/empty/whitespace at runtime, executing it through a real `QueryProcessor` surfaces `ConfigurationException` (from `DefaultCacheKeyGenerator`) and the handler does not run under an empty key.
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_runtime_cache_key_is_empty_should_throw_through_processor.cs`
  - **Test should verify**:
    - Executing an `IAmCacheable` query whose `CacheKey` is `""` (or whitespace) through the processor throws `ConfigurationException`.
    - The handler-execution recorder shows the handler never ran.
  - **Implementation files**:
    - _No new implementation — end-to-end coverage of the FR4 fail-fast path. Add only the test double query/handler._
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_runtime_cache_key_is_empty_should_throw_through_processor"`
  - **References**: requirements.md (FR4, Acceptance Criteria); ADR 0021 (Key Components — `IAmCacheable` fail-fast, "End-to-end pipeline tests are mandatory")

- [ ] **Step ordering: a cache hit skips an inner decorator**
  - **Behavior**: The attribute's `step` orders the cache decorator relative to other decorators. When the cache decorator is ordered to wrap an inner decorator, a cache **hit** short-circuits the pipeline so the inner decorator does **not** run (only `next`, and everything inside it, is skipped). This is the load-bearing short-circuit / ordering guarantee.
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_cache_hit_should_skip_inner_decorator.cs`
  - **Test should verify**:
    - Annotate a handler with the cache attribute plus a second, inner decorator ordered "inside" the cache decorator (via `step`), each recording its invocations via a shared recorder.
    - First execution (miss) runs both the cache decorator and the inner decorator; second execution (hit) runs neither the inner decorator nor the handler (inner-decorator invocation count stays at 1, handler count stays at 1).
  - **Implementation files**:
    - _No new production implementation — add a simple recording inner decorator + its attribute as test doubles in the test project (model on the existing logging/validation decorator shape)._
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_cache_hit_should_skip_inner_decorator"`
  - **References**: requirements.md (FR1 step, NFR correctness/short-circuit, Acceptance Criteria — "a test with the cache decorator wrapping an inner decorator proves the inner decorator is skipped on a hit"); ADR 0021 (Forces — "Short-circuit / ordering is significant"); `src/Paramore.Darker/PipelineBuilder.cs:240` (`OrderByDescending(attr => attr.Step)`)

- [ ] **Document the step-ordering footgun on the attribute and package README**
  - **Behavior**: The requirements make ordering-sensitivity a first-class **documentation** deliverable, not only a tested behavior ("Decorator ordering behaviour … demonstrated by a test **and documented**"; "correct step-ordering guidance is a first-class part of this feature's documentation"). Add developer-facing documentation that a cache **hit short-circuits the pipeline**, so any decorator ordered "inside" (a lower `step` than) the cache decorator — logging, retry, fallback — is **skipped on a hit**, and give guidance on choosing `step` accordingly. This closes the "and documented" clause left open by the step-ordering test task.
  - **Test file**: _none — documentation deliverable (no behavioral test; the behavior is already proven by the step-ordering test task)._
  - **Test should verify**:
    - No behavioral test. Correctness is proven by the documentation existing: XML `<remarks>` on both attributes describing the short-circuit/ordering footgun, and a README section for the caching package.
  - **Implementation files**:
    - `src/Paramore.Darker.Caching/CacheableQueryAttributeAsync.cs` - add `<remarks>` XML docs explaining that a cache hit skips everything ordered inside the cache decorator, and how `step` controls placement (mirroring the `[QueryLogging(1)]` convention). (Only the **async** attribute exists at this point in the ordering; the matching `<remarks>` on the sync `CacheableQueryAttribute.cs` is added in the sync-decorator task that creates that file — do NOT reference or create the sync attribute here.)
    - `src/Paramore.Darker.Caching/README.md` - new package README with an "Ordering matters" section documenting the short-circuit-on-hit footgun and recommended `step` placement relative to logging/retry/fallback.
  - **RALPH-VERIFY**: `dotnet build src/Paramore.Darker.Caching/Paramore.Darker.Caching.csproj -c Release` (compiles with the async-attribute XML-doc additions; documentation task has no test filter)
  - **References**: requirements.md (NFR "The ordering-sensitivity … must be documented", Additional Context "first-class part of this feature's documentation", Acceptance Criteria — "demonstrated by a test **and documented**"); ADR 0021 (Consequences — "Ordering is a footgun … Mitigated by documentation making the cache decorator's position first-class guidance"); the step-ordering test task above (the behavior this documents)

- [ ] **Null result is cached (negative caching) and returned as a hit (FR11)**
  - **Behavior**: A handler that returns `null` has that `null` stored by `GetOrCreateAsync` like any value. A subsequent call within the expiry window returns the cached `null` as a **hit** without re-running the handler. The decorator does not special-case `null`.
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_handler_returns_null_should_cache_null_and_serve_hit.cs`
  - **Test should verify**:
    - Through a real `QueryProcessor`, a handler returning `null` runs once; the first call returns `null`.
    - A second call with the same key returns `null` and the recorder count is still 1 (the cached null was a hit; the handler did not re-run).
  - **Implementation files**:
    - _No new implementation — proves the existing `GetOrCreateAsync` flow does not special-case null. Add only the test double null-returning handler (reference type result)._
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_handler_returns_null_should_cache_null_and_serve_hit"`
  - **References**: requirements.md (FR11, Acceptance Criteria); ADR 0021 (Implementation Approach step 5, Alternatives — "Special-case null … Rejected")

- [ ] **Serialization failure surfaces to the caller unswallowed (FR13)**
  - **Behavior**: If the configured `HybridCache` cannot serialize the result type on a miss, the resulting exception propagates to the caller — it is not swallowed and no result is silently returned uncached without signal.
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_result_cannot_be_serialized_should_surface_exception.cs`
  - **Test should verify**:
    - Configure the `HybridCache` (or its serializer) so the handler's result type fails serialization on a miss (e.g. a result type / serializer combination that throws), then execute through a real `QueryProcessor`.
    - The `ExecuteAsync` call throws (the serialization exception surfaces); the exception is not swallowed into a silent success.
  - **Implementation files**:
    - _No new implementation — confirms the decorator does not catch/swallow serializer exceptions from `GetOrCreateAsync`. Add only the test double result type / serializer setup._
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_result_cannot_be_serialized_should_surface_exception"`
  - **References**: requirements.md (FR13, Acceptance Criteria); ADR 0021 (Implementation Approach step 5 — "Serialization failures … surface to the caller unswallowed")

- [ ] **Decorator records the cache-outcome span attribute (hit/miss) using only the core Activity type**
  - **Behavior**: Introduce the `CacheOutcome { Hit, Miss }` enum (first consumed here) and use it in the async decorator. After `GetOrCreateAsync` returns, the decorator derives the outcome from the local `ran` flag (`ran ? CacheOutcome.Miss : CacheOutcome.Hit`) and writes it onto the ambient query span via `Context.Span?.SetTag(DarkerSemanticConventions.CacheOutcome, "hit"|"miss")` — using only the core `System.Diagnostics.Activity` type, taking **no** OpenTelemetry/metrics dependency in the caching package.
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_recording_outcome_should_set_cache_outcome_span_attribute.cs`
  - **Test should verify**:
    - Driving a `[CacheableQueryAsync]` handler **through a real `QueryProcessor`** with a Darker tracer registered so the pipeline populates `Context.Span` (an `ActivityListener`/`TracerProvider` subscribed to `paramore.darker` captures the span): the first execution (miss) leaves `paramore.darker.cache.outcome = "miss"` on the query span.
    - A subsequent execution on the same key (hit) leaves `paramore.darker.cache.outcome = "hit"` on the query span.
    - With **no** tracer configured (`Context.Span` is null), the same executions still succeed and do not throw (the `?.` guard). (Note: this is the runtime-type-trap-sensitive path, so it must go through the pipeline, not a directly-instantiated decorator — ADR Implementation Approach step 9.)
  - **Implementation files**:
    - `src/Paramore.Darker.Caching/CacheOutcome.cs` - `public enum CacheOutcome { Hit, Miss }`. Add an XML-doc note that this caching enum is distinct from the core `DarkerSemanticConventions.CacheOutcome` **string** constant (same name, different type/namespace — see reuse note).
    - `src/Paramore.Darker.Caching/CacheableQueryDecoratorAsync.cs` - map `ran` → `CacheOutcome`, then `Context.Span?.SetTag(DarkerSemanticConventions.CacheOutcome, outcome == CacheOutcome.Hit ? "hit" : "miss")`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_recording_outcome_should_set_cache_outcome_span_attribute"`
  - **References**: requirements.md (FR10); ADR 0021 (Architecture Overview diagram — `SetTag("paramore.darker.cache.outcome", …)`, "only touches core Activity — NO OTel dep", Implementation Approach steps 4 & 9 "End-to-end pipeline tests are mandatory"); `src/Paramore.Darker/IQueryContext.cs` (`Activity? Span`); `src/Paramore.Darker/Observability/DarkerSemanticConventions.cs` (`CacheOutcome`); `src/Paramore.Darker/Observability/DarkerTracer.cs` (register the **core** `DarkerTracer` so `AddDarker` resolves `IAmADarkerTracer` and the pipeline populates `Context.Span`) + a BCL `System.Diagnostics.ActivityListener` subscribed to `paramore.darker` — **no OpenTelemetry / Diagnostics dependency in the caching test project** (do NOT pull in `AddDarkerInstrumentation`/OTel here)

- [ ] **Sync caching decorator + attribute: fast-path returns synchronously when the ValueTask is already completed**
  - **Behavior**: Introduce the sync pathway: `CacheableQueryAttribute(int step, int expirationSeconds)` (deriving from `QueryHandlerAttribute`, carrying the shared `public const string CacheTag = "Paramore.Darker.Caching.Tag"`, `GetDecoratorType()` → `typeof(CacheableQueryDecorator<,>)`), and the concrete `CacheableQueryDecorator<TQuery,TResult>` implementing `IQueryHandlerDecorator<TQuery,TResult>`. Its `Execute` calls `HybridCache.GetOrCreateAsync` (factory wraps `next` in a completed `ValueTask<TResult>`) and inspects the returned `ValueTask<TResult>`: when `IsCompletedSuccessfully` (the expected in-memory-L1-hit case) it returns `.Result` **synchronously**, consuming the `ValueTask` exactly once. `AddCaching` is extended to also register the sync decorator open generic.
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_sync_cache_value_task_completed_should_return_synchronously.cs`
  - **Test should verify**:
    - Through a real `QueryProcessor` (sync `Execute`), a warm cache entry (populated on a prior call) is served via the `IsCompletedSuccessfully` fast path and returns the correct cached result without re-running the handler (recorder count unchanged).
    - The shared `CacheableQueryAttribute.CacheTag` constant equals `"Paramore.Darker.Caching.Tag"` and both the sync and async decorators reference the same constant.
  - **Implementation files**:
    - `src/Paramore.Darker.Caching/CacheableQueryAttribute.cs` - `(int step, int expirationSeconds)`; `public const string CacheTag = "Paramore.Darker.Caching.Tag"`; `GetDecoratorType()` → `typeof(CacheableQueryDecorator<,>)`; `GetAttributeParams()` → `new object[] { expirationSeconds }`. Also add the same step-ordering/short-circuit `<remarks>` XML docs the async attribute carries (the sync counterpart of the ordering-documentation task), so both attributes document the footgun.
    - `src/Paramore.Darker.Caching/CacheableQueryDecorator.cs` - sync decorator; `Execute` inspects the `ValueTask<TResult>` from `GetOrCreateAsync`, returning `.Result` on `IsCompletedSuccessfully`; same expiry/key/span logic as the async decorator.
    - `src/Paramore.Darker.Caching/CachingDarkerBuilderExtensions.cs` - also `builder.RegisterDecorator(typeof(CacheableQueryDecorator<,>))`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_sync_cache_value_task_completed_should_return_synchronously"`
  - **References**: requirements.md (FR1, FR8 sync, Resolved Decision 2, NFR async-first-with-sync-fast-path); ADR 0021 (Forces — "Async-first with a sync fast-path", Key Components — sync decorator "inspects its returned `ValueTask<TResult>`"); `src/Paramore.Darker/QueryHandlerAttribute.cs`, `src/Paramore.Darker/IQueryHandlerDecorator.cs`, `src/Paramore.Darker/PipelineBuilder.cs:253`

- [ ] **Sync caching decorator blocking fallback materializes a non-completed ValueTask**
  - **Behavior**: When the `ValueTask<TResult>` returned by `GetOrCreateAsync` is **not** already completed (e.g. an L2/async populate on a miss), the sync `Execute` blocks via `.AsTask().GetAwaiter().GetResult()` to obtain the result. Correctness never depends on synchronous completion; the `ValueTask` is still consumed exactly once (fast-path OR fallback, never both).
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_sync_cache_value_task_not_completed_should_block_and_return.cs`
  - **Test should verify**:
    - Force the non-completed branch (e.g. a `HybridCache` / factory that yields asynchronously on a miss so the returned `ValueTask` is not `IsCompletedSuccessfully`), then call the sync `Execute` through a real `QueryProcessor`.
    - The correct result is returned via the blocking fallback, and the handler ran exactly once (the `ValueTask` was consumed once).
  - **Implementation files**:
    - `src/Paramore.Darker.Caching/CacheableQueryDecorator.cs` - `else return valueTask.AsTask().GetAwaiter().GetResult();`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_sync_cache_value_task_not_completed_should_block_and_return"`
  - **References**: requirements.md (FR8, NFR async-first-with-sync-fast-path — "Both the fast-path and the blocking fallback must be covered by tests"); ADR 0021 (Risks — "sync-over-async deadlock … the blocking fallback is documented as the sync-path cost and covered by a test that forces the non-completed branch")

- [ ] **A tag supplied via the Bag key is applied to the entry and enables RemoveByTagAsync eviction (FR9)**
  - **Behavior**: The decorator reads `Context.Bag[CacheableQueryAttribute.CacheTag]` (`"Paramore.Darker.Caching.Tag"`); when the value is a non-empty `string`, it wraps it as a one-element `IEnumerable<string>` and passes it as the `tags` argument to `GetOrCreateAsync`. Application code can then evict the entry via the underlying cache's `RemoveByTagAsync(tag)`, after which the next execution is a miss and re-runs the handler.
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_tag_supplied_in_bag_should_apply_and_allow_remove_by_tag.cs`
  - **Test should verify**:
    - Through a real `QueryProcessor` with the tag placed in `IQueryContext.Bag` under `CacheableQueryAttribute.CacheTag`, a first call populates a tagged entry and a second call is a hit (handler count 1).
    - After calling `HybridCache.RemoveByTagAsync(tag)`, the next execution is a miss and re-runs the handler (count 2).
  - **Implementation files**:
    - `src/Paramore.Darker.Caching/CacheableQueryDecoratorAsync.cs` - read the tag from `Context.Bag`; when a non-empty string, pass `new[] { tag }` as `GetOrCreateAsync` tags.
    - `src/Paramore.Darker.Caching/CacheableQueryDecorator.cs` - same tag-reading logic (shared helper referencing `CacheableQueryAttribute.CacheTag`).
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_tag_supplied_in_bag_should_apply_and_allow_remove_by_tag"`
  - **References**: requirements.md (FR9, Acceptance Criteria); ADR 0021 (Key Components — shared `CacheTag` constant, Implementation Approach step 6); `src/Paramore.Darker/IQueryContext.cs` (`IDictionary<string,object> Bag`)

- [ ] **Absent or non-string Bag tag value stores the entry untagged without throwing (FR9)**
  - **Behavior**: When the `CacheableQueryAttribute.CacheTag` Bag key is absent, or its value is not a non-empty `string` (e.g. `null`, empty, whitespace, or a non-string object), the entry is stored **untagged** and no exception is thrown — consistent with the best-effort stance.
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_bag_tag_absent_or_not_string_should_store_untagged.cs`
  - **Test should verify**:
    - With no `CacheTag` entry in the Bag, execution succeeds and caches (second call is a hit; no throw).
    - With a non-string value under the `CacheTag` key (e.g. an `int`), execution succeeds and caches without throwing (entry is stored untagged).
    - With an empty/whitespace string value, the entry is stored untagged and no throw occurs.
  - **Implementation files**:
    - `src/Paramore.Darker.Caching/CacheableQueryDecoratorAsync.cs` - guard: only wrap-and-pass tags when `Context.Bag.TryGetValue(...)` yields a non-empty `string`; otherwise pass no tags.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_bag_tag_absent_or_not_string_should_store_untagged"`
  - **References**: requirements.md (FR9, Acceptance Criteria); ADR 0021 (Implementation Approach step 6 — "absent or non-string ⇒ stored untagged, no throw")

- [ ] **Tagging against a HybridCache implementation without tag support still caches and never fails the query (FR14)**
  - **Behavior**: Supplying a tag is best-effort. If the configured `HybridCache` implementation does not support tag-based eviction, the entry is still cached (tagging becomes a no-op for eviction) and the query does not fail.
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_cache_impl_lacks_tag_support_should_still_cache_query.cs`
  - **Test should verify**:
    - With a `HybridCache` test double whose tag handling is a no-op (or throws only on `RemoveByTagAsync`), a tagged cacheable query executes successfully through a real `QueryProcessor`: first call populates, second call is a hit (handler count 1).
    - The query never surfaces a tagging-related failure.
  - **Implementation files**:
    - _No new production implementation — confirms tags are passed best-effort and not validated by the decorator. Add a `HybridCache` test double lacking tag-eviction support._
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_cache_impl_lacks_tag_support_should_still_cache_query"`
  - **References**: requirements.md (FR14, Acceptance Criteria); ADR 0021 (Implementation Approach step 6 — "Tagging against an implementation without tag support still caches and never fails the query, best-effort")

- [ ] **AddCaching opt-in exposes a configurable overload with a replaceable key generator**
  - **Behavior**: Introduce the `CachingOptions` type (first used here) and add the `AddCaching(this IDarkerHandlerBuilder, Action<CachingOptions> configure)` overload alongside the parameterless `AddCaching()` created in the vertical-slice task. The `configure` callback (`CachingOptions`) allows supplying a custom `ICacheKeyGenerator` that replaces the default `DefaultCacheKeyGenerator` **without changing the decorator** (NFR extensibility). By this point both concrete decorator open generics are registered (async from the vertical-slice task, sync from the sync-decorator task); `AddCaching` registers no `HybridCache` and wires no metrics.
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_AddCaching_called_should_register_decorators_and_key_generator.cs`
  - **Test should verify**:
    - After `AddDarker().AddCaching()`, the built `ServiceProvider` resolves `ICacheKeyGenerator` as `DefaultCacheKeyGenerator`, and both `CacheableQueryDecorator<,>` and `CacheableQueryDecoratorAsync<,>` are registered as decorators.
    - After `AddCaching(o => o.KeyGenerator = new CustomKeyGenerator())` (a test-double generator), the resolved `ICacheKeyGenerator` is the custom instance, and a cacheable query executed end-to-end uses the custom key (proven via the custom generator's recorded call or a distinct key shape).
  - **Implementation files**:
    - `src/Paramore.Darker.Caching/CachingOptions.cs` - new type exposing a settable custom `ICacheKeyGenerator` (`KeyGenerator`).
    - `src/Paramore.Darker.Caching/CachingDarkerBuilderExtensions.cs` - add the `Action<CachingOptions>` overload; honor `CachingOptions.KeyGenerator` (falling back to `DefaultCacheKeyGenerator` when unset) when registering `ICacheKeyGenerator`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_AddCaching_called_should_register_decorators_and_key_generator"`
  - **References**: requirements.md (FR7, NFR extensibility, Acceptance Criteria — "opt-in via that extension"); ADR 0021 (Key Components — `AddCaching`, "registers `DefaultCacheKeyGenerator` (overridable via an options callback)"); `src/Paramore.Darker.Validation.FluentValidation/FluentValidationDarkerBuilderExtensions.cs`

- [ ] **The Paramore.Darker.Caching assembly takes no OpenTelemetry/metrics dependency**
  - **Behavior**: The caching package depends only on the Darker core and `Microsoft.Extensions.Caching.Hybrid`. It must not reference any OpenTelemetry assembly or `System.Diagnostics.Metrics` types — the cache outcome is recorded purely via the core `Activity` span attribute.
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_inspecting_caching_assembly_should_have_no_otel_or_metrics_dependency.cs`
  - **Test should verify**:
    - `typeof(CacheableQueryDecoratorAsync<,>).Assembly.GetReferencedAssemblies()` contains **no** assembly whose name starts with `OpenTelemetry`.
    - It references `Microsoft.Extensions.Caching.Hybrid` and the Darker core (positive control that the guard is inspecting the right assembly).
  - **Implementation files**:
    - _No new production implementation — a guard test asserting dependency hygiene (model on `test/Paramore.Darker.Validation.Tests/When_inspecting_core_assembly_should_have_no_provider_dependencies.cs` if present, else a straightforward reflection assertion)._
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_inspecting_caching_assembly_should_have_no_otel_or_metrics_dependency"`
  - **References**: requirements.md (FR7, FR10); ADR 0021 (Key Components — "depends on … but NOT on OpenTelemetry / `System.Diagnostics.Metrics`")

- [ ] **Backing HybridCache can be switched to real FusionCache purely via DI (decorator unchanged)**
  - **Behavior**: The cache implementation is selected entirely by DI. Registering the **real FusionCache** `HybridCache` implementation (via its Microsoft HybridCache adapter) in place of Microsoft's makes the same, unchanged caching decorator cache through FusionCache. This test uses the real FusionCache packages (test-only dependency).
  - **Test file**: `test/Paramore.Darker.Caching.Tests/When_backing_cache_is_fusioncache_should_cache_via_di_switch.cs`
  - **Test should verify**:
    - Build a real `QueryProcessor` with `AddCaching()` and register FusionCache's `HybridCache` implementation via its DI extension (the `ZiggyCreatures.FusionCache.MicrosoftHybridCache` adapter) instead of `AddHybridCache()`.
    - A cacheable query executed twice runs the handler once (a hit served by FusionCache), proving the switch required **no** change to handler or decorator code.
  - **Implementation files**:
    - `Directory.Packages.props` - add `<PackageVersion>` entries for `ZiggyCreatures.FusionCache` and `ZiggyCreatures.FusionCache.MicrosoftHybridCache` (latest stable compatible with net8.0/net9.0). These are **test-only** dependencies — keep them clearly separate from the production `Microsoft.Extensions.Caching.Hybrid` pin.
    - `test/Paramore.Darker.Caching.Tests/Paramore.Darker.Caching.Tests.csproj` - `<PackageReference>` the two FusionCache packages.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Caching.Tests/ --filter "FullyQualifiedName~When_backing_cache_is_fusioncache_should_cache_via_di_switch"`
  - **References**: requirements.md (FR6, FR7, NFR extensibility, Acceptance Criteria — "switched … purely via DI registration"); ADR 0021 (Decision, Positive — "Switching Microsoft ↔ FusionCache is a pure DI choice"); product-owner decision 1 (use the REAL FusionCache package as a test-only dependency); `Directory.Packages.props`

- [ ] **CacheMeter derives a hit/miss counter from the cache-outcome span attribute**
  - **Behavior**: In `Paramore.Darker.Extensions.Diagnostics`, add the `IAmADarkerCacheMeter` role and its default `CacheMeter`, modelled exactly on `IAmADarkerQueryMeter`/`QueryMeter`. `CacheMeter(IMeterFactory, MeterProvider)` creates a `Counter<long>` via `meterFactory.Create(DarkerSemanticConventions.MeterName)` named `CacheRequestsMetricName`. `RecordCacheOperation(Activity activity)` reads the `CacheOutcome` tag from the span and — **only when present** — `Add(1, …)` with the `CacheRequestsAllowedTags`-filtered span tags plus the service attributes. `Enabled` exposes the counter's listener state for cheap short-circuiting.
  - **Test file**: `test/Paramore.Darker.Extensions.Diagnostics.Tests/When_recording_cache_operation_should_record_counter_with_allowed_tags.cs`
  - **Test should verify**:
    - Given a stopped `Internal` activity carrying `paramore.darker.cache.outcome = "hit"` and `paramore.darker.querytype`, `RecordCacheOperation` records one measurement on the `paramore.darker.cache.requests` counter with the `cache.outcome` and `querytype` tags only.
    - An activity **without** the `CacheOutcome` tag records nothing (no-op).
    - `Enabled` is `false` when no `MeterProvider` subscribes to `paramore.darker`.
  - **Implementation files**:
    - `src/Paramore.Darker.Extensions.Diagnostics/Observability/IAmADarkerCacheMeter.cs` - `void RecordCacheOperation(Activity activity); bool Enabled { get; }`.
    - `src/Paramore.Darker.Extensions.Diagnostics/Observability/CacheMeter.cs` - `Counter<long>` via `IMeterFactory.Create(MeterName)`, filtering to `CacheRequestsAllowedTags`, reading the `CacheOutcome` tag; model on `QueryMeter`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Extensions.Diagnostics.Tests/ --filter "FullyQualifiedName~When_recording_cache_operation_should_record_counter_with_allowed_tags"`
  - **References**: requirements.md (FR10); ADR 0021 (Key Components — "Package — `Paramore.Darker.Extensions.Diagnostics` additions", Implementation Approach step 7); `src/Paramore.Darker.Extensions.Diagnostics/Observability/QueryMeter.cs` and `IAmADarkerQueryMeter.cs` (template); `src/Paramore.Darker/Observability/DarkerSemanticConventions.cs` (`CacheRequestsMetricName`, `CacheRequestsAllowedTags`)

- [ ] **The metrics-from-traces processor dispatches Internal spans to the cache meter**
  - **Behavior**: `DarkerMetricsFromTracesProcessor` gains an `IAmADarkerCacheMeter` dependency. In its `ActivityKind.Internal` branch it also calls `cacheMeter.RecordCacheOperation(activity)` (a no-op when the span carries no cache outcome, so it coexists with `queryMeter.RecordQueryOperation`), and its cheap short-circuit guard is extended to include `cacheMeter.Enabled` (`if (!(queryMeter.Enabled || dbMeter.Enabled || cacheMeter.Enabled)) return;`). The construction site in `DarkerTracerBuilderExtensions` is updated to resolve and pass the cache meter and to include the cache meter in its registration gate.
  - **Test file**: `test/Paramore.Darker.Extensions.Diagnostics.Tests/When_ending_internal_span_with_cache_outcome_should_dispatch_to_cache_meter.cs`
  - **Test should verify**:
    - An `Internal` span carrying `paramore.darker.cache.outcome = "miss"` routed through `OnEnd` records a measurement on `paramore.darker.cache.requests` (in addition to the query-duration histogram).
    - When no meter (query, db, or cache) is enabled, `OnEnd` short-circuits and does not throw.
  - **Implementation files**:
    - `src/Paramore.Darker.Extensions.Diagnostics/Observability/DarkerMetricsFromTracesProcessor.cs` - add the `IAmADarkerCacheMeter` primary-constructor parameter; call it in the `Internal` branch; extend the `Enabled` guard.
    - `src/Paramore.Darker.Extensions.Diagnostics/DarkerTracerBuilderExtensions.cs` - resolve `IAmADarkerCacheMeter` and pass it into the `new DarkerMetricsFromTracesProcessor(...)` (line ~46); include `IAmADarkerCacheMeter` in the meter-registration gate.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Extensions.Diagnostics.Tests/ --filter "FullyQualifiedName~When_ending_internal_span_with_cache_outcome_should_dispatch_to_cache_meter"`
  - **References**: requirements.md (FR10); ADR 0021 (Key Components — `DarkerMetricsFromTracesProcessor` extension, Implementation Approach step 7); `src/Paramore.Darker.Extensions.Diagnostics/Observability/DarkerMetricsFromTracesProcessor.cs`; `src/Paramore.Darker.Extensions.Diagnostics/DarkerTracerBuilderExtensions.cs:46`; `test/Paramore.Darker.Extensions.Diagnostics.Tests/When_ending_span_through_processor_should_dispatch_to_meter_by_activity_kind.cs` (test template)

- [ ] **AddDarkerInstrumentation registers the cache meter and honors the opt-out toggle**
  - **Behavior**: `AddDarkerInstrumentation` on the `MeterProviderBuilder` is extended to `TryAddSingleton<IAmADarkerCacheMeter, CacheMeter>()` alongside the query and DB meters, and to accept an opt-out toggle (e.g. `AddDarkerInstrumentation(bool emitCacheMetrics = true)`). When the toggle is **disabled**, it registers a no-op `IAmADarkerCacheMeter` whose `Enabled == false`, so the cache counter is never recorded. This toggle is entirely independent of `InstrumentationOptions`.
  - **Test file**: `test/Paramore.Darker.Extensions.Diagnostics.Tests/When_adding_darker_instrumentation_should_register_cache_meter_with_toggle.cs`
  - **Test should verify**:
    - With `AddDarkerInstrumentation()` (default), the provider resolves `IAmADarkerCacheMeter` as `CacheMeter`.
    - With the cache-metrics toggle disabled, the resolved `IAmADarkerCacheMeter.Enabled` is `false` (a no-op meter), and routing a cache-outcome span through the processor records nothing on `paramore.darker.cache.requests`.
  - **Implementation files**:
    - `src/Paramore.Darker.Extensions.Diagnostics/DarkerMetricsBuilderExtensions.cs` - add the toggle parameter; `TryAddSingleton<IAmADarkerCacheMeter, CacheMeter>()` when enabled, else register a no-op implementation (`Enabled == false`).
    - `src/Paramore.Darker.Extensions.Diagnostics/Observability/` - a `NoOpCacheMeter` (or equivalent) implementing `IAmADarkerCacheMeter` with `Enabled == false` and a no-op `RecordCacheOperation`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Extensions.Diagnostics.Tests/ --filter "FullyQualifiedName~When_adding_darker_instrumentation_should_register_cache_meter_with_toggle"`
  - **References**: requirements.md (FR10, Resolved Decision 4, Acceptance Criteria — "turned off via its own opt-out toggle, independent of `InstrumentationOptions`"); ADR 0021 (Key Components — `AddDarkerInstrumentation` extension, Risks — "double-reported metrics"); `src/Paramore.Darker.Extensions.Diagnostics/DarkerMetricsBuilderExtensions.cs`; `src/Paramore.Darker/Observability/InstrumentationOptions.cs` (the independent span-attribute toggle); `test/Paramore.Darker.Extensions.Diagnostics.Tests/When_adding_darker_instrumentation_to_meter_builder_should_register_meters_and_meter.cs` (test template)

- [ ] **End-to-end metrics chain: a cache miss then hit produce hit/miss counters, and the toggle disables them**
  - **Behavior**: With tracing + `AddDarkerInstrumentation` (cache metrics enabled) wired around a real `QueryProcessor`, executing a cacheable query twice writes a `"miss"` then a `"hit"` cache-outcome span attribute, from which `CacheMeter` derives one miss and one hit measurement on `paramore.darker.cache.requests`. When the opt-out toggle is disabled, no cache counter is recorded even though results are unaffected. **Known accepted caveat (do NOT attempt to fix): under HybridCache stampede protection, joined concurrent callers on the same missing key observe `ran == false` and are counted as hits — a metrics-only skew; returned results stay correct.** Keep this test single-threaded so the caveat does not affect assertions.
  - **Test file**: `test/Paramore.Darker.Extensions.Diagnostics.Tests/When_cached_query_executed_with_metrics_should_emit_hit_and_miss_counters.cs`
  - **Test should verify**:
    - Sequential first + second executions of a cacheable query (real processor + registered `HybridCache` + tracing + cache metrics) record exactly one `cache.outcome = "miss"` and one `cache.outcome = "hit"` measurement on `paramore.darker.cache.requests`.
    - With the cache-metrics toggle disabled, the same sequence records **no** measurements on `paramore.darker.cache.requests` while still returning correct results.
  - **Implementation files**:
    - _No new production implementation — end-to-end verification wiring the caching decorator's span attribute to the diagnostics counter. The test lives in the diagnostics test project, which references both `Paramore.Darker.Caching` and `Paramore.Darker.Extensions.Diagnostics` (add the `Paramore.Darker.Caching` project reference to that test csproj if absent)._
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Extensions.Diagnostics.Tests/ --filter "FullyQualifiedName~When_cached_query_executed_with_metrics_should_emit_hit_and_miss_counters"`
  - **References**: requirements.md (FR10, Acceptance Criteria); ADR 0021 (Implementation Approach step 8, Risks — "hit/miss counter is miscounted under cache-stampede protection … metrics-only inaccuracy … documented as a known caveat"); `test/Paramore.Darker.Extensions.Diagnostics.Tests/When_executing_query_with_metrics_wired_should_record_query_and_db_duration_metrics.cs` (full-wiring test template)

- [ ] **Full feature builds and the whole filtered solution's tests pass on net8.0 and net9.0**
  - **Behavior**: The complete feature builds and the **entire** `Darker.Filter.slnf` test suite passes on both target frameworks — not just cache-named subsets — confirming the caching package, core convention additions, and diagnostics additions integrate cleanly and introduce **no regressions** elsewhere in the solution. This is the final integration gate, so it must run the full filtered solution (both TFMs) rather than filtered project subsets.
  - **Test file**: _none — whole-solution cross-framework verification (no new test)._
  - **Test should verify**:
    - No new behavioral test. Correctness is proven by the full `Darker.Filter.slnf` building in Release and its complete test suite passing on net8.0 and net9.0 (the multi-targeted test projects run both TFMs by default).
  - **Implementation files**:
    - _No implementation — verification only. If any framework-specific gap or regression surfaces, fix it in the relevant `src/Paramore.Darker.Caching/*`, core, or diagnostics file._
  - **RALPH-VERIFY**: `dotnet build Darker.Filter.slnf -c Release && dotnet test Darker.Filter.slnf -c Release --no-build`
  - **References**: requirements.md (Targeting NFR, Acceptance Criteria — "Builds and tests pass on net8.0 and net9.0 via `Darker.Filter.slnf`"); ADR 0021 (Implementation Approach step 8); `Darker.Filter.slnf`
