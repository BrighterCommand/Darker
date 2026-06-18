# Tasks — Polly V8 Resilience Pipeline Integration

**Spec**: `specs/009-resilience_pipeline`
**ADR**: [0015 Polly V8 Resilience Pipeline Integration](../../docs/adr/0015-resilience-pipeline-integration.md) (Accepted)
**Issue**: #293

## How to use this list

- **MANDATORY TDD**: Every behavioral task is a single **TEST + IMPLEMENT** unit. Use the
  exact `/test-first` command shown, then **STOP and wait for IDE approval** before implementing.
- **Tidy First**: The structural tasks (S1–S2) come first and change **no behaviour**; use
  `/tidy-first`. Legacy tests must stay green after each.
- **Test locations**: core decorator/attribute/context/builder behaviour lives in
  `test/Paramore.Darker.Core.Tests/` (the legacy policy tests are the templates to mirror);
  DI behaviour lives in `test/Paramore.Darker.Extensions.Tests/`. New test doubles go in
  `test/Paramore.Darker.Core.Tests/TestDoubles/` (namespace `Paramore.Darker.Core.Tests.TestDoubles`).
- **⚠️ Stale skill path**: the `/test-first` skill template and CLAUDE.md reference
  `test/Paramore.Darker.Tests`, which **does not exist** in this repo. For this spec, place tests
  in `test/Paramore.Darker.Core.Tests` (core) or `test/Paramore.Darker.Extensions.Tests` (DI) — the
  paths each task names below — not the skill's default. (Fixing the skill/CLAUDE.md is a separate
  follow-up, out of this spec's scope.)
- **Test pipelines are deterministic** (zero-delay retry, breaker break-duration as needed) per NFR5.
- **Definition of done** (applies to every new public type): XML doc comments + MIT licence header;
  all tests green via `dotnet test Darker.Filter.slnf -c Release`.

---

## Phase 0 — Structural (Tidy First, no behaviour change)

- [x] **S1 — STRUCTURAL: Add resilience members to `IQueryContext` and `QueryContext`**
  - **USE COMMAND**: `/tidy-first add ResiliencePipeline provider and ResilienceContext members to IQueryContext`
  - Add to `src/Paramore.Darker/IQueryContext.cs`:
    - `ResiliencePipelineProvider<string> ResiliencePipeline { get; set; }` (FR1/RD1 — narrow **provider**, not the registry)
    - `ResilienceContext? ResilienceContext { get; set; }` (FR9/RD2)
  - Implement both in `src/Paramore.Darker/QueryContext.cs` (the sole `IQueryContext` impl) as auto-properties; `Policies` and `Bag` untouched.
  - Add `using Polly;` (for `ResilienceContext`) and `using Polly.Registry;` (already present, for the provider).
  - No behaviour: nothing reads the members yet. **Verify legacy tests stay green** (`dotnet test Darker.Filter.slnf -c Release`).
  - Depends on: none.

- [x] **S2 — STRUCTURAL: Add resilience-pipeline default-key constants**
  - **USE COMMAND**: `/tidy-first add RetryPipelineName and CircuitBreakerPipelineName constants`
  - In `src/Paramore.Darker/Policies/Constants.cs` add two constants distinct from the legacy
    `RetryPolicyName` / `CircuitBreakerPolicyName` (RD4), e.g. `RetryPipelineName = "Darker.RetryPipeline"`
    and `CircuitBreakerPipelineName = "Darker.CircuitBreakerPipeline"`.
  - No behaviour: constants are unreferenced until Phase 7. **Legacy tests stay green.**
  - Depends on: none.

---

## Phase 1 — Processor populates the context (behavioural)

- [ ] **B1 — TEST + IMPLEMENT: QueryProcessor sets the resilience provider on the context when absent, preserves a caller-supplied one**
  - **USE COMMAND**: `/test-first when query processor built with a resilience pipeline registry should set the provider on the query context fill-if-absent`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test files (mirror the legacy pair `When_query_context_has_no_policies_should_set_policies_from_processor.cs` and `When_caller_provides_context_with_policies_should_preserve_caller_policies.cs`):
    - `When_query_context_has_no_resilience_provider_should_set_provider_from_processor.cs`
    - `When_caller_provides_context_with_resilience_provider_should_preserve_caller_provider.cs`
  - Test should verify:
    - A `QueryProcessor` constructed with a `ResiliencePipelineProvider<string>` writes it onto `IQueryContext.ResiliencePipeline` when the context's member is null
    - A caller-supplied non-null `IQueryContext.ResiliencePipeline` is **not** overwritten (fill-if-absent)
    - `ResilienceContext` is **not** populated by the processor (stays null) — it is caller-owned (FR9)
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add optional ctor param `ResiliencePipelineProvider<string> resiliencePipelineProvider = null` to `QueryProcessor` (`src/Paramore.Darker/QueryProcessor.cs:27-30`), store in a field beside `_policyRegistry` (`:25`, `:44`)
    - In `InitQueryContext` (`:102-106`) add a fill-if-absent guard mirroring `Policies`: `if (queryContext.ResiliencePipeline == null) queryContext.ResiliencePipeline = _resiliencePipelineProvider;`
    - Add `ResiliencePipelineRegistry<string> ResiliencePipelineRegistry { get; set; }` to `QueryProcessorBuilder` (`src/Paramore.Darker/Builder/QueryProcessorBuilder.cs:12` area) and pass it into the `new QueryProcessor(...)` call (`:93`) as the new param (registry derives from provider)
  - Depends on: S1.

---

## Phase 2 — Attributes (behavioural)

- [ ] **B2 — TEST + IMPLEMENT: `[UseResiliencePipeline]` / `[UseResiliencePipelineAsync]` carry the key + type-scope flag and resolve the decorator type**
  - **USE COMMAND**: `/test-first when use resilience pipeline attribute used should return policy and useTypePipeline params and the matching decorator type`
  - Test location: `test/Paramore.Darker.Core.Tests/Decorators`
  - Test file: `When_use_resilience_pipeline_attribute_used_should_return_params_and_decorator_type.cs`
  - Test should verify:
    - `UseResiliencePipelineAttribute(step, "Name", useTypePipeline)` `GetAttributeParams()` returns `{ "Name", useTypePipeline }`
    - `GetDecoratorType()` returns `typeof(UseResiliencePipelineHandler<,>)` (sync) and `typeof(UseResiliencePipelineHandlerAsync<,>)` for the async attribute
    - `useTypePipeline` defaults to `false`
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `UseResiliencePipelineAttribute : QueryHandlerAttribute` and `UseResiliencePipelineAsyncAttribute : QueryHandlerAttributeAsync` in `src/Paramore.Darker/Policies/Attributes/` (mirror `RetryableQueryAttribute[Async].cs`; names follow Brighter **verbatim spelling**, RD3)
    - Ctor `(int step, string policy, bool useTypePipeline = false)`; `GetAttributeParams()` → `new object[] { policy, useTypePipeline }`
    - `GetDecoratorType()` returns `typeof(UseResiliencePipelineHandler<,>)` (sync attribute) / `typeof(UseResiliencePipelineHandlerAsync<,>)` (async attribute) — a **compile-time** type reference, so those handler types must exist for this to build.
    - **Close the compile-gap (B2-first ordering is deliberate):** in this implement step, create **empty handler shells** — `UseResiliencePipelineHandler<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult> where TQuery : IQuery<TResult>` and the async counterpart (`IQueryHandlerDecoratorAsync<TQuery, TResult> where TQuery : IQuery<TResult>`) — with members present but bodies left to B3/B5 (e.g. `Execute`/`ExecuteAsync` throwing `NotImplementedException`, no resolution logic). **Mirror the full class header of `RetryableQueryDecorator[Async]` including its `where TQuery : IQuery<TResult>` constraint** (the decorator interfaces require it, so a shell without it will not compile). This lets the solution compile and B2's test (which only asserts params + `GetDecoratorType`) pass. B3/B5 then fill in `InitializeFromAttributeParams`/`Execute`/`ExecuteAsync` test-first; they add no new type, only behaviour.
  - Depends on: S1. **Creates the handler-type shells consumed by B3/B5** (B3/B5 add their behaviour). See dependency graph.

---

## Phase 3 — Sync decorator (behavioural)

- [ ] **B3 — TEST + IMPLEMENT: Sync decorator executes `next` through the named (shared) pipeline (AC1, sync)**
  - **USE COMMAND**: `/test-first when sync resilience pipeline decorator executes should run next through the named pipeline from the context provider`
  - Test location: `test/Paramore.Darker.Core.Tests/Decorators`
  - Test file: `When_sync_resilience_pipeline_decorator_executes_should_run_next_through_named_pipeline.cs`
  - Test should verify:
    - With a `ResiliencePipelineRegistry<string>` registered under a key and set on `IQueryContext.ResiliencePipeline`, the sync decorator resolves `GetPipeline(key)` and the handler result flows back through it (AC1)
    - The decorator uses the **non-generic** `GetPipeline(key)` when `useTypePipeline` is false
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Fill in the `UseResiliencePipelineHandler<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>` **shell created in B2** (in `src/Paramore.Darker/Policies/Handlers/`; mirror `RetryableQueryDecorator.cs`)
    - `InitializeFromAttributeParams` reads `_policy = (string)params[0]`, `_useTypePipeline = (bool)params[1]`
    - `Execute`: `Context.ResiliencePipeline.GetPipeline(_policy).Execute(() => next(query))` (type-scoped branch added in B10)
  - Depends on: S1, B2 (B2 created the handler shell).

- [ ] **B4 — TEST + IMPLEMENT: Sync decorator validates configuration at build time (AC5, sync)**
  - **USE COMMAND**: `/test-first when sync resilience pipeline decorator initialized with missing provider or unregistered key should throw ConfigurationException`
  - Test location: `test/Paramore.Darker.Core.Tests/Decorators`
  - Test file: `When_sync_resilience_pipeline_decorator_misconfigured_should_throw_ConfigurationException.cs`
  - Test should verify (failure surfaces in `InitializeFromAttributeParams`, before any query executes):
    - No provider on the context → `ConfigurationException` ("No resilience pipeline provider is configured…")
    - Provider present but key not registered → `ConfigurationException` whose **message contains the unresolved key**
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - In `UseResiliencePipelineHandler.InitializeFromAttributeParams`: guard `Context.ResiliencePipeline == null`; then resolve with `TryGetPipeline(_policy)` (shared) and throw a key-naming `ConfigurationException` on false — mirroring `RetryableQueryDecorator.cs:23-24,34-35`
  - Depends on: B3.

---

## Phase 4 — Async decorator + context propagation (behavioural)

- [ ] **B5 — TEST + IMPLEMENT: Async decorator runs `next` through the pipeline and honours the caller's token when no `ResilienceContext` (AC1 async, AC7)**
  - **USE COMMAND**: `/test-first when async resilience pipeline decorator executes without a resilience context should run next through the pipeline and honour the cancellation token`
  - Test location: `test/Paramore.Darker.Core.Tests/Decorators`
  - Test file: `When_async_resilience_pipeline_decorator_executes_without_context_should_honour_cancellation_token.cs`
  - Test should verify:
    - Result flows back through `GetPipeline(key).ExecuteAsync(...)` (AC1 async)
    - `IQueryContext.ResilienceContext == null` branch: (a) an already-cancelled token surfaces `OperationCanceledException`; (b) a token cancelled while the handler is in flight aborts execution (AC7)
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Fill in the `UseResiliencePipelineHandlerAsync<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>` **shell created in B2** (mirror `RetryableQueryDecoratorAsync.cs`)
    - Null-context branch: `await pipeline.ExecuteAsync(ct => new ValueTask<TResult>(next(query, ct)), cancellationToken)` — caller token flows through unchanged
    - Same build-time validation as B4 (provider null + unregistered key via `TryGetPipeline`)
  - Depends on: S1, B2 (B2 created the handler shell).

- [ ] **B6 — TEST + IMPLEMENT: Async decorator executes through a caller-supplied `ResilienceContext` (AC8)**
  - **USE COMMAND**: `/test-first when async resilience pipeline decorator executes with a resilience context should pass it to the pipeline and use its cancellation token`
  - Test location: `test/Paramore.Darker.Core.Tests/Decorators`
  - Test file: `When_async_resilience_pipeline_decorator_executes_with_context_should_use_supplied_context.cs`
  - Test should verify:
    - With `IQueryContext.ResilienceContext` set, a strategy in the pipeline reads a property set on `context.Properties` before execution (AC8)
    - The `ResilienceContext.CancellationToken` is the token used for the execution (tokens are **not** merged)
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Non-null-context branch: execute the `ResilienceContext` overload of `ExecuteAsync`, passing the supplied context; mirror Brighter's `ResilienceExceptionPolicyHandlerAsync` branch on `Context?.ResilienceContext != null`
  - Depends on: B5.

- [ ] **B7 — TEST + IMPLEMENT: Async decorator build-time validation (AC5, async)**
  - **USE COMMAND**: `/test-first when async resilience pipeline decorator initialized with missing provider or unregistered key should throw ConfigurationException`
  - Test location: `test/Paramore.Darker.Core.Tests/Decorators`
  - Test file: `When_async_resilience_pipeline_decorator_misconfigured_should_throw_ConfigurationException.cs`
  - Test should verify: same two failure modes as B4, on the async decorator, surfaced in `InitializeFromAttributeParams`
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should: reuse the same validation logic in `UseResiliencePipelineHandlerAsync.InitializeFromAttributeParams`
  - Depends on: B5.

---

## Phase 5 — Real resilience behaviours (behavioural)

- [ ] **B8 — TEST + IMPLEMENT: A retry pipeline retries a transiently-failing handler to success, sync and async (AC2)**
  - **USE COMMAND**: `/test-first when a retry resilience pipeline wraps a transiently failing handler should retry and ultimately succeed`
  - Test location: `test/Paramore.Darker.Core.Tests/Decorators`
  - Test files:
    - `When_retry_resilience_pipeline_wraps_transient_failure_should_succeed_sync.cs`
    - `When_retry_resilience_pipeline_wraps_transient_failure_should_succeed_async.cs`
  - Test should verify:
    - A handler that throws N-1 times then succeeds returns the success result when wrapped in a zero-delay retry pipeline (registered via `AddRetry`), on both sync and async paths (AC2, NFR2)
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should: require no new production code beyond B3/B5 if those are correct; if a gap surfaces, fix the decorator. Use a counting test-double handler in `TestDoubles/`.
  - Depends on: B3, B5.

- [ ] **B9 — TEST + IMPLEMENT: A circuit-breaker pipeline opens after the configured failures, sync and async (AC3)**
  - **USE COMMAND**: `/test-first when a circuit breaker resilience pipeline reaches its failure threshold should open the circuit`
  - Test location: `test/Paramore.Darker.Core.Tests/Decorators`
  - Test files:
    - `When_circuit_breaker_resilience_pipeline_reaches_threshold_should_open_sync.cs`
    - `When_circuit_breaker_resilience_pipeline_reaches_threshold_should_open_async.cs`
  - Test should verify:
    - After the configured number of consecutive failures the breaker opens and a subsequent call throws `BrokenCircuitException` (or Polly's open-circuit exception), on both paths (AC3, NFR2)
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should: rely on B3/B5 decorators; pipeline built with `AddCircuitBreaker` using a deterministic threshold/break duration in the test.
  - Depends on: B3, B5.

---

## Phase 6 — Type-scoped pipelines (behavioural)

- [ ] **B10 — TEST + IMPLEMENT: `UseTypePipeline` gives independent breaker state per result type and shared state within a result type (AC4, FR8)**
  - **USE COMMAND**: `/test-first when useTypePipeline is set should resolve a pipeline per result type giving independent state across result types and shared state within one`
  - Test location: `test/Paramore.Darker.Core.Tests/Decorators`
  - Test file: `When_use_type_pipeline_set_should_isolate_breaker_state_per_result_type.cs`
  - Test should verify (registry registers a generic builder `TryAddBuilder<TResult>(key, …)` per result type — the FR8 precondition):
    - Two query types with **different** result types under the same breaker key open **independently** (opening one does not open the other)
    - Two queries with the **same** result type **share** breaker state under the key
    - Pairing `useTypePipeline:true` with a key that has only a non-generic builder fails build-time validation with the unresolved-key message (separate generic/non-generic namespaces)
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add the type-scoped branch to **both** decorators: when `_useTypePipeline`, resolve `GetPipeline<TResult>(_policy)` and validate with `TryGetPipeline<TResult>(_policy)`; otherwise the shared `GetPipeline(_policy)` path (ADR §4)
  - Depends on: B3, B4, B5, B7.

---

## Phase 7 — Builder registration + defaults (behavioural)

- [ ] **B11 — TEST + IMPLEMENT: Builder `ResiliencePipelines(registry)` wires the decorators and executes; `null` throws (AC10)**
  - **USE COMMAND**: `/test-first when query processor builder configured with a resilience pipeline registry should register the decorators and reject a null registry`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_builder_configured_with_resilience_pipeline_registry_should_register_decorators.cs`
  - Test should verify:
    - `builder.ResiliencePipelines(registry)` registers both decorators so a `[UseResiliencePipeline]`-decorated handler resolves and executes through the named pipeline at runtime (AC10)
    - A `null` registry throws `ArgumentNullException` (null-guard only — **no** legacy content-key check, FR4)
    - The supplied registry is set on `QueryProcessorBuilder.ResiliencePipelineRegistry` and threaded onto the processor (B1)
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `ResiliencePipelines(this IBuildTheQueryProcessor, ResiliencePipelineRegistry<string>)` and `AddResiliencePipelines<TBuilder>(...)` to `src/Paramore.Darker/Policies/QueryProcessorBuilderExtensions.cs` (mirror `Policies(...)` / `AddPolicies(...)`); register `UseResiliencePipelineHandler<,>` + `…HandlerAsync<,>`; set `ResiliencePipelineRegistry`; guard null with `ArgumentNullException`; **omit** the `Constants` content check (FR4)
  - Depends on: B1, B3, B5.

- [ ] **B12 — TEST + IMPLEMENT: `DefaultResiliencePipelines()` registers a default retry + circuit-breaker pipeline under the new keys, resolvable and executable (AC6, builder side)**
  - **USE COMMAND**: `/test-first when DefaultResiliencePipelines is called should register default retry and circuit breaker pipelines under their well-known keys`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_DefaultResiliencePipelines_called_should_register_resolvable_default_pipelines.cs`
  - Test should verify:
    - After `builder.DefaultResiliencePipelines()`, the provider resolves a pipeline under `Constants.RetryPipelineName` and under `Constants.CircuitBreakerPipelineName`, and each executes a callback without further configuration (AC6)
    - The default keys do **not** collide with the legacy `RetryPolicyName` / `CircuitBreakerPolicyName` (RD4)
    - (Exact strategy params are an implementation choice — assert resolvable + executable, not specific timings, per FR6)
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `DefaultResiliencePipelines()` / `AddDefaultResiliencePipelines<TBuilder>()` building a `ResiliencePipelineRegistry<string>` with **non-generic** `TryAddBuilder` for a retry pipeline (`AddRetry`) under `RetryPipelineName` and a breaker (`AddCircuitBreaker`) under `CircuitBreakerPipelineName`, then delegate to `ResiliencePipelines(...)`. Non-generic builders only → defaults do **not** support `UseTypePipeline` (ADR §4/§5)
  - Depends on: B11, S2.

---

## Phase 8 — DI registration + defaults (behavioural)

- [ ] **B13 — TEST + IMPLEMENT: DI `AddResiliencePipelines(registry)` wires the registry + decorators and executes; `null` throws (AC11)**
  - **USE COMMAND**: `/test-first when AddResiliencePipelines is called should register the provider and decorators and reject a null registry`
  - Test location: `test/Paramore.Darker.Extensions.Tests`
  - Test file: `When_AddResiliencePipelines_called_should_register_provider_and_decorators.cs`
  - Test should verify:
    - `services.AddDarker(...).AddResiliencePipelines(registry)` registers the registry as the `ResiliencePipelineProvider<string>` service and both decorators, so a `[UseResiliencePipeline]` handler resolves and executes through the named pipeline at runtime under the configured `QueryProcessor` lifetime (AC11)
    - The **end-to-end** execution actually flows through the named pipeline (a transient-failure handler retried to success, or a property observed) — proving the provider reached the `IQueryContext`, not merely that a service was registered
    - A `null` registry throws `ArgumentNullException`
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `AddResiliencePipelines(this IDarkerHandlerBuilder, ResiliencePipelineRegistry<string>)` to `src/Paramore.Darker.Extensions.DependencyInjection/PolicyDIExtensions.cs` (mirror `AddPolicies`): delegate to the builder extension for decorator registration, register the registry as `ResiliencePipelineProvider<string>`; guard null
    - **CRITICAL (Finding 1 of review-tasks.md): thread the provider into the processor.** The DI path constructs `QueryProcessor` in `ServiceCollectionExtensions.BuildQueryProcessor` (`src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionExtensions.cs:43-49`), which resolves `IPolicyRegistry<string>` from the provider (`:43`) and passes it to the constructor (`:45-49`). Resolve `ResiliencePipelineProvider<string>` there the **same way** (`provider.GetService<ResiliencePipelineProvider<string>>()`) and pass it as the new `QueryProcessor` ctor parameter. Without this, `InitQueryContext` fills `Context.ResiliencePipeline` from a null field and the decorator's build-time validation throws "no provider configured" — AC11 would fail despite the service being registered.
  - Depends on: B11, B1 (B1 added the `QueryProcessor` ctor parameter this step feeds on the DI path).

- [ ] **B14 — TEST + IMPLEMENT: DI `AddDefaultResiliencePipelines()` registers resolvable default pipelines (AC6, DI side)**
  - **USE COMMAND**: `/test-first when AddDefaultResiliencePipelines is called should register resolvable default retry and circuit breaker pipelines`
  - Test location: `test/Paramore.Darker.Extensions.Tests`
  - Test file: `When_AddDefaultResiliencePipelines_called_should_register_resolvable_default_pipelines.cs`
  - Test should verify: the resolved `ResiliencePipelineProvider<string>` returns executable pipelines under `RetryPipelineName` and `CircuitBreakerPipelineName` (AC6)
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should: add `AddDefaultResiliencePipelines(this IDarkerHandlerBuilder)` mirroring the **DI** `PolicyDIExtensions.AddDefaultPolicies` (`:18-40`), which **delegates** to `AddPolicies` (`:39`) — i.e. build the default registry (reuse B12's default-pipeline helper) and delegate to `AddResiliencePipelines`. (Note: the *builder* `DefaultPolicies` likewise delegates to `Policies`, but the legacy *builder* `AddDefaultPolicies<TBuilder>` at `QueryProcessorBuilderExtensions.cs:66-92` inlines instead — do not mirror that inlined shape here; delegate.)
  - Depends on: B12, B13.

---

## Phase 9 — Regression, parity, and docs (verification)

- [ ] **B15 — VERIFY: Legacy path unchanged + sync/async parity + DoD**
  - Not a `/test-first` task — a verification gate over the completed work.
  - Verify:
    - All pre-existing legacy-policy tests (`When_retryable_decorator_*`, `*_policies_*`, `FallbackPolicyTests`, `When_AddDefaultPolicies_*`) pass **unchanged** (AC9 / NFR1) — no edits to legacy files
    - Every new capability exists on **both** sync and async paths (NFR2): attributes, decorators, build-time validation, retry, breaker, type-scoping
    - **DoD**: each new public type (`UseResiliencePipelineAttribute[Async]`, `UseResiliencePipelineHandler[Async]`, the new extension methods, new `Constants`) has XML doc comments + MIT licence header
    - `dotnet test Darker.Filter.slnf -c Release` is fully green
    - No new NuGet dependency added (NFR3) — `Directory.Packages.props` unchanged
  - Depends on: all behavioural tasks.

---

## Dependency summary

```
S1 ─┬─ B1 ──┬─ B11 ─┬─ B12 ─┬─ B14
    │        │       └─ B13 ─┘   (B13 also needs B1: DI threads provider into BuildQueryProcessor)
    ├─ B2 ──┬─ B3 ─┬─ B4 ─┐      (B2 creates the handler SHELLS; B3/B5 fill in behaviour)
    │       │      │       ├─ B10
    │       └─ B5 ─┼─ B6   │
    │              ├─ B7 ──┘
    │              └─(B3,B5)─ B8, B9
    └─ (used by all decorators)
S2 ─── B12
all behavioural ─── B15
```

Notes:
- **B2 → B3/B5**: B2's implement step creates empty `UseResiliencePipelineHandler[Async]<,>` shells so the
  solution compiles and B2's attribute test passes; B3 (sync) and B5 (async) add the behaviour test-first.
  No type is introduced after B2.
- **B1 → B13**: B1 adds the `QueryProcessor` ctor parameter and the builder-path threading; B13 additionally
  threads the provider on the **DI** path via `ServiceCollectionExtensions.BuildQueryProcessor` (review Finding 1).

## Requirement → task coverage

| Req | Task(s) | Req | Task(s) |
|---|---|---|---|
| FR1 provider on context | S1, B1 | FR9 ResilienceContext propagation | B5 (null), B6 (present) |
| FR2 sync+async decorators | B3, B5 | NFR1 backward compat | B15 (AC9) |
| FR3 attributes | B2 | NFR2 sync/async parity | B3/B5, B8, B9, B15 |
| FR4 builder reg (null-guard, no content check) | B11 | NFR3 no new deps | B15 |
| FR5 DI reg | B13 | NFR4 Brighter naming | B2 |
| FR6 defaults (distinct keys, no UseTypePipeline) | B12, B14, S2 | NFR5 testability (real doubles, deterministic) | all test tasks |
| FR7 build-time validation | B4, B7 | RD1 narrow provider | S1 |
| FR8 type-scoped per result type | B10 | RD2/RD3/RD4 | S1/B2/S2 |

| AC | Task | AC | Task |
|---|---|---|---|
| AC1 | B3, B5 | AC7 | B5 |
| AC2 | B8 | AC8 | B6 |
| AC3 | B9 | AC9 | B15 |
| AC4 | B10 | AC10 | B11 |
| AC5 | B4, B7 | AC11 | B13 |
| AC6 | B12, B14 | DoD | B15 |
