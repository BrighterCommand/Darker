# Tasks — Spec 015: DI lifetime conformance

**Spec**: `specs/015-di_lifetime_conformance/requirements.md`
**Linked issue**: [#377 — Bring DI lifetime handling into conformance with Brighter](https://github.com/BrighterCommand/Darker/issues/377) (sub-issues #378 teardown, #379 pipeline scope handle, #380 ambient scope adoption)
**ADRs (all Accepted)**: [0022 Robust Pipeline Teardown](../../docs/adr/0022-robust-pipeline-teardown.md), [0023 Pipeline Scope Handle](../../docs/adr/0023-pipeline-scope-handle.md), [0024 Ambient Scope Adoption](../../docs/adr/0024-ambient-scope-adoption.md)

**How the phases depend on each other.** The phases must run in order. Phase 1 (ADR 0022) makes teardown isolated, never-throwing and asynchronous on the async and stream paths. It adds `IAsyncDisposable` to `IAmALifetime` and `ReleaseAsync` to the async factories. Phase 2 (ADR 0023) relies on those disposal rules: it moves the per-query scope onto the lifetime as an `IAmAScope` handle, migrates the Phase 1 tests from `lifetime.Add` to a recording handle, and then deletes `Add`. Phase 3 (ADR 0024, DI package) needs Phase 2's single decision point, `CreatePipelineScope()`, and its one recognition method. Phase 3 adds the ambient seam, the adoption ladder, the probe and Transient decorators. Phase 4 (ADR 0024, new ASP.NET Core package) is a thin opt-in over Phase 3's `AddDarkerScopeProvider`, so it cannot start until Phase 3 is green. Phase 5 (documentation and release) describes what Phases 1–4 built, so it comes last. Each phase ends with a VERIFY checkpoint. No phase may start until the previous checkpoint is green on net8.0 and net9.0.

### Conventions for every task in this file

- **Public surface only.** Tests reach the code through `IQueryProcessor`, `QueryProcessorBuilder`, `HandlerConfiguration`, `AddDarker` and the other public types. Never use `InternalsVisibleTo`. Never drive `QueryLifetimeScope`, `PipelineBuilder<TResult>`, `ServiceProviderComponentFactory`, `ServiceProviderLifetimeScope`, `BorrowedServiceProviderScope`, `AdoptionDiagnostics`, `HttpContextScopeProvider` or `HttpRequestScope` directly.
- **Test doubles.** Use them in the order Real > Simple > InMemory > Mock. Put each new double in the test project's `TestDoubles/` folder, one class per file, in the namespace `{TestProject}.TestDoubles`. The names suggested below are suggestions. Reuse an existing double where it already fits, for example `RecordingHandlerFactory`, `RecordingDecoratorFactory`, `TrackedDependencyScenario`, `TrackedDependency` or `DependencyTracker`.
- **Teardown log isolation (ADR 0022 step 2).** `PipelineBuilder<TResult>` and the lifetime log through one static logger per closed `TResult`. Every test that asserts a teardown log entry must therefore give its query **its own result type**.
  - In `test/Paramore.Darker.Core.Tests`, assert through `test/Paramore.Darker.Core.Tests/Logging/LoggerCaptureFixture.cs`.
  - In `test/Paramore.Darker.Extensions.Tests`, assert through `test/Paramore.Darker.Extensions.Tests/Logging/LoggerCaptureFixture.cs`. Register a capturing provider into the container's logging. Put the test in the `DarkerHostBootstrap` collection (`test/Paramore.Darker.Extensions.Tests/Logging/DarkerHostBootstrapCollection.cs`), because `AddDarker` resets the process-global `ApplicationLogging.LoggerFactory`.
  - Adoption Warning tests (Phases 3–4) follow the same rule. Use a fresh `CapturingLoggerProvider` per test so that counting Warnings is exact.
- **Naming.** Name test files and methods `When_[condition]_should_[behaviour]`. Name test classes `[Behavior]Tests`. Write Arrange/Act/Assert comments.
- **CHARACTERISE tasks.** Observe RED by applying the named mutation to **production** code. The test must fail on the assertion it exists for. Revert, confirm green, run the full suite, and commit the test alone as `test:`, with the mutation noted in the message.
- **Commits.** Each behavioural task produces one `feat:`/`test:` commit, followed by a separate `docs:` commit that ticks the box.

---

## Phase 1 — Robust teardown (ADR 0022)

### 1A — Tidy first (ADR 0022 step 1)

- [x] **STRUCTURAL: T1.1 — Extract the release steps and the lifetime loop body into private methods**
  - In `src/Paramore.Darker/PipelineBuilder.cs`, `Dispose()` (currently around lines 424–460): extract the handler release, and each of the three decorator-list releases (sync, async and stream), into private methods. Do not change behaviour.
  - In `src/Paramore.Darker/QueryLifetimeScope.cs`, `Dispose()` (lines 49–60): extract the per-item disposal into a private method.
  - Guarded by the existing suite. In particular:
    - `test/Paramore.Darker.Core.Tests/QueryProcessorTests.cs` and `QueryProcessorAsyncTests.cs`;
    - `When_stream_consumer_breaks_early_should_release_pipeline_and_end_span_once.cs`;
    - `test/Paramore.Darker.Core.Tests/Decorators/FallbackPolicyTests.cs` (decorator release counts);
    - `test/Paramore.Darker.Extensions.Tests/When_pipeline_fails_should_dispose_dependency.cs` and the other `When_handler_lifetime_is_*` tests.
  - The suite is green before and after. No test changes.

### 1B — Lifetime fault isolation (ADR 0022 step 2, FR4)

- [x] **TEST + IMPLEMENT: T1.2 — A tracked lifetime item that throws on Dispose does not stop the other tracked items being disposed, and is logged at Error**
  - **USE COMMAND**: `/test-first when a tracked lifetime item throws on dispose should dispose the other tracked items and log the fault at Error with the item type`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_a_tracked_lifetime_item_throws_on_dispose_should_dispose_the_others_and_log_error.cs`
  - Test should verify:
    - The query runs on `Execute`. The handler factory is a new test double, `LifetimeAttachingHandlerFactory`. It delegates creation and release to `SimpleHandlerFactory` semantics, and in `Create` it attaches three `RecordingDisposable`s through the public `IAmALifetime.Add`. The middle one throws from `Dispose`.
    - Both non-throwing recorders are disposed, newest first.
    - The query returns its result.
    - Exactly one Error entry is captured. It carries the thrown exception and names the throwing item's type.
    - The query uses a result type of its own, for example `LifetimeIsolationResult`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Give `QueryLifetimeScope` a constructor that takes an `ILogger`. Pass `PipelineBuilder<TResult>`'s static `_logger` at each `new QueryLifetimeScope` (`PipelineBuilder.cs` lines ~62, ~123, ~277).
    - Wrap each tracked item's `Dispose` in its own try/catch, inside the private method extracted in T1.1. Log at Error with `item.GetType()`.
    - Keep the existing `_disposed` flag.

- [x] **CHARACTERISE: T1.3 — Disposing the lifetime a second time does nothing**
  - **USE COMMAND**: `/test-first when the lifetime is disposed twice should dispose each tracked item only once`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_lifetime_is_disposed_twice_should_dispose_tracked_items_once.cs`
  - Test should verify:
    - A test handler factory attaches a `RecordingDisposable` through `lifetime.Add`. In `Release` it calls `lifetime.Dispose()` itself. The pipeline builder then disposes the lifetime again, as its last step.
    - The recorder's dispose count is exactly 1.
    - The query returns its result.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `QueryLifetimeScope.Dispose`, delete the `if (_disposed) return;` guard **and** the `_trackedDisposables.Clear()` call that follows the loop. Either one alone keeps a second call from disposing again, so both must go. The test must fail on `disposeCount.ShouldBe(1)`, because the actual value is 2.
  - Implementation should:
    - Need no production change. The `_disposed` flag kept in T1.2 already delivers this (FR4 idempotency).

- [x] **CHARACTERISE: T1.4 — On Execute, a Scoped service that implements only IAsyncDisposable is logged at Error and the query still returns (ADR 0022 row 7; risk mitigation)**
  - **USE COMMAND**: `/test-first when Execute tears down a scope holding an IAsyncDisposable-only service should log the container fault at Error and return the result`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_sync_teardown_meets_async_only_disposable_should_log_error_and_return_result.cs`
  - Test should verify:
    - The container is built with `AddDarker` and `HandlerLifetime = Scoped`. The handler depends on a Scoped `AsyncOnlyDisposableDependency` (new test double) that implements only `IAsyncDisposable`.
    - `Execute` returns the handler's result.
    - One Error entry is captured. Its exception is an `InvalidOperationException` whose message names `AsyncOnlyDisposableDependency`.
    - The test is in the `DarkerHostBootstrap` collection and uses its own result type.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `QueryLifetimeScope`, remove the per-item try/catch added in T1.2. The test must fail on the assertion that `Execute` returns the result (`Should.NotThrow`), because the `InvalidOperationException` escapes.
  - Implementation should:
    - Need no production change. This pins the release-notes statement that such services need `ExecuteAsync`.

### 1C — Sync teardown isolation (ADR 0022 step 3, FR1–FR3 on `Execute`)

- [x] **TEST + IMPLEMENT: T1.5 — When the handler's Dispose throws (Simple factory), the decorators are still released, the lifetime is still disposed and the query returns its result**
  - **USE COMMAND**: `/test-first when the handler's dispose throws on Execute should still release the decorators, dispose the lifetime, return the result and log at Error`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_handler_dispose_throws_should_release_decorators_and_dispose_lifetime.cs`
  - Test should verify:
    - The handler is a `ThrowingDisposeQueryHandler` (new). It is created by a handler factory with Simple release semantics, and that factory attaches a `RecordingDisposable` through `lifetime.Add`. The handler is decorated by two decorators from a `RecordingDecoratorFactory`.
    - `Execute` returns the handler's result.
    - `RecordingDecoratorFactory.Released` contains both decorators.
    - The lifetime's recorder is disposed.
    - One Error entry is captured with the handler's type and the thrown exception. The query has its own result type.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - In `PipelineBuilder.Dispose`, wrap each release (the handler, then each decorator in creation order) in its own try/catch. Log at Error with `component.GetType()`.
    - Move `_lifetime?.Dispose()` outside every release's try/catch, as the final statement.

- [x] **CHARACTERISE: T1.6 — A handler that throws plus a release that throws: the caller sees the handler's exception**
  - **USE COMMAND**: `/test-first when the handler throws and its release also throws on Execute should surface the handler's original exception`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_handler_throws_and_release_throws_should_surface_handler_exception.cs`
  - Test should verify:
    - The handler throws a distinctive `HandlerFaultException` (new) from `Execute`, and throws a different exception from `Dispose`.
    - `Execute` throws `HandlerFaultException`, the same instance.
    - The release fault is logged at Error.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in the handler-release catch in `PipelineBuilder.Dispose`, add `throw;` after logging. The test must fail on `Should.Throw<HandlerFaultException>`, because the caller sees the dispose exception.
  - Implementation should:
    - Need no production change. T1.5 delivers this.

- [x] **CHARACTERISE: T1.7 — When every release throws, each fault is logged once, the lifetime is disposed and the result returns (ADR 0022 row 3)**
  - **USE COMMAND**: `/test-first when every release throws on Execute should log one Error per fault, dispose the lifetime and return the result`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_every_release_throws_should_log_each_fault_and_dispose_lifetime.cs`
  - Test should verify:
    - The handler and two decorators all throw from `Dispose`. The handler and decorator test doubles are new.
    - `Execute` returns the result.
    - Exactly three Error entries are captured, one per component type.
    - The lifetime's `RecordingDisposable` is disposed.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `PipelineBuilder.Dispose`, replace the per-decorator try/catch with one try/catch around the whole decorator loop. The test must fail on the Error-count assertion: 2 instead of 3, because the second decorator is never released.
  - Implementation should:
    - Need no production change. T1.5 delivers this.

- [x] **CHARACTERISE: T1.8 — With the DI factory, a Scoped service whose Dispose throws inside the per-query scope: the query returns its result and the fault is logged at Error**
  - **USE COMMAND**: `/test-first when a scoped service throws on dispose inside the per-query scope should return the result and log the fault at Error`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_scoped_service_dispose_throws_should_return_result_and_log_error.cs`
  - Test should verify:
    - `AddDarker` uses `HandlerLifetime = Scoped`. The handler depends on a Scoped `ThrowingDisposeDependency` (new).
    - `Execute` returns the result.
    - One Error entry carries the dependency's exception.
    - The test is in the `DarkerHostBootstrap` collection and uses its own result type.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: remove the per-item try/catch in `QueryLifetimeScope.Dispose`. The test must fail on `Should.NotThrow` / the result assertion.
  - Implementation should:
    - Need no production change. T1.2 delivers this, because the child scope is a tracked item.

- [x] **TEST + IMPLEMENT: T1.9 — A throwing logging provider cannot break the never-throw guarantee (ADR 0022 risk mitigation: guarded log call)**
  - **USE COMMAND**: `/test-first when the logging provider throws while teardown logs a fault should still return the result`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_logging_provider_throws_during_teardown_should_still_return_result.cs`
  - Test should verify:
    - **Lifetime fault.** The container registers a `ThrowingLoggerProvider` (new) whose logger throws only at `LogLevel.Error`. The handler (Scoped) depends on `ThrowingDisposeDependency`. `Execute` returns the result.
    - **Release fault.** Inside the serialised `DarkerHostBootstrap` collection, swap `ApplicationLogging.LoggerFactory` to the throwing factory and restore it in a `finally`. A processor built with `QueryProcessorBuilder` and a Simple handler factory has a handler whose `Dispose` throws. `Execute` returns the result.
    - Each fact uses a result type of its own, so the static per-`TResult` logger is created against the throwing factory.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Guard the log call inside every teardown catch, in `PipelineBuilder` and `QueryLifetimeScope`, with its own try/catch. Use one small private helper per class, for example `TryLogTeardownFault`.

### 1D — Partial builds (ADR 0022 step 4)

- [x] **TEST + IMPLEMENT: T1.10 — A decorator whose creation or initialisation throws: the caller sees that exception, and the decorators created before it are released**
  - **USE COMMAND**: `/test-first when a decorator's creation or initialisation throws should surface that exception and release the decorators created before it on Execute, ExecuteAsync and ExecuteStream`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_decorator_initialisation_throws_should_release_decorators_created_before_it.cs`
  - Test should verify:
    - **Execute.** The handler has two decorator attributes. The second decorator's `InitializeFromAttributeParams` throws (new `ThrowingInitDecorator` and attribute). The caller sees that exception. `RecordingDecoratorFactory.Released` contains the first decorator, and the second decorator was released too, because it was created. The handler is released.
    - **ExecuteAsync** and **ExecuteStream.** The same, with async and stream decorator doubles.
    - **Creation throws.** One fact has the decorator factory's `Create` throw for the second decorator. The first decorator is still released.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - In `GetDecorators` (`PipelineBuilder.cs` ~241–271), `GetDecoratorsAsync` (~388–422) and `GetStreamDecorators` (~326–365): assign the decorator list to its field before the loop. Add each decorator to the list immediately after `Create`, before `Context` is set and before `InitializeFromAttributeParams` runs.

### 1E — Async seams (ADR 0022 step 5)

- [x] **STRUCTURAL: T1.11 — Add IAsyncDisposable to IAmALifetime and ReleaseAsync beside Release on the async factory interfaces**
  - `src/Paramore.Darker/IAmALifetime.cs`: extend `IAsyncDisposable`. `Microsoft.Bcl.AsyncInterfaces` supplies it on netstandard2.0.
  - `QueryLifetimeScope`: implement `DisposeAsync` as a temporary forward to `Dispose`. T1.12 replaces it.
  - `src/Paramore.Darker/IQueryHandlerFactoryAsync.cs` and `IQueryHandlerDecoratorFactoryAsync.cs`: add `ValueTask ReleaseAsync(...)` beside `Release`.
  - Implement `ReleaseAsync` in:
    - `src/Paramore.Darker/SimpleHandlerFactory.cs` and `SimpleHandlerDecoratorFactory.cs`: await `DisposeAsync` when the component is `IAsyncDisposable`, otherwise call `Dispose`;
    - `src/Paramore.Darker.Extensions.DependencyInjection/ServiceProviderComponentFactory.cs`: return `default` (a no-op);
    - `test/Paramore.Darker.Core.Tests/TestDoubles/RecordingHandlerFactory.cs` and `RecordingDecoratorFactory.cs`: record into the same `Released` list.
  - Nothing calls the new members yet. Guarded by the full suite, green before and after.

### 1F — Async lifetime and async teardown (ADR 0022 steps 6–7, FR5, FR5a)

> ADR 0022 step 6 (async lifetime) has no behaviour that is observable through the public surface until step 7 wires `DisposeAsync` into the processor. T1.12 therefore drives both steps from the first async test.

- [x] **TEST + IMPLEMENT: T1.12 — On ExecuteAsync and ExecuteStream, a Scoped dependency that implements only IAsyncDisposable is disposed, and its DisposeAsync is awaited**
  - **USE COMMAND**: `/test-first when ExecuteAsync or ExecuteStream completes with a scoped IAsyncDisposable-only dependency should await its DisposeAsync`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_async_query_completes_with_async_only_disposable_should_await_dispose_async.cs`
  - Test should verify:
    - `AddDarker` uses `HandlerLifetime = Scoped`. The async handler and the stream handler (new doubles) depend on a Scoped `AsyncOnlyDisposableDependency`. Its `DisposeAsync` completes only after an awaited `Task.Yield()`, and then sets `DisposedAsync = true`.
    - After `await ExecuteAsync(...)` returns, `DisposedAsync` is true.
    - After `await foreach` over `ExecuteStream(...)` completes, `DisposedAsync` is true.
    - No Error is logged.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Implement `QueryLifetimeScope.DisposeAsync`: newest first, awaiting `DisposeAsync` for `IAsyncDisposable` items and calling `Dispose` otherwise, sharing the `_disposed` flag with `Dispose`. Per-item fault isolation on this path is T1.13's.
    - In `src/Paramore.Darker.Extensions.DependencyInjection/ServiceProviderLifetimeScope.cs` (~36–42), create the scope with `CreateAsyncScope()` and add `DisposeAsync`.
    - Add `PipelineBuilder<TResult> : IAsyncDisposable`. `DisposeAsync` reuses T1.5's guarded sync release helpers unchanged, then awaits the lifetime's `DisposeAsync` last. It takes no cancellation token. `ReleaseAsync` wiring comes in T1.14 and T1.15; null-tolerance comes in T1.17a.
    - In `src/Paramore.Darker/QueryProcessor.cs`, replace `using` with `await using (pipelineBuilder.ConfigureAwait(false))` in `ExecuteAsync` (~line 104). In the `ExecuteStream` `finally` (~line 168), replace `pipelineBuilder.Dispose()` with `await pipelineBuilder.DisposeAsync().ConfigureAwait(false)`.

- [x] **TEST + IMPLEMENT: T1.13 — On the async paths, a tracked lifetime item whose DisposeAsync faults does not stop the others and is logged at Error**
  - **USE COMMAND**: `/test-first when a tracked lifetime item faults in DisposeAsync on ExecuteAsync or ExecuteStream should dispose the other items and log the fault at Error`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_tracked_item_dispose_async_faults_should_dispose_the_others_and_log_error.cs`
  - Test should verify:
    - `LifetimeAttachingHandlerFactory` (from T1.2) attaches three `RecordingAsyncDisposable`s. The middle one faults from `DisposeAsync`.
    - On `ExecuteAsync` and on `ExecuteStream`, the other two are disposed asynchronously and the result or items are returned.
    - One Error entry per path names the faulting type.
    - Each fact uses its own result type.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - In `QueryLifetimeScope.DisposeAsync`, wrap each item's disposal in its own fault boundary and log at Error with the item type through the guarded helper from T1.9 (`TryLogTeardownFault`).

- [x] **CHARACTERISE: T1.13a — With the DI factory on ExecuteAsync and ExecuteStream, a Scoped service whose DisposeAsync faults inside the per-query scope: the result returns and the fault is logged at Error**
  - **USE COMMAND**: `/test-first when a scoped service faults in DisposeAsync inside the per-query scope on ExecuteAsync or ExecuteStream should return the result and log the fault at Error`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_scoped_service_dispose_async_faults_should_return_result_and_log_error.cs`
  - Test should verify:
    - `AddDarker` uses `HandlerLifetime = Scoped`. The async and stream handlers depend on a Scoped `FaultingAsyncDisposeDependency` (new) whose `DisposeAsync` faults.
    - `ExecuteAsync` returns the result, and `await foreach` over `ExecuteStream` yields every item.
    - One Error entry per path carries the dependency's exception.
    - The test is in the `DarkerHostBootstrap` collection, and each fact uses its own result type.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `QueryLifetimeScope.DisposeAsync`, remove the per-item try/catch added in T1.13. The test must fail on `Should.NotThrowAsync` / the result assertion.
  - Implementation should:
    - Need no production change. T1.13 delivers this, because the child scope is a tracked item. This completes the DI teardown-fault acceptance criterion on all three paths (T1.8 is `Execute`).

- [x] **TEST + IMPLEMENT: T1.14 — On ExecuteAsync and ExecuteStream, the handler is released by the factory that created it**
  - **USE COMMAND**: `/test-first when ExecuteAsync or ExecuteStream completes should release the handler through the async factory that created it`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_async_query_completes_should_release_handler_through_creating_factory.cs`
  - Test should verify:
    - The sync and async slots hold two distinct `RecordingHandlerFactory` instances.
    - On `ExecuteAsync`, the async factory's `ReleaseCount(handler)` is 1 and the sync factory's is 0.
    - On `ExecuteStream`, with an async factory configured, the same holds.
    - On `ExecuteStream` with no async factory, the sync factory releases its handler.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Record the creating factory in `ResolveHandlerAsync` (`PipelineBuilder.cs` ~218–239) and `ResolveStreamHandler` (~367–386).
    - In `DisposeAsync`, release through that factory: `await ReleaseAsync` for an async factory, `Release` for the sync fallback. Keep the awaited release inside the same per-release fault boundary, with the guarded Error log, that T1.5 gave the sync release. Changing what is called must not remove the isolation.
    - Update `test/Paramore.Darker.Core.Tests/QueryProcessorAsyncTests.cs` lines ~24–29 (the comment) and ~61 (`_handlerFactoryAsync.ReleaseCount(handler).ShouldBe(0)`) to assert release through the async factory.

- [x] **TEST + IMPLEMENT: T1.15 — On the async paths, the Simple factories await DisposeAsync on an IAsyncDisposable handler and decorator**
  - **USE COMMAND**: `/test-first when ExecuteAsync or ExecuteStream completes with IAsyncDisposable handler and decorator created by the Simple factories should await their DisposeAsync`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_async_query_completes_should_await_dispose_async_on_simple_factory_components.cs`
  - Test should verify:
    - The processor is built with `SimpleHandlerFactory` and `SimpleHandlerDecoratorFactory`. The handler and decorator (new doubles) implement both `IDisposable` and `IAsyncDisposable`, and record which one was called.
    - On `ExecuteAsync` and on `ExecuteStream`, both the handler and the decorator record `DisposeAsync`, not `Dispose`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - In `PipelineBuilder.DisposeAsync`, release `_asyncDecorators` and `_streamDecorators` through `await _decoratorFactoryAsync.ReleaseAsync(...)` when an async decorator factory is configured. Fall back to the sync `Release` otherwise. Keep each awaited release in its own fault boundary with the guarded Error log, as in T1.14.

- [x] **CHARACTERISE: T1.16 — Async teardown isolates each release: a faulting ReleaseAsync does not stop the other releases or the lifetime disposal, and the result returns**
  - **USE COMMAND**: `/test-first when a release faults during async teardown should still release the other components, dispose the lifetime, return the result and log each fault at Error`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_release_faults_during_async_teardown_should_release_others_and_dispose_lifetime.cs`
  - Test should verify:
    - On `ExecuteAsync`, the handler's `DisposeAsync` faults. Both decorators are still released, the lifetime's recorder is disposed, the result returns, and one Error names the handler type.
    - On `ExecuteAsync`, when every component faults, there is one Error per component and the lifetime is still disposed.
    - On `ExecuteStream`, when the handler faults, every item is yielded and the decorators and lifetime are torn down.
    - Each fact uses its own result type.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `PipelineBuilder.DisposeAsync`, remove the try/catch around the awaited handler release. The test must fail on the assertions that both decorators were released and the lifetime's recorder was disposed.
  - Implementation should:
    - Need no production change. T1.12, T1.14 and T1.15 deliver this.

- [x] **CHARACTERISE: T1.17 — On the async paths, a handler that throws plus a release that faults: the caller sees the handler's exception**
  - **USE COMMAND**: `/test-first when the handler throws and its release faults on ExecuteAsync or ExecuteStream should surface the handler's original exception`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_async_handler_throws_and_release_faults_should_surface_handler_exception.cs`
  - Test should verify:
    - On `ExecuteAsync`, the caller sees the handler's `HandlerFaultException`.
    - On `ExecuteStream`, a handler that throws during enumeration is seen by the caller as that exception, while its `DisposeAsync` faults.
    - The release fault is logged at Error.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in the handler-release catch of `PipelineBuilder.DisposeAsync`, add `throw;` after logging. The test must fail on `Should.ThrowAsync<HandlerFaultException>`.
  - Implementation should:
    - Need no production change. T1.14 delivers this.

- [x] **CHARACTERISE: T1.16a — On the async paths, a throwing logging provider cannot break the never-throw guarantee (ADR 0022 risk mitigation: guarded log call)**
  - **USE COMMAND**: `/test-first when the logging provider throws while async teardown logs a fault should still return the result on ExecuteAsync and ExecuteStream`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_logging_provider_throws_during_async_teardown_should_still_return_result.cs`
  - Test should verify:
    - **Lifetime fault.** As T1.9, with `ThrowingLoggerProvider` and a Scoped `FaultingAsyncDisposeDependency` (T1.13a). `ExecuteAsync` returns the result and `ExecuteStream` yields every item.
    - **Release fault.** As T1.9's release fact, swapping `ApplicationLogging.LoggerFactory` inside the `DarkerHostBootstrap` collection, with a Simple-factory handler whose `DisposeAsync` faults. Both async paths return.
    - Each fact uses a result type of its own.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `PipelineBuilder.DisposeAsync`, log the release fault directly instead of through `TryLogTeardownFault`. The release-fault facts must fail on `Should.NotThrowAsync`.
  - Implementation should:
    - Need no production change. T1.13, T1.14 and T1.15 route every async teardown log through the T1.9 helper.

- [x] **TEST + IMPLEMENT: T1.17a — Teardown skips the releases when no component was created, and tolerates a builder that never built (ADR 0022 row 1)**
  - **USE COMMAND**: `/test-first when the query context cannot be created or the handler lookup fails should surface that exception, never release a null handler and not throw from teardown on Execute, ExecuteAsync and ExecuteStream`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_no_component_was_created_should_skip_releases_and_surface_build_exception.cs`
  - Test should verify:
    - **The build never starts.** A `ThrowingQueryContextFactory` (new) throws a distinctive exception from `Create`. On each path, the caller sees that exact exception (on `ExecuteStream`, at the first `MoveNextAsync`), not a `NullReferenceException`.
    - **The handler lookup fails.** A query with no registered handler surfaces the existing `ConfigurationException` (or `RoutingException`) on each path.
    - In both cases the `RecordingHandlerFactory` instances (sync and async) record no `Release`/`ReleaseAsync` call at all, null or otherwise.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - In `PipelineBuilder.Dispose` and `DisposeAsync`, release the handler only when `_handler` is not null, and dispose the lifetime only when one exists (`_lifetime?`). Today the sync path calls `Release` with a null handler.
    - Keep the decorator releases as they are: the lists are empty when nothing was created.

### 1G — Remove the old async Release (ADR 0022 step 8)

- [x] **STRUCTURAL: T1.18 — Remove Release from IQueryHandlerFactoryAsync and IQueryHandlerDecoratorFactoryAsync**
  - Delete `Release` from both async interfaces, and from the async role of:
    - `SimpleHandlerFactory` and `SimpleHandlerDecoratorFactory`;
    - `ServiceProviderComponentFactory`;
    - `RecordingHandlerFactory` and `RecordingDecoratorFactory`. Their sync `Release` stays.
  - `PipelineBuilder` no longer calls `_decoratorFactoryAsync.Release`. `FactoryFuncWrapper` is sync-only and unchanged.
  - Guarded by T1.12–T1.17, `QueryProcessorAsyncTests.cs`, `FallbackPolicyTests.cs` and the full suite, green before and after.

### 1H — Documentation and checkpoint (ADR 0022 step 9)

- [x] **DOC: T1.19 — XML documentation for the teardown contract, and a release-notes check for ADR 0022**
  - On `IAmALifetime`, document that `Dispose`/`DisposeAsync`:
    - never throw;
    - dispose each item in its own fault boundary and log each fault at Error;
    - are idempotent across both forms.
  - On `IQueryHandlerFactoryAsync.ReleaseAsync` and `IQueryHandlerDecoratorFactoryAsync.ReleaseAsync`, document that:
    - the factory decides whether release means disposal;
    - the pipeline contains and logs faults;
    - the methods take no cancellation token.
  - Confirm that the `release_notes.md` spec 015 section (`<!-- spec: 015-di_lifetime_conformance -->`) states:
    - the `ReleaseAsync` and `IAsyncDisposable` breaking change;
    - the never-throw teardown;
    - that IAsyncDisposable-only services on `Execute` need `ExecuteAsync`;
    - release through the creating factory;
    - Simple factories awaiting `DisposeAsync`;
    - partial-build releases.
  - Use `/spec:write_release_notes` if any of these needs correcting.

- [x] **VERIFY: T1.20 — Phase 1 checkpoint**
  - Run `dotnet build Darker.Filter.slnf -c Release` with no new warnings.
  - Run `dotnet test Darker.Filter.slnf -c Release --no-build`, green on net8.0 and net9.0.
  - Confirm that the existing cancellation and failure lifetime tests in `test/Paramore.Darker.Extensions.Tests` pass unchanged, for example `When_pipeline_fails_should_dispose_dependency.cs` (teardown takes no cancellation token).

---

## Phase 2 — Pipeline scope handle (ADR 0023)

### 2A — Structural seams (ADR 0023 steps 1–3)

- [x] **STRUCTURAL: T2.1 — Add the IAmAScope handle**
  - Add `src/Paramore.Darker/IAmAScope.cs`: `public interface IAmAScope : IDisposable, IAsyncDisposable { }`.
  - Make `ServiceProviderLifetimeScope` implement it. After T1.12 it is already sync- and async-disposable.
  - Nothing uses the interface yet. Guarded by the full suite.

- [x] **STRUCTURAL: T2.2 — Add CreatePipelineScope() to the handler factory interfaces, returning null everywhere for now**
  - Add `IAmAScope? CreatePipelineScope()` to `src/Paramore.Darker/IQueryHandlerFactory.cs` and `IQueryHandlerFactoryAsync.cs`. Use the `#nullable enable annotations` pattern of `IQueryContext.cs`.
  - Return null from:
    - `SimpleHandlerFactory`;
    - `src/Paramore.Darker/Builder/FactoryFuncWrapper.cs`;
    - `RecordingHandlerFactory`;
    - for now, `ServiceProviderComponentFactory`;
    - any other test implementer, for example the factory used in `When_executing_sync_query_with_tracer_should_create_and_end_query_span.cs`.
  - The pipeline builder does not call the member yet. Guarded by the full suite.

- [x] **STRUCTURAL: T2.3 — Choose the handler factory once per build**
  - In `PipelineBuilder`, extract into one decision at the start of `Build`, `BuildAsync` and `BuildStream` the inline choice that `ResolveHandlerAsync` and `ResolveStreamHandler` make between the async and sync handler factory. Reuse the "creating factory" record from T1.14.
  - Behaviour is unchanged. Guarded by T1.14, `QueryProcessorAsyncTests.cs`, `When_query_processor_has_both_sync_and_async_handlers_should_dispatch_to_correct_pipeline.cs` and the stream tests.

### 2B — The lifetime carries the handle (ADR 0023 step 4, FR7, FR8, FR12)

- [x] **TEST + IMPLEMENT: T2.4 — The handler factory's handle is the PipelineScope that every handler and decorator Create receives**
  - **USE COMMAND**: `/test-first when the handler factory supplies a pipeline scope should pass a lifetime carrying that handle to the handler and every decorator Create on Execute, ExecuteAsync and ExecuteStream`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_handler_factory_supplies_pipeline_scope_should_carry_it_on_the_lifetime.cs`
  - Test should verify:
    - A new `ScopeSupplyingHandlerFactory` returns a fresh `RecordingScope` (new `IAmAScope` double) from each `CreatePipelineScope()` and records the lifetime passed to `Create`. A new `LifetimeRecordingDecoratorFactory` records each lifetime it receives.
    - On each of the three paths, `CreatePipelineScope()` is called exactly once. The handler's and every decorator's lifetime is the same instance, and its `PipelineScope` is the returned handle.
    - With `SimpleHandlerFactory` paired with `LifetimeRecordingDecoratorFactory`, the decorator factory sees `PipelineScope == null` (FR12). `SimpleHandlerDecoratorFactory`'s delegate never sees the lifetime, so it cannot observe this.
    - With `SimpleHandlerFactory` and `SimpleHandlerDecoratorFactory` together, the query still runs on each path (FR12).
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add `IAmAScope? PipelineScope { get; }` to `IAmALifetime`.
    - Give `QueryLifetimeScope` a constructor `(IAmAScope? pipelineScope, ILogger logger)` that only assigns.
    - In `Build`, `BuildAsync` and `BuildStream`, create the lifetime as `new QueryLifetimeScope(chosenFactory.CreatePipelineScope(), _logger)` in one expression (ADR 0023 risk mitigation: the handle can never escape).
    - Do not dispose the handle yet. T2.5 adds that.

- [ ] **TEST + IMPLEMENT: T2.5 — The pipeline scope is disposed exactly once, after every release**
  - **USE COMMAND**: `/test-first when a query completes should dispose the pipeline scope handle once after the handler and every decorator are released on Execute, ExecuteAsync and ExecuteStream`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_query_completes_should_dispose_pipeline_scope_once_after_every_release.cs`
  - Test should verify:
    - A shared `TeardownEventLog` (new) records the release events from the factories and the handle's `Dispose`/`DisposeAsync`.
    - On `Execute`, the handle's `Dispose` is the last event and occurs exactly once.
    - On `ExecuteAsync` and `ExecuteStream`, `DisposeAsync` is the last event and occurs exactly once, and `Dispose` is never called.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - In `QueryLifetimeScope.Dispose`/`DisposeAsync`, dispose `PipelineScope`, after any remaining tracked items, through the same per-item fault-boundary method from T1.2 (sync) and T1.13 (async), with its guarded Error log.
    - Do nothing when `PipelineScope` is null.

- [ ] **CHARACTERISE: T2.6 — A pipeline scope whose disposal throws is logged at Error, and the query still returns**
  - **USE COMMAND**: `/test-first when the pipeline scope handle throws on dispose should log the fault at Error and still return the result on Execute, ExecuteAsync and ExecuteStream`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_pipeline_scope_dispose_throws_should_log_error_and_return_result.cs`
  - Test should verify:
    - A `ThrowingScope` (new `IAmAScope`) throws from `Dispose` and faults from `DisposeAsync`.
    - On each path the result or items return, and one Error entry names `ThrowingScope`.
    - Each fact uses its own result type.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `QueryLifetimeScope`, dispose `PipelineScope` directly, outside the fault-boundary helper. The test must fail on `Should.NotThrow` / the result assertion.
  - Implementation should:
    - Need no production change. T2.5 delivers this.

- [ ] **CHARACTERISE: T2.7 — Each path asks the factory that will create the handler for the pipeline scope**
  - **USE COMMAND**: `/test-first when BuildAsync has no async registry or BuildStream has no async factory should ask the sync handler factory for the pipeline scope`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_async_path_falls_back_to_sync_factory_should_ask_it_for_the_pipeline_scope.cs`
  - Test should verify:
    - **ExecuteAsync.** An async handler factory is configured, but there is no async registry and the handler is registered in the sync registry. The sync `ScopeSupplyingHandlerFactory` is asked once, and the async one is asked zero times.
    - **ExecuteStream.** There is no async factory. The sync factory is asked once.
    - **ExecuteAsync.** With both the async registry and the async factory configured, only the async factory is asked.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `BuildAsync`, ask `(_handlerFactoryAsync ?? _handlerFactory).CreatePipelineScope()` instead of the chosen factory. The test must fail on the assertion that the async factory's `CreatePipelineScope` count is 0.
  - Implementation should:
    - Need no production change. T2.3 and T2.4 deliver this.

- [ ] **CHARACTERISE: T2.8 — If CreatePipelineScope throws, the caller sees that exception (ADR 0023 row 5)**
  - **USE COMMAND**: `/test-first when the handler factory's CreatePipelineScope throws should surface that exception on Execute, ExecuteAsync and ExecuteStream`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_create_pipeline_scope_throws_should_surface_that_exception.cs`
  - Test should verify:
    - A `ThrowingScopeHandlerFactory` (new) throws a distinctive exception from `CreatePipelineScope`.
    - Each path surfaces that exact exception: on `ExecuteStream`, at the first `MoveNextAsync`.
    - `Create` is never called, and neither is `Release`/`ReleaseAsync` (ADR 0022 row 1).
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `PipelineBuilder.Dispose`/`DisposeAsync`, change the null-tolerant lifetime disposal from T1.17a (`_lifetime?.Dispose()` / `_lifetime?.DisposeAsync()`) to an unconditional call. The test must fail on `Should.Throw<TheDistinctiveException>`, because a `NullReferenceException` from teardown replaces it.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T2.9 — A build that fails after the scope exists still disposes the scope (ADR 0023 row 6; risk mitigation: a handle never escapes ownership)**
  - **USE COMMAND**: `/test-first when the handler lookup fails after the pipeline scope is created should dispose the scope and surface the lookup exception`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: `When_handler_lookup_fails_after_scope_created_should_dispose_the_scope.cs`
  - Test should verify:
    - A query with no registered handler runs through `ScopeSupplyingHandlerFactory`.
    - The caller sees the existing `ConfigurationException` (or `RoutingException`).
    - The factory created exactly one `RecordingScope`, and it is disposed exactly once.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `Build`, split the expression into `var scope = chosenFactory.CreatePipelineScope();`, then `ResolveHandler(...)`, then `_lifetime = new QueryLifetimeScope(scope, _logger);`. The test must fail on `createdScope.DisposeCount.ShouldBe(1)`, because the actual value is 0.
  - Implementation should:
    - Need no production change.

### 2C — Migrate the ADR 0022 tests to the recording handle (ADR 0023 step 4)

- [ ] **CHARACTERISE: T2.10 — Move the Phase 1 teardown tests from lifetime.Add to a recording IAmAScope handle, and prove each migrated test can still fail**
  - **USE COMMAND**: `/test-first characterise the Phase 1 teardown tests against a recording pipeline scope handle instead of lifetime.Add`
  - Test location: "test/Paramore.Darker.Core.Tests"
  - Test file: the migrated files below. No new behaviour file.
  - Test should verify:
    - `LifetimeAttachingHandlerFactory` (T1.2) becomes a factory that returns a `RecordingScope` from `CreatePipelineScope()` instead of calling `lifetime.Add`.
    - These tests now assert that the recording handle is disposed, where they asserted a tracked recorder: T1.3 (`When_lifetime_is_disposed_twice_should_dispose_tracked_items_once.cs`, now "the handle is disposed once when the lifetime is disposed twice"), T1.5 (`When_handler_dispose_throws_should_release_decorators_and_dispose_lifetime.cs`), T1.6, T1.7 (`When_every_release_throws_should_log_each_fault_and_dispose_lifetime.cs`) and T1.16 (`When_release_faults_during_async_teardown_should_release_others_and_dispose_lifetime.cs`).
    - The multi-item tests T1.2 (`When_a_tracked_lifetime_item_throws_on_dispose_should_dispose_the_others_and_log_error.cs`) and T1.13 (`When_tracked_item_dispose_async_faults_should_dispose_the_others_and_log_error.cs`) are **deleted**. The lifetime now holds one item, and T2.6 already owns "a faulting handle is logged at Error and the result returns" on every path. Do not rewrite them into duplicates of T2.6.
    - Each migrated file keeps its log-isolating result type.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutations, one per migrated file, applied to production code and reverted:
    - T1.3: delete the `_disposed` guard in `QueryLifetimeScope.Dispose`. It must fail on the handle's `DisposeCount.ShouldBe(1)` (the handle is not cleared, so the guard alone protects it).
    - T1.5: move the lifetime disposal inside the handler-release try in `PipelineBuilder.Dispose`. It must fail on the handle-disposed assertion.
    - T1.6: T1.6's own mutation (`throw;` in the handler-release catch). It must fail on `Should.Throw<HandlerFaultException>`.
    - T1.7: T1.7's own mutation (one try/catch around the decorator loop). It must fail on the Error count.
    - T1.16: T1.16's own mutation. It must fail on the decorators-released and handle-disposed assertions.
  - Implementation should:
    - Need no production change. Record each mutation and its failing assertion in the `test:` commit message. Afterwards no test calls `IAmALifetime.Add`; check with `grep -rn "\.Add(" test` on lifetime usages.

### 2D — The DI factory creates and recognises the scope (ADR 0023 step 5, FR9, FR10, FR11)

> Through `AddDarker` the DI factory's observable behaviour is unchanged. Scoped and Transient components already resolve from a per-query child scope, and FR10's rows 3–4 cannot be reached. The tests below are therefore CHARACTERISE pins written **before** the rewrite. The rewrite itself is T2.13.

- [ ] **CHARACTERISE: T2.11 — With a Scoped or Transient handler lifetime, nothing resolves from the root: a disposable handler is disposed at the end of each query (ADR 0023 row 2)**
  - **USE COMMAND**: `/test-first when handler lifetime is scoped or transient should dispose a disposable handler at the end of each query on Execute, ExecuteAsync and ExecuteStream`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_handler_lifetime_is_scoped_or_transient_should_dispose_disposable_handler_each_query.cs`
  - Test should verify:
    - `AddDarker` uses `HandlerLifetime` Scoped (theory data: Scoped, Transient). `DisposableTrackedHandler` (new sync, async and stream variants) records each instance's `IsDisposed`.
    - After two queries on each path, there are two distinct handler instances and both are disposed.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `ServiceProviderComponentFactory`, resolve Scoped and Transient components from the captured `serviceProvider` instead of the per-query scope. The test must fail on `handler.IsDisposed.ShouldBeTrue()`.
  - Implementation should:
    - Need no production change. This pins the behaviour that T2.13 must preserve.

- [ ] **CHARACTERISE: T2.12 — With a Singleton handler lifetime, the handler is resolved and Darker never disposes it (ADR 0023 row 1)**
  - **USE COMMAND**: `/test-first when handler lifetime is singleton should resolve the same handler each query and never dispose it on Execute, ExecuteAsync and ExecuteStream`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_handler_lifetime_is_singleton_should_resolve_handler_and_never_dispose_it.cs`
  - Test should verify:
    - Over two queries on each path there is one handler instance, and `IsDisposed` is false.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: make both the sync `Release` and `ReleaseAsync` of `ServiceProviderComponentFactory` dispose an `IDisposable` handler. The test must fail on `handler.IsDisposed.ShouldBeFalse()`.
  - Implementation should:
    - Need no production change.

- [ ] **STRUCTURAL: T2.13 — The DI factory creates the scope up front, resolves only from a recognised handle, fails fast, and holds no per-query state**
  - In `src/Paramore.Darker.Extensions.DependencyInjection/ServiceProviderComponentFactory.cs`:
    - implement `CreatePipelineScope()`: null for a Singleton handler lifetime, otherwise `new ServiceProviderLifetimeScope(serviceProvider)`;
    - replace `Resolve` with a lookup of `lifetime.PipelineScope` through one private recognition method. The method tests for `ServiceProviderLifetimeScope` and throws `ConfigurationException` for a null lifetime or handle (row 3, naming the component type and handler lifetime) and for an unrecognised handle (row 4, naming the handle type);
    - delete the `ConditionalWeakTable` field and `CreateScope`, so the factory keeps only its constructor arguments (FR9).
  - The ADR marks step 5 Behavioural. Through the public surface, however, behaviour is preserved. Rows 3–4 have no direct test (requirements, FR10). A reviewer confirms by code review that the guard exists and that no field beyond the constructor arguments remains.
  - Guarded by:
    - T2.11 and T2.12;
    - `When_handler_lifetime_is_scoped_should_share_dependency_across_handler_and_decorator.cs`, `When_handler_lifetime_is_transient_should_create_fresh_and_dispose_after_pipeline.cs`, `When_handler_lifetime_is_singleton_should_reuse_dependency_and_not_dispose.cs`, `When_no_lifetime_configured_should_create_once_per_query_and_dispose.cs`, `When_pipeline_fails_should_dispose_dependency.cs`, `When_query_processor_is_singleton_and_dependency_is_scoped_should_resolve_and_dispose.cs` and `When_two_queries_run_concurrently_should_isolate_scopes.cs`.
  - Green before and after.

- [ ] **VERIFY: T2.14 — The existing scoped-sharing and concurrent-isolation tests still guard the rewritten DI factory (FR11, NFR Concurrency)**
  - Run `When_two_queries_run_concurrently_should_isolate_scopes.cs` and `When_handler_lifetime_is_scoped_should_share_dependency_across_handler_and_decorator.cs` unchanged, green.
  - Prove they still guard the rewrite by applying each mutation to production code, observing the named failure, and reverting:
    - (a) make `CreatePipelineScope()` cache one `ServiceProviderLifetimeScope` in a field and return it on every call. The concurrent test must fail on `ReferenceEquals(queryA.HandlerDependency, queryB.HandlerDependency).ShouldBeFalse()`.
    - (b) make the decorator `Create<T>` resolve from a fresh child scope instead of `lifetime.PipelineScope`. The sharing test must fail on its same-instance assertion.
  - No source change remains. Record both mutations and their failures in the `docs:` tick commit.

- [ ] **CHARACTERISE: T2.15 — On ExecuteStream, the handler and decorator share one scope, and concurrent streams are isolated (NFR Parity for FR11)**
  - **USE COMMAND**: `/test-first when stream queries run with a scoped handler lifetime should share one scoped dependency between handler and decorator and isolate concurrent streams`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_stream_queries_run_with_scoped_lifetime_should_share_within_and_isolate_between_pipelines.cs`
  - Test should verify:
    - The new test doubles are a tracked stream query, handler and stream decorator that each record their `ITrackedDependency`. Within one stream, the handler and decorator dependency are the same instance.
    - Two streams enumerated concurrently get distinct instances. Each is disposed after its own enumeration ends, and not before.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: the cached-scope mutation (a) listed in T2.14. The test must fail on the isolation `ReferenceEquals(...).ShouldBeFalse()` assertion.
  - Implementation should:
    - Need no production change.

### 2E — Remove `Add` (ADR 0023 step 6)

- [ ] **STRUCTURAL: T2.16 — Remove IAmALifetime.Add and the tracked list**
  - Delete `Add` from `src/Paramore.Darker/IAmALifetime.cs`. Delete `Add` and `_trackedDisposables` from `QueryLifetimeScope`. The lifetime now disposes only `PipelineScope`, under the fault-boundary, logging and idempotency rules.
  - Precondition: T2.10 and T2.13 left no caller in production or test code.
  - Guarded by the full suite, green before and after.

### 2F — Documentation and checkpoint (ADR 0023 step 7)

- [ ] **DOC: T2.17 — XML documentation for the pipeline scope handle, and a release-notes check for ADR 0023**
  - Document the following:
    - `IAmAScope`: a disposal-only capability with no resolution members.
    - `IAmALifetime.PipelineScope`: fixed at construction, null when the factory creates no scope.
    - `CreatePipelineScope()` on both handler factory interfaces. It is called once per build on the factory that creates the handler. **Each call must return a distinct handle that is never shared between queries** (ADR 0023 risk mitigation). It returns null when the factory resolves nothing per query.
    - The contract for container-backed factories: throw `ConfigurationException` rather than create a scope.
  - Update the summary on `ServiceProviderComponentFactory` (lines ~30–35) so it no longer mentions the table.
  - Confirm that `release_notes.md` records the `CreatePipelineScope` addition, the removal of `Add` with `PipelineScope` added, and the custom decorator factory contract.

- [ ] **VERIFY: T2.18 — Phase 2 checkpoint**
  - Run `dotnet build Darker.Filter.slnf -c Release` and `dotnet test Darker.Filter.slnf -c Release --no-build`, green on net8.0 and net9.0.
  - Confirm with `grep` that the code contains no `ConditionalWeakTable` and no `IAmALifetime.Add`.

---

## Phase 3 — Ambient scope adoption (ADR 0024)

### 3A — Structural seams and plumbing (ADR 0024 steps 1–3)

- [ ] **STRUCTURAL: T3.1 — Add the core ambient seam**
  - Add `src/Paramore.Darker/IAmAScopeProvider.cs` (`IAmAScope? GetAmbient(ScopeAffinity affinity)`) and `src/Paramore.Darker/ScopeAffinity.cs` (`AlwaysNew = 0`, `JoinAmbient`). Both use `#nullable enable annotations`.
  - Add `src/Paramore.Darker/Exceptions/AmbientScopeSourceException.cs`: sealed, with a constructor `(string message, Exception innerException)`.
  - Nothing uses them yet. Guarded by the full suite.

- [ ] **STRUCTURAL: T3.2 — Widen scope recognition to IAmAServiceProviderScope, and correct the DI package description**
  - Add the public `IAmAServiceProviderScope : IAmAScope { IServiceProvider Services { get; } }` to `src/Paramore.Darker.Extensions.DependencyInjection/`.
  - Make `ServiceProviderLifetimeScope` implement it, exposing the child scope's provider.
  - Change the T2.13 recognition method to test for `IAmAServiceProviderScope` and resolve through `Services`.
  - Change `<Description>` in `Paramore.Darker.Extensions.DependencyInjection.csproj` (line 4) from "ASP.NET Core integration for Darker" to a Microsoft.Extensions.DependencyInjection description.
  - Only owned scopes exist, so behaviour is unchanged. Guarded by T2.11–T2.15 and the full suite.

- [ ] **STRUCTURAL: T3.3 — Configuration plumbing: DefaultScopeAffinity, the affinity registration, AdoptionDiagnostics and AddDarkerScopeProvider**
  - Add `public ScopeAffinity DefaultScopeAffinity { get; set; } = ScopeAffinity.AlwaysNew;` to `src/Paramore.Darker.Extensions.DependencyInjection/DarkerOptions.cs`.
  - Add the internal `ScopeAffinityRegistration`.
  - Add the internal `AdoptionDiagnostics`:
    - It creates its logger at first use.
    - Its latch is a `ConcurrentDictionary`/concurrent set keyed on the reason, set with one atomic `TryAdd` whose result decides whether to log (ADR 0024 risk mitigation).
  - In `AddDarker` (`ServiceCollectionExtensions.cs` ~13–32), register it with `TryAddSingleton`.
  - Add `AddDarkerScopeProvider<TProvider>(this IServiceCollection, ScopeAffinity affinity = JoinAmbient)`. It registers `TProvider` as the **singleton** `IAmAScopeProvider` (risk mitigation: no Scoped provider resolved from the root) and the affinity as a `ScopeAffinityRegistration`, replacing any earlier registration of either (`Replace`/`RemoveAll` + add). Its null guard is T3.8a's; the replacement behaviour is pinned by T3.8b.
  - In `BuildQueryProcessor` (~34–57), resolve the provider, the registration and `AdoptionDiagnostics`, and compute the effective affinity: the registration overrides the option. Pass all three to the factory constructor. The factory stores them but does not use them yet.
  - Guarded by the full suite, green before and after.

### 3B — Configuration rows 1–4 (ADR 0024 step 4, FR14, FR15)

- [ ] **CHARACTERISE: T3.4 — With AlwaysNew, a registered provider is never asked, so even a throwing provider cannot fail a query**
  - **USE COMMAND**: `/test-first when affinity is AlwaysNew and a throwing scope provider is registered should run the query with an owned scope and never call the provider`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_affinity_is_always_new_should_never_call_the_scope_provider.cs`
  - Test should verify:
    - The container uses `HandlerLifetime = Scoped` and `AddDarkerScopeProvider<ThrowingScopeProvider>(ScopeAffinity.AlwaysNew)`. `ThrowingScopeProvider` is new and counts its calls.
    - `Execute` succeeds. Its Scoped dependency is disposed after the query, so the scope was owned.
    - The provider's call count is 0.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `CreatePipelineScope()`, call `GetAmbient(...)` whenever a provider is registered, whatever the affinity. The test must fail on `provider.CallCount.ShouldBe(0)`, or on `Should.NotThrow`.
  - Implementation should:
    - Need no production change. T3.3 stores the provider but does not call it. T3.9's implementation must keep this green.

- [ ] **TEST + IMPLEMENT: T3.5 — JoinAmbient with a Transient handler lifetime falls back to an owned scope and logs one Warning across many queries**
  - **USE COMMAND**: `/test-first when affinity is JoinAmbient and handler lifetime is transient should use an owned scope and log one Warning that adoption needs a scoped handler lifetime`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_join_ambient_with_transient_handler_lifetime_should_use_owned_scope_and_warn_once.cs`
  - Test should verify:
    - `HandlerLifetime = Transient`, with `AddDarkerScopeProvider<AsyncLocalScopeProvider>()` and an ambient scope set.
    - Over ten queries, each handler dependency is distinct from the ambient instance and is disposed after its query.
    - Exactly one Warning is captured, naming the reason that a Scoped handler lifetime is needed.
    - The test is in the `DarkerHostBootstrap` collection with a per-test capturing provider.
    - The test doubles it introduces, in `TestDoubles/`: `AsyncLocalScopeProvider` (an `AsyncLocal<IAmAScope?>`-backed `IAmAScopeProvider` with a static `Use(scope)` returning an `IDisposable` that restores the previous value) and `RecordingAmbientScope` (an `IAmAServiceProviderScope` over an `IServiceScope` whose `Dispose`/`DisposeAsync` record calls **and** dispose the underlying scope, simulating a third-party adapter).
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - In the `ServiceProviderComponentFactory` constructor, compute `adoptionEnabled` (`JoinAmbient`, Scoped, provider present). Log the configuration Warning once through `AdoptionDiagnostics` when the affinity is `JoinAmbient` but the handler lifetime is not Scoped.
    - Order `CreatePipelineScope()` as: Singleton returns null; adoption disabled returns an owned scope.

- [ ] **CHARACTERISE: T3.6 — JoinAmbient with a Singleton handler lifetime does not adopt and logs one Warning**
  - **USE COMMAND**: `/test-first when affinity is JoinAmbient and handler lifetime is singleton should not adopt and log one Warning`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_join_ambient_with_singleton_handler_lifetime_should_not_adopt_and_warn_once.cs`
  - Test should verify:
    - Over several queries, the Singleton handler's dependency is the root singleton, not the ambient scope's.
    - Exactly one Warning is captured, with the Scoped-lifetime reason.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in the constructor, log the Scoped-lifetime Warning only when `handlerLifetime == ServiceLifetime.Transient`. The test must fail on `warnings.Count.ShouldBe(1)`, because the actual value is 0.
  - Implementation should:
    - Need no production change. T3.5 delivers this.

- [ ] **TEST + IMPLEMENT: T3.7 — JoinAmbient with no scope provider registered falls back to an owned scope and logs one Warning**
  - **USE COMMAND**: `/test-first when affinity is JoinAmbient and no scope provider is registered should use an owned scope and log one Warning that no provider is registered`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_join_ambient_without_scope_provider_should_use_owned_scope_and_warn_once.cs`
  - Test should verify:
    - The configuration is `HandlerLifetime = Scoped` and `DefaultScopeAffinity = JoinAmbient`, with no provider.
    - Over ten queries, each dependency is distinct and disposed after its query.
    - Exactly one Warning names the no-provider reason.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - In the constructor, log the no-provider reason once through `AdoptionDiagnostics` when the affinity is `JoinAmbient`, the lifetime is Scoped and no provider is registered.

- [ ] **CHARACTERISE: T3.8 — A registered affinity overrides DefaultScopeAffinity, whether AddDarkerScopeProvider is called before or after AddDarker**
  - **USE COMMAND**: `/test-first when a scope provider is registered with an affinity before or after AddDarker should use the registered affinity over DefaultScopeAffinity`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_scope_provider_registered_with_affinity_should_override_default_scope_affinity.cs`
  - Test should verify:
    - Theory over call order (before and after `AddDarker`).
    - With `DefaultScopeAffinity = AlwaysNew`, `HandlerLifetime = Transient` and `AddDarkerScopeProvider<AsyncLocalScopeProvider>(JoinAmbient)`, one Scoped-lifetime Warning is logged, so `JoinAmbient` is effective.
    - With `DefaultScopeAffinity = JoinAmbient`, `HandlerLifetime = Transient` and `AddDarkerScopeProvider<...>(AlwaysNew)`, no Warning is logged, so `AlwaysNew` is effective.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `BuildQueryProcessor`, compute the effective affinity from `options.DefaultScopeAffinity` only. The test must fail on the Warning-count assertions.
  - Implementation should:
    - Need no production change. T3.3 delivers this.

- [ ] **TEST + IMPLEMENT: T3.8a — AddDarkerScopeProvider rejects a null service collection with ArgumentNullException naming the parameter**
  - **USE COMMAND**: `/test-first when AddDarkerScopeProvider is called on a null service collection should throw ArgumentNullException naming services`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_scope_provider_registered_on_null_services_should_throw_ArgumentNullException.cs`
  - Test should verify:
    - `((IServiceCollection)null!).AddDarkerScopeProvider<AsyncLocalScopeProvider>()` throws `ArgumentNullException`.
    - Its `ParamName` is `"services"`. Without a guard the call would fail inside a framework helper with a different parameter name, or with `NullReferenceException`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the guard as the first statement of `AddDarkerScopeProvider`, in the style of the file's other extension methods.

- [ ] **CHARACTERISE: T3.8b — A later AddDarkerScopeProvider call replaces an earlier one (ADR 0024 "How the affinity is chosen")**
  - **USE COMMAND**: `/test-first when AddDarkerScopeProvider is called twice should use the later provider and affinity`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_scope_provider_registered_twice_should_use_the_later_registration.cs`
  - Test should verify:
    - First `AddDarkerScopeProvider<ThrowingScopeProvider>(JoinAmbient)`, then `AddDarkerScopeProvider<AsyncLocalScopeProvider>(JoinAmbient)`, with `HandlerLifetime = Scoped` and an ambient scope set.
    - The query adopts the ambient scope: its dependency is the ambient instance, and no `AmbientScopeSourceException` is thrown.
    - The container holds exactly one `IAmAScopeProvider` descriptor and one `ScopeAffinityRegistration` descriptor.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `AddDarkerScopeProvider`, use `TryAddSingleton` for both registrations instead of replacing them. The test must fail on `Should.NotThrow`, because the earlier `ThrowingScopeProvider` wins.
  - Implementation should:
    - Need no production change. T3.3 delivers this.

### 3C — Provider failure and absence (ADR 0024 step 5, FR16)

- [ ] **TEST + IMPLEMENT: T3.9 — A scope provider that throws surfaces as AmbientScopeSourceException wrapping the original exception**
  - **USE COMMAND**: `/test-first when the scope provider throws under JoinAmbient should surface AmbientScopeSourceException wrapping the provider's exception on Execute, ExecuteAsync and ExecuteStream`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_scope_provider_throws_should_surface_AmbientScopeSourceException.cs`
  - Test should verify:
    - The configuration is `HandlerLifetime = Scoped` with `AddDarkerScopeProvider<ThrowingScopeProvider>()`.
    - Each path throws `AmbientScopeSourceException`: on `ExecuteStream`, at the first `MoveNextAsync`.
    - `InnerException` is the provider's exception instance, and the message names `ThrowingScopeProvider`.
    - No handler was created: the `DependencyTracker` construction count is 0.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - When adoption is enabled, call `GetAmbient(ScopeAffinity.JoinAmbient)` once in `CreatePipelineScope()`. Wrap any exception in `AmbientScopeSourceException` naming the provider type.
    - Return an owned scope for anything else, for now.

- [ ] **CHARACTERISE: T3.10 — A provider that offers no scope gives an owned scope and logs nothing (ADR 0024 row 6)**
  - **USE COMMAND**: `/test-first when the scope provider returns no ambient scope should use an owned scope and log no Warning`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_scope_provider_offers_no_scope_should_use_owned_scope_without_warning.cs`
  - Test should verify:
    - The configuration is `HandlerLifetime = Scoped` and `AsyncLocalScopeProvider` with no ambient scope set.
    - The dependency is disposed after each query.
    - Zero Warnings are captured.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `CreatePipelineScope()`, log a Warning through `AdoptionDiagnostics` when `GetAmbient` returns null. The test must fail on `warnings.ShouldBeEmpty()`.
  - Implementation should:
    - Need no production change.

### 3D — Adoption (ADR 0024 step 6, FR15 row 12, FR17, FR18, FR19)

- [ ] **TEST + IMPLEMENT: T3.11 — A Scoped handler adopts an AsyncLocal-provided ambient scope, resolves its Scoped instance, and Darker never disposes it**
  - **USE COMMAND**: `/test-first when an AsyncLocal provider offers an ambient scope under JoinAmbient with scoped handlers should resolve the ambient scoped instance and never dispose the ambient scope on Execute`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_ambient_scope_offered_should_adopt_it_and_never_dispose_it.cs`
  - Test should verify:
    - The configuration is `HandlerLifetime = Scoped` and `AddDarkerScopeProvider<AsyncLocalScopeProvider>()`. A `RecordingAmbientScope` is created over `provider.CreateScope()` and made ambient.
    - The handler's `ITrackedDependency` is `ReferenceEquals` to `ambientScope.Services.GetRequiredService<ITrackedDependency>()`.
    - After the query, that dependency's `IsDisposed` is false, and the ambient scope still resolves it.
    - `RecordingAmbientScope.DisposeCalls` and `DisposeAsyncCalls` are both 0.
    - No Warning is logged.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the internal `BorrowedServiceProviderScope : IAmAServiceProviderScope`, which exposes the offered scope's `Services` and does nothing on `Dispose`/`DisposeAsync`.
    - In `CreatePipelineScope()`, wrap an offered `IAmAServiceProviderScope` in it. Probe checks come in T3.19–T3.23.

- [ ] **CHARACTERISE: T3.12 — Adoption, and the never-dispose rule, hold on ExecuteAsync and ExecuteStream**
  - **USE COMMAND**: `/test-first when an ambient scope is adopted on ExecuteAsync or ExecuteStream should resolve the ambient instance and never dispose the ambient scope`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_ambient_scope_adopted_on_async_paths_should_never_dispose_it.cs`
  - Test should verify:
    - The T3.11 assertions hold for `ExecuteAsync` and for an `await foreach` over `ExecuteStream`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: make `BorrowedServiceProviderScope.DisposeAsync` forward to the offered scope's `DisposeAsync`, while `Dispose` stays a no-op. The test must fail on `DisposeAsyncCalls.ShouldBe(0)` / `IsDisposed.ShouldBeFalse()`.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T3.13 — With an adopted scope, decorators resolve from the same scope as the handler (FR18; the owned case is pinned by the existing sharing test)**
  - **USE COMMAND**: `/test-first when a decorated query runs with an adopted scope should resolve the decorator's scoped dependency from the same scope as the handler`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_decorated_query_runs_should_resolve_decorator_from_handler_scope.cs`
  - Test should verify:
    - **Adopted** (Execute, ExecuteAsync, ExecuteStream): `DecoratedTrackedQuery`'s `HandlerDependency` and `DecoratorDependency` are both the ambient scope's instance.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `ServiceProviderComponentFactory.Create<T>` (decorators), resolve from a new `ServiceProviderLifetimeScope` instead of `lifetime.PipelineScope`. The test must fail on `ReferenceEquals(result.DecoratorDependency, ambientDependency).ShouldBeTrue()`.
  - Implementation should:
    - Need no production change. ADR 0023's single handle delivers this. The owned-scope case is already pinned by `When_handler_lifetime_is_scoped_should_share_dependency_across_handler_and_decorator.cs` (T2.14).

- [ ] **CHARACTERISE: T3.14 — Without the opt-in, or with AlwaysNew, an offered ambient scope is ignored and each query gets its own owned scope**
  - **USE COMMAND**: `/test-first when an ambient scope is offered but affinity is AlwaysNew should give each query its own owned scope`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_ambient_scope_offered_with_always_new_should_use_owned_scope_per_query.cs`
  - Test should verify:
    - Theory with two configurations: (a) `AddDarkerScopeProvider<AsyncLocalScopeProvider>(AlwaysNew)`; (b) a provider registered directly as `IAmAScopeProvider` with the default `DefaultScopeAffinity`.
    - In both, with the ambient scope set and `HandlerLifetime = Scoped`, two queries get distinct dependencies, neither is the ambient one, and both are disposed after their queries.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: compute `adoptionEnabled` without the affinity check. The test must fail on `ReferenceEquals(first.HandlerDependency, ambientDependency).ShouldBeFalse()`.
  - Implementation should:
    - Need no production change.

### 3E — Decorators per query (ADR 0024 step 7, FR11, NFR Concurrency)

- [ ] **TEST + IMPLEMENT: T3.15 — In one adopted scope, two query types that share a result type get distinct decorator instances, and each resolves the ambient Scoped services**
  - **USE COMMAND**: `/test-first when two query types with the same result type run in one adopted ambient scope should create distinct decorator instances that each resolve the ambient scoped dependency`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_two_query_types_share_a_result_type_in_one_ambient_scope_should_get_distinct_decorators.cs`
  - Test should verify:
    - The new test doubles, one per file, are:
      - `InstanceRecordingDecorator<TQuery,TResult>` with `InstanceRecordingDecoratorAttribute`. It takes `ITrackedDependency` in its constructor and writes its own identity and its dependency's identity into the result;
      - `FirstSharedResultQuery` and `SecondSharedResultQuery`, with one `SharedRecordingResult`;
      - the two handlers, decorated with the attribute.
    - With `HandlerLifetime = Scoped` and one ambient scope, the two queries report different decorator identities.
    - Both decorator dependencies equal the ambient scope's instance.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - In `src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionDecoratorRegistry.cs` (line 21), register each decorator as `ServiceLifetime.Transient` when the handler lifetime is Scoped or Transient, and as `Singleton` when it is Singleton. Keep `TryAdd`.

- [ ] **CHARACTERISE: T3.16 — Two queries of one handler type in one ambient scope share the Scoped handler instance (the documented sharing of FR11; risk mitigation: the pinned shared-handler test)**
  - **USE COMMAND**: `/test-first when two queries of one handler type run in one adopted ambient scope should resolve the same scoped handler instance`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_two_queries_of_one_handler_type_run_in_one_ambient_scope_should_share_the_handler.cs`
  - Test should verify:
    - The handler (a new `InstanceRecordingHandler`) writes its own identity into the result.
    - Two `FirstSharedResultQuery` executions in one ambient scope report the same handler identity.
    - The test's Arrange comment states that it pins a known contract, not an accident.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `ServiceCollectionHandlerRegistry`, register handlers Transient whenever the handler lifetime is Scoped. This applies the decorator rule to handlers. The test must fail on `first.HandlerId.ShouldBe(second.HandlerId)`.
  - Implementation should:
    - Need no production change. T3.11 delivers this.

- [ ] **CHARACTERISE: T3.17 — With an owned scope, a pipeline that applies the same decorator type twice gets two decorator instances, each sharing the handler's Scoped dependency (ADR 0024: the one visible change without the opt-in)**
  - **USE COMMAND**: `/test-first when a handler applies the same decorator type twice under a scoped handler lifetime should create two decorator instances that share the handler's scoped dependency`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_same_decorator_type_applied_twice_should_create_two_instances.cs`
  - Test should verify:
    - The test doubles it introduces: a `RepeatableInstanceRecordingDecoratorAttribute` (`AllowMultiple = true`) for `InstanceRecordingDecorator`, and a handler that applies it twice at different steps. Each decorator appends its own identity and its dependency's identity to the result.
    - With `HandlerLifetime = Scoped` and no provider, one query records two **different** decorator identities.
    - Both decorators' dependencies are the handler's Scoped instance.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `ServiceCollectionDecoratorRegistry.Register`, register decorators with the handler lifetime again (Scoped), the inverse of T3.18's mutation. The test must fail on the distinct-identities assertion, because one Scoped instance is initialised twice.
  - Implementation should:
    - Need no production change. T3.15 delivers this.

- [ ] **CHARACTERISE: T3.18 — With a Singleton handler lifetime, decorators stay Singleton**
  - **USE COMMAND**: `/test-first when handler lifetime is singleton should reuse the same decorator instance across queries`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_handler_lifetime_is_singleton_should_reuse_decorator_instance.cs`
  - Test should verify:
    - Two queries report the same `InstanceRecordingDecorator` identity.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `ServiceCollectionDecoratorRegistry.Register`, always use `ServiceLifetime.Transient`. The test must fail on `first.DecoratorId.ShouldBe(second.DecoratorId)`.
  - Implementation should:
    - Need no production change. Also confirm that `When_decorator_lifetime_configured_should_follow_same_rules_as_handler.cs` passes unchanged.

### 3F — The probe (ADR 0024 step 8, FR15 rows 7–11, FR17)

> Each probe test does the following:
> - It runs **many concurrent queries**, for example 20 queries via `Task.WhenAll`, each inside the ambient scope.
> - It asserts that every query used an owned scope: the dependency is distinct from the ambient one and disposed after its query.
> - It asserts that the offered scope recorded no `Dispose`/`DisposeAsync`.
> - It asserts that **exactly one** Warning names the reason.

- [ ] **TEST + IMPLEMENT: T3.19 — An offered scope that exposes no service provider is declined (row 7)**
  - **USE COMMAND**: `/test-first when the offered ambient scope is not an IAmAServiceProviderScope should decline it, use an owned scope and warn once`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_offered_scope_exposes_no_service_provider_should_decline_and_warn_once.cs`
  - Test should verify:
    - A `PlainRecordingScope` (new, `IAmAScope` only) is offered.
    - The common probe assertions above hold, with the reason "exposes no service provider".
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Decline a non-`IAmAServiceProviderScope` through `AdoptionDiagnostics` with that reason, and return an owned scope. Drop the offered scope; never dispose it.

- [ ] **TEST + IMPLEMENT: T3.20 — An offered scope whose Services is null is declined (row 8)**
  - **USE COMMAND**: `/test-first when the offered ambient scope has a null service provider should decline it, use an owned scope and warn once`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_offered_scope_has_null_services_should_decline_and_warn_once.cs`
  - Test should verify:
    - A `NullServicesScope` (new) is offered.
    - The common probe assertions above hold, with the reason "has no service provider".
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the null check to the probe.

- [ ] **TEST + IMPLEMENT: T3.21 — An offered root provider is declined, whether it is a real root or a root captured through a singleton factory delegate (row 9; risk mitigation)**
  - **USE COMMAND**: `/test-first when the offered ambient scope's provider is the root provider should decline it, use an owned scope and warn once, for both a real root and a captured root`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_offered_scope_is_the_root_provider_should_decline_and_warn_once.cs`
  - Test should verify:
    - **Fact 1.** `Services` is the `ServiceProvider` returned by `BuildServiceProvider()`.
    - **Fact 2.** `Services` is the `IServiceProvider` captured by a singleton factory delegate, `services.AddSingleton(sp => new RootCapture(sp))`.
    - In both facts, the common probe assertions above hold with the reason "is the root".
    - No Scoped dependency is resolved from the root, so it is disposed after the query.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Detect the root by Microsoft DI's behaviour. The root answers `IServiceProvider` and `IServiceScopeFactory` with the root scope. A request scope answers `IServiceProvider` with itself.
    - Resolve only singletons in the probe.

- [ ] **TEST + IMPLEMENT: T3.22 — A scope from a container that does not hold this Darker registration is declined (row 10)**
  - **USE COMMAND**: `/test-first when the offered ambient scope belongs to another container should decline it, use an owned scope and warn once`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_offered_scope_is_from_another_container_should_decline_and_warn_once.cs`
  - Test should verify:
    - The offered scope comes from a second `ServiceCollection`/provider that registers `ITrackedDependency` but has no `AddDarker`.
    - The common probe assertions above hold, with the reason "from another container".
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Compare the `AdoptionDiagnostics` singleton resolved from the offered provider with the one the factory holds.

- [ ] **TEST + IMPLEMENT: T3.23 — An ambient scope already disposed when the pipeline is built is declined with one Warning (row 11; FR17 acceptance criterion)**
  - **USE COMMAND**: `/test-first when the offered ambient scope is already disposed should decline it, use an owned scope and warn once naming the exception type`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_offered_scope_is_already_disposed_should_decline_and_warn_once.cs`
  - Test should verify:
    - The underlying `IServiceScope` is disposed before the queries run, through the test, not through the `RecordingAmbientScope` record.
    - Every query returns using an owned scope.
    - Exactly one Warning names `ObjectDisposedException`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Wrap the probe resolutions in a try/catch. On any exception, decline with a reason that names the exception type.

- [ ] **CHARACTERISE: T3.24 — The fallback Warning latches once per container: across concurrent queries, across request scopes with a Scoped processor, and independently for each container (NFR Performance and Diagnosability; risk mitigation: the atomic latch)**
  - **USE COMMAND**: `/test-first when a misconfiguration fallback recurs across concurrent queries, Scoped processors and separate containers should log the Warning once per container`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_fallback_recurs_should_log_warning_once_per_container.cs`
  - Test should verify:
    - **Concurrency.** 20 queries start concurrently and are offered a root scope. A `GatedScopeProvider` (new) awaits a shared `TaskCompletionSource` before returning, so the queries reach the probe together without blocking thread-pool threads. The test releases the gate once all 20 are waiting. Exactly one Warning is logged.
    - **Per reason.** In one container, some queries are offered a root scope and others a `PlainRecordingScope`. Exactly two Warnings are logged, one per reason.
    - **Scoped processor.** With `QueryProcessorLifetime = Scoped`, queries run in five separate `CreateScope()` scopes, so five factories are built. Exactly one Warning is logged.
    - **Per container.** Two separately built containers with the same misconfiguration log one Warning each.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation (a): make the latch per factory, with a field on `ServiceProviderComponentFactory` instead of `AdoptionDiagnostics`. The Scoped-processor fact must fail on `warnings.Count.ShouldBe(1)`, because the actual value is 5.
  - 🔁 RED mutation (b): make the latch `static`. The per-container fact must fail, because the second container logs 0.
  - 🔁 RED mutation (c): latch on a single flag instead of per reason. The per-reason fact must fail on `warnings.Count.ShouldBe(2)`, because the actual value is 1.
  - Note: a non-atomic check-then-add cannot be made to fail deterministically. The single atomic `TryAdd` in `AdoptionDiagnostics` (T3.3) is held by code review, and the concurrency fact guards against regressions in the latch's existence.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T3.25 — A borrowed scope disposed by its owner after the probe surfaces an unwrapped ObjectDisposedException (FR17)**
  - **USE COMMAND**: `/test-first when the ambient scope's owner disposes it after the probe should surface ObjectDisposedException unwrapped during the build and from user services after it`
  - Test location: "test/Paramore.Darker.Extensions.Tests"
  - Test file: `When_borrowed_scope_disposed_after_probe_should_surface_ObjectDisposedException_unwrapped.cs`
  - Test should verify:
    - **During the build.** A `ScopeDisposingHandler` (new) disposes the underlying ambient `IServiceScope` in its constructor, through a singleton `AmbientScopeOwner` test double. The decorator's later resolution throws, and the caller sees `ObjectDisposedException`, not a `ConfigurationException` or `AmbientScopeSourceException`.
    - **After the build.** A handler disposes the ambient scope and then resolves a service from its injected `IServiceProvider`. The caller sees `ObjectDisposedException`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `ServiceProviderComponentFactory.Create`/`Create<T>`, catch `ObjectDisposedException` and rethrow it wrapped in `ConfigurationException` (ADR 0024 Alternative 15). The build-time fact must fail on `Should.Throw<ObjectDisposedException>`.
  - Implementation should:
    - Need no production change.

- [ ] **VERIFY: T3.26 — Phase 3 checkpoint**
  - Run `dotnet build Darker.Filter.slnf -c Release` and `dotnet test Darker.Filter.slnf -c Release --no-build`, green on net8.0 and net9.0.
  - Confirm that these pass unchanged: `When_two_queries_run_concurrently_should_isolate_scopes.cs`, `When_handler_lifetime_is_scoped_should_share_dependency_across_handler_and_decorator.cs` and `When_decorator_lifetime_configured_should_follow_same_rules_as_handler.cs`.
  - Confirm that `Paramore.Darker.Extensions.DependencyInjection.csproj` has no ASP.NET Core reference.

---

## Phase 4 — ASP.NET Core package (ADR 0024)

- [ ] **SETUP: T4.1 — Create Paramore.Darker.Extensions.AspNetCore and its test project (ADR 0024 step 9)**
  - Create `src/Paramore.Darker.Extensions.AspNetCore/Paramore.Darker.Extensions.AspNetCore.csproj`, following `src/Paramore.Darker.Caching/Paramore.Darker.Caching.csproj`, with:
    - `net8.0;net9.0` and `<Nullable>enable</Nullable>`;
    - `<FrameworkReference Include="Microsoft.AspNetCore.App" />`;
    - a project reference to `Paramore.Darker.Extensions.DependencyInjection`;
    - a package description.
    It inherits signing from `src/Directory.Build.props`.
  - Create `test/Paramore.Darker.Extensions.AspNetCore.Tests/Paramore.Darker.Extensions.AspNetCore.Tests.csproj` with:
    - `net8.0;net9.0` and the same `FrameworkReference`;
    - the test packages of `test/Paramore.Darker.Extensions.Tests/Paramore.Darker.Extensions.Tests.csproj`;
    - project references to the new package and to `test/Paramore.Darker.Extensions.Tests`, to reuse `CapturingLoggerProvider`. This follows the existing Extensions.Tests → Core.Tests precedent.
  - Add a `TestDoubles/` folder and a non-parallel collection definition for the tests that build Darker hosts.
  - Add both projects to the `/src/` and `/test/` folders of `Darker.slnx`, and to `Darker.Filter.slnf`.
  - Add no `Directory.Packages.props` entry.
  - `dotnet build Darker.Filter.slnf -c Release` succeeds.

- [ ] **TEST + IMPLEMENT: T4.2 — Within one request, AddDarkerRequestScope with HandlerLifetime = Scoped gives the query handler the same Scoped instance as HttpContext.RequestServices, and Darker does not dispose it**
  - **USE COMMAND**: `/test-first when a query runs inside an HTTP request with AddDarkerRequestScope and scoped handlers should resolve the same scoped instance as RequestServices and not dispose it on Execute, ExecuteAsync and ExecuteStream`
  - Test location: "test/Paramore.Darker.Extensions.AspNetCore.Tests"
  - Test file: `When_query_runs_inside_a_request_should_share_request_scoped_services.cs`
  - Test should verify:
    - The container uses `AddDarker(o => o.HandlerLifetime = Scoped)` and `AddDarkerRequestScope()`. A `DefaultHttpContext`, whose `RequestServices` is `provider.CreateScope().ServiceProvider`, is set on the registered `IHttpContextAccessor`.
    - The new test doubles are a `RequestScopedDependency` (disposable, recording `IsDisposed`) and sync, async and stream query handlers that return their dependency.
    - On each path, the handler's dependency is `ReferenceEquals` to `RequestServices.GetRequiredService<RequestScopedDependency>()`.
    - After the query, `IsDisposed` is false.
    - It becomes true only when the test disposes the request scope.
    - A separate fact: `AddDarkerRequestScope` on a null collection throws `ArgumentNullException` with `ParamName` `"services"`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - Add the public `DarkerRequestScopeServiceCollectionExtensions.AddDarkerRequestScope(this IServiceCollection, ScopeAffinity affinity = ScopeAffinity.JoinAmbient)`. It throws `ArgumentNullException` for null, calls `AddHttpContextAccessor()`, then calls `AddDarkerScopeProvider<HttpContextScopeProvider>(affinity)`.
    - Add the internal `HttpContextScopeProvider`, which reads `IHttpContextAccessor.HttpContext` on each call and returns `new HttpRequestScope(context.RequestServices)` or null.
    - Add the internal `HttpRequestScope : IAmAServiceProviderScope`, whose `Dispose`/`DisposeAsync` do nothing.
    - Add no middleware.

- [ ] **CHARACTERISE: T4.3 — Adoption holds with both a Singleton and a Scoped QueryProcessorLifetime, across successive requests (FR21)**
  - **USE COMMAND**: `/test-first when the query processor is singleton or scoped should adopt each request's own scope across successive requests`
  - Test location: "test/Paramore.Darker.Extensions.AspNetCore.Tests"
  - Test file: `When_query_processor_lifetime_is_singleton_or_scoped_should_adopt_each_request_scope.cs`
  - Test should verify:
    - Theory over `QueryProcessorLifetime` Singleton and Scoped. For a Scoped processor, resolve it from the request scope.
    - Two sequential requests, each with its own `DefaultHttpContext` and scope.
    - Each query resolves its own request's dependency instance.
    - The two instances differ.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: make `HttpContextScopeProvider` capture `HttpContext` in its constructor, or on first call, instead of reading the accessor on each call. The Singleton-processor case must fail on the second request's `ReferenceEquals(..., secondRequestDependency).ShouldBeTrue()`.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T4.4 — Outside a request (no HttpContext), Darker falls back to an owned scope and logs no Warning**
  - **USE COMMAND**: `/test-first when a query runs with AddDarkerRequestScope but no HttpContext should use an owned scope and log no Warning`
  - Test location: "test/Paramore.Darker.Extensions.AspNetCore.Tests"
  - Test file: `When_query_runs_outside_a_request_should_use_owned_scope_without_warning.cs`
  - Test should verify:
    - `IHttpContextAccessor.HttpContext` is null.
    - The dependency is disposed after each query.
    - Zero Warnings are captured by a per-test `CapturingLoggerProvider`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: make `HttpContextScopeProvider` return `new HttpRequestScope(null)` when there is no context, instead of null. The test must fail on `warnings.ShouldBeEmpty()`, because the row 8 Warning is logged.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T4.5 — AddDarkerRequestScope(ScopeAffinity.AlwaysNew) gives an owned scope inside a request**
  - **USE COMMAND**: `/test-first when AddDarkerRequestScope is called with AlwaysNew should use an owned scope inside a request`
  - Test location: "test/Paramore.Darker.Extensions.AspNetCore.Tests"
  - Test file: `When_request_scope_registered_with_always_new_should_use_owned_scope.cs`
  - Test should verify:
    - Inside a request, the handler's dependency is not the `RequestServices` instance, and it is disposed after the query.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: make `AddDarkerRequestScope` ignore its `affinity` parameter and pass `JoinAmbient`. The test must fail on `ReferenceEquals(handlerDependency, requestDependency).ShouldBeFalse()`.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T4.6 — A Transient or Singleton HandlerLifetime with AddDarkerRequestScope does not adopt, and logs one Warning**
  - **USE COMMAND**: `/test-first when AddDarkerRequestScope is used with a transient or singleton handler lifetime should not adopt the request scope and should log one Warning`
  - Test location: "test/Paramore.Darker.Extensions.AspNetCore.Tests"
  - Test file: `When_request_scope_used_with_non_scoped_handler_lifetime_should_not_adopt_and_warn_once.cs`
  - Test should verify:
    - Theory over Transient and Singleton.
    - Over several queries inside one request, the dependency is never the `RequestServices` instance.
    - Exactly one Warning is logged, with the Scoped-lifetime reason.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: drop the Scoped check from `adoptionEnabled`. The Transient case must fail on the not-the-request-instance assertion.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T4.7 — AddDarkerRequestScope behaves the same whether it is called before or after AddDarker**
  - **USE COMMAND**: `/test-first when AddDarkerRequestScope is called before or after AddDarker should adopt the request scope either way`
  - Test location: "test/Paramore.Darker.Extensions.AspNetCore.Tests"
  - Test file: `When_request_scope_registered_before_or_after_AddDarker_should_adopt_either_way.cs`
  - Test should verify:
    - Theory over call order. In both orders, the handler's dependency is the `RequestServices` instance.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `AddDarker`, read any `ScopeAffinityRegistration` eagerly from the `IServiceCollection` at call time, instead of resolving it in `BuildQueryProcessor`. The "after" case must fail on `ReferenceEquals(...).ShouldBeTrue()`.
  - Implementation should:
    - Need no production change.

- [ ] **CHARACTERISE: T4.8 — A stream enumerated after its request has ended gets an owned scope (ADR 0024 risk mitigation; ADR 0019 lazy build)**
  - **USE COMMAND**: `/test-first when a stream query is first enumerated after the request has ended should use an owned scope rather than the ended request scope`
  - Test location: "test/Paramore.Darker.Extensions.AspNetCore.Tests"
  - Test file: `When_stream_enumerated_after_request_ends_should_use_owned_scope.cs`
  - Test should verify:
    - Inside a request, one `Execute` query runs first, so the provider has served that request's scope. Then `ExecuteStream` is called, still inside the request.
    - The accessor's `HttpContext` is cleared and the request scope disposed before the first `MoveNextAsync`.
    - Enumeration succeeds. Its dependency is not the disposed request's instance, and it is disposed when enumeration ends.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation (a), laziness: in `QueryProcessor.ExecuteStream`, build the pipeline (and so ask for the pipeline scope) when `ExecuteStream` is called, before the iterator starts, instead of on the first `MoveNextAsync`. The test must fail on `Should.NotThrowAsync` (`ObjectDisposedException` from the disposed request scope) or on the not-the-request-instance assertion.
  - 🔁 RED mutation (b), provider: make `HttpContextScopeProvider` remember the last non-null `RequestServices` and return it when the accessor has no context. The earlier `Execute` gives it something to remember, so the test must fail the same way.
  - Implementation should:
    - Need no production change.

- [ ] **DOC: T4.9 — XML documentation on every new public type of ADR 0024, including the sharing warnings (ADR 0024 step 11, XML part; risk mitigations)**
  - Document the following:
    - `IAmAScopeProvider`: must be safe to call concurrently. Darker never disposes what it returns.
    - `ScopeAffinity` and its values. `AlwaysNew` is the default and zero.
    - `AmbientScopeSourceException`.
    - `IAmAServiceProviderScope`.
    - `DarkerOptions.DefaultScopeAffinity`. A registered affinity overrides it.
    - `AddDarkerScopeProvider<TProvider>`: registers a singleton and replaces any earlier registration.
    - `AddDarkerRequestScope`.
  - On **`ScopeAffinity.JoinAmbient`** and **`AddDarkerRequestScope`**, state:
    - adoption requires `HandlerLifetime = Scoped`;
    - queries in one ambient scope share its Scoped services, **including a Scoped handler instance of the same handler type**, so concurrent queries in one request (for example `Task.WhenAll`) need `AlwaysNew`, and EF Core's `DbContext` rejects concurrent operations;
    - the stream path reads the ambient scope on the first `MoveNextAsync`.
  - `dotnet build` produces no missing-XML-doc warnings for the new public types.

- [ ] **VERIFY: T4.10 — Phase 4 checkpoint**
  - Run `dotnet build Darker.Filter.slnf -c Release` and `dotnet test Darker.Filter.slnf -c Release --no-build`, green on net8.0 and net9.0, including `Paramore.Darker.Extensions.AspNetCore.Tests`.
  - Run `dotnet pack src/Paramore.Darker.Extensions.AspNetCore -c Release` and confirm that the package is produced and signed.

---

## Phase 5 — Documentation and release

- [ ] **DOC: T5.1 — Correct CLAUDE.md "Handler Lifecycle" and "ASP.NET Core Registration" (FR23)**
  - In `CLAUDE.md` "Handler Lifecycle" (~lines 104–108):
    - Replace "With ASP.NET Core + EF Core: QueryProcessor must be Scoped". State that a Scoped `QueryProcessor` gets a sibling scope and does **not** share the request's `DbContext`, and that `HandlerLifetime = Scoped` plus `AddDarkerRequestScope()` does.
    - Describe teardown as isolated, scope-last and never-throwing.
    - Describe the pipeline scope as created by the handler factory and carried on the lifetime.
  - In "ASP.NET Core Registration" (~lines 166–172), replace the `QueryProcessorLifetime = Scoped // Required for EF Core` example with ADR 0024's *The corrected guidance* snippet. It shows `AddDarkerRequestScope()` beside Brighter's `AddBrighterRequestScope()` and notes that the two opt-ins are independent.

- [ ] **DOC: T5.2 — List the new package in CLAUDE.md Key Components (FR24)**
  - Add `src/Paramore.Darker.Extensions.AspNetCore`: the ASP.NET Core request-scope opt-in (`AddDarkerRequestScope`), net8.0+. Place it beside the DI package entry (~lines 99–102).

- [ ] **DOC: T5.3 — Replace the README's EF Core warning (FR23)**
  - In `README.md` (~lines 25–31), replace the "QueryProcessor must also be registered as Scoped" warning and snippet with the corrected guidance:
    - `HandlerLifetime = Scoped` plus `services.AddDarkerRequestScope()` from `Paramore.Darker.Extensions.AspNetCore`;
    - Brighter's `AddBrighterRequestScope()` alongside it;
    - a one-line note that concurrent queries in one request need `AlwaysNew`.

- [ ] **DOC: T5.4 — Add the new package to CONTRIBUTING.md Project Structure (FR24)**
  - In `CONTRIBUTING.md` "Project Structure" (~lines 345–366), add `Paramore.Darker.Extensions.AspNetCore` under `src/` and `Paramore.Darker.Extensions.AspNetCore.Tests` to the `test/` list.

- [ ] **DOC: T5.5 — Add the new package to .agent_instructions/project_structure.md (FR24)**
  - Add the `src/Paramore.Darker.Extensions.AspNetCore/` line after the DI package line (line 4).

- [ ] **DOC: T5.6 — Decide on a SampleMinimalApi usage example (ADR 0024 step 11 leaves this to the documentation tasks)**
  - Decide whether `samples/SampleMinimalApi` gains an `AddDarkerRequestScope()` call with `HandlerLifetime = Scoped`.
  - Recommendation: add it only if the sample already has a Scoped resource to share. Otherwise record "no sample change; the README and CLAUDE.md carry the example" in the PR description.
  - If it is added, `dotnet build Darker.Filter.slnf` must stay green.

- [ ] **DOC: T5.7 — Confirm the ADR record for FR22**
  - Confirm that ADRs 0022, 0023 and 0024 are Accepted, that ADR 0014's Status section records the partial supersession, and that the frontmatter and `docs/adr/index.md` agree.
  - `index.md` currently lists 0014 as plain "Accepted". If the index should show the partial supersession, update it with `/adr:write_adr_metadata`.

- [ ] **DOC: T5.8 — Check that the release_notes.md spec 015 section matches what was built (FR25)**
  - Walk the `<!-- spec: 015-di_lifetime_conformance -->` section of `release_notes.md` against the merged code and the tasks above. Check each of these:
    - every breaking change: `ReleaseAsync`; `IAsyncDisposable`; `CreatePipelineScope`; `Add` removed and `PipelineScope` added; the container-backed decorator factory contract;
    - every behavioural change: never-throw teardown; release through the creating factory; Simple `DisposeAsync`; partial-build releases; Transient decorators;
    - the Added list: the package, the core types, `IAmAServiceProviderScope`, `DefaultScopeAffinity`, `AddDarkerScopeProvider`;
    - the `JoinAmbient` sharing note;
    - the corrected EF Core guidance.
  - Correct any drift with `/spec:write_release_notes`.

- [ ] **VERIFY: T5.9 — Final checkpoint**
  - Run `dotnet build Darker.Filter.slnf -c Release` and `dotnet test Darker.Filter.slnf -c Release --no-build`. The full suite must be green on net8.0 and net9.0.
  - Confirm that no project contains `InternalsVisibleTo` for a test assembly.
  - Confirm that no test drives an internal type directly.
  - Optional (NFR Performance): run `test/Paramore.Darker.Benchmarks` on the Singleton-handler path before and after the spec. Confirm there is no added per-query allocation.

---

## Coverage cross-reference

### Functional requirements → tasks

| FR | Tasks |
| --- | --- |
| FR1 (each release isolated, logged at Error) | T1.1, T1.5, T1.7, T1.14, T1.15, T1.16, T2.10 |
| FR2 (scope disposed last, unconditionally) | T1.5, T1.7, T1.16, T2.5 |
| FR3 (teardown never throws; original exception kept) | T1.5, T1.6, T1.8, T1.9, T1.13a, T1.16, T1.16a, T1.17, T1.17a, T2.6, T2.8 |
| FR4 (lifetime disposes all, logs, idempotent) | T1.2, T1.3, T1.13, T1.13a, T2.5, T2.6, T2.10 |
| FR5 (sync, async and stream parity) | T1.10, T1.12–T1.17a, T2.4–T2.8 |
| FR5a (async teardown awaited; sync stays sync) | T1.11, T1.12, T1.13, T1.15, T1.4 (sync limitation) |
| FR6 (`IAmAScope`) | T2.1, T2.17 |
| FR7 (`CreatePipelineScope`: null for Singleton) | T2.2, T2.4, T2.7, T2.12, T2.13 |
| FR8 (lifetime carries a fixed scope; factories resolve from it) | T2.4, T2.9 |
| FR9 (no per-query state on factories) | T2.13 (code review), T2.18 (grep) |
| FR10 (fail fast, no root fallback) | T2.13 (implemented; no direct test by design), T2.11 (root-resolution pin) |
| FR11 (one scope per query; isolation; per-query decorators; documented sharing) | T2.14 (VERIFY), T2.15, T3.15, T3.16, T3.17, T3.18 |
| FR12 (Simple factories keep working) | T2.2, T2.4, T1.20/T2.18 (existing Simple-factory suite) |
| FR13 (`IAmAScopeProvider`, `ScopeAffinity`) | T3.1, T3.9 |
| FR14 (`DefaultScopeAffinity`; unchanged default) | T3.3, T3.4, T3.8, T3.8a, T3.8b, T3.14 |
| FR15 (adoption ladder; Warning once per reason; none for "no scope") | T3.5, T3.6, T3.7, T3.10, T3.11, T3.19–T3.24 |
| FR16 (`AmbientScopeSourceException`) | T3.9 |
| FR17 (ownership; disposed scope declined; unwrapped `ObjectDisposedException`) | T3.11, T3.12, T3.19–T3.23, T3.25, T4.2 |
| FR18 (decorators share the handler's scope) | T3.13 (adopted), T3.17, T2.14 (owned) |
| FR19 (non-ASP.NET provider, `AsyncLocal`) | T3.5 (double), T3.11, T3.12 |
| FR20 (`AddDarkerRequestScope` package) | T4.1, T4.2, T4.4, T4.5 |
| FR21 (Singleton and Scoped processor lifetime) | T4.3 |
| FR22 (ADR supersession) | T5.7 (ADRs 0022–0024 already Accepted) |
| FR23 (CLAUDE.md and README corrections) | T5.1, T5.3 |
| FR24 (package listed in three docs) | T5.2, T5.4, T5.5 |
| FR25 (release notes) | T1.19, T2.17, T5.8 |

No FR is without a task. FR10's guard has no direct test, as the requirements and ADR 0023 state. It is kept by code review in T2.13.

### Acceptance criteria → asserting task

| Acceptance criterion | Task |
| --- | --- |
| Handler `Dispose` throws (Simple): decorators released, lifetime disposed | T1.5 (sync), T1.16 (async and stream) |
| A Scoped service's `Dispose` throws (DI): result returned, Error logged | T1.8 (sync), T1.13a (async and stream) |
| Handler throws plus release throws: caller sees the handler's exception | T1.6, T1.17 |
| Successful query plus throwing release: result returned | T1.5, T1.7, T1.16 |
| Decorator creation or initialisation throws: that exception surfaces, earlier decorators released | T1.10 |
| `ExecuteAsync`/`ExecuteStream`: handler released by its creating factory | T1.14 |
| Covered on the sync, async and stream paths | T1.5–T1.9 (sync); T1.12–T1.17 and T1.13a, T1.16a (async and stream); T1.10, T1.17a (all three) |
| Async and stream: an IAsyncDisposable-only Scoped dependency is disposed and `DisposeAsync` awaited | T1.12 |
| Handler and decorators share one Scoped instance; concurrent owned scopes differ | T2.14, T2.15 |
| `ServiceProviderComponentFactory` holds no per-query state | T2.13 (code review), T2.18 (grep): no test possible through the public surface |
| Scoped or Transient owned scope: nothing resolved from the root; disposable handler disposed each query | T2.11 |
| In a request with Scoped handlers and `AddDarkerRequestScope`: same instance as `RequestServices`, not disposed | T4.2 |
| Without the opt-in, or with `AlwaysNew`: owned scope | T3.14, T4.5, T3.4 |
| Outside a request: owned scope, no Warning | T4.4, T3.10 |
| Holds with Singleton and Scoped `QueryProcessorLifetime` | T4.3 |
| `AsyncLocal` test provider adopted the same way | T3.11, T3.12 |
| Transient or Singleton `HandlerLifetime` with `JoinAmbient`: no adoption, one Warning | T3.5, T3.6, T4.6 |
| An already-disposed ambient scope is declined, with an owned scope and one Warning | T3.23 |
| Adopted scope, Scoped handler lifetime: distinct decorator instances for two query types with the same result type, each resolving ambient services | T3.15 |
| Adopted scope: two queries of one handler type share the Scoped handler instance | T3.16 |
| Testing approach (real registries, `ServiceCollection`, Simple factories, public surface only) | Conventions section; every TEST/CHARACTERISE task |
| New test project for the AspNetCore package | T4.1 |
| Definition of done: suite green on net8.0 and net9.0; docs and release notes updated | T1.20, T2.18, T3.26, T4.10, T5.1–T5.9 |

No acceptance criterion is without a task. "Holds no per-query state" cannot be asserted through the public surface and is verified by review and grep.

### ADR decisions → tasks

| ADR step / mitigation | Tasks |
| --- | --- |
| 0022 step 1 (structural extract) | T1.1 |
| 0022 step 2 (lifetime isolation, builder's logger) | T1.2, T1.3, T1.4 |
| 0022 step 3 (sync teardown, guarded log, DI Scoped fault) | T1.5, T1.6, T1.7, T1.8, T1.9 |
| 0022 row 1 (skip releases when nothing was created; tolerate a builder that never built) | T1.17a, T2.8 |
| 0022 step 4 (partial builds) | T1.10 |
| 0022 step 5 (async seams) | T1.11 |
| 0022 step 6 (async lifetime, `AsyncServiceScope`) | T1.12, T1.13, T1.13a |
| 0022 step 7 (async teardown, creating factory, `await using`, `QueryProcessorAsyncTests` update) | T1.12, T1.14, T1.15, T1.16, T1.17 |
| 0022 step 8 (remove async `Release`) | T1.18 |
| 0022 step 9 (XML docs, release notes) | T1.19 |
| 0022 risk: swallowed fault hides a defect (assert the log entry) | T1.2, T1.5, T1.7, T1.8, T1.16 |
| 0022 risk: throwing logging provider (guarded log) | T1.9 (sync), T1.16a (async and stream) |
| 0022 risk: IAsyncDisposable-only service on `Execute` | T1.4, T1.19 |
| 0022 risk: static logger captured first (own result type per test) | Conventions; T1.2, T1.5, T1.7–T1.9, T1.13, T1.16, T2.6 |
| 0023 step 1 (`IAmAScope`) | T2.1 |
| 0023 step 2 (`CreatePipelineScope`) | T2.2 |
| 0023 step 3 (choose factory once) | T2.3 |
| 0023 step 4 (lifetime carries handle; four test bullets; migration of 0022 tests) | T2.4, T2.5, T2.6, T2.7, T2.10 |
| 0023 step 5 (DI factory creates and recognises the scope; rows 1 and 2; existing tests unchanged) | T2.11, T2.12, T2.13, T2.14, T2.15 |
| 0023 step 6 (remove `Add`) | T2.16 |
| 0023 step 7 (docs, release notes) | T2.17 |
| 0023 rows 5 and 6 (scope creation fails; build fails after the scope exists) | T2.8, T2.9 |
| 0023 risk: handle escapes before the lifetime holds it | T2.4 (one expression), T2.9 |
| 0023 risk: custom factory returns a shared handle | T2.17 (XML doc) |
| 0023 risk: per-handler lifetime future change | Out of scope (no task; ADR accepts) |
| 0023 risk: recognition by concrete type | T3.2 (widened to an interface) |
| 0024 step 1 (core seam) | T3.1 |
| 0024 step 2 (widen recognition, package description) | T3.2 |
| 0024 step 3 (plumbing, atomic latch; provider registration replaces, null guard) | T3.3, T3.8a, T3.8b, T4.2 (request-scope null guard) |
| 0024 step 4 (rows 1–4; five test bullets) | T3.4, T3.5, T3.6, T3.7, T3.8 |
| 0024 step 5 (rows 5 and 6) | T3.9, T3.10 |
| 0024 step 6 (row 12 adoption; `AsyncLocal` provider; three paths; existing tests unchanged) | T3.11, T3.12, T3.13, T3.14, T3.26 |
| 0024 step 7 (Transient decorators; four test bullets) | T3.15, T3.16, T3.17, T3.18 |
| 0024 step 8 (probe rows 7–11, concurrent, one Warning) | T3.19, T3.20, T3.21, T3.22, T3.23, T3.24 |
| 0024 step 9 (new projects) | T4.1 |
| 0024 step 10 (`AddDarkerRequestScope`; six test bullets) | T4.2, T4.3, T4.4, T4.5, T4.6, T4.7 |
| 0024 step 11 (XML docs, CLAUDE.md, README, CONTRIBUTING, project_structure, sample, release notes) | T4.9, T5.1–T5.6, T5.8 |
| 0024 risk: concurrent queries share a `DbContext` under `JoinAmbient` | T4.9, T5.3 |
| 0024 risk: shared Scoped handler instance (pinned test) | T3.16, T4.9 |
| 0024 risk: Microsoft DI root behaviour changes (real and captured root) | T3.21 |
| 0024 risk: Scoped scope provider resolved from the root | T3.3 (singleton registration) |
| 0024 risk: stream enumerated after the request ended (lazy read on the first `MoveNextAsync`) | T4.8, T4.9 |
| 0024 risk: Warning lost with no logging provider | No task: the ADR accepts it as a diagnostic, not a guard |
| 0024 latch per container and per reason, atomic | T3.3, T3.24 |
| 0024 FR17 unwrapped `ObjectDisposedException` (Alternative 15) | T3.25 |

Decisions with no task are the two the ADRs themselves accept without mitigation work: ADR 0023's per-handler-lifetime risk (out of scope) and ADR 0024's lost-Warning risk. ADR 0022 row 1 has its own task, T1.17a.

### NFRs → tasks

| NFR | Tasks |
| --- | --- |
| Performance (no Singleton-path allocation; latched Warning) | T1.11 (`ValueTask` no-op release), T2.2/T2.13 (null scope for Singleton), T3.3, T3.24, T5.9 (optional benchmark) |
| Reliability (the owned scope is always disposed) | T1.5, T1.7, T1.8, T1.13a, T1.16, T1.17a, T2.5, T2.6, T2.9 |
| Diagnosability (Error with component type; Warning with reason) | T1.2, T1.4, T1.5, T1.7, T1.8, T1.16, T2.6, T3.5–T3.7, T3.19–T3.23 |
| Parity (sync, async and stream) | T1.10, T1.12–T1.17, T2.4–T2.8, T2.11, T2.12, T2.15, T3.9, T3.12, T3.13, T4.2 |
| Concurrency (no shared owned scope; no shared Darker-registered decorator unless Singleton) | T2.14, T2.15, T3.15, T3.24 |

### Scope-creep check

Every task traces to an FR, an acceptance criterion, an NFR or an ADR step or mitigation. These are the ones to look at closely:

- **T1.3** (lifetime idempotency) traces to FR4.
- **T2.8 and T2.9** trace to ADR 0023 rows 5 and 6 and its "handle escapes" mitigation.
- **T3.25** traces to FR17's unwrapped `ObjectDisposedException` clause.
- **T5.6** is the sample decision that ADR 0024 step 11 explicitly delegates.
- **T5.9's benchmark** is optional, and traces to NFR Performance.

No task is untraced.

### Deviations from the ADR step labels (for the reviewer)

- **ADR 0022 step 6 is merged into the first step-7 test (T1.12).** The async lifetime cannot be observed through the public surface until the processor awaits `DisposeAsync`.
- **ADR 0023 step 5 is labelled Behavioural, but becomes two CHARACTERISE pins (T2.11, T2.12), a STRUCTURAL rewrite (T2.13) and a VERIFY run of the existing tests with named mutations (T2.14).** Through `AddDarker`, the DI factory's observable behaviour does not change, and rows 3 and 4 cannot be reached. TEST + IMPLEMENT would claim a RED that cannot occur.
- **Several ADR test bullets are CHARACTERISE rather than TEST + IMPLEMENT** because an earlier task in the same ADR already delivers them: T1.6, T1.7, T1.8, T1.13a, T1.16, T1.16a, T2.6, T2.7, T3.4, T3.6, T3.8, T3.8b, T3.10, T3.12–T3.14 and T3.16–T3.18. T1.16 is CHARACTERISE because T1.14 and T1.15 keep each awaited release inside its fault boundary; T2.10 is CHARACTERISE because migrating a test must re-prove it can fail. ADR 0024 step 10's bullets after the first (T4.3–T4.8) are the same case. Each one names its RED mutation.
- **T3.24's atomicity mutation.** A non-atomic latch cannot be made to fail deterministically, so the atomic `TryAdd` is held by code review. The task's named mutations test the latch's per-container scope and its per-reason key.
