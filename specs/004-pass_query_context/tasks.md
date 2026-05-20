# Tasks: Pass QueryContext into QueryProcessor

**Spec**: 004-pass_query_context
**Issue**: #320
**ADR**: [0010-pass-query-context](../../docs/adr/0010-pass-query-context.md)

## Task Ordering Strategy

1. **Structural/tidy tasks first** — additive changes that don't break existing behavior
2. **Core behavioral changes** — new QueryProcessor behavior with tests
3. **Decorator behavioral changes** — policy and logging decorator changes with tests
4. **Builder and DI changes** — update the registration/builder infrastructure
5. **Consumer updates** — FakeQueryProcessor, samples, benchmarks
6. **Cleanup** — remove old mechanisms that are now unused

Dependencies are noted per task. Each behavioral task uses `/test-first` with mandatory approval gates. **Each task must leave the build green.**

---

## Phase 1: Structural Foundation

These are additive changes that don't alter existing behavior. Existing tests must pass before and after.

- [x] **TIDY: Add Polly package reference to core Paramore.Darker**
  - Add `<PackageReference Include="Polly" />` to `src/Paramore.Darker/Paramore.Darker.csproj`
  - Version is already managed centrally in `Directory.Packages.props` (Polly 8.6.6)
  - Verify: `dotnet build Darker.Filter.slnf -c Release` still succeeds
  - Covers: ADR section 4 note, FR6a package dependency
  - No behavioral change — existing tests must still pass

- [x] **TIDY: Add typed `Policies` property to `IQueryContext` and `QueryContext`**
  - Depends on: Polly package reference added to core
  - Add `IPolicyRegistry<string>? Policies { get; set; }` to `IQueryContext` interface
  - Add `public IPolicyRegistry<string>? Policies { get; set; }` to `QueryContext` class
  - Add `using Polly.Registry;` to both files
  - Verify: `dotnet build Darker.Filter.slnf -c Release` still succeeds, existing tests pass
  - Covers: FR6, AC10, ADR section 2

---

## Phase 2: Core QueryProcessor Behavior

These tasks change `IQueryProcessor` and `QueryProcessor` to accept and use external context. Note: `CreateQueryContext()` is kept alive until both sync and async paths are updated, then removed in a tidy task at the end.

- [x] **TEST + IMPLEMENT: QueryProcessor.Execute creates context via factory when no context provided**
  - **USE COMMAND**: `/test-first when executing query without context should create context via factory`
  - Depends on: Phase 1 complete
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `When_executing_query_without_context_should_create_context_via_factory.cs`
  - Test should verify:
    - `Execute(query)` called without context parameter
    - Handler receives a non-null `IQueryContext` (created by factory)
    - Query executes successfully and returns expected result
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Update `IQueryProcessor.Execute` signature to add `IQueryContext queryContext = null`
    - Update `QueryProcessor.Execute`: if `queryContext` is null, call `_queryContextFactory.Create()` (inline, not via `CreateQueryContext()`)
    - Do NOT remove `CreateQueryContext()` yet — `ExecuteAsync` still uses it
  - Covers: FR1, FR3, AC1

- [x] **TEST + IMPLEMENT: QueryProcessor.Execute uses caller-provided context**
  - **USE COMMAND**: `/test-first when executing query with provided context should use that context`
  - Depends on: previous task
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `When_executing_query_with_provided_context_should_use_that_context.cs`
  - Test should verify:
    - Create an `IQueryContext` with a known Bag entry
    - Call `Execute(query, myContext)`
    - Handler receives the same `IQueryContext` instance (reference equality)
    - Handler can read the known Bag entry
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Already handled by the `queryContext ??= ...` logic — this test validates the non-null path
  - Covers: FR4, AC3, AC5

- [x] **TEST + IMPLEMENT: QueryProcessor.ExecuteAsync creates context via factory when no context provided**
  - **USE COMMAND**: `/test-first when executing async query without context should create context via factory`
  - Depends on: Execute sync tasks complete
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `When_executing_async_query_without_context_should_create_context_via_factory.cs`
  - Test should verify:
    - `ExecuteAsync(query, cancellationToken: ct)` called without context parameter
    - Handler receives a non-null `IQueryContext` (created by factory)
    - Query executes successfully and returns expected result
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Update `IQueryProcessor.ExecuteAsync` signature: add `IQueryContext queryContext = null` before `CancellationToken`
    - Update `QueryProcessor.ExecuteAsync`: if `queryContext` is null, call `_queryContextFactory.Create()` (inline)
  - Covers: FR2, FR3, AC2

- [x] **TEST + IMPLEMENT: QueryProcessor.ExecuteAsync uses caller-provided context**
  - **USE COMMAND**: `/test-first when executing async query with provided context should use that context`
  - Depends on: previous task
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `When_executing_async_query_with_provided_context_should_use_that_context.cs`
  - Test should verify:
    - Create an `IQueryContext` with a known Bag entry
    - Call `ExecuteAsync(query, myContext, cancellationToken)`
    - Handler receives the same `IQueryContext` instance (reference equality)
    - Handler can read the known Bag entry
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Already handled by the `queryContext ??= ...` logic — this test validates the non-null async path
  - Covers: FR4, AC4, AC5

- [x] **TIDY: Remove dead `CreateQueryContext()` method from QueryProcessor**
  - Depends on: Both sync and async Execute paths updated above
  - Remove `CreateQueryContext()` private method (no longer called)
  - Verify: `dotnet build Darker.Filter.slnf -c Release` succeeds, existing tests pass
  - Covers: FR9 (partial — removes the method; field removal in Phase 4)

- [x] **TEST + IMPLEMENT: InitQueryContext sets Policies from constructor when context has no policies**
  - **USE COMMAND**: `/test-first when query context has no policies should set policies from processor constructor`
  - Depends on: CreateQueryContext removed
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `When_query_context_has_no_policies_should_set_policies_from_processor.cs`
  - Test should verify:
    - Create `QueryProcessor` with a non-null `IPolicyRegistry<string>` constructor param
    - Execute a query (without providing context)
    - Handler's `Context.Policies` is the same registry instance passed to the constructor
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `IPolicyRegistry<string>? policyRegistry = null` constructor param to `QueryProcessor`
    - Store as `_policyRegistry` field
    - Add private `InitQueryContext(IQueryContext queryContext)` method: `queryContext.Policies ??= _policyRegistry;`
    - Call `InitQueryContext(queryContext)` after context creation/assignment in both `Execute` and `ExecuteAsync`
  - Covers: FR5, FR6a, AC10a

- [x] **TEST + IMPLEMENT: InitQueryContext preserves caller-supplied policies**
  - **USE COMMAND**: `/test-first when caller provides context with policies should preserve caller policies`
  - Depends on: previous task
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `When_caller_provides_context_with_policies_should_preserve_caller_policies.cs`
  - Test should verify:
    - Create `QueryProcessor` with a policy registry (processorRegistry)
    - Create `IQueryContext` with a different policy registry (callerRegistry) set on `Policies`
    - Execute query with the caller-provided context
    - Handler's `Context.Policies` is callerRegistry (not processorRegistry)
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Already handled by `??=` semantics — this test validates caller-wins behavior
  - Covers: FR5, AC6

---

## Phase 3: Decorator Behavior Changes

- [x] **TEST + IMPLEMENT: Policy decorator reads from typed Context.Policies property**
  - **USE COMMAND**: `/test-first when retryable query decorator executes should read policy from context policies property`
  - Depends on: Phase 2 complete
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `When_retryable_decorator_executes_should_read_policy_from_context_policies.cs`
  - Test should verify:
    - Create a `QueryContext` with `Policies` set to a registry containing the required policy
    - `RetryableQueryDecorator` reads from `Context.Policies` and applies the policy
    - Query executes successfully through the policy
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - In `RetryableQueryDecorator.GetPolicyRegistry()`: replace `Context.Bag[Constants.ContextBagKey]` with `Context.Policies ?? throw new ConfigurationException(...)`
    - Same change in `RetryableQueryDecoratorAsync.GetPolicyRegistry()`
    - Do NOT remove `Constants.ContextBagKey` yet — the builder extension methods still reference it (removed in Phase 4)
  - Covers: FR6b, AC14

- [x] **TEST + IMPLEMENT: Policy decorator throws ConfigurationException when Policies is null**
  - **USE COMMAND**: `/test-first when policy decorator executes without policies configured should throw ConfigurationException`
  - Depends on: previous task
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `When_policy_decorator_executes_without_policies_should_throw_ConfigurationException.cs`
  - Test should verify:
    - Create a `QueryContext` with `Policies` left as null
    - `RetryableQueryDecorator` execution throws `ConfigurationException`
    - Exception message indicates policy registry is not set
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Already handled by the `?? throw new ConfigurationException(...)` — this test validates the error path
  - Covers: FR6a (decorator error), AC11

- [x] **TEST + IMPLEMENT: Logging decorator uses constructor-injected JsonSerializerSettings**
  - **USE COMMAND**: `/test-first when logging decorator executes should use constructor-injected serializer settings`
  - Depends on: Phase 2 complete
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `When_logging_decorator_executes_should_use_injected_serializer_settings.cs`
  - Test should verify:
    - Create `QueryLoggingDecorator` with `JsonSerializerSettings` via constructor
    - Decorator executes and logs query using the injected settings (no `Context.Bag` lookup)
    - Query executes successfully
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `JsonSerializerSettings? serializerSettings` constructor param to `QueryLoggingDecorator`
    - Store as field, create `JsonSerializer` from it in `GetSerializer()` (replacing `Context.Bag` lookup)
    - Same change in `QueryLoggingDecoratorAsync`
    - Do NOT remove `Constants.ContextBagKey` yet — the builder extension methods (`AddJsonQueryLogging<TBuilder>`) still reference it (removed in Phase 4)
  - Covers: FR7, AC12

- [x] **TEST + IMPLEMENT: Logging decorator throws ConfigurationException when settings null**
  - **USE COMMAND**: `/test-first when logging decorator executes without serializer settings should throw ConfigurationException`
  - Depends on: previous task
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `When_logging_decorator_executes_without_settings_should_throw_ConfigurationException.cs`
  - Test should verify:
    - Create `QueryLoggingDecorator` with null `JsonSerializerSettings`
    - Decorator execution throws `ConfigurationException`
    - Exception message indicates missing serializer setup
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Already handled by null check at execution time — this test validates the error path
  - Covers: FR7, AC13

- [ ] **TIDY: Remove `NewtonsoftJsonSerializer` wrapper class**
  - Depends on: Logging decorator tasks complete (no more usages)
  - Delete `src/Paramore.Darker.QueryLogging/NewtonsoftJsonSerializer.cs`
  - Verify: no references remain (`grep -r NewtonsoftJsonSerializer src/`)
  - Verify: `dotnet build Darker.Filter.slnf -c Release` succeeds
  - Covers: ADR section 7 removal table

---

## Phase 4: Builder and DI Registration Changes

These tasks are ordered so the build stays green at each step. The strategy:
1. First, update **all** `AddContextBagItem` call sites — both non-generic (cast to `QueryProcessorBuilder`) and generic (`TBuilder : IQueryProcessorExtensionBuilder`)
2. Then remove `AddContextBagItem` from the interface
3. Then update `QueryProcessor` constructor (removing `contextBagData`, updating both call sites atomically)
4. Finally, register typed services in DI

### How the two builder paths work after this change

There are two sets of extension methods for both Policies and QueryLogging:

**Non-generic methods** (fluent builder path): `Policies(this IBuildTheQueryProcessor)`, `DefaultPolicies(this IBuildTheQueryProcessor)`, `JsonQueryLogging(this IBuildTheQueryProcessor)` — these cast to `QueryProcessorBuilder` and can store state on it directly. After this change, they store `_policyRegistry` on the builder.

**Generic methods** (DI path): `AddPolicies<TBuilder>(this TBuilder)`, `AddDefaultPolicies<TBuilder>(this TBuilder)`, `AddJsonQueryLogging<TBuilder>(this TBuilder)` where `TBuilder : IQueryProcessorExtensionBuilder` — these are called on `IDarkerHandlerBuilder` from the DI path (e.g. `services.AddDarker().AddDefaultPolicies()`). After this change, they **only register decorator types** via `builder.RegisterDecorator()`. The policy registry and serializer settings are registered in DI separately (tasks 5 and 6 below). The `QueryProcessor` constructor receives the registry from DI resolution, and logging decorators receive settings via constructor injection from DI.

This separation is clean: the generic methods handle decorator registration only, while DI handles typed service registration. The `AddContextBagItem` call is simply removed from the generic methods — no replacement needed because DI handles what it previously did.

- [ ] **TIDY: Update ALL AddContextBagItem call sites in Policies and QueryLogging packages**
  - Depends on: Phase 3 complete
  - **Non-generic methods** in `src/Paramore.Darker.Policies/QueryProcessorBuilderExtensions.cs`:
    - Add public `IPolicyRegistry<string>? PolicyRegistry` property to `QueryProcessorBuilder` (public because the extension methods in a separate assembly need to set it)
    - `Policies(this IBuildTheQueryProcessor)`: cast to `QueryProcessorBuilder`, set `PolicyRegistry` field, keep `RegisterDecorator` calls
    - `DefaultPolicies(this IBuildTheQueryProcessor)`: same — create default registry, set on `QueryProcessorBuilder.PolicyRegistry`
  - **Generic methods** in the same file:
    - `AddPolicies<TBuilder>()`: remove `builder.AddContextBagItem(Constants.ContextBagKey, policyRegistry)`, keep `RegisterDecorator` calls and validation. The policy registry is registered in DI by a later task.
    - `AddDefaultPolicies<TBuilder>()`: same — remove `AddContextBagItem`, keep decorator registration
  - **Non-generic methods** in `src/Paramore.Darker.QueryLogging/QueryProcessorBuilderExtensions.cs`:
    - `JsonQueryLogging(this IBuildTheQueryProcessor)`: remove `AddContextBagItem` call, keep `RegisterDecorator` calls
  - **Generic methods** in the same file:
    - `AddJsonQueryLogging<TBuilder>()`: remove `AddContextBagItem(Constants.ContextBagKey, new NewtonsoftJsonSerializer(...))` call, keep `RegisterDecorator` calls. The serializer settings are registered in DI by a later task.
  - **Also**: Remove `Constants.ContextBagKey` from both `Paramore.Darker.Policies/Constants.cs` and `Paramore.Darker.QueryLogging/Constants.cs` (all references now removed)
  - Verify: `dotnet build Darker.Filter.slnf -c Release` succeeds — `AddContextBagItem` still exists on the interface but is no longer called
  - Covers: FR15

- [ ] **TIDY: Remove `AddContextBagItem` from interfaces and implementations**
  - Depends on: All `AddContextBagItem` call sites removed above
  - Remove `AddContextBagItem` from `IQueryProcessorExtensionBuilder` interface
  - Remove `AddContextBagItem` from `QueryProcessorBuilder`
  - Remove `AddContextBagItem` from `ServiceCollectionDarkerHandlerBuilder`
  - Do NOT remove `_contextBagData` dictionary from `QueryProcessorBuilder` yet — `Build()` still references it (removed in the next task atomically with the `Build()` update)
  - Verify: `dotnet build Darker.Filter.slnf -c Release` succeeds, no remaining `AddContextBagItem` references
  - Covers: FR10, AC8

- [ ] **TEST + IMPLEMENT: QueryProcessorBuilder passes policy registry to QueryProcessor constructor**
  - **USE COMMAND**: `/test-first when query processor built with policy registry should set policies on context`
  - Depends on: AddContextBagItem removed
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `When_query_processor_built_with_policy_registry_should_set_policies_on_context.cs`
  - Test should verify:
    - Build `QueryProcessor` via `QueryProcessorBuilder` with `DefaultPolicies()` or `Policies(registry)`
    - Execute a query
    - Handler's `Context.Policies` is the policy registry (not null)
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Update `QueryProcessorBuilder.Build()`: pass `PolicyRegistry` to `QueryProcessor` constructor instead of `_contextBagData`
    - Remove `_contextBagData` dictionary from `QueryProcessorBuilder` (now unused — `Build()` uses `PolicyRegistry` instead)
    - Remove `IReadOnlyDictionary<string, object> contextBagData` constructor param from `QueryProcessor` (the `_policyRegistry` param was already added in Phase 2)
    - Remove `_contextBagData` field from `QueryProcessor`
    - Update `ServiceCollectionExtensions.BuildQueryProcessor`: stop passing `contextBag`, resolve `IPolicyRegistry<string>?` from `IServiceProvider`, pass to `QueryProcessor` constructor
    - **Both call sites updated in the same task as the constructor change** — build stays green
  - Covers: FR8, FR9, FR14, AC7, AC17

- [ ] **TEST + IMPLEMENT: AddDefaultPolicies/AddPolicies registers IPolicyRegistry in DI**
  - **USE COMMAND**: `/test-first when AddDefaultPolicies called should register policy registry in service collection`
  - Depends on: previous task
  - Test location: `test/Paramore.Darker.Tests/Integrations`
  - Test file: `When_AddDefaultPolicies_called_should_register_policy_registry.cs`
  - Test should verify:
    - Call `services.AddDarker().AddDefaultPolicies()` on a `ServiceCollection`
    - Build `ServiceProvider` and resolve `IQueryProcessor`
    - Execute a query — handler's `Context.Policies` is non-null and contains default policies
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Update generic `AddDefaultPolicies<TBuilder>`: also register `IPolicyRegistry<string>` in `IServiceCollection` (the `IDarkerHandlerBuilder` provides access to the service collection; add an `IServiceCollection Services` property to `IDarkerHandlerBuilder` if not already present, or cast to `ServiceCollectionDarkerHandlerBuilder`)
    - Update generic `AddPolicies<TBuilder>`: same pattern — register the provided registry in DI
    - Remove `DarkerContextBag` creation and usage from `ServiceCollectionExtensions`
    - Delete `DarkerContextBag.cs`
  - Covers: FR11, FR12, AC9, AC15

- [ ] **TEST + IMPLEMENT: AddJsonQueryLogging registers JsonSerializerSettings in DI**
  - **USE COMMAND**: `/test-first when AddJsonQueryLogging called should register serializer settings in service collection`
  - Depends on: previous task
  - Test location: `test/Paramore.Darker.Tests/Integrations`
  - Test file: `When_AddJsonQueryLogging_called_should_register_serializer_settings.cs`
  - Test should verify:
    - Call `services.AddDarker().AddJsonQueryLogging()` on a `ServiceCollection`
    - Build `ServiceProvider` and resolve `JsonSerializerSettings` (or verify it was registered)
    - Logging decorator can be resolved with injected settings
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Update generic `AddJsonQueryLogging<TBuilder>`: register `JsonSerializerSettings` as singleton in `IServiceCollection`
  - Covers: FR13, AC16

---

## Phase 5: Consumer Updates

- [ ] **TEST + IMPLEMENT: FakeQueryProcessor accepts and stores provided context**
  - **USE COMMAND**: `/test-first when FakeQueryProcessor called with context should store provided context`
  - Depends on: Phase 2 complete (IQueryProcessor signature changed)
  - Test location: `test/Paramore.Darker.Tests`
  - Test file: `When_FakeQueryProcessor_called_with_context_should_store_context.cs`
  - Test should verify:
    - Create `FakeQueryProcessor` and a `QueryContext` with known Bag data
    - Call `Execute(query, myContext)` — `LastProvidedContext` is the same instance
    - Call `ExecuteAsync(query, myContext, ct)` — `LastProvidedContext` is the same instance
    - Call `Execute(query)` without context — `LastProvidedContext` is null
  - **STOP HERE — WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Update `FakeQueryProcessor.Execute` signature: add `IQueryContext queryContext = null`
    - Update `FakeQueryProcessor.ExecuteAsync` signature: add `IQueryContext queryContext = null` before `CancellationToken`
    - Add `public IQueryContext LastProvidedContext { get; private set; }` property
    - Store `queryContext` in both methods
  - Covers: FR17, AC19

- [ ] **TIDY: Update sample app and benchmarks for new API**
  - Depends on: All previous phases complete
  - Update `samples/SampleMinimalApi/Program.cs`: verify `.AddPolicies(...)` still works with new DI registration path
  - Update `test/Paramore.Darker.Benchmarks/Benchmark.cs`: update `QueryProcessor` construction if needed (builder no longer passes `contextBagData`); update any `ExecuteAsync` calls to use named `cancellationToken:` parameter if using positional syntax
  - Verify: `dotnet build Darker.Filter.slnf -c Release` succeeds
  - Covers: FR16 (partial)

---

## Phase 6: Compile and Verify

- [ ] **VERIFY: All first-party projects compile and existing tests pass**
  - Depends on: All previous phases complete
  - Run `dotnet build Darker.Filter.slnf -c Release`
  - Run `dotnet test Darker.Filter.slnf -c Release --no-build`
  - NFR1 verification: The default (null context) path adds only a null-coalescing assignment (`??=`) — negligible overhead. Confirmed by code inspection; no benchmark regression test required.
  - Verify no remaining references to removed APIs:
    - `grep -r "AddContextBagItem" src/` — should return nothing
    - `grep -r "DarkerContextBag" src/` — should return nothing
    - `grep -r "_contextBagData" src/` — should return nothing
    - `grep -r "NewtonsoftJsonSerializer" src/` — should return nothing
    - `grep -r "Constants.ContextBagKey" src/` — should return nothing (both Policies and QueryLogging)
  - Covers: FR16, AC18, AC20

---

## FR/AC Coverage Map

| FR | Task(s) | AC |
|----|---------|-----|
| FR1 | Execute without context | AC1 |
| FR2 | ExecuteAsync without context | AC2 |
| FR3 | Execute/ExecuteAsync without context | AC1, AC2 |
| FR4 | Execute/ExecuteAsync with context | AC3, AC4, AC5 |
| FR5 | InitQueryContext sets/preserves Policies | AC5, AC6 |
| FR6 | Add Policies property (Phase 1 tidy) | AC10 |
| FR6a | Constructor param + InitQueryContext | AC10a, AC11 |
| FR6b | Policy decorator reads Context.Policies | AC14 |
| FR7 | Logging decorator constructor injection | AC12, AC13 |
| FR8 | Remove contextBagData constructor param (Phase 4 tidy) | AC7 |
| FR9 | Remove _contextBagData field + CreateQueryContext (Phase 2 + Phase 4 tidy) | AC7 |
| FR10 | Remove AddContextBagItem (Phase 4 tidy) | AC8 |
| FR11 | Remove DarkerContextBag (AddDefaultPolicies DI task) | AC9 |
| FR12 | AddDefaultPolicies/AddPolicies registers in DI (builder relocation deferred to #321 per ADR section 8) | AC15 |
| FR13 | AddJsonQueryLogging registers in DI | AC16 |
| FR14 | Builder passes policy registry | AC17 |
| FR15 | Fluent builder extension methods (Policies + JsonQueryLogging) | AC17 |
| FR16 | All projects compile (Phase 5 consumer updates + Phase 6 verify) | AC18 |
| FR17 | FakeQueryProcessor update | AC19 |
| NFR1 | Phase 6 verification (code inspection — `??=` is negligible overhead) | — |
| NFR2 | Positional CancellationToken breaking change | AC20 |
