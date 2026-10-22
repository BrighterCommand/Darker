# Tasks — Spec 016: Pipeline validation at startup

**Spec**: `specs/016-pipeline_validation/requirements.md`
**Linked issue**: [#305 — ValidatePipelines() startup validation](https://github.com/BrighterCommand/Darker/issues/305) (lifetime rules added in the scope of #377)
**ADRs (all Accepted)**: [0025 Pipeline Validation at Startup](../../docs/adr/0025-pipeline-validation-at-startup.md), [0026 Lifetime Scope Validation](../../docs/adr/0026-lifetime-scope-validation.md)

**How the phases depend on each other.** The phases must run in order, and the order is ADR 0025's step order and then ADR 0026's.
- **Phases 1–2 (ADR 0025 steps 1–2)** copy Brighter's Specification pattern into core and give a finding a rule identifier. Every rule in this spec is built on these.
- **Phase 3 (step 3)** extracts `PipelineAttributeReader` from `PipelineBuilder` without changing behaviour, so that validation and the builder share one reflection path.
- **Phases 4–5 (steps 4–5)** make the registrations readable.
- **Phases 6–9 (steps 6–9)** build the core validator, the pipeline rules, the probe and the ordering table, all tested in `test/Paramore.Darker.Core.Tests` with no container.
- **Phase 10 (step 10)** adds the hosting package reference.
- **Phase 11 (step 11)** wires the validator into the container and the host.
- **Phase 12 (step 12)** documents the pipeline half and ends with the ADR 0025 checkpoint.
- **Phases 13–18 (ADR 0026 steps 1–6)** cannot start until that checkpoint is green, because ADR 0026 builds on ADR 0025 steps 1–11. They add the lifetime validator in the DI package.
- **Phase 19 (ADR 0026 step 7)** is the one documentation commit that ADR 0026 allows.
- **Phase 20** is the final checkpoint.

No phase may start until the phase before it is green on net8.0 and net9.0.

### Conventions for every task in this file

- **Public surface only.** Tests reach the code through `PipelineValidator`, `IAmAPipelineValidator`, `PipelineValidationResult`, the Specification types, the core registries, `AddDarker`, `ValidatePipelines()`, `ValidateAll`, the `IHostedService` instances resolved from the container, and a real host. Never use `InternalsVisibleTo`. Never drive `PipelineAttributeReader`, `PipelineDescriber`, `DarkerValidationHostedService`, `ServiceProviderResolvabilityProbe`, `ScopeConfigurationValidator`, `ServiceRegistrationSnapshot`, `DescriptorRecord`, `ScopeConfiguration`, `ComponentRegistration`, `ComponentConstructorSelector` or `ScopeConfigurationRules` directly.
- **Where tests go.**
  - Core tests go in `test/Paramore.Darker.Core.Tests/Specifications/` (Phases 1–2), the `test/Paramore.Darker.Core.Tests/` root (the Phase 3 pre-refactor guards, beside the existing builder tests) and `test/Paramore.Darker.Core.Tests/PipelineValidation/` (Phases 5–9).
  - DI tests go in `test/Paramore.Darker.Extensions.Tests/PipelineValidation/` (Phases 11 and 13–18).
  - The one `AddDarkerRequestScope()` test (T15.9) goes in `test/Paramore.Darker.Extensions.AspNetCore.Tests/`.
- **Test doubles.** Prefer Real, then Simple, then InMemory. Use no mocks. Put each new double in its project's `TestDoubles/` folder, one class per file, in the namespace `{TestProject}.TestDoubles`. The names below are suggestions; reuse an existing double where it fits.
  - **Core.Tests handler doubles stay `internal`** (ADR 0011), so assembly scanning never collects them, and each test registers them explicitly. `HandlerPublic` therefore flags them. A core rule test asserts on findings with its own rule identifier, never on total counts. A test that expects no errors uses a public handler that `test/Paramore.Darker.Core.Tests/Exported/` already provides.
  - **Extensions.Tests handler doubles for this spec are `public`** (ADR 0025 step 11, ADR 0026 conventions). No scan targets that assembly, so they carry no `HandlerPublic` error. Register them explicitly.
- **Rule assertions.** Every rule test asserts four things:
  - `RuleId` equals `new ValidationRuleId("<RuleName>")`;
  - the `ValidationSeverity`;
  - that the message names the offender;
  - for ADR 0026 rules, that the message ends with the README link `https://github.com/BrighterCommand/Darker/blob/master/README.md#<lower-case rule id>`.

  From Phase 8 on, a validator built without a probe also reports a `ResolvabilityNotChecked` warning. So core tests assert on errors, or on their own rule's findings, never on warnings in general.
- **Hosts and logging.**
  - Pass `TestContext.Current.CancellationToken` to every host start and stop.
  - Tests that build a host, or a provider with real logging, carry `[Collection("DarkerHostBootstrap")]`. Use `test/Paramore.Darker.Extensions.Tests/Logging/DarkerHostBootstrapCollection.cs`, or the AspNetCore test project's own `DarkerHostBootstrapCollection.cs`.
  - Never dispose a provider whose `ILoggerFactory` is real. `BuildQueryProcessor` stores it in the static `ApplicationLogging` (`ServiceCollectionExtensions.cs:75-76`).
  - Capture hosted-service log entries with `CapturingLoggerProvider` (`test/Paramore.Darker.Extensions.Tests/Logging/LoggerCaptureFixture.cs`), added to the container's logging. Use a fresh provider for each test.
- **Positions, not numbers.** The builder applies attributes in descending `Step` order and wraps each decorator around the previous one, so **the lowest `Step` is outermost**. Rules, messages and test names speak of "outer" and "inner".
- **Source files.**
  - Every new source file carries the full `#region Licence … #endregion` MIT header, in the form of `src/Paramore.Darker/ScopeAffinity.cs:1` (core) or `src/Paramore.Darker.Extensions.DependencyInjection/ScopeAffinityRegistration.cs:1-22` (DI).
  - Files copied from Brighter keep their Brighter copyright line.
  - New core files use `#nullable enable`.
  - Records rely on `src/Paramore.Darker/IsExternalInit.cs` for netstandard2.0.
- **Building validators in core tests.** Phases 6–9 build every `PipelineValidator` through the test-data builder `PipelineValidatorBuilder` (T6.1), so the constructor can grow without editing earlier tests.
- **Naming.** Test files and methods are named `When_[condition]_should_[behaviour]`, test classes `[Behavior]Tests`. Name the class-under-test variable after its class, for example `pipelineValidator`. Write Arrange/Act/Assert comments and use Evident Data.
- **CHARACTERISE tasks.** Observe RED by applying the named mutation to **production** code. The test must fail on the assertion it exists for. Revert, confirm green, check that `git status` shows no modified production file, run the full suite, and commit the test alone as `test:`, noting the mutation in the message.
- **Commits.** Each behavioural task produces one `feat:`/`test:` commit, then a separate `docs:` commit that ticks the box. STRUCTURAL tasks commit as `refactor:`, with the suite green before and after.
- **Gear.** The gate is armed (`review-before`) unless `/spec:gear` has recorded a scoped shift. The section headings below are stable, so a shift can be scoped to one of them.

---

## Phase 1 — Specification pattern copy (ADR 0025 step 1)

- [x] **STRUCTURAL: T1.1 — Copy Brighter's Specification types into core, in `Paramore.Darker.Specifications` (ADR 0025 step 1; Constraints: copy, do not reference; NFR core purity, NFR multi-target)**
  - Copy these files from `Brighter/src/Paramore.Brighter/` (the sibling checkout `../Brighter`) into `src/Paramore.Darker/Specifications/`:
    - `Specification.cs`, which also holds `ISpecification<T>`;
    - `AndSpecification.cs`, `OrSpecification.cs`, `NotSpecification.cs`;
    - `ISpecificationVisitor.cs`, `ValidationResultCollector.cs`;
    - `ValidationResult.cs`, `ValidationError.cs`, `ValidationSeverity.cs`.
  - Change only the namespace, to `Paramore.Darker.Specifications`. Add `#nullable enable` where the source relies on nullable annotations. Keep the Brighter copyright line.
  - Do **not** copy `Brighter/src/Paramore.Brighter.Validation.Specification/`. It is a request-validation handler, which is a different thing.
  - Risk check, for the name collision (ADR 0025 forces): build `src/Paramore.Darker.Validation.DataAnnotations`. Confirm that `ValidationResult` in `DataAnnotationsQueryValidatorDecorator.cs` still binds to `System.ComponentModel.DataAnnotations`.
  - `dotnet build Darker.Filter.slnf -c Release` succeeds for netstandard2.0, net8.0 and net9.0. The suite is green before and after. The commit adds new code only.

- [x] **CHARACTERISE: T1.2 — The copied Brighter specification tests pass against Darker's copy (ADR 0025 step 1; FR3, which relies on And/Or not short-circuiting)**
  - **USE COMMAND**: `/test-first when Brighter's specification tests run against Darker's copied Specification types should pass unchanged apart from namespace (copy verbatim from Brighter)`
  - Test location: "test/Paramore.Darker.Core.Tests/Specifications/"
  - Test file: `When_and_specification_left_fails_should_evaluate_both_sides.cs` (the group also includes `When_evaluating_a_specification.cs`, `When_specification_with_error_factory_fails_should_collect_error.cs` and `When_collector_visits_specification_graph_should_collect_all_failures.cs`, all copied from `Brighter/tests/Paramore.Brighter.Core.Tests/Specifications/`)
  - Test should verify:
    - Every copied fact passes, with its `[Behavior]Tests` class name kept (for example `AndSpecificationTests`).
    - `SpecificationTestState.cs` is copied from `Brighter/tests/Paramore.Brighter.Core.Tests/Specifications/TestDoubles/` into `test/Paramore.Darker.Core.Tests/TestDoubles/`, in the namespace `Paramore.Darker.Core.Tests.TestDoubles`.
    - Only `namespace` and `using` lines differ from Brighter. The tests keep Brighter's xUnit `Assert` style. If anything else has to change to compile under xunit.v3, stop and ask.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `src/Paramore.Darker/Specifications/AndSpecification.cs`, make `IsSatisfiedBy` short-circuit (`Left.IsSatisfiedBy(entity) && Right.IsSatisfiedBy(entity)`, replacing the separate evaluations at Brighter's `AndSpecification.cs:48-50`). The fact `AndSpecificationTests.When_both_sides_fail_should_collect_errors_from_both` (in `When_and_specification_left_fails_should_evaluate_both_sides.cs`) must fail first on `Assert.Equal(2, results.Count(r => !r.Success))` (line 52 of Brighter's test), because only the left side's error is collected.
  - Implementation should:
    - Need no production change. T1.1 delivers this.

## Phase 2 — Rule identifiers (ADR 0025 step 2)

> `Specification<T>` has two catch blocks: `EvaluateSimple` for the predicate form and `EvaluateCollapsed` for the result-evaluator form. T2.1 stamps the first and T2.2 the second, so each has its own RED. The pipeline rules' predicates (T8.4) rely on T2.1, and `CaptiveDependency`'s result evaluator (T17.12) relies on T2.2.

- [x] **TEST + IMPLEMENT: T2.1 — A predicate that throws yields a collected error whose RuleId is `RuleEvaluationFailed` (ADR 0025 step 2; FR3 rule identifier; catalogue row `RuleEvaluationFailed`)**
  - **USE COMMAND**: `/test-first when a specification's predicate throws should collect an error whose RuleId is ValidationRuleIds.EvaluationFailed`
  - Test location: "test/Paramore.Darker.Core.Tests/Specifications/"
  - Test file: `When_predicate_throws_should_report_evaluation_failed_rule_id.cs`
  - Test should verify:
    - A `Specification<string>` whose predicate throws `InvalidOperationException("boom")` returns false from `IsSatisfiedBy` and does not throw.
    - `ValidationResultCollector<string>` collects one `ValidationError` whose `RuleId` equals `ValidationRuleIds.EvaluationFailed`, whose value is `new ValidationRuleId("RuleEvaluationFailed")`. Its `Severity` is `Error`, and its message contains "boom".
    - A `ValidationError` built with only the three positional members has a null `RuleId`, so the copied tests compile unchanged.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add `src/Paramore.Darker/Specifications/ValidationRuleId.cs`, a record that wraps the identifier string. Add `ValidationRuleIds.cs`, which holds only the neutral constant `EvaluationFailed`.
    - Give `ValidationError` an init-only nullable `RuleId` that defaults to null.
    - In the predicate-form catch block of the copied `Specification<T>` (`EvaluateSimple`, Brighter's `Specification.cs:175-192`), stamp `RuleId = ValidationRuleIds.EvaluationFailed`. The general-purpose types name no pipeline rule. The result-evaluator catch block (`EvaluateCollapsed`) is T2.2's.

- [x] **TEST + IMPLEMENT: T2.2 — A result evaluator that throws yields a collected error whose RuleId is `RuleEvaluationFailed` (ADR 0025 step 2; ADR 0026 step 5 "constructor reflection runs inside that evaluator"; catalogue row `RuleEvaluationFailed`)**
  - **USE COMMAND**: `/test-first when a specification built with a result evaluator throws should collect an error whose RuleId is ValidationRuleIds.EvaluationFailed`
  - Test location: "test/Paramore.Darker.Core.Tests/Specifications/"
  - Test file: `When_result_evaluator_throws_should_report_evaluation_failed_rule_id.cs`
  - Test should verify:
    - A `Specification<string>` built with the result-evaluator constructor (`Func<T, IEnumerable<ValidationResult>>`), whose evaluator throws `InvalidOperationException("boom")`, returns false from `IsSatisfiedBy` and does not throw.
    - `ValidationResultCollector<string>` collects one `ValidationError` whose `RuleId` equals `ValidationRuleIds.EvaluationFailed`, whose `Severity` is `Error`, and whose message contains "boom".
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - In the result-evaluator catch block of the copied `Specification<T>` (`EvaluateCollapsed`, Brighter's `Specification.cs:155-173`), stamp `RuleId = ValidationRuleIds.EvaluationFailed`. After T2.1 and T2.2 both catch blocks stamp it.

## Phase 3 — Shared attribute reader (ADR 0025 step 3)

> Risk mitigation, for ADR 0025's Negative "PipelineAttributeReader must stay the single reflection path" and "the documentation contradicts the code". The core suite pins step order only on the stream path (`When_stream_handler_has_multiple_decorators_should_order_by_step_descending.cs`). All six wrong-kind paths already have tests (`When_sync_attribute_on_async_handler_should_throw_configuration_exception.cs`, `When_stream_handler_has_mismatched_decorator_attribute_should_throw_ConfigurationException.cs`), but each asserts only `ShouldContain("sync")`, `"async"` or `"stream"`, so the exact text is not pinned. T3.1 and T3.2 pin what the extraction must not change, before it starts.

- [x] **CHARACTERISE: T3.1 — On Execute and ExecuteAsync, the decorator with the lowest Step is outermost (pre-refactor guard; ADR 0025 Terms "Outer and inner"; FR9 semantics)**
  - **USE COMMAND**: `/test-first when a sync or async handler carries several decorator attributes should run the decorator with the lowest Step outermost`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_a_handler_has_several_decorators_should_run_the_lowest_step_outermost.cs`
  - Test should verify:
    - A sync handler carries recording decorator attributes at Steps 1, 2 and 3 (new `StepOrderRecordingDecorator`, its async twin, and their attributes). A shared recorder logs the entry order. `Execute` records 1, 2, 3, then the handler.
    - The async handler records the same order on `ExecuteAsync`.
    - The processor uses `QueryHandlerRegistry`/`QueryHandlerRegistryAsync`, `SimpleHandlerFactory`, `SimpleHandlerDecoratorFactory` and `InMemoryDecoratorRegistry`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `src/Paramore.Darker/PipelineBuilder.cs:255`, change `OrderByDescending(attr => attr.Step)` to `OrderBy`. The sync fact must fail on the recorded-order assertion (3, 2, 1 instead of 1, 2, 3). Apply the same at `:412` for the async fact.
  - Implementation should:
    - Need no production change.

- [x] **CHARACTERISE: T3.2 — The builder's six wrong-kind ConfigurationException messages are pinned by exact text (pre-refactor guard; ADR 0025 step 3, "messages unchanged")**
  - **USE COMMAND**: `/test-first when a handler method carries another path's decorator attribute should throw ConfigurationException whose message equals the existing text exactly, for all six wrong-kind paths`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_a_handler_method_carries_another_paths_attribute_should_throw_the_existing_configuration_message.cs`
  - Test should verify:
    - A sync handler with an async attribute on `Execute` throws `ConfigurationException`. Its message equals the text at `PipelineBuilder.cs:80-81` ("Sync handler has async attribute(s) on Execute. …").
    - A sync handler with a stream attribute on `Execute` gives the message at `:82-83`.
    - An async handler with a sync attribute on `ExecuteAsync` gives the message at `:145-146` ("Async handler has sync attribute(s) on ExecuteAsync. …").
    - An async handler with a stream attribute on `ExecuteAsync` gives the message at `:147-148`.
    - A stream handler with a sync attribute on `ExecuteAsync` gives the message at `:306-307` ("Stream handler has sync attribute(s) on ExecuteAsync. …").
    - A stream handler with an async attribute on `ExecuteAsync` gives the message at `:308-309` ("Stream handler has async attribute(s) on ExecuteAsync. …").
    - Every fact asserts equality with the full message, not `ShouldContain`. Reuse `SyncHandlerWithAsyncAttribute`, `SyncHandlerWithStreamAttribute`, `AsyncHandlerWithSyncAttribute`, `AsyncHandlerWithStreamAttribute`, `StreamHandlerWithSyncAttribute` and `StreamHandlerWithAsyncAttribute` from `TestDoubles/`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: change one word other than "sync", "async" or "stream" (for example "Use" to "Apply") in each message string passed at `PipelineBuilder.cs:81`, `:83`, `:146`, `:148`, `:307` and `:309`. Each fact must fail on its own message-equality assertion (the existing `ShouldContain` tests stay green under the same change).
  - Implementation should:
    - Need no production change.

- [x] **STRUCTURAL: T3.3 — Extract `PipelineAttributeReader`: Execute-method lookup, attribute read and order, decorator closing, with failures returned rather than thrown (ADR 0025 step 3; force "one source of pipeline knowledge")**
  - Add `src/Paramore.Darker/PipelineAttributeReader.cs` (internal, `#nullable enable`). Move into it:
    - the method lookups at `PipelineBuilder.cs:194`, `:199` and the stream signature lookup at `:300-302`;
    - the attribute read and order logic at `:253-256`, `:342-345` and `:410-413`;
    - the decorator closing (`MakeGenericType`) at `:264`, `:354` and `:425`.
  - The reader catches an exception from each reflective operation (method lookup, `GetCustomAttributes`, `GetDecoratorType`, `MakeGenericType`) and returns it as a failure **typed by the operation that failed** (method lookup, attribute read, decorator type, decorator closing), carrying the exception. It treats a null decorator type as a decorator-type failure, and it throws nothing itself. The typing lets the describer turn each kind into a description problem in its own task (T7.8–T7.11).
  - `PipelineBuilder` rethrows the original exception (`ExceptionDispatchInfo`), or raises today's `ConfigurationException` message, so its behaviour is unchanged. Where today's failure is a `NullReferenceException` (a null decorator type, or a sync handler with no `Execute`), the builder keeps it.
  - Keep the wrapping loop at `:110-122` as it is, so the highest `Step` stays next to the handler.
  - Guarded by T3.1, T3.2 and the existing suite. In particular:
    - `When_stream_handler_has_multiple_decorators_should_order_by_step_descending.cs`;
    - `When_sync_attribute_on_async_handler_should_throw_configuration_exception.cs`;
    - `When_stream_handler_has_mismatched_decorator_attribute_should_throw_ConfigurationException.cs`;
    - `When_decorator_factory_cannot_create_decorator_should_throw_ConfigurationException.cs`;
    - `PipelineBuilderExceptionTests.cs`;
    - the caching ordering test that uses `OrderingTestQueryHandlerAsync`.
  - Run the full suite before and after. No test changes.

- [ ] **STRUCTURAL: T3.4 — Move the wrong-kind checks and their messages into `PipelineAttributeReader` (ADR 0025 step 3)**
  - Move `ValidateNoMismatchedAttributes` and its call sites at `PipelineBuilder.cs:80-83`, `:145-148` and `:306-309` into the reader. The reader names the wrong kinds. The builder raises the same `ConfigurationException` with the same message.
  - Guarded by T3.2 and the tests listed in T3.3. Run the full suite before and after.

## Phase 4 — DI builder exposes the registries (ADR 0025 step 4)

- [ ] **STRUCTURAL: T4.1 — Give `ServiceCollectionDarkerHandlerBuilder` internal read access to its registries (ADR 0025 step 4)**
  - In `src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionDarkerHandlerBuilder.cs`, add internal read-only properties for the sync, async and stream handler registries. `AddDarker` creates them at `ServiceCollectionExtensions.cs:22-33`.
  - `IDarkerHandlerBuilder` gains no member (NFR opt-in and non-breaking). The suite is green before and after.

## Phase 5 — Registry inspection (ADR 0025 step 5)

- [ ] **TEST + IMPLEMENT: T5.1 — The sync registry lists its registrations in registration order (ADR 0025 step 5; FR6 input; role "Lists registrations")**
  - **USE COMMAND**: `/test-first when handlers are registered in the sync registry should list each query type, result type and handler type in registration order`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_handlers_are_registered_should_list_them_in_registration_order.cs`
  - Test should verify:
    - A `QueryHandlerRegistry` cast to `IAmAHandlerRegistryInspector` returns one `HandlerRegistration` per `Register` call (generic and `Register(Type, Type, Type)` forms), in call order. Each carries the query type, the result type and a single candidate handler type.
    - Registrations made by `RegisterFromAssemblies` over the `Exported/` handlers are listed too.
    - `Register(typeof(Q), typeof(R), null)` is listed with a null candidate. `HandlerRegistration` does not validate its arguments.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the public `src/Paramore.Darker/IAmAHandlerRegistryInspector.cs` (`GetRegistrations()`) and `HandlerRegistration.cs` (a public record with an unvalidated public constructor). Neither is in the `PipelineValidation` namespace, per ADR 0025 "Where each type is touched".
    - In `QueryHandlerRegistry.cs`, record inside the virtual `Register(Type, Type, Type)` overload, so that DI subclasses that call `base.Register` inherit it (`ServiceCollectionHandlerRegistry.cs:18-22`).
    - Leave `IQueryHandlerRegistry` unchanged.

- [ ] **TEST + IMPLEMENT: T5.2 — A router registration lists every candidate in order (ADR 0025 step 5; Invariant "one description per candidate")**
  - **USE COMMAND**: `/test-first when a query is registered with a routing function and candidates should list one registration carrying every candidate in order`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_router_is_registered_should_list_its_candidates_in_order.cs`
  - Test should verify:
    - `Register<TQuery, TResult>(router, typeof(A), typeof(B))` yields one `HandlerRegistration` whose candidates are `[A, B]`.
    - A router registered with no candidates yields one registration with an empty candidate list.
    - Fixed and routed registrations keep their relative order.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Record in the router `Register<TQuery, TResult>` overload of `QueryHandlerRegistry.cs`.

- [ ] **TEST + IMPLEMENT: T5.3 — The async and stream registries list their registrations, fixed and routed, in order (ADR 0025 step 5)**
  - **USE COMMAND**: `/test-first when handlers are registered in the async and stream registries should list them in registration order with their candidates`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_async_and_stream_handlers_are_registered_should_list_them_in_registration_order.cs`
  - Test should verify:
    - `QueryHandlerRegistryAsync` and `StreamQueryHandlerRegistry`, as `IAmAHandlerRegistryInspector`, list fixed and router registrations in call order, with candidates in order.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Implement the inspector in `QueryHandlerRegistryAsync.cs` and `StreamQueryHandlerRegistry.cs`, recording in the base `Register` overloads.

## Phase 6 — Result, exception and validator skeleton (ADR 0025 step 6)

> The `PipelineValidator` constructor grows across Phases 6, 8 and 9 as each parameter gains a test. Here it takes the sync, async and stream registries, each of which may be null. T8.1 adds the optional probe and T9.1 the ordering-rule collection, ending in ADR 0025's order (registries, rules, optional probe).
>
> **Every Phase 6–9 test builds its validator through the test-data builder `PipelineValidatorBuilder`**, which T6.1 adds in `test/Paramore.Darker.Core.Tests/TestDoubles/`. The builder is the only place that changes when the constructor grows: T8.1 adds `WithProbe` and T9.1 adds `WithOrderingRules`, and each updates `Build()`. No earlier test file is edited when the constructor changes. The one exception is T9.5, which tests the constructor's own guard and so calls it directly.

- [ ] **TEST + IMPLEMENT: T6.1 — A clean configuration of public handlers yields no errors (ADR 0025 step 6; FR3, FR5; acceptance criterion "starts cleanly")**
  - **USE COMMAND**: `/test-first when the registries hold only correctly configured public handlers should return a valid result with no errors`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_validating_a_clean_configuration_should_report_no_errors.cs`
  - Test should verify:
    - The registries hold the `Exported/` public handlers (`TestQueryHandler`, `TestQueryHandlerAsync`, `ExportedStreamQueryHandler`). A validator built with `new PipelineValidatorBuilder().WithSyncRegistry(sync).WithAsyncRegistry(async).WithStreamRegistry(stream).Build()` returns from `Validate()` a result with `IsValid` true and `Errors` empty. Assert errors only; from T8.3 a probe-less validator also warns.
    - Null registries (the builder's default) are accepted and validated as empty.
    - `Validate()` does not throw.
    - The test adds `PipelineValidatorBuilder` (public, in `test/Paramore.Darker.Core.Tests/TestDoubles/`), a fluent test-data builder with `WithSyncRegistry`, `WithAsyncRegistry`, `WithStreamRegistry` and `Build()`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add, in `src/Paramore.Darker/PipelineValidation/`, the public `IAmAPipelineValidator`, `PipelineValidator` (taking the three nullable registries) and `PipelineValidationResult` (with `Errors`, `Warnings` and `IsValid`). Only what this test needs: `Validate()` returns an empty result.
    - The description types and `PipelineDescriber` arrive with the first task whose test needs them (T7.1, and T7.13 for `PipelineStepDescription`).

- [ ] **TEST + IMPLEMENT: T6.2 — `PipelineValidationException` lists the error count and every finding, errors first, as `[rule] source: message` (ADR 0025 step 6; FR4 message; `ThrowIfInvalid`)**
  - **USE COMMAND**: `/test-first when a result with errors and warnings is thrown via ThrowIfInvalid should raise PipelineValidationException listing the error count and every finding errors first`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_result_with_errors_is_thrown_should_list_every_finding_in_the_exception_message.cs`
  - Test should verify:
    - A `PipelineValidationResult` built from two errors and one warning, each a `ValidationError` with a `RuleId`, throws `PipelineValidationException` from `ThrowIfInvalid()`.
    - The message states the error count, then one line per finding as `[rule] source: message`, errors first, then warnings.
    - `exception.Result` is the same result.
    - A result with warnings only does not throw from `ThrowIfInvalid()`.
    - `PipelineValidationException` derives from `System.Exception` and is not a `ConfigurationException`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the sealed `PipelineValidationException : Exception`, with a `Result` property.
    - Add `ThrowIfInvalid()`, and a public constructor on `PipelineValidationResult` that takes errors and warnings.
    - `ConfigurationException` stays sealed and unchanged.

- [ ] **TEST + IMPLEMENT: T6.3 — `PipelineValidationResult.Combine` keeps the input order (ADR 0025 step 6; FR3, FR5; ADR 0026 ordering relies on it)**
  - **USE COMMAND**: `/test-first when results are combined should keep every error and warning in input order`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_results_are_combined_should_keep_findings_in_input_order.cs`
  - Test should verify:
    - `Combine(r1, r2)` holds r1's errors then r2's errors in `Errors`, and r1's warnings then r2's warnings in `Warnings`.
    - `IsValid` is false if any input has an error.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the static `PipelineValidationResult.Combine(params PipelineValidationResult[])`, or an `IEnumerable` overload.

- [ ] **TEST + IMPLEMENT: T6.4 — A registry whose inspector throws yields one `RuleEvaluationFailed` finding naming that registry, and the other registries are still validated (ADR 0025 step 6; FR3; Invariant "never throws out of Validate")**
  - **USE COMMAND**: `/test-first when a registry's inspector throws should report one RuleEvaluationFailed error naming that registry and still validate the remaining registries`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_registry_inspector_throws_should_report_rule_evaluation_failed_and_validate_the_others.cs`
  - Test should verify:
    - The sync and async registries are new doubles, `ThrowingInspectorQueryHandlerRegistry` and `ThrowingInspectorQueryHandlerRegistryAsync`, in `TestDoubles/`. They implement the registry interface and `IAmAHandlerRegistryInspector`, and their `GetRegistrations()` throws.
    - `Validate()` does not throw. It returns two `RuleEvaluationFailed` errors, the sync registry's first, each naming its registry type and carrying the exception message. The second error is the evidence that validation went on.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - In `PipelineValidator.Validate`, read each registry's `GetRegistrations()` inside one try/catch per registry. Emit one error with `RuleId = ValidationRuleIds.EvaluationFailed`, then continue with the next registry. From T7.1, describing the registry's registrations happens inside the same guard: the descriptions are materialised inside it, and the rules are evaluated over them afterwards, outside it. A registry that cannot be read or described yields one finding (ADR 0025 step 6).

## Phase 7 — Pipeline rules (ADR 0025 step 7)

> One rule per commit, in the ADR's order: `HandlerRegistered`, `HandlerPublic`, `HandlerMatchesPath`, `DecoratorAttributeKind`, `PipelineDescribable`, `DuplicateStep`. Add `QueryPipelineRules` in T7.1. It creates fresh specifications on every `Validate` call. Rule identifiers live in core's `PipelineValidation` namespace. Every rule except `HandlerRegistered` is satisfied by a description with no handler type (ADR 0025 catalogue note). Tests drive this rather than this note: T7.19 for the Phase 7 rules, T8.5 and T8.6 for the resolvability rules, and T9.6 for `DecoratorOrdering`. A rule written in T7.3–T7.13 needs no null-handler guard until T7.19 asks for one.

### 7A — HandlerRegistered rule (behaviour)

- [ ] **TEST + IMPLEMENT: T7.1 — A registration with a null handler type is an error naming the query and the registry path (ADR 0025 step 7; FR6; catalogue `HandlerRegistered`)**
  - **USE COMMAND**: `/test-first when a query is registered with a null handler type should report a HandlerRegistered error naming the query type and registry path`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_registration_has_no_handler_should_report_handler_registered.cs`
  - Test should verify:
    - `Register(typeof(Q), typeof(R), null)` on the sync registry yields one error with `RuleId` `HandlerRegistered` and severity Error. Its message names `Q` and the sync path.
    - The same on the async and stream registries names their paths.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the public `HandlerPipelineDescription` and `PipelinePath`, and the internal `PipelineDescriber` over `IAmAHandlerRegistryInspector` and `PipelineAttributeReader`, called inside T6.4's per-registry guard. The describer iterates the registration's candidates and yields one description each (a fixed registration has one); T7.2 adds the empty-list case, and T7.4 pins the fan-out.
    - Add the public `QueryPipelineRules`, which creates a fresh rule set for each `Validate`, and the `HandlerRegistered` specification.
    - Make the describer yield one description with no handler type for a null candidate.
    - Evaluate every rule against every description, and collect with `ValidationResultCollector<T>`. Evaluate the rules over the descriptions that T6.4's per-registry guard materialised; add no further try/catch around rule evaluation. A predicate that throws is already turned into a `RuleEvaluationFailed` Error, with its `RuleId` and the exception message, by `Specification<T>`'s catch paths (T2.1, T2.2), and every other rule's findings are kept (FR3).
    - **Constraint on error factories.** `Specification<T>` calls the error factory outside its try (Brighter's `Specification.cs:194-195`), so a factory that throws would escape `Validate`. Every error factory in `QueryPipelineRules` (and later in `ScopeConfigurationRules`) must be null-safe and must not throw: render `description.HandlerType?.Name` and the like, never dereference a value the predicate did not check.

- [ ] **TEST + IMPLEMENT: T7.2 — A router registration with no candidates is a `HandlerRegistered` error (ADR 0025 step 7; FR6; "Configuration mistakes" table, router row)**
  - **USE COMMAND**: `/test-first when a router is registered with no candidate handlers should report a HandlerRegistered error naming the query`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_router_has_no_candidates_should_report_handler_registered.cs`
  - Test should verify:
    - `Register<Q, R>((q, c) => null)` with no candidates yields one `HandlerRegistered` Error naming `Q`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Make the describer emit one description with no handler type for an empty candidate list.

### 7B — HandlerPublic rule (behaviour)

- [ ] **TEST + IMPLEMENT: T7.3 — An explicitly registered handler that is not public is an error, with advice to make it public (ADR 0025 step 7; FR6; catalogue `HandlerPublic`)**
  - **USE COMMAND**: `/test-first when an internal handler is registered explicitly should report a HandlerPublic error naming the handler and advising to make it public`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_an_internal_handler_is_registered_explicitly_should_report_handler_public.cs`
  - Test should verify:
    - An `internal` sync handler double registered explicitly yields a `HandlerPublic` Error. The message names the handler and says to make it public, because a handler is the public facade of a module.
    - A public handler nested inside an internal type also yields the error (`Type.IsVisible` is false).
    - A public `Exported/` handler yields no `HandlerPublic` finding.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the `HandlerPublic` specification, judged on `Type.IsVisible`.

- [ ] **CHARACTERISE: T7.4 — A router registration yields one description per candidate, so each internal candidate is reported, in candidate order (ADR 0025 step 6 router test, placed here; Invariant "one description per candidate")**
  - **USE COMMAND**: `/test-first when a router registration has two internal candidate handlers should report one HandlerPublic error per candidate in candidate order`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_router_has_two_internal_candidates_should_report_each_candidate.cs`
  - Test should verify:
    - A router with internal candidates `A` then `B` yields two `HandlerPublic` Errors, the first naming `A` and the second naming `B`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `PipelineDescriber`, describe only `registration.Candidates.First()`. The test must fail on the assertion that a second `HandlerPublic` Error names `B`.
  - Implementation should:
    - Need no production change. T7.1's describer yields one description per candidate, and T7.2 handles the empty list.

### 7C — HandlerMatchesPath rule (behaviour)

- [ ] **TEST + IMPLEMENT: T7.5 — A handler in the registry of the wrong path is an error naming the handler, the query and the expected interface (ADR 0025 step 7; FR8; catalogue `HandlerMatchesPath`)**
  - **USE COMMAND**: `/test-first when a sync-only handler is registered in the async registry should report a HandlerMatchesPath error naming the handler, query and IQueryHandlerAsync`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_sync_only_handler_is_in_the_async_registry_should_report_handler_matches_path.cs`
  - Test should verify:
    - A sync-only handler registered through `QueryHandlerRegistryAsync.Register(Type, Type, Type)` yields a `HandlerMatchesPath` Error naming the handler, the query and `IQueryHandlerAsync<,>`.
    - An async-only handler in the sync registry names `IQueryHandler<,>`.
    - A non-stream handler in the stream registry names `IStreamQueryHandler<,>`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the `HandlerMatchesPath` specification. Read the path's interface from the description's `PipelinePath`.

- [ ] **TEST + IMPLEMENT: T7.6 — A handler that implements the path's interface explicitly is an error that advises implementing the method publicly (ADR 0025 step 7; FR8)**
  - **USE COMMAND**: `/test-first when a handler implements ExecuteAsync only as an explicit interface member should report a HandlerMatchesPath error advising a public implementation`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_handler_implements_the_interface_explicitly_should_report_handler_matches_path_with_advice.cs`
  - Test should verify:
    - A handler double with `Task<R> IQueryHandlerAsync<Q, R>.ExecuteAsync(...)` implemented explicitly yields a `HandlerMatchesPath` Error. The message advises implementing the method publicly.
    - Assert nothing about `PipelineDescribable`, which arrives in T7.8. T7.12 pins that an absent path method yields no `PipelineDescribable` finding.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Report an absent public method for the path from `HandlerMatchesPath`. The describer records "no method" without making it a description problem.

### 7D — DecoratorAttributeKind rule (behaviour)

- [ ] **TEST + IMPLEMENT: T7.7 — The path's method carrying another path's attributes is an error naming the handler, the attribute and the correct kind (ADR 0025 step 7; FR8; catalogue `DecoratorAttributeKind`)**
  - **USE COMMAND**: `/test-first when a handler's path method carries attributes of another path's kind should report a DecoratorAttributeKind error naming the handler, attribute and correct kind`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_sync_attribute_is_on_execute_async_should_report_decorator_attribute_kind.cs`
  - Test should verify:
    - `AsyncHandlerWithSyncAttribute` (existing double) in the async registry yields a `DecoratorAttributeKind` Error naming the handler, the attribute type and `QueryHandlerAttributeAsync` as the correct kind.
    - An async attribute on `Execute`, and a stream attribute on `Execute` or `ExecuteAsync` (reuse `AsyncHandlerWithStreamAttribute`), each yield the error.
    - Stream path: a sync attribute (reuse `StreamHandlerWithSyncAttribute`) or an async attribute (reuse `StreamHandlerWithAsyncAttribute`) on a stream handler's `ExecuteAsync`, in the stream registry, each yield the error naming `StreamQueryHandlerAttribute` as the correct kind. These are the builder's checks at `PipelineBuilder.cs:306-309`.
    - `Validate()` does not throw. The builder would.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Record the wrong-kind attributes that `PipelineAttributeReader` names (T3.4) as the description's misplaced attributes. Add the `DecoratorAttributeKind` specification.

### 7E — PipelineDescribable rule (behaviour)

- [ ] **TEST + IMPLEMENT: T7.8 — An ambiguous (overloaded) Execute method is a `PipelineDescribable` error, not an exception (ADR 0025 step 7; FR3, FR8; catalogue `PipelineDescribable`)**
  - **USE COMMAND**: `/test-first when an async handler has overloaded ExecuteAsync methods should report a PipelineDescribable error naming the handler and method and not throw`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_handler_has_overloaded_execute_async_should_report_pipeline_describable.cs`
  - Test should verify:
    - A handler double with two public `ExecuteAsync` overloads yields a `PipelineDescribable` Error naming the handler and `ExecuteAsync`, and carrying the `AmbiguousMatchException` message.
    - `Validate()` does not throw.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Turn only the reader's method-lookup failure into a description problem. Add the `PipelineDescribable` specification. The attribute-read, decorator-type and decorator-closing failures stay unhandled here; T7.9, T7.10 and T7.11 add them, so each is RED.

- [ ] **TEST + IMPLEMENT: T7.9 — An attribute on the path's method that cannot be read is a `PipelineDescribable` error (ADR 0025 step 7; FR3)**
  - **USE COMMAND**: `/test-first when a decorator attribute's constructor throws should report a PipelineDescribable error carrying the exception message and not throw`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_decorator_attribute_cannot_be_read_should_report_pipeline_describable.cs`
  - Test should verify:
    - A handler whose method carries `ThrowingConstructorAttribute` (new) yields a `PipelineDescribable` Error naming the handler and the method, with the exception message.
    - `Validate()` does not throw.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Turn the reader's `GetCustomAttributes` failure into a description problem.

- [ ] **TEST + IMPLEMENT: T7.10 — A decorator type that is null, or whose `GetDecoratorType` throws, is a `PipelineDescribable` error (ADR 0025 step 7; FR3)**
  - **USE COMMAND**: `/test-first when a decorator attribute's GetDecoratorType throws or returns null should report a PipelineDescribable error naming the attribute and not throw`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_decorator_type_is_null_or_throws_should_report_pipeline_describable.cs`
  - Test should verify:
    - `NullDecoratorTypeAttribute` (new) yields a `PipelineDescribable` Error naming the attribute.
    - `ThrowingDecoratorTypeAttribute` (new) yields one with the exception message.
    - `Validate()` does not throw.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Turn the reader's null-type and `GetDecoratorType` failures into description problems.

- [ ] **TEST + IMPLEMENT: T7.11 — A decorator type that cannot be closed over `IQuery<TResult>`/`IStreamQuery<TResult>` and `TResult` is a `PipelineDescribable` error (ADR 0025 step 7; FR3)**
  - **USE COMMAND**: `/test-first when a decorator attribute names a decorator type that cannot be closed over the query and result should report a PipelineDescribable error and not throw`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_decorator_type_cannot_be_closed_should_report_pipeline_describable.cs`
  - Test should verify:
    - An attribute naming a decorator with the wrong generic arity, or constraints that `TResult` violates (new double), yields a `PipelineDescribable` Error carrying the `MakeGenericType` exception message.
    - The same on the stream path, closing over `IStreamQuery<TResult>`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Turn the reader's `MakeGenericType` failure into a description problem.

- [ ] **CHARACTERISE: T7.12 — A sync-only handler in the async registry yields no `PipelineDescribable` finding (ADR 0025 step 7, the ADR's further `PipelineDescribable` test)**
  - **USE COMMAND**: `/test-first when a sync-only handler is in the async registry should report HandlerMatchesPath and no PipelineDescribable finding`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_sync_only_handler_is_in_the_async_registry_should_report_no_pipeline_describable.cs`
  - Test should verify:
    - No finding with `RuleId` `PipelineDescribable` is reported, and a `HandlerMatchesPath` finding is.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `PipelineDescriber`, record an absent path method as a description problem. The test must fail on the "no `PipelineDescribable`" assertion.
  - Implementation should:
    - Need no production change. T7.6 and T7.8 deliver this.

### 7F — DuplicateStep rule (behaviour)

- [ ] **TEST + IMPLEMENT: T7.13 — Two attributes of the path's kind with the same Step are a warning naming the handler, the step and both attributes (ADR 0025 step 7; FR9; catalogue `DuplicateStep`)**
  - **USE COMMAND**: `/test-first when two decorator attributes on one handler method share a Step should report a DuplicateStep warning naming the handler, the step and both attributes`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_two_decorator_attributes_share_a_step_should_report_duplicate_step.cs`
  - Test should verify:
    - A sync handler with `[QueryLogging(2)]` and `[RetryableQuery(2)]`, or doubles, yields one `DuplicateStep` Warning naming the handler, step 2 and both attribute types.
    - The same on the async and stream paths.
    - Distinct steps yield no `DuplicateStep`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the public `PipelineStepDescription`, and make the describer record the path's decorator steps in the builder's order, one step for each decorator the reader closed. A decorator that yields a problem adds no step (ADR 0025 Invariant; T7.14 pins it). Add the `DuplicateStep` specification over the description's ordered `PipelineStepDescription`s.

- [ ] **CHARACTERISE: T7.14 — A decorator that fails to describe takes no part in `DuplicateStep` (ADR 0025 Invariant "a decorator that yields a problem adds no step")**
  - **USE COMMAND**: `/test-first when a decorator that cannot be closed shares a Step with a valid decorator should report PipelineDescribable and no DuplicateStep`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_failed_decorator_shares_a_step_should_report_no_duplicate_step.cs`
  - Test should verify:
    - A handler with a valid attribute at Step 1 and an unclosable one at Step 1 yields a `PipelineDescribable` Error and no `DuplicateStep` Warning.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `PipelineDescriber`, add a `PipelineStepDescription` for a decorator whose closing failed. The test must fail on the "no `DuplicateStep`" assertion.
  - Implementation should:
    - Need no production change. T7.13 records a step only for a decorator the reader closed.

### 7G — Cross-cutting pipeline guarantees (behaviour and risk pins)

- [ ] **TEST + IMPLEMENT: T7.15 — A custom registry that is not an inspector yields a `RegistryNotInspectable` warning (ADR 0025 step 7; FR6; catalogue `RegistryNotInspectable`)**
  - **USE COMMAND**: `/test-first when a custom registry does not implement IAmAHandlerRegistryInspector should report a RegistryNotInspectable warning naming the registry type`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_registry_is_not_inspectable_should_report_registry_not_inspectable.cs`
  - Test should verify:
    - `NonInspectableQueryHandlerRegistry` (new; implements `IQueryHandlerRegistry` only), passed with `PipelineValidatorBuilder.WithSyncRegistry`, yields one `RegistryNotInspectable` Warning naming its type, and no error.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - In `PipelineValidator.Validate`, emit the warning when a non-null registry is not an `IAmAHandlerRegistryInspector`.

- [ ] **CHARACTERISE: T7.16 — Several mistakes appear together in one result (ADR 0025 step 7; FR3; acceptance criterion "several misconfigurations at once")**
  - **USE COMMAND**: `/test-first when the registries hold several different misconfigurations should report every one of them in a single result`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_several_pipelines_are_misconfigured_should_report_every_mistake_in_one_result.cs`
  - Test should verify:
    - One null-handler registration, one sync-only handler in the async registry, one wrong-kind attribute and one duplicate step. The result contains a finding for each of `HandlerRegistered`, `HandlerMatchesPath`, `DecoratorAttributeKind` and `DuplicateStep`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `PipelineValidator.Validate`, `break` out of the description loop after the first description that has a finding. The test must fail on the presence of the later rules' findings.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T7.17 — Two descriptions failing the same rule each yield a finding (ADR 0025 risk: `Specification<T>` is stateful)**
  - **USE COMMAND**: `/test-first when two handlers fail the same rule should report one finding for each handler`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_two_handlers_fail_the_same_rule_should_report_a_finding_for_each.cs`
  - Test should verify:
    - Two queries, each with a duplicate-step handler, yield two `DuplicateStep` Warnings, each naming its own handler.
    - Calling `Validate()` twice on the same validator returns equal findings both times.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: build the rule specifications once in the `PipelineValidator` constructor and run the collector once, after the description loop. The test must fail on the count of `DuplicateStep` findings (1 instead of 2).
  - Implementation should:
    - Need no production change. T7.1's `QueryPipelineRules` creates a fresh rule set for each `Validate` call, and the validator collects each description's findings before evaluating the next.

- [ ] **CHARACTERISE: T7.18 — Findings are deterministic: registry order, then registration order, then candidate order, then rule order (ADR 0025 step 7 determinism test; Invariant "order is deterministic")**
  - **USE COMMAND**: `/test-first when the same configuration is validated twice should produce identical findings ordered by registry, registration, candidate and rule`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_validating_the_same_configuration_twice_should_produce_identical_ordered_findings.cs`
  - Test should verify:
    - Mistakes registered in the sync, async and stream registries, more than one per registry, are reported in that registry order, then in registration order within each registry.
    - Two validators built over the same registries return equal error lists and equal warning lists.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `PipelineValidator.Validate`, visit the registries in the order stream, async, sync. The test must fail on the order assertion.
  - Implementation should:
    - Need no production change.

- [ ] **TEST + IMPLEMENT: T7.19 — A description with no handler type yields only the `HandlerRegistered` error from the Phase 7 rules (ADR 0025 step 7; catalogue note "every rule except HandlerRegistered is satisfied by a description that has no handler type")**
  - **USE COMMAND**: `/test-first when a registration has no handler type should report only the HandlerRegistered error`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_description_has_no_handler_type_should_report_only_handler_registered.cs`
  - Test should verify:
    - One validator per path, built through `PipelineValidatorBuilder` with only that path's registry, after `Register(typeof(Q), typeof(R), null)` on it (three facts: sync, async, stream). For each, `Errors` contains exactly one finding, and its `RuleId` is `HandlerRegistered`. This exact-count assertion is a deliberate exception to "never on total counts": the setup holds no handler type, so `HandlerPublic` cannot fire, and the single error is the behaviour under test. No `RuleEvaluationFailed`, `HandlerPublic`, `HandlerMatchesPath`, `DecoratorAttributeKind`, `PipelineDescribable` or `DuplicateStep` finding appears. (Errors only: Phase 7 has no probe-less warning yet, and the convention is to assert on errors.)
    - `Validate()` does not throw.
    - It is RED until the guards land: T7.3–T7.13 assert only their own rule. A minimal predicate that dereferences the null handler type is caught by T2.1's catch path and yields a `RuleEvaluationFailed` Error; one that returns false yields a bogus finding of its own rule; and an error factory that dereferences the null handler type makes `Validate()` throw. Each fails an assertion of this test.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Make each Phase 7 rule other than `HandlerRegistered` satisfied when the description has no handler type, where it is not already: `HandlerPublic`, `HandlerMatchesPath`, `DecoratorAttributeKind`, `PipelineDescribable` and `DuplicateStep`. A rule over an empty list (no misplaced attributes, no problems, no steps) needs no guard; add one only where the test shows a finding. Keep every error factory null-safe (T7.1's constraint).
    - The resolvability rules and `DecoratorOrdering` do not exist yet. T8.5, T8.6 and T9.6 pin them.

## Phase 8 — Resolvability probe and rules (ADR 0025 step 8)

- [ ] **TEST + IMPLEMENT: T8.1 — A handler type that the probe says is not registered is a `HandlerResolvable` error (ADR 0025 step 8; FR6; catalogue `HandlerResolvable`)**
  - **USE COMMAND**: `/test-first when the resolvability probe says a handler type is not registered should report a HandlerResolvable error naming the handler and advising to register it`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_the_probe_says_a_handler_is_not_registered_should_report_handler_resolvable.cs`
  - Test should verify:
    - A `SimpleResolvabilityProbe` (new, in `TestDoubles/`, wraps a `Func<Type, bool>`) returns false for the `Exported/` `TestQueryHandler`. The validator is built with `PipelineValidatorBuilder.WithProbe(probe)`. The result has a `HandlerResolvable` Error naming the handler, with the advice to register it.
    - A probe that returns true yields no `HandlerResolvable`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the public `IAmAComponentResolvabilityProbe` (`bool Resolves(Type)`) and an optional probe parameter, last, on the `PipelineValidator` constructor. Add the `HandlerResolvable` rule to `QueryPipelineRules` as a **predicate-form** specification (`Specification<T>(predicate, errorFactory)`).
    - Add the rule only when a probe was supplied. A validator without a probe (every earlier test, built through `PipelineValidatorBuilder` with no probe) evaluates no resolvability rule, so T6.1 and T7.19 stay green. T8.3 adds only the `ResolvabilityNotChecked` warning to that case.
    - Add `WithProbe(IAmAComponentResolvabilityProbe)` to `PipelineValidatorBuilder` (default: no probe) and pass it in `Build()`. The builder is the only call site that changes; no earlier test file is edited.

- [ ] **TEST + IMPLEMENT: T8.2 — A closed decorator type that the probe says is not registered is a `DecoratorResolvable` error (ADR 0025 step 8; FR7; catalogue `DecoratorResolvable`)**
  - **USE COMMAND**: `/test-first when the resolvability probe says a closed decorator type is not registered should report a DecoratorResolvable error naming the decorator and handler`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_the_probe_says_a_decorator_is_not_registered_should_report_decorator_resolvable.cs`
  - Test should verify:
    - The probe records each type it is asked about. For a sync handler with `[RetryableQuery(1)]`, it is asked about `RetryableQueryDecorator<IQuery<R>, R>`, closed over `IQuery<R>` and not the concrete query type.
    - Returning false yields a `DecoratorResolvable` Error naming the decorator and the handler, with the advice to register it.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the `DecoratorResolvable` rule over the description's closed step types, as a **predicate-form** specification, added only when a probe was supplied (as T8.1).

- [ ] **TEST + IMPLEMENT: T8.3 — A validator without a probe reports one `ResolvabilityNotChecked` warning, ahead of every registry, and no resolvability error (ADR 0025 step 8; Invariant "without a probe, validation says so")**
  - **USE COMMAND**: `/test-first when PipelineValidator is built without a probe should report one ResolvabilityNotChecked warning first and no HandlerResolvable or DecoratorResolvable finding`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_validating_without_a_probe_should_report_resolvability_not_checked.cs`
  - Test should verify:
    - `Warnings[0]` has `RuleId` `ResolvabilityNotChecked`, with the reason and the advice to supply a probe. It appears once, even with three registries.
    - No `HandlerResolvable` or `DecoratorResolvable` finding appears. `IsValid` stays true for the clean `Exported/` configuration.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - When the probe is null, emit the `ResolvabilityNotChecked` warning once, before the registries. T8.1 and T8.2 already skip both resolvability rules in that case.

- [ ] **CHARACTERISE: T8.4 — A probe that throws yields a `RuleEvaluationFailed` error through `PipelineValidator` (ADR 0025 step 8; FR3)**
  - **USE COMMAND**: `/test-first when the resolvability probe throws should report a RuleEvaluationFailed error with rule identifier RuleEvaluationFailed and not throw`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_the_probe_throws_should_report_rule_evaluation_failed.cs`
  - Test should verify:
    - A `SimpleResolvabilityProbe` whose delegate throws yields an Error with `RuleId` `new ValidationRuleId("RuleEvaluationFailed")`, naming the entity and carrying the exception message. `Validate()` does not throw.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in the `HandlerResolvable` and `DecoratorResolvable` predicates, wrap the probe call in `try { … } catch { return true; }`. The test must fail on the presence of the `RuleEvaluationFailed` error.
  - Implementation should:
    - Need no production change. T8.1 and T8.2 build both rules as predicate-form specifications, so T2.1's predicate catch path (`EvaluateSimple`) delivers this.

- [ ] **TEST + IMPLEMENT: T8.5 — A description with no handler type yields no `HandlerResolvable` finding, and the probe is never asked about it (ADR 0025 step 8; catalogue note "every rule except HandlerRegistered is satisfied by a description that has no handler type")**
  - **USE COMMAND**: `/test-first when a registration has no handler type and the probe answers false should report no HandlerResolvable finding and never ask the probe about a null type`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_description_has_no_handler_type_should_report_no_handler_resolvable.cs`
  - Test should verify:
    - `Register(typeof(Q), typeof(R), null)` on the sync registry, and a `SimpleResolvabilityProbe` that records every type it is asked about and returns false, passed with `PipelineValidatorBuilder.WithProbe(probe)`.
    - `Validate()` does not throw. No finding with `RuleId` `HandlerResolvable` or `RuleEvaluationFailed` is reported, and the probe's recorded types contain no null. The `HandlerRegistered` Error is still reported.
    - It is RED against T8.1's rule, which asks `probe.Resolves(description.HandlerType)` with a null type and gets false. With a null-safe error factory (T7.1's constraint) a bogus `HandlerResolvable` Error appears; with a factory that dereferences the null handler type, `Validate()` throws. Either way an assertion of this test fails, and the probe-records-null assertion fails in both cases.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Make `HandlerResolvable` satisfied, without calling the probe, when the description has no handler type.

- [ ] **CHARACTERISE: T8.6 — A description with no handler type yields no `DecoratorResolvable` finding (ADR 0025 step 8; catalogue note "every rule except HandlerRegistered is satisfied by a description that has no handler type")**
  - **USE COMMAND**: `/test-first when a registration has no handler type and the probe answers false should report no DecoratorResolvable finding`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_description_has_no_handler_type_should_report_no_decorator_resolvable.cs`
  - Test should verify:
    - `Register(typeof(Q), typeof(R), null)` on the sync registry, and a `SimpleResolvabilityProbe` that returns false for every type, passed with `PipelineValidatorBuilder.WithProbe(probe)`.
    - `Validate()` does not throw. No finding with `RuleId` `DecoratorResolvable` is reported, and no `RuleEvaluationFailed` finding is reported.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in the `DecoratorResolvable` predicate, return false when the description has no handler type (`description.HandlerType == null ? false : …`). The test must fail on one of its own assertions: on "no `DecoratorResolvable`" if the rule's error factory renders the null handler safely, as T7.1 requires, or on "`Validate()` does not throw" if it dereferences it, because `EvaluateSimple` calls the error factory outside its try (Brighter's `Specification.cs:194-195`).
  - Implementation should:
    - Need no production change. A description with no handler type has no steps (T7.1, T7.13), so T8.2's rule has nothing to ask the probe about.

## Phase 9 — Decorator ordering table (ADR 0025 step 9)

- [ ] **TEST + IMPLEMENT: T9.1 — A Fallback inside a Retry on the sync path is a `DecoratorOrdering` warning naming the handler, both decorators and the reason (ADR 0025 step 9; FR9; catalogue `DecoratorOrdering`)**
  - **USE COMMAND**: `/test-first when a sync handler's FallbackPolicy sits inside its RetryableQuery should report a DecoratorOrdering warning naming the handler, both decorators and the reason`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_fallback_sits_inside_a_retry_should_report_decorator_ordering.cs`
  - Test should verify:
    - A sync handler with `[RetryableQuery(1)]` (outer) and `[FallbackPolicy(2)]` (inner) yields one `DecoratorOrdering` Warning. It names the handler, `FallbackPolicyDecorator`, `RetryableQueryDecorator` and the reason (the inner Fallback catches the failure, so the Retry never retries).
    - The validator is built through `PipelineValidatorBuilder`, whose ordering rules default to `DecoratorOrderingRules.Default`. The Warning's `RuleId` equals `new ValidationRuleId("DecoratorOrdering")`.
    - Entry identifier: a validator built with `WithOrderingRules` holding only `new DecoratorOrderingRule(typeof(FallbackPolicyDecorator<,>), typeof(RetryableQueryDecorator<,>), new ValidationRuleId("CustomOrdering"), "reason")` reports the same violation with `RuleId` equal to `new ValidationRuleId("CustomOrdering")`. This fact makes a hard-coded `DecoratorOrdering` identifier fail.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the public `DecoratorOrderingRule` (outer type, inner type, rule identifier, reason) and `DecoratorOrderingRules.Default`, with the sync pair `FallbackPolicyDecorator<,>` outer and `RetryableQueryDecorator<,>` inner. A finding's `RuleId` is the violated table entry's rule identifier; every `Default` entry uses `new ValidationRuleId("DecoratorOrdering")`, the catalogue identifier.
    - Add the `IEnumerable<DecoratorOrderingRule>` constructor parameter, after the registries and before the optional probe (ADR 0025's order), and the `DecoratorOrdering` rule. `QueryPipelineRules` creates one **predicate-form** `DecoratorOrdering` specification per `DecoratorOrderingRule` in the supplied table, so each violated table entry yields its own finding (T9.4 relies on this). Each compares positions in the description's step order, which the builder's reader produces.
    - Add `WithOrderingRules(IEnumerable<DecoratorOrderingRule>)` to `PipelineValidatorBuilder`, defaulting to `DecoratorOrderingRules.Default`, and pass it in `Build()`. The builder is the only call site that changes; no T6–T8 test file is edited.

- [ ] **TEST + IMPLEMENT: T9.2 — The same ordering rule holds on the async path (ADR 0025 step 9; FR9)**
  - **USE COMMAND**: `/test-first when an async handler's FallbackPolicyAsync sits inside its RetryableQueryAsync should report a DecoratorOrdering warning`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_an_async_fallback_sits_inside_an_async_retry_should_report_decorator_ordering.cs`
  - Test should verify:
    - The async pair yields one `DecoratorOrdering` Warning naming `FallbackPolicyDecoratorAsync` and `RetryableQueryDecoratorAsync`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the async pair to `DecoratorOrderingRules.Default`. Add no stream pair.

- [ ] **CHARACTERISE: T9.3 — Fallback outside Retry (lower Fallback Step) yields no ordering warning (ADR 0025 risk "users read lower step against stale documentation")**
  - **USE COMMAND**: `/test-first when FallbackPolicy has a lower Step than RetryableQuery should report no DecoratorOrdering finding`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_fallback_sits_outside_a_retry_should_report_no_decorator_ordering.cs`
  - Test should verify:
    - `[FallbackPolicy(1)]` with `[RetryableQuery(2)]`, on the sync and async paths, yields no `DecoratorOrdering` finding.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in the `DecoratorOrdering` rule, invert the position comparison (treat the higher `Step` as outer). The test must fail on the "no finding" assertion.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T9.4 — A caller-supplied ordering rule extends the table (ADR 0025 step 9; force "Extensibility")**
  - **USE COMMAND**: `/test-first when a caller passes Default plus its own DecoratorOrderingRule should report violations of both`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_caller_adds_an_ordering_rule_should_report_its_violation.cs`
  - Test should verify:
    - `PipelineValidatorBuilder.WithOrderingRules(DecoratorOrderingRules.Default.Append(new DecoratorOrderingRule(typeof(OuterDouble<,>), typeof(InnerDouble<,>), new ValidationRuleId("CustomOrdering"), "reason")))` with a handler that violates both the custom pair and Fallback/Retry yields two ordering Warnings, each naming its decorators and reason: one whose `RuleId` is `new ValidationRuleId("CustomOrdering")` (the custom pair) and one whose `RuleId` is `new ValidationRuleId("DecoratorOrdering")` (Fallback/Retry).
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in the `PipelineValidator` constructor, ignore the supplied table and use `DecoratorOrderingRules.Default`. The test must fail on the assertion that a Warning with `RuleId` `CustomOrdering` names the custom pair's decorators and reason.
  - Implementation should:
    - Need no production change. T9.1 evaluates every rule in the supplied table, and T9.2 already relies on that for the async pair.

- [ ] **TEST + IMPLEMENT: T9.5 — A null ordering-rule collection throws `ArgumentNullException` (ADR 0025 `PipelineValidator` constructor error condition)**
  - **USE COMMAND**: `/test-first when PipelineValidator is constructed with a null ordering rule collection should throw ArgumentNullException`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_constructing_a_validator_with_null_ordering_rules_should_throw.cs`
  - Test should verify:
    - The constructor, called directly (not through `PipelineValidatorBuilder`, because the guard is the constructor's own), throws `ArgumentNullException` naming the rules parameter.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Guard the parameter.

- [ ] **CHARACTERISE: T9.6 — A description with no handler type yields no `DecoratorOrdering` finding (ADR 0025 step 9; catalogue note "every rule except HandlerRegistered is satisfied by a description that has no handler type")**
  - **USE COMMAND**: `/test-first when a registration has no handler type should report no DecoratorOrdering finding`
  - Test location: "test/Paramore.Darker.Core.Tests/PipelineValidation/"
  - Test file: `When_a_description_has_no_handler_type_should_report_no_decorator_ordering.cs`
  - Test should verify:
    - `Register(typeof(Q), typeof(R), null)` on the sync and async registries, with a validator built through `PipelineValidatorBuilder` (so `DecoratorOrderingRules.Default` applies).
    - `Validate()` does not throw. No finding with `RuleId` `DecoratorOrdering` is reported, and no `RuleEvaluationFailed` finding is reported.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in each `DecoratorOrdering` predicate (one per table entry, T9.1), return false when the description has no handler type (`description.HandlerType == null ? false : …`). The test must fail on one of its own assertions: on "no `DecoratorOrdering`" if the rule's error factory renders the null handler safely, as T7.1 requires, or on "`Validate()` does not throw" if it dereferences it, because `EvaluateSimple` calls the error factory outside its try.
  - Implementation should:
    - Need no production change. A description with no handler type has no steps, so T9.1's rule finds no pair to compare.

## Phase 10 — Hosting package references (ADR 0025 step 10)

- [ ] **SETUP: T10.1 — Add `Microsoft.Extensions.Hosting.Abstractions` to the DI package and `Microsoft.Extensions.Hosting` to the Extensions test project (ADR 0025 step 10; FR2; NFR multi-target)**
  - `Directory.Packages.props`: add `<PackageVersion Include="Microsoft.Extensions.Hosting.Abstractions" Version="10.0.10" />` and `<PackageVersion Include="Microsoft.Extensions.Hosting" Version="10.0.10" />`, beside the other 10.0.10 `Microsoft.Extensions.*` entries.
  - `src/Paramore.Darker.Extensions.DependencyInjection/Paramore.Darker.Extensions.DependencyInjection.csproj`: add the abstractions `PackageReference`, with no version.
  - `test/Paramore.Darker.Extensions.Tests/Paramore.Darker.Extensions.Tests.csproj`: add the `Microsoft.Extensions.Hosting` `PackageReference`. It is a test-only reference.
  - Core (`src/Paramore.Darker/Paramore.Darker.csproj`) gains no reference (NFR core purity).
  - `dotnet build Darker.Filter.slnf -c Release` succeeds for netstandard2.0, net8.0 and net9.0. Confirm that `IHostedLifecycleService` resolves on netstandard2.0 (ADR 0025 Technology Choices). The suite is green.

## Phase 11 — DI wiring and hosted service (ADR 0025 step 11)

> Tests go in `test/Paramore.Darker.Extensions.Tests/PipelineValidation/`. Handler doubles are public classes in `test/Paramore.Darker.Extensions.Tests/TestDoubles/`, registered explicitly. Risk check before T11.1: confirm with `grep -rn "AddHandlersFromAssemblies(" test/Paramore.Darker.Extensions.Tests` that no call scans the Extensions.Tests assembly. Every current call targets Core.Tests types. If any call does, stop and ask.

### 11A — Opt-in registration (behaviour)

- [ ] **TEST + IMPLEMENT: T11.1 — `ValidatePipelines()` registers the validator, and without it nothing is registered (ADR 0025 step 11 bullet 1; FR1; NFR opt-in and non-breaking; acceptance criterion "an application that does not call it is unaffected")**
  - **USE COMMAND**: `/test-first when ValidatePipelines is called on the Darker builder should register an IAmAPipelineValidator, and when it is not called should register none`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_ValidatePipelines_is_called_should_register_the_pipeline_validator.cs`
  - Test should verify:
    - After `AddDarker().ValidatePipelines()`, `GetServices<IAmAPipelineValidator>().OfType<PipelineValidator>().Count()` is 1. (Not a total count: T14.1 adds a second, different validator.)
    - After `AddDarker()` alone, no `IAmAPipelineValidator` descriptor and no new `IHostedService` descriptor exist.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the public `ValidatePipelines(this IDarkerHandlerBuilder, bool throwOnError = true)` extension in the DI package.
    - It captures the registries through T4.1's internal properties, and registers `PipelineValidator` in a factory delegate (lazy, at resolve time), over those live registries and `DecoratorOrderingRules.Default`.

- [ ] **TEST + IMPLEMENT: T11.2 — `ValidatePipelines()` on a builder that `AddDarker` did not create throws `InvalidOperationException` (ADR 0025 `ValidatePipelines` error condition)**
  - **USE COMMAND**: `/test-first when ValidatePipelines is called on a builder not created by AddDarker should throw InvalidOperationException`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_ValidatePipelines_is_called_on_a_foreign_builder_should_throw_invalid_operation.cs`
  - Test should verify:
    - A `ForeignDarkerHandlerBuilder` (new; implements `IDarkerHandlerBuilder`) makes `ValidatePipelines()` throw `InvalidOperationException`. The message says the builder must come from `AddDarker`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Throw when the builder is not a `ServiceCollectionDarkerHandlerBuilder`.

- [ ] **TEST + IMPLEMENT: T11.3 — `ValidateAll` runs every registered validator, combines their results in registration order and never throws (ADR 0025 step 11 bullet "on-demand ValidateAll"; FR5)**
  - **USE COMMAND**: `/test-first when ValidateAll is called on the provider should return one combined result from every registered validator in registration order without throwing`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_ValidateAll_is_called_should_combine_every_validator_in_registration_order.cs`
  - Test should verify:
    - A public duplicate-step handler is registered explicitly with `AddDarker()`, then `ValidatePipelines()`. Then `AddSingleton<IAmAPipelineValidator>(new FixedResultValidator(…))` (new double) adds a known Error and a known Warning.
    - `provider.ValidateAll()` does not throw and has `IsValid` false. Its `Errors` contain the fixed Error. In `Warnings`, the pipeline validator's `DuplicateStep` Warning comes before the fixed Warning: both validators' findings share one list, so registration order is observable. Under a `ValidateAll` that reverses the validators, the order assertion fails.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the public `ValidateAll(this IServiceProvider)`. It calls `Validate` on each `GetServices<IAmAPipelineValidator>()` in order and returns `PipelineValidationResult.Combine(...)`.

### 11B — Container-backed resolvability (behaviour)

- [ ] **TEST + IMPLEMENT: T11.4 — An unregistered decorator is reported through a real container (ADR 0025 step 11 bullet 3; FR7; acceptance criterion for FR7)**
  - **USE COMMAND**: `/test-first when a public handler names a decorator that is not registered in the container should report a DecoratorResolvable error through ValidateAll`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_decorator_is_not_registered_in_the_container_should_report_decorator_resolvable.cs`
  - Test should verify:
    - A public handler double carries an attribute for a public decorator that no registration adds (new attribute and decorator doubles). After `ValidatePipelines()`, `ValidateAll()` includes a `DecoratorResolvable` Error naming the decorator and the handler.
    - No `ResolvabilityNotChecked` warning appears.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the internal `ServiceProviderResolvabilityProbe` over `IServiceProviderIsService`, resolved from the provider in the validator factory. Pass null when the container supplies no `IServiceProviderIsService`.

- [ ] **CHARACTERISE: T11.5 — A handler whose descriptor was removed is reported through a real container (ADR 0025 step 11 bullet 3; FR6; acceptance criterion for FR6)**
  - **USE COMMAND**: `/test-first when a registered handler's service descriptor is removed from the container should report a HandlerResolvable error through ValidateAll`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_handler_descriptor_is_removed_should_report_handler_resolvable.cs`
  - Test should verify:
    - After explicit registration, `services.RemoveAll<THandler>()` before the provider is built yields a `HandlerResolvable` Error naming the handler.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `ServiceProviderResolvabilityProbe.Resolves`, return true for any non-generic type (`!type.IsGenericType`). The test must fail on the assertion that a `HandlerResolvable` Error naming the handler is reported after `RemoveAll<THandler>()`.
  - Implementation should:
    - Need no production change. T8.1's `HandlerResolvable` rule and T11.4's container probe deliver this.

- [ ] **CHARACTERISE: T11.6 — An open-generic decorator registration counts as registered once closed (ADR 0025 step 11 bullet 3; `IServiceProviderIsService` open-generic choice)**
  - **USE COMMAND**: `/test-first when a decorator is registered as an open generic should report no DecoratorResolvable error for its closed type`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_decorator_is_registered_as_an_open_generic_should_report_no_decorator_resolvable.cs`
  - Test should verify:
    - A public handler with `[RetryableQuery(1)]`, after `AddDefaultPolicies()` (which registers the open generic), yields no `DecoratorResolvable` finding.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `ServiceProviderResolvabilityProbe.Resolves`, return false for any constructed generic type (`type.IsConstructedGenericType`). The test must fail on the "no `DecoratorResolvable`" assertion.
  - Implementation should:
    - Need no production change.

### 11C — Ordering table in the container (behaviour)

- [ ] **TEST + IMPLEMENT: T11.7 — An ordering rule contributed with `AddSingleton` accumulates beside the default table (ADR 0025 step 11 bullet 2; force "Extensibility")**
  - **USE COMMAND**: `/test-first when a package adds a DecoratorOrderingRule with AddSingleton should report its violation beside the default Fallback and Retry rule`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_package_adds_an_ordering_rule_should_accumulate_it_beside_the_default_table.cs`
  - Test should verify:
    - `services.AddSingleton(new DecoratorOrderingRule(typeof(OuterDouble<,>), typeof(InnerDouble<,>), new ValidationRuleId("CustomOrdering"), "reason"))`, plus a handler that violates it and Fallback/Retry, yields two ordering Warnings: one with `RuleId` `CustomOrdering` and one with `RuleId` `DecoratorOrdering`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Register each `DecoratorOrderingRules.Default` rule with `AddSingleton`, not `TryAddEnumerable`, which would drop them. Have the validator factory read `GetServices<DecoratorOrderingRule>()`.

- [ ] **TEST + IMPLEMENT: T11.8 — Calling `ValidatePipelines()` twice registers once, and yields a single `DecoratorOrdering` finding (ADR 0025 step 11 bullet 1; risk "duplicate registrations repeat findings")**
  - **USE COMMAND**: `/test-first when ValidatePipelines is called twice should register one pipeline validator and report a single DecoratorOrdering finding`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_ValidatePipelines_is_called_twice_should_register_once.cs`
  - Test should verify:
    - `GetServices<IAmAPipelineValidator>().OfType<PipelineValidator>()` has count 1. This survives ADR 0026's second validator.
    - A Fallback-inside-Retry handler yields exactly one `DecoratorOrdering` Warning.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add a once-only guard in `ValidatePipelines`: on the first call register an internal marker instance, and on later calls find it through its descriptor (`ImplementationInstance`). Register the validator and the default table only on the first call. (T11.10's options type may later take over as the marker.)

### 11D — Hosted service at start (behaviour)

- [ ] **TEST + IMPLEMENT: T11.9 — Validation runs in the host's starting phase, before another hosted service's `StartAsync` (ADR 0025 step 11 bullet 4; FR2)**
  - **USE COMMAND**: `/test-first when a host starts with ValidatePipelines should run validation in StartingAsync before another hosted service's StartAsync even if that service was registered first`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_the_host_starts_should_validate_before_any_hosted_service_starts.cs`
  - Test should verify:
    - A real host (`Host.CreateApplicationBuilder` or `HostBuilder`) registers `RecordingHostedService` (new) **before** `AddDarker(...).ValidatePipelines()`, and a `RecordingValidator` (new `IAmAPipelineValidator`) sharing one `StartupSequence` recorder.
    - After `StartAsync(TestContext.Current.CancellationToken)`, the sequence is: validation, then the other service's `StartAsync`.
    - The test is in the `DarkerHostBootstrap` collection. Stop the host at the end.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the internal `DarkerValidationHostedService : IHostedLifecycleService`. `StartingAsync` calls `ValidateAll` and returns a completed task. The other lifecycle methods return completed tasks.
    - Register it with `AddHostedService` inside the once-only branch.

- [ ] **TEST + IMPLEMENT: T11.10 — With `throwOnError` true, an error fails host start with `PipelineValidationException` listing every finding (ADR 0025 step 11 bullet 5; FR4; acceptance criterion "throwOnError = true fails start")**
  - **USE COMMAND**: `/test-first when validation finds an error and throwOnError is true should fail host start with PipelineValidationException listing every finding`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_validation_finds_an_error_and_throw_on_error_is_true_should_fail_host_start.cs`
  - Test should verify:
    - An unregistered decorator (as in T11.4) plus a duplicate step makes `host.StartAsync(...)` throw `PipelineValidationException`. The message lists the `DecoratorResolvable` and `DuplicateStep` findings.
    - In the `DarkerHostBootstrap` collection.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Register a `DarkerPipelineValidationOptions` holding `ThrowOnError`. In the hosted service, call `ThrowIfInvalid()` on the combined result when `ThrowOnError` is true.

- [ ] **TEST + IMPLEMENT: T11.11 — The last `ValidatePipelines()` call sets `throwOnError` (ADR 0025 Scope FR1, "the last call sets throwOnError")**
  - **USE COMMAND**: `/test-first when ValidatePipelines is called first with throwOnError false and then with true should fail host start on an error`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_ValidatePipelines_is_called_twice_should_take_throw_on_error_from_the_last_call.cs`
  - Test should verify:
    - `ValidatePipelines(false)` then `ValidatePipelines()`, with an error, makes host start throw.
    - The reverse order lets host start succeed.
    - In the `DarkerHostBootstrap` collection.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - On every call, update the options instance found through its descriptor's `ImplementationInstance`.

- [ ] **TEST + IMPLEMENT: T11.12 — With `throwOnError` false, each error is logged at Error and the host starts (ADR 0025 step 11 bullet 5; FR4)**
  - **USE COMMAND**: `/test-first when validation finds errors and throwOnError is false should log each error at Error level and let the host start`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_validation_finds_errors_and_throw_on_error_is_false_should_log_and_start.cs`
  - Test should verify:
    - Two errors yield two `LogLevel.Error` entries from a fresh `CapturingLoggerProvider`, each containing its rule and source. `host.StartAsync` completes.
    - In the `DarkerHostBootstrap` collection.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Inject `ILogger<DarkerValidationHostedService>`, and log each error at Error when not throwing.

- [ ] **TEST + IMPLEMENT: T11.13 — Warnings are logged at Warning and never block start, and a clean result logs only a Debug summary (ADR 0025 step 11 bullet 5; FR4; acceptance criterion "warnings alone never fail start")**
  - **USE COMMAND**: `/test-first when validation finds only warnings with throwOnError true should log each at Warning and start, and when clean should log nothing above Debug`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_validation_finds_only_warnings_should_log_them_and_start.cs`
  - Test should verify:
    - A duplicate-step handler with `throwOnError` true lets the host start, with one `LogLevel.Warning` entry naming `DuplicateStep`.
    - A clean configuration of public doubles logs no entry above Debug from the hosted service's category, and exactly one Debug summary.
    - In the `DarkerHostBootstrap` collection.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Always log warnings at Warning, and log one Debug summary when the combined result has no findings.

- [ ] **TEST + IMPLEMENT: T11.14 — A host that never calls the starting phase still validates, from `StartAsync` (ADR 0025 `DarkerValidationHostedService.StartAsync`; Technology Choices limit 3)**
  - **USE COMMAND**: `/test-first when only StartAsync is called on the registered hosted services should run validation`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_only_start_async_is_called_should_validate.cs`
  - Test should verify:
    - A `CountingValidator` (new `IAmAPipelineValidator`) is registered beside `ValidatePipelines()`, with `AddLogging()` so the hosted service's `ILogger` resolves. The test resolves `GetServices<IHostedService>()` from a built provider and calls only `StartAsync(TestContext.Current.CancellationToken)` on each, once. `CountingValidator` reports 1.
    - It is RED against T11.9, whose `StartAsync` returns a completed task without validating (count 0).
    - Throw path: with `ValidatePipelines()` (`throwOnError` true) and a `FixedResultValidator` (from T11.3) returning an Error, calling only `StartAsync` throws `PipelineValidationException` listing that finding (ADR 0025 `StartAsync` row, "As `StartingAsync`").
    - In the `DarkerHostBootstrap` collection (`[Collection("DarkerHostBootstrap")]`), because the provider has real logging. Do not dispose the provider.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Make `StartingAsync` set a flag when it has validated, and make `StartAsync` run the same validation as `StartingAsync` (`ValidateAll`, then throw or log exactly as `StartingAsync` does; this task's throw-path fact pins the throw, and the logging is shared code that T11.12 and T11.13 test through `StartingAsync`) only when that flag is not set. A real host therefore still validates once, in `StartingAsync`, so T11.9, T11.12 and T11.13 stay green.
    - Add no once-only guard against a second `StartAsync`; T11.15 drives it.

- [ ] **TEST + IMPLEMENT: T11.15 — Validation runs exactly once, when both `StartingAsync` and `StartAsync` run and when `StartAsync` is called twice (ADR 0025 step 11 bullet 4; Invariant "a once-only guard")**
  - **USE COMMAND**: `/test-first when StartAsync is called a second time, or the host runs both StartingAsync and StartAsync, should run validation exactly once`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_start_async_is_called_twice_should_validate_once.cs`
  - Test should verify:
    - The test resolves `GetServices<IHostedService>()` from a built provider (with `AddLogging()`) and calls only `StartAsync(TestContext.Current.CancellationToken)` on each, twice. `CountingValidator` (from T11.14) reports 1. This fact is the RED: after T11.14, `StartingAsync` never ran, so each `StartAsync` validates and the count is 2.
    - A real host that runs both phases leaves `CountingValidator` at 1. This fact is already green after T11.14's flag and pins the both-phases case under the new guard.
    - In the `DarkerHostBootstrap` collection. Stop the host at the end, and do not dispose the provider.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Replace T11.14's flag with a once-only guard in `DarkerValidationHostedService` (for example `Interlocked.Exchange` on an int), taken by whichever of `StartingAsync` and `StartAsync` runs first, so validation runs at most once per hosted-service instance.

- [ ] **CHARACTERISE: T11.16 — Under `ServicesStartConcurrently`, validation still precedes the other service's `StartAsync` (ADR 0025 step 11 bullet 4; FR2 "does not depend on registration order")**
  - **USE COMMAND**: `/test-first when the host starts services concurrently should still validate before another hosted service's StartAsync`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_services_start_concurrently_should_still_validate_before_any_hosted_service_starts.cs`
  - Test should verify:
    - The host is configured with `HostOptions.ServicesStartConcurrently = true`. A `GatedRecordingHostedService` (new) is registered **before** `AddDarker(...).ValidatePipelines()`, beside the `RecordingValidator` and `StartupSequence` from T11.9.
    - The gated service's `StartAsync` synchronously records its entry, and whether validation had already been recorded at that moment, then signals an `entered` `TaskCompletionSource` and awaits a `release` `TaskCompletionSource`. Awaiting the gate keeps its `StartAsync` incomplete, so the host takes its concurrent path.
    - The test starts the host without awaiting it, awaits `entered`, and asserts that validation was recorded before the gated service's `StartAsync` entered. Then it releases the gate and awaits the host start. The release sits in a `finally` block, so a failed assertion never leaves the host start hanging.
    - In the `DarkerHostBootstrap` collection. Stop the host at the end.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: make `StartingAsync` a no-op, so validation runs only from `StartAsync`. The test must fail on the assertion that validation was recorded before the gated service's `StartAsync` entered. The failure is deterministic: even under `ServicesStartConcurrently`, the host runs the synchronous part of each `StartAsync` serially in registration order (`Host.ForeachService`), so the gated service, registered first, enters and records before Darker's `StartAsync` can validate.
  - Implementation should:
    - Need no production change.

### 11E — Call order and side effects (risk pins)

- [ ] **CHARACTERISE: T11.17 — A handler added after `ValidatePipelines()` is still validated (ADR 0025 step 11 bullet 7; FR14, pipeline part)**
  - **USE COMMAND**: `/test-first when a handler is registered after ValidatePipelines should still be validated`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_handler_is_registered_after_ValidatePipelines_should_still_be_validated.cs`
  - Test should verify:
    - `var builder = AddDarker(); builder.ValidatePipelines();` then a duplicate-step public handler registered explicitly on the same builder. `ValidateAll()` reports its `DuplicateStep` Warning.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `ValidatePipelines`, construct `PipelineValidator` eagerly at call time and register the instance. In the `PipelineValidator` constructor, copy `GetRegistrations()` into a field. Together these snapshot the registrations at call time. The test must fail on the presence of the `DuplicateStep` Warning.
  - Implementation should:
    - Need no production change. The lazy factory from T11.1 delivers this.

- [ ] **CHARACTERISE: T11.18 — Validation creates no handler and no decorator (ADR 0025 step 11 bullet 8; NFR no side effects; acceptance criterion "creates no handler, decorator or scope")**
  - **USE COMMAND**: `/test-first when validation runs at host start should never construct a handler or a decorator`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_validation_runs_should_create_no_handler_or_decorator.cs`
  - Test should verify:
    - `ConstructionRecordingHandler` and `ConstructionRecordingDecorator` (new; constructors increment a static or injected counter) are registered. After the host starts and `ValidateAll()` is called, both counters are 0.
    - In the `DarkerHostBootstrap` collection.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `ServiceProviderResolvabilityProbe.Resolves`, answer with `provider.GetService(type) != null`. The test must fail on the handler-counter assertion.
  - Implementation should:
    - Need no production change.

## Phase 12 — Pipeline validation documentation and ADR 0025 checkpoint (ADR 0025 step 12)

- [ ] **DOC: T12.1 — README section on `ValidatePipelines()` (ADR 0025 step 12; FR13 pipeline part; FR14 pipeline part; acceptance criterion "README documents the call, the order, the rules")**
  - Add a `## Validating pipelines at startup` section to `README.md`, after "Usage with Microsoft.Extensions.DependencyInjection". It covers:
    - the opt-in call and `throwOnError`;
    - that it should be called last;
    - the start-phase guarantee ("before the server", not "before any query") and its three limits: hosted-service constructors, another service's `StartingAsync`, and older hosts that never call the starting phase;
    - the pipeline rule catalogue (identifier, severity, cause, fix), including `ResolvabilityNotChecked`, `RegistryNotInspectable` and `RuleEvaluationFailed`;
    - the position meaning of `Step`: the lowest `Step` is outermost;
    - `ValidateAll` for on-demand use, and that a single resolve of `IAmAPipelineValidator` sees one validator;
    - that a missing constructor dependency is out of scope, with a pointer to `ValidateOnBuild`.
  - Do not correct the "higher step is outermost" wording in ADR 0002, `CLAUDE.md`, the caching README or `.agent_instructions/skills_overview.md`. ADR 0025 leaves that to a separate change.
  - Leave the lifetime rules to T19.1.

- [ ] **DOC: T12.2 — XML documentation on every new public type of ADR 0025 (house standard, `.agent_instructions/documentation.md`)**
  - Document the Specification types, `ValidationRuleId`/`ValidationRuleIds`, `IAmAHandlerRegistryInspector`/`HandlerRegistration`, every public `PipelineValidation` type, `ValidatePipelines`, `ValidateAll` and `DarkerPipelineValidationOptions`.
  - Copied Brighter types keep their XML docs.
  - State on `IAmAComponentResolvabilityProbe` that it must never create the type. State on `ValidateAll` that it never throws for a finding.

- [ ] **VERIFY: T12.3 — ADR 0025 checkpoint**
  - Run `dotnet build Darker.Filter.slnf -c Release` with no new warnings, on netstandard2.0, net8.0 and net9.0.
  - Run `dotnet test Darker.Filter.slnf -c Release --no-build`, green on net8.0 and net9.0.
  - Confirm the existing suite passed unchanged, which covers the acceptance criterion "existing suite passes unchanged". `git diff` shows no edits to existing test files except where a task above names them.
  - Confirm that `src/Paramore.Darker` has no `using Microsoft.Extensions.` in any new file (NFR core purity).
  - Confirm that `IQueryHandlerRegistry`, `IQueryHandlerRegistryAsync`, `IStreamQueryHandlerRegistry` and `IDarkerHandlerBuilder` gained no members (NFR non-breaking).
  - Confirm that `PipelineBuilder`'s per-query path did not gain validation work (NFR startup cost).

## Phase 13 — Builder carries the options (ADR 0026 step 1)

- [ ] **STRUCTURAL: T13.1 — Pass `DarkerOptions` to `ServiceCollectionDarkerHandlerBuilder` and expose it internally (ADR 0026 step 1; force "DarkerOptions is not in the container")**
  - Add a `DarkerOptions` parameter to the constructor at `ServiceCollectionDarkerHandlerBuilder.cs:17`, with an internal `Options` property.
  - Pass `options` from `AddDarker` at `ServiceCollectionExtensions.cs:33`.
  - No behaviour changes. Run the full suite before and after.

## Phase 14 — Lifetime snapshot and validator skeleton (ADR 0026 step 2)

- [ ] **TEST + IMPLEMENT: T14.1 — `ValidatePipelines()` registers two validators, the pipeline validator first (ADR 0026 step 2 bullet 1; FR1, FR5)**
  - **USE COMMAND**: `/test-first when ValidatePipelines is called should register the pipeline validator first and then the lifetime validator`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_ValidatePipelines_is_called_should_register_the_lifetime_validator_after_the_pipeline_validator.cs`
  - Test should verify:
    - Two `IAmAPipelineValidator` descriptors exist.
    - `GetServices<IAmAPipelineValidator>()` returns a `PipelineValidator` first, and a second validator that is not a `PipelineValidator`.
    - `ValidateAll()` on a clean configuration has no errors.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the internal `ServiceRegistrationSnapshot`, whose `Capture(IServiceCollection, DarkerOptions)` copies the list with `new List<ServiceDescriptor>(services)` and throws `ArgumentNullException` for either argument.
    - Add the internal `ScopeConfigurationValidator`, with an empty rule list and an `ArgumentNullException` guard on its snapshot.
    - In `ValidatePipelines`, capture on every call. Inside the once-only branch, register the snapshot instance, then `AddSingleton<IAmAPipelineValidator>(sp => new ScopeConfigurationValidator(snapshot))`, after the pipeline validator.
    - A later call finds the snapshot through its descriptor's `ImplementationInstance`.

- [ ] **CHARACTERISE: T14.2 — Calling `ValidatePipelines()` twice registers two validators in total, not three (ADR 0026 step 2 bullet 2; Invariant "the last call wins")**
  - **USE COMMAND**: `/test-first when ValidatePipelines is called twice should register exactly two validators`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_ValidatePipelines_is_called_twice_should_register_two_validators_in_total.cs`
  - Test should verify:
    - The `IAmAPipelineValidator` descriptor count is 2.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: move the `ScopeConfigurationValidator` registration outside the once-only branch. The test must fail on the count (3).
  - Implementation should:
    - Need no production change.

## Phase 15 — InertScopeAffinity rule (ADR 0026 step 3)

- [ ] **TEST + IMPLEMENT: T15.1 — `AddDarkerScopeProvider<T>(JoinAmbient)` with a Transient `HandlerLifetime` is an error naming `T` and `Transient` (ADR 0026 step 3 bullet 1; FR10; FR13 link)**
  - **USE COMMAND**: `/test-first when AddDarkerScopeProvider registers JoinAmbient and HandlerLifetime is Transient should report an InertScopeAffinity error naming the provider and Transient with the README link`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_join_ambient_is_registered_with_a_transient_handler_lifetime_should_report_inert_scope_affinity.cs`
  - Test should verify:
    - `AddDarkerScopeProvider<TestScopeProvider>()` (new public provider double) plus `AddDarker(o => o.HandlerLifetime = Transient).ValidatePipelines()` yields, through `ValidateAll()`, one Error with `RuleId` `InertScopeAffinity` and source `"Scope affinity"`.
    - The message names `TestScopeProvider`, `Transient`, the advice to set `ServiceLifetime.Scoped` and the advice to use `AlwaysNew`. It ends with `…README.md#inertscopeaffinity`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the internal `DescriptorRecord`, `ScopeConfiguration` and `ScopeConfigurationRules` (the rule identifiers, the README link base, and fresh specifications on each call). Their error factories are null-safe and never throw (T7.1's constraint).
    - `Capture` reads `ImplementationType`/`ImplementationInstance` only when `IsKeyedService` is false, and the keyed properties otherwise. The keyed properties throw on unkeyed descriptors in 10.0.10.
    - The effective affinity is the last unkeyed `ScopeAffinityRegistration` instance, otherwise `Options.DefaultScopeAffinity`, as at `ServiceCollectionExtensions.cs:79-81`.

- [ ] **TEST + IMPLEMENT: T15.2 — `DefaultScopeAffinity = JoinAmbient` with a Singleton `HandlerLifetime` and a provider added with `AddSingleton` is an error naming the option (ADR 0026 step 3 bullet 2; FR10)**
  - **USE COMMAND**: `/test-first when DefaultScopeAffinity is JoinAmbient and HandlerLifetime is Singleton with a directly registered provider should report an InertScopeAffinity error naming DarkerOptions.DefaultScopeAffinity`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_the_default_affinity_is_join_ambient_with_a_singleton_handler_lifetime_should_report_inert_scope_affinity.cs`
  - Test should verify:
    - The message names `DarkerOptions.DefaultScopeAffinity` and `Singleton`, and carries the link.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Render the option as the affinity's source when no registration exists. Read `HandlerLifetime` from `snapshot.Options` inside `Validate`, at host start.

- [ ] **CHARACTERISE: T15.3 — A registered `AlwaysNew` that overrides a `JoinAmbient` default yields nothing (ADR 0026 step 3 bullet 3; "Why the effective affinity")**
  - **USE COMMAND**: `/test-first when a registered AlwaysNew affinity overrides a JoinAmbient default should report no InertScopeAffinity finding`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_registered_always_new_overrides_a_join_ambient_default_should_report_nothing.cs`
  - Test should verify:
    - `DefaultScopeAffinity = JoinAmbient`, a Transient lifetime and `AddDarkerScopeProvider<P>(AlwaysNew)` yield no `InertScopeAffinity` finding.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in the rule, treat the affinity as `JoinAmbient` when either the registration or the option says so. The test must fail on the "no finding" assertion.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T15.4 — `JoinAmbient` with a Scoped `HandlerLifetime` and a registered provider yields nothing (ADR 0026 step 3 bullet 4; acceptance criterion "correctly configured application starts cleanly")**
  - **USE COMMAND**: `/test-first when JoinAmbient is registered with a Scoped handler lifetime and a scope provider should report no InertScopeAffinity finding`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_join_ambient_has_a_scoped_lifetime_and_a_provider_should_report_nothing.cs`
  - Test should verify:
    - No `InertScopeAffinity` finding is reported.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: make the rule fail whenever the effective affinity is `JoinAmbient`, ignoring the lifetime and the provider. The test must fail on the "no finding" assertion.
  - Implementation should:
    - Need no production change.

- [ ] **TEST + IMPLEMENT: T15.5 — `DefaultScopeAffinity = JoinAmbient` with a Scoped lifetime and no provider is an error that advises `AddDarkerScopeProvider` (ADR 0026 step 3 bullet 5; FR10 "no unkeyed provider")**
  - **USE COMMAND**: `/test-first when DefaultScopeAffinity is JoinAmbient with a Scoped handler lifetime and no scope provider should report an InertScopeAffinity error advising AddDarkerScopeProvider or AddDarkerRequestScope`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_the_default_affinity_is_join_ambient_without_a_provider_should_report_inert_scope_affinity.cs`
  - Test should verify:
    - The message names the option, the missing provider and the advice to call `AddDarkerScopeProvider` (or `AddDarkerRequestScope()` in ASP.NET Core), and carries the link.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the missing-provider cause, read from the unkeyed `IAmAScopeProvider` descriptors in the snapshot (`DescriptorsFor`, which excludes keyed descriptors, because Darker resolves the provider unkeyed). Add it to the same finding as the `HandlerLifetime` cause, not as a second finding, as the ADR 0026 catalogue says ("every cause that applies, in one finding"). T15.6 pins this.

- [ ] **CHARACTERISE: T15.6 — A Transient lifetime and no provider yield one finding that names both causes (ADR 0026 step 3 bullet 6; catalogue "every cause that applies, in one finding")**
  - **USE COMMAND**: `/test-first when JoinAmbient has both a Transient handler lifetime and no scope provider should report one InertScopeAffinity error naming both causes`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_join_ambient_has_both_causes_should_report_one_finding_naming_both.cs`
  - Test should verify:
    - Exactly one `InertScopeAffinity` Error, whose message contains both the `Transient` advice and the missing-provider advice.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in the `InertScopeAffinity` rule, return after the first failing cause (the `HandlerLifetime` cause), so the missing-provider cause is never added. The test must fail on the assertion that the single finding's message names both causes.
  - Implementation should:
    - Need no production change. T15.5 composes every applicable cause into one finding, as the ADR 0026 catalogue requires.

- [ ] **CHARACTERISE: T15.7 — A provider added with `AddSingleton` satisfies the rule, and a keyed-only provider does not (ADR 0026 step 3 bullet 7; risk "reading the wrong property family")**
  - **USE COMMAND**: `/test-first when the only scope provider is keyed should report InertScopeAffinity, and when an unkeyed provider is added with AddSingleton should report nothing`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_the_only_scope_provider_is_keyed_should_report_inert_scope_affinity.cs`
  - Test should verify:
    - `AddKeyedSingleton<IAmAScopeProvider, P>("k")` with `DefaultScopeAffinity = JoinAmbient` and a Scoped `HandlerLifetime` yields the Error. (The default affinity is `AlwaysNew`, `DarkerOptions.cs:17`, so the test sets it.) `ValidateAll()` does not throw `InvalidOperationException`.
    - `AddSingleton<IAmAScopeProvider, P>()` yields nothing.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: drop the `IsKeyedService` filter from `ServiceRegistrationSnapshot.DescriptorsFor`, so the keyed provider counts as registered. The keyed-only fact must fail on the assertion that the `InertScopeAffinity` Error is reported.
  - Implementation should:
    - Need no production change. T15.1's `Capture` checks `IsKeyedService` before reading a property family, and T15.5's `DescriptorsFor` returns unkeyed descriptors only.

- [ ] **CHARACTERISE: T15.8 — A host with `throwOnError` true fails start on an `InertScopeAffinity` error (ADR 0026 step 3 bullet 8; FR4, FR10)**
  - **USE COMMAND**: `/test-first when the host starts with an inert JoinAmbient configuration and throwOnError true should fail with PipelineValidationException listing the InertScopeAffinity finding`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_the_host_starts_with_an_inert_scope_affinity_should_fail_start.cs`
  - Test should verify:
    - `host.StartAsync(TestContext.Current.CancellationToken)` throws `PipelineValidationException`, whose message contains `[InertScopeAffinity]`.
    - In the `DarkerHostBootstrap` collection.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: register `ScopeConfigurationValidator` as its concrete type (`AddSingleton(sp => new ScopeConfigurationValidator(snapshot))`) and not as `IAmAPipelineValidator`. The test must fail on `Should.ThrowAsync<PipelineValidationException>`.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T15.9 — `AddDarkerRequestScope()` with a Transient `HandlerLifetime` is an `InertScopeAffinity` error (ADR 0026 step 3 bullet 9; FR10 "including via AddDarkerRequestScope()")**
  - **USE COMMAND**: `/test-first when AddDarkerRequestScope is registered before AddDarker with a Transient handler lifetime should report an InertScopeAffinity error naming HttpContextScopeProvider`
  - Test location: "test/Paramore.Darker.Extensions.AspNetCore.Tests/"
  - Test file: `When_the_request_scope_is_registered_with_a_transient_handler_lifetime_should_report_inert_scope_affinity.cs`
  - Test should verify:
    - `services.AddDarkerRequestScope()` before `AddDarker(o => o.HandlerLifetime = Transient).ValidatePipelines()`. `ValidateAll()` returns an `InertScopeAffinity` Error naming the provider in use and `Transient`.
    - In the AspNetCore project's `DarkerHostBootstrap` collection. The project already references Extensions.Tests.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: make the rule read the affinity only from `Options.DefaultScopeAffinity`, ignoring `ScopeAffinityRegistration`. The test must fail on the presence of the finding.
  - Implementation should:
    - Need no production change.

- [ ] **TEST + IMPLEMENT: T15.10 — When the scope provider in use is a factory registration, the `InertScopeAffinity` message renders it as "a factory registration at position N" (ADR 0026 catalogue, `InertScopeAffinity` "the scope provider in use … rendered as in DuplicateScopeProvider")**
  - **USE COMMAND**: `/test-first when the scope provider in use is registered by a factory and JoinAmbient is inert should report an InertScopeAffinity error rendering the provider as a factory registration at its position`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_the_scope_provider_in_use_is_a_factory_should_render_it_by_position_in_inert_scope_affinity.cs`
  - Test should verify:
    - `AddDarkerScopeProvider<TestScopeProvider>()` (which registers `JoinAmbient` and the provider type), then `services.AddSingleton<IAmAScopeProvider>(sp => new TestScopeProvider())`, then `AddDarker(o => o.HandlerLifetime = Transient).ValidatePipelines()`. The factory is the last unkeyed `IAmAScopeProvider` descriptor, so it is the provider in use.
    - `ValidateAll()` yields one `InertScopeAffinity` Error whose message contains "a factory registration at position 2" (1-based among the unkeyed `IAmAScopeProvider` descriptors) and `Transient`, and ends with `…README.md#inertscopeaffinity`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Make `DescriptorRecord` record that a factory registered the descriptor.
    - Render the provider in use from its record: its implementation type, or "a factory registration at position N", where N is its 1-based position among the unkeyed `IAmAScopeProvider` descriptors. Put the rendering where `DuplicateScopeProvider` (Phase 16) can reuse it. An instance's runtime type is T16.3's.

## Phase 16 — DuplicateScopeProvider rule (ADR 0026 step 4)

- [ ] **TEST + IMPLEMENT: T16.1 — Two different provider types are a warning naming both and saying the last is used (ADR 0026 step 4 bullet 1; FR12; FR13 link)**
  - **USE COMMAND**: `/test-first when two different IAmAScopeProvider types are registered with AddSingleton should report a DuplicateScopeProvider warning naming both and the last as the one used`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_two_different_scope_providers_are_registered_should_report_duplicate_scope_provider.cs`
  - Test should verify:
    - One Warning with `RuleId` `DuplicateScopeProvider` and source `"Scope provider registration"`. It names both types and marks the second as used.
    - It advises keeping one provider, registered with `AddDarkerScopeProvider` or `AddDarkerRequestScope`, and ends with `#duplicatescopeprovider`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the `DuplicateScopeProvider` specification to `ScopeConfigurationRules`, evaluated after `InertScopeAffinity`. It fails when the unkeyed provider records name more than one distinct implementation type, and renders each with T15.10's rendering.

- [ ] **TEST + IMPLEMENT: T16.2 — A factory registration beside a type registration is a warning that renders "a factory registration at position N" (ADR 0026 step 4 bullet 2)**
  - **USE COMMAND**: `/test-first when a factory-registered scope provider sits beside a type-registered one should report a DuplicateScopeProvider warning rendering the factory by its position`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_factory_scope_provider_sits_beside_a_type_registration_should_report_duplicate_scope_provider.cs`
  - Test should verify:
    - The message contains "a factory registration at position 1" (1-based among the unkeyed provider descriptors), and the other type's name.
    - The rendering also applies when the factory is the one in use (put it last in a second fact).
    - Two factory registrations and nothing else yield the Warning, rendering "a factory registration at position 1" and "a factory registration at position 2". This fact is the RED: two factories carry no type, so without the rule below they collapse to one implementation.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Reuse T15.10's rendering of a provider record. Count each factory descriptor as distinct from every other descriptor (ADR 0026 `DuplicateScopeProvider` comparison rules).

- [ ] **TEST + IMPLEMENT: T16.3 — An instance of a different type beside a type registration is a warning naming the instance's runtime type (ADR 0026 step 4 bullet 3)**
  - **USE COMMAND**: `/test-first when an instance-registered scope provider of a different type sits beside a type registration should report a DuplicateScopeProvider warning naming the instance's runtime type`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_an_instance_scope_provider_sits_beside_a_type_registration_should_report_duplicate_scope_provider.cs`
  - Test should verify:
    - The message names the instance's runtime type, not `IAmAScopeProvider`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Derive the instance's runtime type from `DescriptorRecord`'s instance, both for the distinct-type comparison and in the shared rendering from T15.10.

- [ ] **CHARACTERISE: T16.4 — The same provider type registered twice yields nothing (ADR 0026 step 4 bullet 4)**
  - **USE COMMAND**: `/test-first when the same IAmAScopeProvider type is registered twice should report no DuplicateScopeProvider finding`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_the_same_scope_provider_type_is_registered_twice_should_report_nothing.cs`
  - Test should verify:
    - No `DuplicateScopeProvider` finding is reported.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: make the rule fail when the unkeyed descriptor count exceeds 1, instead of the distinct implementation count. The test must fail on the "no finding" assertion.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T16.5 — A keyed provider beside one unkeyed provider yields nothing (ADR 0026 step 4 bullet 5)**
  - **USE COMMAND**: `/test-first when a keyed scope provider sits beside one unkeyed provider should report no DuplicateScopeProvider finding`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_keyed_scope_provider_sits_beside_an_unkeyed_one_should_report_nothing.cs`
  - Test should verify:
    - One unkeyed provider `AddSingleton<IAmAScopeProvider, TestScopeProvider>()` and a keyed provider of a **different** type, `AddKeyedSingleton<IAmAScopeProvider, KeyedTestScopeProvider>("k")` (new). No `DuplicateScopeProvider` finding is reported. The types must differ, or the mutation below leaves one distinct implementation and the test stays green.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: drop the `IsKeyedService` filter from `ServiceRegistrationSnapshot.DescriptorsFor`. The test must fail on the "no finding" assertion.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T16.6 — A provider registered with `AddDarkerScopeProvider` and then replaced through the same API yields nothing (ADR 0026 step 4 bullet 6; "the Darker API alone never produces this finding")**
  - **USE COMMAND**: `/test-first when AddDarkerScopeProvider is called twice with different providers should report no DuplicateScopeProvider finding`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_AddDarkerScopeProvider_replaces_a_provider_should_report_no_duplicate_scope_provider.cs`
  - Test should verify:
    - No `DuplicateScopeProvider` finding is reported.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `AddDarkerScopeProvider` (`ServiceCollectionExtensions.cs`), change `services.Replace(ServiceDescriptor.Singleton<IAmAScopeProvider, TProvider>())` to `services.Add(...)`. The test must fail on the "no finding" assertion.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T16.7 — A `DuplicateScopeProvider` warning alone never blocks host start (ADR 0026 step 4 bullet 7; FR4)**
  - **USE COMMAND**: `/test-first when the only finding is a DuplicateScopeProvider warning and throwOnError is true should let the host start and log the warning`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_the_only_finding_is_a_duplicate_scope_provider_should_let_the_host_start.cs`
  - Test should verify:
    - `host.StartAsync` completes. A `LogLevel.Warning` entry names `DuplicateScopeProvider`.
    - In the `DarkerHostBootstrap` collection.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `DarkerValidationHostedService`, throw when `Errors.Count + Warnings.Count > 0`. The test must fail on the host-start assertion.
  - Implementation should:
    - Need no production change.

## Phase 17 — CaptiveDependency rule (ADR 0026 step 5)

> Tests build the provider with `BuildServiceProvider()`, without `ValidateOnBuild` or `ValidateScopes`, and call `ValidateAll()`. They never resolve the handler. The ladder rows cited are ADR 0026's `CaptiveDependency` table.

- [ ] **TEST + IMPLEMENT: T17.1 — A Singleton `HandlerLifetime` and a handler that takes a Scoped dependency is a warning, and a non-component Singleton service taking one is not (ADR 0026 step 5 bullet 1; FR11; FR13 link; ladder rows 3 (second clause), 8 and 11)**
  - **USE COMMAND**: `/test-first when HandlerLifetime is Singleton and a handler takes a Scoped dependency should report a CaptiveDependency warning naming the handler, why it is Singleton, the parameter and the fix, and a plain non-component Singleton service taking a Scoped dependency should report none`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_singleton_handler_takes_a_scoped_dependency_should_report_captive_dependency.cs`
  - Test should verify:
    - A public `CaptiveHandler(ScopedDependency dependency)` is registered explicitly, with `services.AddScoped<ScopedDependency>()`.
    - One Warning with `RuleId` `CaptiveDependency` and source `"Handler 'CaptiveHandler'"`. The message names why it counts as Singleton (`HandlerLifetime`), the parameter's name and type, and the advice to register the dependency Singleton or Transient or to change the lifetime. It ends with `#captivedependency`.
    - Ladder row 3, second clause: in the same setup, `services.AddSingleton<PlainService>()`, where the public non-component `PlainService(ScopedDependency dependency)` implements neither `IQueryHandler` nor `IQueryHandlerDecorator`, yields no `CaptiveDependency` finding naming `PlainService`. This fact drives the component-kind filter below.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Implement only what this test needs: a self-registered, unkeyed handler with one public constructor, a Singleton `HandlerLifetime` and a Scoped dependency.
    - Add the internal `ComponentRegistration` and `ComponentConstructorSelector`. Here the selector returns the handler's single public constructor. The widest-constructor choice is T17.11's, and the tie is T17.13's.
    - Add `ServiceRegistrationSnapshot.Components`: one candidate for each `DescriptorRecord` whose implementation type implements `IQueryHandler`. The `PlainService` fact requires the filter (ladder row 3, second clause), and the source `"Handler 'X'"` needs to know the component is a handler. It reads the record's implementation type, which T15.1 fills from `KeyedImplementationType` for a keyed descriptor and which is null for a factory or instance descriptor. Decorators are T17.3's; the keyed exclusion (row 1) is T17.6's; the alias exclusion (row 3) is T17.9's; effective descriptors (row 4) are T17.7's.
    - Add the rule with `Specification<T>`'s **result-evaluator** constructor, returning one result per Scoped parameter, each read from the parameter type's last unkeyed descriptor (row 8), with `LastOrDefault`: a parameter with no descriptor yields no result (row 10), which T17.5 pins. Reflection runs inside the evaluator, with no bespoke try/catch. Evaluate it after `DuplicateScopeProvider`, in snapshot order. The source is `"Handler 'X'"`, where X is the component type's `Type.Name`.
    - Here, only a Singleton `HandlerLifetime` counts as Singleton.

- [ ] **CHARACTERISE: T17.2 — A Singleton handler with two Scoped parameters yields two warnings, in parameter order, and two captive handlers each yield their own (ADR 0026 step 5 bullet 2; Invariant "one finding for each Scoped parameter"; Risk "stateful `Specification<T>`")**
  - **USE COMMAND**: `/test-first when a Singleton handler takes two Scoped dependencies should report two CaptiveDependency warnings in parameter order`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_singleton_handler_takes_two_scoped_dependencies_should_report_two_warnings_in_order.cs`
  - Test should verify:
    - Two `CaptiveDependency` Warnings. The first names the first parameter, the second the second.
    - Second fact: two Singleton handlers, `CaptiveHandler` and `SecondCaptiveHandler` (new), each taking one Scoped dependency, yield two `CaptiveDependency` Warnings, each naming its own handler.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in the evaluator, return only the first Scoped parameter's result (`.Take(1)`). The first fact must fail on the count assertion. For the second fact, build the `CaptiveDependency` specification once in the `ScopeConfigurationValidator` constructor and run the collector once, after the component loop; the second fact must fail on the count of `CaptiveDependency` findings (1 instead of 2).
  - Implementation should:
    - Need no production change.

- [ ] **TEST + IMPLEMENT: T17.3 — An open-generic decorator registered through `RegisterDecorator` under a Singleton lifetime, taking a Scoped dependency, is a warning naming the decorator (ADR 0026 step 5 bullet 3; FR11 "handler or decorator")**
  - **USE COMMAND**: `/test-first when an open-generic decorator registered with RegisterDecorator under a Singleton handler lifetime takes a Scoped dependency should report a CaptiveDependency warning naming the decorator`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_singleton_open_generic_decorator_takes_a_scoped_dependency_should_report_captive_dependency.cs`
  - Test should verify:
    - A public **async** open-generic decorator `CaptiveDecoratorAsync<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>`, taking `ScopedDependency d`, registered with `.RegisterDecorator(typeof(CaptiveDecoratorAsync<,>))`, yields one `CaptiveDependency` Warning naming the parameter.
    - Its source is exactly ``"Decorator 'CaptiveDecoratorAsync`2'"``: X is the registered type's `Type.Name`, as `"Handler 'CaptiveHandler'"` is in T17.1, and an open generic type definition's `Type.Name` carries its arity.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Recognise as decorators the types, including open generic type definitions, that implement the **non-generic** `IQueryHandlerDecorator` (ADR 0026 Terms). That covers `IQueryHandlerDecorator<,>`, `IQueryHandlerDecoratorAsync<,>` (`src/Paramore.Darker/IQueryHandlerDecoratorAsync.cs:7`) and `IStreamQueryHandlerDecorator<,>` (`IStreamQueryHandlerDecorator.cs:10`). Read their constructors, and use the source `"Decorator 'X'"`.

- [ ] **TEST + IMPLEMENT: T17.4 — A Transient `HandlerLifetime` with the handler registered Singleton before `AddDarker` is a warning (ADR 0026 step 5 bullet 4; "Why a component counts as Singleton when either lifetime says so"; ladder row 5)**
  - **USE COMMAND**: `/test-first when HandlerLifetime is Transient but the handler was registered Singleton before AddDarker and takes a Scoped dependency should report a CaptiveDependency warning naming its Singleton registration`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_handler_registered_singleton_before_AddDarker_takes_a_scoped_dependency_should_report_captive_dependency.cs`
  - Test should verify:
    - The message says it counts as Singleton because of its own registration.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Count a component as Singleton when its own descriptor is Singleton, as well as under a Singleton `HandlerLifetime`. `ComponentRegistration` records why. (T17.7 narrows "its own descriptor" to the effective one.)

- [ ] **CHARACTERISE: T17.5 — Transient or Singleton dependencies, `IServiceProvider` and unregistered parameters yield nothing (ADR 0026 step 5 bullet 5; ladder rows 10 and 11)**
  - **USE COMMAND**: `/test-first when a Singleton handler takes a Transient, Singleton, IServiceProvider or unregistered dependency should report no CaptiveDependency finding`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_singleton_handler_takes_no_scoped_dependency_should_report_nothing.cs`
  - Test should verify:
    - Under a Singleton `HandlerLifetime`, a handler taking a Transient dependency, a handler taking a Singleton dependency, a handler taking `IServiceProvider`, and a handler taking an unregistered type each yield no `CaptiveDependency` finding.
    - For the `IServiceProvider` and unregistered-type facts (ladder row 10, no descriptor), also: no `RuleEvaluationFailed` finding, and `ValidateAll().Errors` is empty.
    - A public open-generic decorator taking `IServiceProvider`, registered with `RegisterDecorator` under a Singleton `HandlerLifetime` (the shape of `CacheableQueryDecorator` and `FluentValidationQueryValidatorDecorator`), yields no `CaptiveDependency` and no `RuleEvaluationFailed` finding, and `ValidateAll().Errors` is empty.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in the evaluator, report every parameter for which a descriptor was found (instead of only `Lifetime == Scoped`). The test must fail on the Transient fact's and the Singleton fact's "no finding" assertions. Second mutation: replace the parameter's descriptor lookup with `.Last()` on the unkeyed descriptor list, which throws when nothing matches; the `IServiceProvider`, unregistered-type and decorator facts must fail on their "no `RuleEvaluationFailed`" assertions.
  - Implementation should:
    - Need no production change. T17.1's lookup yields no result for a parameter with no descriptor, and T17.3 makes decorators candidates.

- [ ] **TEST + IMPLEMENT: T17.6 — A keyed component registration yields nothing (ADR 0026 step 5 bullet 6, keyed part; ladder row 1)**
  - **USE COMMAND**: `/test-first when a Singleton handler is registered keyed before AddDarker with a Transient handler lifetime should report no CaptiveDependency finding`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_component_is_registered_keyed_should_report_nothing.cs`
  - Test should verify:
    - A Transient `HandlerLifetime`, a public handler `H` taking a Scoped dependency, registered `AddKeyedSingleton<H>("k")` before `AddDarker`, and registered explicitly with Darker. No `CaptiveDependency` finding is reported, and `ValidateAll()` does not throw.
    - It is RED after T17.4: the keyed record carries `H` as its implementation type, because T15.1's `Capture` reads `KeyedImplementationType` for a keyed descriptor, and its Singleton lifetime makes `H` count as Singleton.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Exclude keyed records from `ServiceRegistrationSnapshot.Components` (row 1: Darker resolves components unkeyed). Keep reading the keyed properties in `Capture`, which T15.7 pins.

- [ ] **TEST + IMPLEMENT: T17.7 — Only the effective descriptor counts: `AddSingleton<H>()` then `AddTransient<H>()` yields nothing (ADR 0026 step 5 bullet 7; ladder row 4)**
  - **USE COMMAND**: `/test-first when a handler is registered Singleton and then Transient before AddDarker with a Transient handler lifetime should report no CaptiveDependency finding`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_later_descriptor_replaces_a_singleton_component_should_report_nothing.cs`
  - Test should verify:
    - No `CaptiveDependency` finding is reported.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add `ServiceRegistrationSnapshot.EffectiveDescriptor(type, key)`: the last match, where a null key matches unkeyed only. Make `Components` keep only effective descriptors.

- [ ] **TEST + IMPLEMENT: T17.8 — A closed generic parameter falls back to its open generic registration (ADR 0026 step 5 bullet 8; ladder row 9)**
  - **USE COMMAND**: `/test-first when a Singleton handler takes IRepository<Order> and only the open generic IRepository<> is registered Scoped should report a CaptiveDependency warning naming the parameter`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_singleton_handler_takes_a_closed_generic_of_a_scoped_open_generic_should_report_captive_dependency.cs`
  - Test should verify:
    - `AddScoped(typeof(IRepository<>), typeof(Repository<>))` with a handler taking `IRepository<Order>` yields one Warning naming the parameter.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - When no descriptor matches a constructed generic parameter, read the effective descriptor of its generic type definition, ignoring constraints.

- [ ] **TEST + IMPLEMENT: T17.9 — An interface registration of a handler beside Darker's own registration yields nothing (ADR 0026 step 5 bullet 9; ladder row 3)**
  - **USE COMMAND**: `/test-first when a handler is also registered Singleton under IQueryHandlerAsync beside the Transient registration Darker uses should report no CaptiveDependency finding`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_handler_has_an_interface_alias_registration_should_report_nothing.cs`
  - Test should verify:
    - `AddSingleton<IQueryHandlerAsync<Q, R>, H>()` plus a Transient `HandlerLifetime` registration of `H`, which takes a Scoped dependency, yields no `CaptiveDependency` finding.
    - It is RED after T17.4 and T17.7: the alias descriptor's implementation type is `H`, it is the effective descriptor for its own service type, and its Singleton lifetime makes `H` count as Singleton.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the row-3 alias exclusion to `Components`: keep only records whose service type equals their implementation type.

- [ ] **TEST + IMPLEMENT: T17.10 — `[FromKeyedServices("k")]` reads the keyed registration, and a parameterless attribute reads unkeyed (ADR 0026 step 5 bullet 10; ladder rows 7 and 8; "Why [FromKeyedServices] is honoured")**
  - **USE COMMAND**: `/test-first when a Singleton handler's parameter carries FromKeyedServices with a key should judge the keyed registration and ignore an unkeyed Scoped registration of the same type`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_singleton_handler_parameter_is_from_keyed_services_should_judge_the_keyed_registration.cs`
  - Test should verify:
    - `[FromKeyedServices("k")] IDb db` bound to `AddKeyedSingleton<IDb, Db>("k")`, beside `AddScoped<IDb, Db>()`, yields nothing.
    - Bound to `AddKeyedScoped<IDb, Db>("k")`, it yields one Warning.
    - A parameterless `[FromKeyedServices]` with an unkeyed Scoped `IDb` yields one Warning.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - For a parameter with an explicit non-null key, read the last keyed descriptor for that type and key. Otherwise fall through to the unkeyed lookup.

- [ ] **TEST + IMPLEMENT: T17.11 — The widest public constructor is the one inspected, and a handler whose constructors take no parameters yields nothing (ADR 0026 step 5; "Why the widest public constructor"; ladder row 6, first clause)**
  - **USE COMMAND**: `/test-first when a Singleton handler has two public constructors of different widths and only the wider takes a Scoped dependency should report a CaptiveDependency warning`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_handler_has_constructors_of_different_widths_should_inspect_the_widest.cs`
  - Test should verify:
    - Under a Singleton `HandlerLifetime`, a public handler with two public constructors, declared narrower first: `(OtherDependency other)` and `(OtherDependency other, ScopedDependency dependency)`. One `CaptiveDependency` Warning names the `dependency` parameter. It is RED against T17.1's selector, which expects a single public constructor.
    - A public handler whose only public constructor takes no parameters yields no `CaptiveDependency` finding (row 6, "no public constructor with parameters").
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Make `ComponentConstructorSelector` return the public constructor with the most parameters, and nothing when no public constructor has parameters. The tie is T17.13's.

- [ ] **CHARACTERISE: T17.12 — Constructor reflection that throws yields a `RuleEvaluationFailed` error and no `CaptiveDependency` finding (ADR 0026 step 5 bullet 12; Invariant "a misconfiguration never throws out of Validate")**
  - **USE COMMAND**: `/test-first when reading a Singleton handler's constructor throws should report a RuleEvaluationFailed error with its rule identifier and no CaptiveDependency finding`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_constructor_reflection_throws_should_report_rule_evaluation_failed.cs`
  - Test should verify:
    - An Error with `RuleId` `RuleEvaluationFailed` names the component and carries the exception message. No `CaptiveDependency` finding is reported, and `ValidateAll()` does not throw.
    - **The double (Path A, inside the existing test project).** In `test/Paramore.Darker.Extensions.Tests/TestDoubles/`, add a public `ThrowingKeyedAttribute : FromKeyedServicesAttribute` whose constructor calls `base("k")` and then throws `InvalidOperationException`. `FromKeyedServicesAttribute` is not sealed in `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.10. Add a public handler double `ThrowingConstructorReflectionHandler([ThrowingKeyed] ScopedDependency dependency)`, registered explicitly under a Singleton `HandlerLifetime`, with `AddScoped<ScopedDependency>()`. Reading the parameter's `[FromKeyedServices]` attributes (ladder row 7, added by T17.10, so this task stays after T17.10) rethrows the attribute's exception inside the evaluator. The pipeline validator never reads constructor parameter attributes, so the pipeline half reports nothing for it.
    - **Fallback (Path B), only with the user's approval.** If Path A cannot be made to throw inside the evaluator, a tiny class library referenced with `<ProjectReference … Private="false" />` defines the parameter type, so `GetParameters()` throws `FileNotFoundException` at run time. That adds a project to `Darker.slnx` and `Darker.Filter.slnf`, which no ADR mentions, so stop and ask before taking it.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: wrap the evaluator's constructor reflection in `try { … } catch { return Enumerable.Empty<ValidationResult>(); }`. The test must fail on the presence of the `RuleEvaluationFailed` error.
  - Implementation should:
    - Need no production change. `CaptiveDependency` uses the result-evaluator form (T17.1), so T2.2's result-evaluator catch path (`EvaluateCollapsed`) delivers this.

- [ ] **TEST + IMPLEMENT: T17.13 — Two equal-width public constructors yield nothing (ADR 0026 step 5 bullet 11; ladder row 6, second clause)**
  - **USE COMMAND**: `/test-first when a Singleton handler has two public constructors with the same widest parameter count should report no CaptiveDependency finding`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_handler_has_two_equal_width_constructors_should_report_nothing.cs`
  - Test should verify:
    - Under a Singleton `HandlerLifetime`, two public handler doubles, each with two one-parameter public constructors, `(ScopedDependency dependency)` and `(OtherDependency other)`, declared in opposite orders. Neither yields a `CaptiveDependency` finding.
    - The CLR does not guarantee the order of `GetConstructors()`. Using both declaration orders makes the RED independent of which order reflection uses, provided it uses the same rule for both types (declaration order, or its reverse, as current runtimes do): T17.11's selector returns the first of the tied constructors, so for one of the two doubles it picks the Scoped one and reports it.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Make `ComponentConstructorSelector` return nothing when two or more public constructors share the widest parameter count. The rule counts constructors of the widest width, so it does not depend on the order `GetConstructors()` returns.

- [ ] **CHARACTERISE: T17.14 — A component registered by a factory delegate yields nothing (ADR 0026 step 5 bullet 6, factory part; ladder row 2)**
  - **USE COMMAND**: `/test-first when a Singleton handler is registered by a factory delegate before AddDarker with a Transient handler lifetime should report no CaptiveDependency finding`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_component_is_registered_by_factory_should_report_nothing.cs`
  - Test should verify:
    - A Transient `HandlerLifetime`, a public handler `H` taking a Scoped dependency, registered `AddSingleton(sp => new H(...))` before `AddDarker` and registered explicitly with Darker (whose `TryAdd` then adds nothing, so the factory descriptor is the effective one). No `CaptiveDependency` finding is reported, and `ValidateAll()` does not throw.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `ServiceRegistrationSnapshot.Components`, use the descriptor's service type as the implementation type for a factory record. The test must fail on the "no finding" assertion.
  - Implementation should:
    - Need no production change. A factory record has no implementation type (T15.1, T15.10), so T17.1's `Components` never makes it a candidate.

- [ ] **CHARACTERISE: T17.15 — A component registered as an instance yields nothing (ADR 0026 step 5 bullet 6, instance part; ladder row 2, "an instance's runtime type does not count")**
  - **USE COMMAND**: `/test-first when a Singleton handler is registered as an instance before AddDarker with a Transient handler lifetime should report no CaptiveDependency finding`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_component_is_registered_as_an_instance_should_report_nothing.cs`
  - Test should verify:
    - A Transient `HandlerLifetime`, a public handler `H` taking a Scoped dependency, registered `AddSingleton(new H(...))` before `AddDarker` and registered explicitly with Darker. No `CaptiveDependency` finding is reported, and `ValidateAll()` does not throw.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `ServiceRegistrationSnapshot.Components`, use the instance's runtime type as the implementation type for an instance record (as T16.3 does for scope providers). The test must fail on the "no finding" assertion.
  - Implementation should:
    - Need no production change. An instance record has no implementation type, so T17.1's `Components` never makes it a candidate.

## Phase 18 — Snapshot semantics and side effects (ADR 0026 step 6)

- [ ] **CHARACTERISE: T18.1 — A Scoped dependency added after `ValidatePipelines()` is not reported (ADR 0026 step 6 bullet 1; FR14 lifetime part, "call it last")**
  - **USE COMMAND**: `/test-first when a Scoped dependency is registered after ValidatePipelines should not report a CaptiveDependency for it`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_scoped_dependency_is_added_after_ValidatePipelines_should_not_be_reported.cs`
  - Test should verify:
    - A Singleton `HandlerLifetime` handler taking `ScopedDependency`, with `AddScoped<ScopedDependency>()` called after `ValidatePipelines()`. No `CaptiveDependency` finding is reported.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `ServiceRegistrationSnapshot.Capture`, keep a reference to `services` instead of copying it. The test must fail on the "no finding" assertion.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T18.2 — A scope provider added between two `ValidatePipelines()` calls is seen through the second call's snapshot (ADR 0026 step 6 bullet 2; Invariant "the last call wins")**
  - **USE COMMAND**: `/test-first when a scope provider is added between two ValidatePipelines calls should see it and report no InertScopeAffinity`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_a_scope_provider_is_added_between_two_ValidatePipelines_calls_should_be_seen.cs`
  - Test should verify:
    - `DefaultScopeAffinity = JoinAmbient` and Scoped, then `ValidatePipelines()`, then `AddSingleton<IAmAScopeProvider, P>()`, then `ValidatePipelines()`. No `InertScopeAffinity` finding is reported.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: capture the snapshot only inside the once-only branch. The test must fail on the "no finding" assertion.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T18.3 — Validation creates no handler and no scope provider, and `ValidateAll` creates no scope at its own call level (ADR 0026 step 6 bullet 3; NFR no side effects; acceptance criterion "creates no handler, decorator or scope")**
  - **USE COMMAND**: `/test-first when lifetime validation runs should never construct a handler or a scope provider and ValidateAll should create no scope`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_lifetime_validation_runs_should_create_no_handler_or_scope_provider.cs`
  - Test should verify:
    - `ConstructionRecordingHandler` (from T11.18) and `ConstructionRecordingScopeProvider` (new) have counters of 0 after `ValidateAll()` and after a host start.
    - Scope creation: `ScopeRecordingServiceProvider` (new) wraps the built provider and, when asked for `IServiceScopeFactory`, returns a `RecordingServiceScopeFactory` (new) over the real one that counts `CreateScope` calls; every other request passes through. `ValidateAll()` called on the wrapper, with both validators registered, leaves the count at 0. (Microsoft's container answers `IServiceScopeFactory` itself and ignores a registered one, so the double wraps the provider rather than registering a factory.)
    - In the `DarkerHostBootstrap` collection.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in the `ScopeConfigurationValidator` registration factory, add `_ = sp.GetServices<IAmAScopeProvider>().ToList();`, which names the provider in use by resolving it. The test must fail on the provider-counter assertion. For the scope fact, in `ValidateAll`, create a scope with `provider.CreateScope()` and resolve the validators from `scope.ServiceProvider`; the test must fail on the `CreateScope` count assertion (1 instead of 0).
  - Limit: the scope fact pins scope creation only at the `ValidateAll` call level. Validators are built by factories that receive the real root provider, so a scope created inside a validator factory, the probe or a validator bypasses `RecordingServiceScopeFactory`. No second mutation is named for that case, because it would not be observed.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T18.4 — Several lifetime mistakes appear in one combined result beside a pipeline finding (ADR 0026 step 6 bullet 4; FR3)**
  - **USE COMMAND**: `/test-first when there are lifetime mistakes and a pipeline mistake should report all of them in one ValidateAll result`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_lifetime_and_pipeline_mistakes_coexist_should_report_all_in_one_result.cs`
  - Test should verify:
    - An unregistered decorator, an inert `JoinAmbient`, two different providers and a captive handler yield `DecoratorResolvable`, `InertScopeAffinity`, `DuplicateScopeProvider` and `CaptiveDependency` in one result.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `ValidateAll`, stop after the first validator whose result is invalid. The test must fail on the presence of the lifetime findings.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T18.5 — Lifetime findings follow the pipeline findings in each list, and `DuplicateScopeProvider` precedes `CaptiveDependency` (ADR 0026 step 6 bullet 5; Invariant "order is deterministic within each severity list")**
  - **USE COMMAND**: `/test-first when pipeline and lifetime findings of both severities coexist should list lifetime findings after pipeline findings and DuplicateScopeProvider before CaptiveDependency`
  - Test location: "test/Paramore.Darker.Extensions.Tests/PipelineValidation/"
  - Test file: `When_pipeline_and_lifetime_findings_coexist_should_order_them_by_validator_then_rule.cs`
  - Test should verify:
    - The setup has one pipeline Error (`DecoratorResolvable`), one pipeline Warning (`DuplicateStep`), `InertScopeAffinity`, `DuplicateScopeProvider`, and two captive handlers (`CaptiveHandler` and `SecondCaptiveHandler`), so two `CaptiveDependency` Warnings.
    - Filter each list to the rule identifiers of interest, then compare order. `Errors` filtered to `DecoratorResolvable` and `InertScopeAffinity` is `[DecoratorResolvable, InertScopeAffinity]`. `Warnings` filtered to `DuplicateStep`, `DuplicateScopeProvider` and `CaptiveDependency` is `[DuplicateStep, DuplicateScopeProvider, CaptiveDependency, CaptiveDependency]`, and the two `CaptiveDependency` Warnings name different handlers. Never compare whole lists or total counts.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `ScopeConfigurationRules`, evaluate `CaptiveDependency` before `DuplicateScopeProvider`. The test must fail on the `Warnings` order assertion.
  - Implementation should:
    - Need no production change.

## Phase 19 — Lifetime documentation and sample (ADR 0026 step 7)

- [ ] **DOC: T19.1 — README lifetime rule sections, the "call it last" rule, XML remarks and the sample, in one commit (ADR 0026 step 7; FR13 lifetime part; FR14 lifetime part; acceptance criterion "README documents … how to fix each lifetime finding")**
  - In T12.1's README section:
    - add one `####` heading per rule, named exactly `InertScopeAffinity`, `CaptiveDependency` and `DuplicateScopeProvider`, each stating the cause and the fix. Confirm that GitHub's generated anchors match the links in the messages (`#inertscopeaffinity`, `#captivedependency`, `#duplicatescopeprovider`);
    - state that `ValidatePipelines()` must be called last, after `AddDarker`, handler registration, `AddDarkerScopeProvider` or `AddDarkerRequestScope`, and the registrations of handlers' dependencies. Show both safe shapes: the scope provider registered before a chain that ends in `ValidatePipelines()`, and `ValidatePipelines()` called on the held builder as the final statement;
    - list the cases that `CaptiveDependency` cannot see (ADR 0026 Negative): factory or instance components, keyed components, `IEnumerable<T>`, transitive captivity, open-generic decorator type-parameter dependencies, and `KeyedService.AnyKey`.
  - In the README's existing EF Core snippet (around `README.md:25-29`), move `AddDarkerRequestScope()` before the `AddDarker` chain.
  - In `src/Paramore.Darker.Extensions.AspNetCore/DarkerRequestScopeServiceCollectionExtensions.cs` (around line 47), remove "The order of this call relative to `AddDarker` does not matter", and say to call it before `ValidatePipelines()`.
  - In `AddDarkerScopeProvider`'s XML remarks (`ServiceCollectionExtensions.cs`), add the call-order sentence.
  - In `samples/SampleMinimalApi/Program.cs`, register `AddDarkerRequestScope()` before the chain (currently line 17, after the chain at line 13), and end the chain with `.ValidatePipelines()`. Run the sample, or build it, to confirm that it starts clean.
  - These are the only documentation edits of ADR 0026, and they land together in one commit.

## Phase 20 — Release notes and final verification

- [ ] **DOC: T20.1 — Release notes for spec 016 (house convention; ADR 0025 Negative "every user of the DI package gains a package reference")**
  - Use `/spec:write_release_notes` to write the `<!-- spec: 016-pipeline_validation -->` section in `release_notes.md`. It covers:
    - the opt-in `ValidatePipelines()`, with the "call it last" rule;
    - the rule list;
    - `ValidateAll`;
    - the new `Microsoft.Extensions.Hosting.Abstractions` dependency of the DI package;
    - that an explicitly registered internal handler now fails validation once the application opts in;
    - that no existing signature changed.

- [ ] **VERIFY: T20.2 — Final checkpoint**
  - Run `dotnet build Darker.Filter.slnf -c Release` with no new warnings, on netstandard2.0, net8.0 and net9.0.
  - Run `dotnet test Darker.Filter.slnf -c Release --no-build`, green on net8.0 and net9.0, including `test/Paramore.Darker.Extensions.AspNetCore.Tests`.
  - `grep -rn "InternalsVisibleTo" src test` finds nothing new.
  - `grep -rn "Microsoft.Extensions" src/Paramore.Darker --include=*.cs` finds no new use (NFR core purity). ADR 0026 added no core type.
  - The `ServiceProviderComponentFactory`, `AdoptionDiagnostics` and `QueryProcessorBuilder` diffs are empty (ADR 0026 and ADR 0025 "deliberately unchanged").
  - Docs checks:
    - every rule identifier in the code has a README entry;
    - each lifetime message's link resolves to a README heading;
    - the sample builds;
    - the ADR index or frontmatter still lists 0025 and 0026 as Accepted.
  - Optionally, run `test/Paramore.Darker.Benchmarks` before and after to show that per-query cost is unchanged (NFR startup cost).

---

## Coverage cross-reference

### Functional requirements → tasks

| FR | Tasks |
| --- | --- |
| FR1 opt-in registration | T11.1, T11.2, T11.8, T11.11, T14.1, T14.2 |
| FR2 runs at host start, before the server, not order-dependent | T10.1, T11.9, T11.14, T11.15, T11.16 |
| FR3 report everything; severity, rule id, message | T1.2, T2.1, T2.2, T6.3, T6.4, T7.8–T7.11, T7.16, T7.17, T8.4, T17.12, T18.4 |
| FR4 outcome (throw, log Error, Warning never blocks, clean logs Debug only) | T6.2, T11.10, T11.11, T11.12, T11.13, T15.8, T16.7 |
| FR5 on-demand use | T6.1, T6.3, T11.3 |
| FR6 handler registered, public, resolvable | T5.1–T5.3, T7.1–T7.4, T7.15, T7.19, T8.1, T8.3, T8.5, T11.5 |
| FR7 decorator resolvable | T8.2, T8.3, T8.6, T11.4, T11.6 |
| FR8 sync/async consistency and attribute kind | T7.5, T7.6, T7.7, T7.8–T7.12 |
| FR9 duplicate step and ordering | T3.1, T7.13, T7.14, T9.1–T9.4, T9.6, T11.7 |
| FR10 inert scope opt-in | T15.1–T15.10, T18.2 |
| FR11 captive dependency | T17.1–T17.15, T18.1 |
| FR12 duplicate scope providers | T16.1–T16.7 |
| FR13 guidance and README | T12.1, T15.1, T16.1, T17.1, T19.1 |
| FR14 call order | T11.17 (pipeline: independent), T18.1, T18.2, T19.1 (lifetime: call last) |

Every FR has at least one task.

### NFRs → tasks

| NFR | Tasks |
| --- | --- |
| Opt-in and non-breaking | T4.1, T5.1, T11.1, T12.3, T20.2 |
| Startup cost (reflection once, no per-query change) | T3.3, T3.4 (builder behaviour unchanged), T12.3, T20.2 (optional benchmark) |
| No side effects | T11.18, T18.3, T8.1/T11.4 (membership probe) |
| Core purity | T1.1, T10.1, T12.3, T20.2 |
| Messages actionable | Every rule task asserts the offender's name and the fix (T7.x, T8.x, T9.x, T15.x, T16.x, T17.x) |
| Multi-target | T1.1, T10.1, T12.3, T20.2 |

Every NFR has at least one task.

### Acceptance criteria → asserting task

| Criterion | Tasks |
| --- | --- |
| Correct configuration starts clean | T6.1, T11.13 (clean Debug only), T15.4 |
| One test per FR6–FR12 asserting rule id, severity, offender | T7.1–T7.13, T8.1, T8.2, T9.1, T15.1, T16.1, T17.1 |
| Several misconfigurations in one result | T7.16, T18.4 |
| throw / log / warnings never block | T11.10, T11.12, T11.13, T16.7 |
| Not calling it is unaffected; suite passes unchanged | T11.1, T12.3, T20.2 |
| Creates no handler, decorator or scope | T11.18, T18.3 |
| README documents the call, the order, the rules, the lifetime fixes | T12.1, T19.1 |

### ADR 0025 decisions and steps → tasks

| ADR 0025 item | Tasks |
| --- | --- |
| Step 1 (Specification copy and Brighter tests) | T1.1, T1.2 |
| Step 2 (rule identifiers, `EvaluationFailed`; both catch blocks) | T2.1 (predicate), T2.2 (result evaluator) |
| Step 3 (extract `PipelineAttributeReader`, builder unchanged) | T3.1, T3.2 (guards), T3.3, T3.4 |
| Step 4 (expose registries) | T4.1 |
| Step 5 (inspector, `HandlerRegistration`) | T5.1, T5.2, T5.3 |
| Step 6 (result, exception, describer, skeleton; router; throwing inspector) | T6.1, T6.2, T6.3, T6.4, T7.1 (describer and description types), T7.4 (router) |
| Step 7 (six rules in order; RegistryNotInspectable; FR3; determinism; no-handler description) | T7.1–T7.19 |
| Step 8 (probe, resolvability rules, no-probe warning, throwing probe) | T8.1–T8.4; T8.5, T8.6 (no-handler description) |
| Step 9 (ordering table, extension) | T9.1–T9.5; T9.6 (no-handler description) |
| Step 10 (hosting references) | T10.1 |
| Step 11 (DI wiring; eight test bullets) | T11.1–T11.18 (bullet 1: T11.1, T11.8; bullet 2: T11.7; bullet 3: T11.4–T11.6; bullet 4: T11.9, T11.14–T11.16; bullet 5: T11.10–T11.13; bullet 6: T11.3; bullet 7: T11.17; bullet 8: T11.18) |
| Step 12 (README) | T12.1 |
| Catalogue note: every rule except `HandlerRegistered` is satisfied by a description with no handler type | T7.19 (Phase 7 rules), T8.5 (`HandlerResolvable`), T8.6 (`DecoratorResolvable`), T9.6 (`DecoratorOrdering`) |
| Invariant: nothing runs without opt-in | T11.1 |
| Invariant: every finding reported (no short-circuit) | T1.2, T7.16 |
| Invariant: misconfiguration never throws out of `Validate` | T6.4, T7.8–T7.11, T8.4, T8.5, T8.6, T9.6, T7.19 (null-safe error factories) |
| Invariant: never creates a component | T11.18 |
| Invariant: runs once, before the server | T11.9, T11.14, T11.15, T11.16 |
| Invariant: one description per candidate | T5.2, T7.2, T7.4 |
| Invariant: without a probe, says so | T8.3 |
| Invariant: deterministic order | T7.18, T8.3 (`ResolvabilityNotChecked` first) |
| Invariant: registration order does not matter | T11.17 |
| `PipelineValidationException` derives from `Exception` | T6.2 |
| `ValidatePipelines` `InvalidOperationException` | T11.2 |
| Last call sets `throwOnError` | T11.11 |
| Risk: stateful `Specification<T>` | T7.17 |
| Risk: start-phase limits | T12.1 (README) |
| Risk: `IsService` vs missing constructor dependency | T12.1 (points to `ValidateOnBuild`) |
| Risk: duplicate registrations | T11.8 |
| Risk: "lower step" against stale docs | T3.1, T9.3, T12.1 |
| Risk: AOT (`MakeGenericType` reused) | T3.3 (reuses the builder's pattern; no new dynamic code) |
| Negative: reader must stay the single reflection path | T3.1–T3.4 |
| Out of scope: correcting ADR 0002, CLAUDE.md, caching README, skills_overview wording | **No task, deliberately** (ADR 0025 assigns it to a separate change; T12.1 states the correct semantics) |

### ADR 0026 decisions and steps → tasks

| ADR 0026 item | Tasks |
| --- | --- |
| Step 1 (builder carries options) | T13.1 |
| Step 2 (snapshot, validator skeleton; two bullets) | T14.1, T14.2 |
| Step 3 (`InertScopeAffinity`; nine bullets) | T15.1–T15.9; T15.10 (catalogue: factory provider rendering) |
| Step 4 (`DuplicateScopeProvider`; seven bullets) | T16.1–T16.7 |
| Step 5 (`CaptiveDependency`; twelve bullet groups) | T17.1–T17.15 (bullet 6 split into T17.6 keyed, T17.14 factory and T17.15 instance; bullet 11 is T17.13; the widest-constructor choice, "Why the widest public constructor", is T17.11) |
| Step 6 (snapshot semantics and side effects; five bullets) | T18.1–T18.5 |
| Step 7 (README, XML remarks, sample, one commit) | T19.1 |
| Ladder rows 1–11 | Row 1: T17.6; row 2: T17.14 (factory), T17.15 (instance); row 3: T17.9 (alias), T17.1 (component kind: a handler is a candidate, and the `PlainService` fact shows a non-component Singleton gives nothing), T17.3 (component kind: decorator); row 4: T17.7; row 5: T17.4 (own descriptor Singleton, inspected), T17.7 (neither Singleton, not inspected); row 6: T17.11 (widest; no constructor with parameters), T17.13 (tie); row 7: T17.10, T17.12; row 8: T17.1, T17.10; row 9: T17.8; row 10: T17.5 (no `CaptiveDependency` and no `RuleEvaluationFailed`, for a handler and an `IServiceProvider` decorator); row 11: T17.1, T17.5 |
| `DuplicateScopeProvider` comparison rules (type, instance, factory position) | T16.1, T16.2, T16.3, T16.4, T16.5; T15.10 (shared factory rendering) |
| `InertScopeAffinity` names the provider in use, rendered as in `DuplicateScopeProvider` | T15.1 (type), T15.9 (`AddDarkerRequestScope`), T15.10 (factory) |
| Invariant: descriptors read, never resolved | T18.3 |
| Invariant: snapshot is a copy | T18.1 |
| Invariant: last call wins | T14.2, T18.2 |
| Invariant: rules read what the runtime reads | T15.3, T15.7, T16.6, T17.7 |
| Invariant: misconfiguration never throws (evaluator catch) | T2.2 (stamps the evaluator catch block), T17.12; the rules' error factories are null-safe under T7.1's constraint |
| Invariant: one finding per Scoped parameter | T17.2 |
| Invariant: ordering within severity lists | T18.5 |
| Risk: users chain and register afterwards | T18.1, T19.1 |
| Risk: wrong property family | T15.1 (implementation), T15.7 |
| Risk: rule/runtime drift | Every Phase 15–17 test uses `AddDarker`, `AddDarkerScopeProvider` and `RegisterDecorator`; T16.6 |
| Risk: two `AddDarker` calls | **No task** (ADR 0026 records no mitigation; unsupported) |
| Risk: stateful `Specification<T>` | T17.2, T18.5 (fresh rules per `Validate`, T15.1) |
| Negative: `CaptiveDependency` blind spots | T19.1 (README list); T17.6, T17.14 and T17.15 pin the keyed, factory and instance cases |
| Negative: four wrong-warning cases | **No task** (accepted by the ADR; no requirement asks for them to be fixed) |

### Tasks not traceable to an FR, NFR or ADR step

- **T12.2** (XML documentation) traces to the house standard in `.agent_instructions/documentation.md`, not to an ADR step.
- **T20.1** (release notes) traces to the repository's release-notes convention (`release_notes.md`, `/spec:write_release_notes`) and to ADR 0025's Negative about the new package reference. It is not an ADR step.
- **T3.1 and T3.2** are risk-mitigation pins that this task list adds before ADR 0025 step 3. They trace to ADR 0025's Negative on the single reflection path and to step 3's "messages unchanged".

Every other task traces to an ADR step, invariant or risk, and to at least one FR or NFR.

### Deviations from the ADR step labels (for the reviewer)

- **ADR 0025 step 1 is split** into a STRUCTURAL copy of the source files (T1.1) and a CHARACTERISE task for the copied Brighter tests (T1.2). The copied tests are green on arrival, so they need a named mutation to show they guard the copy.
- **ADR 0025 step 3 is split** into two guard pins (T3.1, T3.2) and two STRUCTURAL commits (T3.3 reader extraction, T3.4 wrong-kind checks).
- **Error factories must be null-safe and must not throw (T7.1), an implementation constraint beyond ADR 0025's text.** `Specification<T>` calls the error factory outside its try (Brighter's `Specification.cs:194-195`), so a throwing factory would escape `Validate`. Predicates that throw are already reported as `RuleEvaluationFailed` by the catch paths (T2.1, T2.2), and T6.4's per-registry guard covers reading and describing a registry; nothing else wraps rule evaluation, so the findings of rules that do not throw are always kept (FR3). The constraint applies equally to `ScopeConfigurationValidator`'s rules (Phases 15–17). No ADR change is needed: it serves ADR 0025's invariant "a misconfiguration never throws out of `Validate`". A throwing error factory cannot be injected through the public surface, so the constraint is pinned through the "`Validate()` does not throw" assertions of T7.19, T8.5, T8.6 and T9.6.
- **ADR 0025 step 6's router test moves to T7.4.** A description cannot be seen through the public surface until a rule reports on it. The throwing-inspector test (T6.4) uses two throwing registries, so that "the others are still validated" can be seen without rules.
- **ADR 0025 step 6's describer and description types move to Phase 7.** T6.1 adds only `IAmAPipelineValidator`, `PipelineValidator` and `PipelineValidationResult`, which its test needs. `HandlerPipelineDescription`, `PipelinePath` and `PipelineDescriber` arrive in T7.1, and `PipelineStepDescription` in T7.13, the first tasks whose tests need them.
- **The `ValidatePipelines()` "twice" behaviour is split** into once-only registration (T11.8) and last-call `throwOnError` (T11.11), so that each change has its own RED.
- **Several ADR test bullets are CHARACTERISE rather than TEST + IMPLEMENT**, because an earlier task delivers them. Each names its RED mutation. They are: T7.4, T7.12, T7.14, T7.16–T7.18, T8.4, T8.6, T9.3, T9.4, T9.6, T11.5, T11.6, T11.16–T11.18, T14.2, T15.3, T15.4, T15.6, T15.7, T15.8, T15.9, T16.4–T16.7, T17.2, T17.5, T17.12, T17.14, T17.15 and T18.1–T18.5.
- **No task is left to "stop and ask" if it arrives green.** T9.4, T11.5, T15.6 and T15.7 are CHARACTERISE with named mutations, because T9.1, T8.1 with T11.4, T15.5, and T15.1 with T15.5 deliver them. T11.14 (only `StartAsync`, guarded by a flag that `StartingAsync` sets, so a real host still validates once) precedes T11.15 (a second `StartAsync`, then the once-only guard), so each is RED against the code before it and the suite stays green between them. T7.4 is CHARACTERISE, because T7.1's describer already iterates the candidates. T7.19, T17.6, T17.9, T17.11 and T17.13 are TEST + IMPLEMENT, because T7.3–T7.13 and T17.1 implement only what their own tests need.
- **The no-handler facts for `DecoratorResolvable` (T8.6) and `DecoratorOrdering` (T9.6) are CHARACTERISE.** A description with no handler type has no steps, so both rules are satisfied without a guard; only `HandlerResolvable` (T8.5) asks the probe about the handler type and needs one.
- **ADR 0026 step 5 bullet 6 is split three ways.** The keyed fact (T17.6) is TEST + IMPLEMENT, because `Capture` fills a keyed record's implementation type from `KeyedImplementationType`. The factory (T17.14) and instance (T17.15) facts are CHARACTERISE, each with its own mutation, because their records carry no implementation type. ADR 0026 step 5 bullet 11 (the tie) is T17.13, after the widest-constructor task T17.11 it depends on.
- **T6.2 adds a public constructor beyond ADR 0025's member table.** `PipelineValidationResult(errors, warnings)` is public, although ADR 0025's `PipelineValidationResult` table lists only `Errors`, `Warnings`, `IsValid`, `ThrowIfInvalid` and `Combine`. `FixedResultValidator` (T11.3) and third-party `IAmAPipelineValidator` implementations need it to return findings. It is recorded here as an addition, not a change to the ADR.
- **T17.12's test double** is a public `ThrowingKeyedAttribute : FromKeyedServicesAttribute` on a handler's constructor parameter, inside the existing Extensions test project (Path A). Path A substitutes for ADR 0026 step 5 bullet 12's "a type whose parameter fails to load" double: reflection still throws inside the evaluator, which is what the bullet tests. A helper class library absent at run time (Path B), which is the ADR's literal double, is a documented fallback only, because it adds a project to the solution files; it needs the user's approval.

### Critical Files for Implementation
- /Users/ian.cooper/CSharpProjects/github/BrighterCommand/Darker/src/Paramore.Darker/PipelineBuilder.cs
- /Users/ian.cooper/CSharpProjects/github/BrighterCommand/Darker/src/Paramore.Darker/QueryHandlerRegistry.cs (and QueryHandlerRegistryAsync.cs, StreamQueryHandlerRegistry.cs)
- /Users/ian.cooper/CSharpProjects/github/BrighterCommand/Darker/src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionExtensions.cs
- /Users/ian.cooper/CSharpProjects/github/BrighterCommand/Darker/src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionDarkerHandlerBuilder.cs
- /Users/ian.cooper/CSharpProjects/github/BrighterCommand/Brighter/src/Paramore.Brighter/Specification.cs (copy source, with its sibling files and tests/Paramore.Brighter.Core.Tests/Specifications/)