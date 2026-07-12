# Tasks — 012 Streaming Results

**Spec**: `specs/012-streaming_results/`
**Design**: `docs/adr/0019-streaming-query-pipeline.md` (Accepted)
**Issue**: [#299](https://github.com/BrighterCommand/Darker/issues/299)

> **TDD is mandatory.** Every task tagged **TEST + IMPLEMENT** MUST be started with the exact
> `/test-first` command shown. Write the test, then **STOP and wait for the user to approve the test
> in their IDE** before writing any implementation. Do **not** hand-write a test and continue.
>
> Tasks tagged **STRUCTURAL** are Tidy-First refactors/scaffolding with *no behavioural change*
> (new marker interfaces, ctor slots, package refs). They carry no test of their own; use
> `/tidy-first` and keep them in separate commits from behavioural work. They must still compile.

## Conventions

- **Core src**: `src/Paramore.Darker/`
- **Core tests**: `test/Paramore.Darker.Core.Tests/` (flat `When_*.cs` files; test doubles in
  `TestDoubles/`, namespace `Paramore.Darker.Core.Tests.TestDoubles`)
- **DI src / tests**: `src/Paramore.Darker.Extensions.DependencyInjection/` /
  `test/Paramore.Darker.Extensions.Tests/`
- Prefer real/Simple/InMemory doubles (`StreamQueryHandlerRegistry`, `SimpleHandlerFactory`,
  `InMemoryDecoratorRegistry`, `InMemoryQueryContextFactory`); Moq only as a last resort.

---

## Phase 0 — Structural foundations (Tidy First, no behaviour)

- [x] **STRUCTURAL: T001 — `IAsyncEnumerable` available on all targets**
  - Add `Microsoft.Bcl.AsyncInterfaces` version to `Directory.Packages.props` (CPM).
  - Add a **conditional** `<PackageReference>` in `src/Paramore.Darker/Paramore.Darker.csproj`
    guarded to `netstandard2.0` only (native on net8.0/net9.0).
  - Verify `dotnet build Darker.Filter.slnf -c Release` still succeeds on all targets.
  - ADR §7. No behaviour; separate commit.

- [x] **STRUCTURAL: T002 — Stream query + handler contracts**
  - Add `src/Paramore.Darker/IStreamQuery.cs`:
    `public interface IStreamQuery<out TResult> : IQuery<TResult> { }` (TResult = **item** type).
  - Add `src/Paramore.Darker/IStreamQueryHandler.cs`:
    `IStreamQueryHandler<in TQuery, TResult> : IQueryHandler where TQuery : IStreamQuery<TResult>`
    with `IAsyncEnumerable<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default)`.
  - Add `src/Paramore.Darker/StreamQueryHandler.cs`: abstract base with `IQueryContext Context { get; set; }`
    and abstract `ExecuteAsync` (no `Fallback` method — ADR §2). XML docs incl. licence header.
  - ADR §2. Must compile; no behaviour.

- [x] **STRUCTURAL: T003 — Stream decorator contract + attribute base**
  - Add `src/Paramore.Darker/IStreamQueryHandlerDecorator.cs`:
    `IStreamQueryHandlerDecorator<TQuery, TResult> : IQueryHandlerDecorator where TQuery : IStreamQuery<TResult>`
    with `IAsyncEnumerable<TResult> Execute(TQuery query, Func<TQuery, CancellationToken, IAsyncEnumerable<TResult>> next, CancellationToken cancellationToken)`.
  - Add `src/Paramore.Darker/StreamQueryHandlerAttribute.cs`: abstract `Attribute`
    (`AttributeTargets.Method`) mirroring `QueryHandlerAttributeAsync` — `int Step`,
    `abstract object[] GetAttributeParams()`, `abstract Type GetDecoratorType()`.
  - ADR §3. Must compile; no behaviour.

---

## Phase 1 — Handler registry (behaviour)

- [x] **TEST + IMPLEMENT: T004 — Stream registry resolves a stream handler by query type**
  - **USE COMMAND**: `/test-first when stream query registered should resolve its stream handler type from the stream registry`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_stream_query_registered_should_resolve_stream_handler_type.cs`
  - Test should verify:
    - `StreamQueryHandlerRegistry.Register<TQuery, TResult, THandler>()` then `Get(typeof(TQuery))`
      returns the handler type
    - `Get` for an unregistered query type returns `null`
    - Registering a duplicate query type throws `ConfigurationException` (mirror async registry)
    - Registering with a result type that does not match the query throws `ConfigurationException`
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `src/Paramore.Darker/IStreamQueryHandlerRegistry.cs` (ADR §5 signature: `Get`,
      generic `Register<TQuery,TResult,THandler>` constrained to `IStreamQueryHandler<TQuery,TResult>`,
      `Register(Type, Type, Type)`)
    - Add `src/Paramore.Darker/StreamQueryHandlerRegistry.cs` modelled on
      `QueryHandlerRegistryAsync` (dictionary, duplicate + result-type guards)
    - Requires a stream query + handler test double in `TestDoubles/`

- [x] **TEST + IMPLEMENT: T005 — Assembly scan registers only stream handlers**
  - **USE COMMAND**: `/test-first when scanning assemblies for stream handlers should register IStreamQueryHandler implementations and ignore async handlers`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_scanning_assemblies_for_stream_handlers_should_register_only_stream_handlers.cs`
  - Test should verify:
    - `RegisterFromAssemblies` on the stream registry picks up a public `IStreamQueryHandler<,>`
      implementation and maps query → handler
    - It does **not** register `IQueryHandlerAsync<,>` / `IQueryHandler<,>` implementations
    - Only public (`ExportedTypes`) concrete non-abstract classes are registered (ADR 0011 §9-10)
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `RegisterFromAssemblies(IEnumerable<Assembly>)` to `StreamQueryHandlerRegistry`
      hard-matching `i.GetGenericTypeDefinition() == typeof(IStreamQueryHandler<,>)` (ADR §5.1)

---

## Phase 2 — Pipeline build + processor entry point (MVP happy path)

- [x] **STRUCTURAL: T006 — Thread the stream registry through builder + configuration**
  - `PipelineBuilder<TResult>` gains a new ctor parameter for `IStreamQueryHandlerRegistry`
    (ADR §5.3) — additive, existing ctors unaffected.
  - `IHandlerConfiguration` / `HandlerConfiguration` gain an optional
    `IStreamQueryHandlerRegistry StreamHandlerRegistry` member (null when streaming unused, ADR §5.2).
  - `QueryProcessor` reads `StreamHandlerRegistry` off the configuration into a field (ADR §5.4).
  - Must compile; no behaviour yet.

- [x] **TEST + IMPLEMENT: T007 — `BuildStream` resolves the handler and yields its items (no decorators)**
  - **USE COMMAND**: `/test-first when building a stream pipeline with no decorators should invoke the handler and yield its items`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_building_stream_pipeline_without_decorators_should_yield_handler_items.cs`
  - Test should verify:
    - `PipelineBuilder<TResult>.BuildStream(query, context, options)` returns a
      `Func<IStreamQuery<TResult>, CancellationToken, IAsyncEnumerable<TResult>>`
    - `await foreach` over the returned delegate yields exactly the items the handler produces, in order
    - The stream method is resolved **by signature** (`IAsyncEnumerable<TResult>` return,
      `(TQuery, CancellationToken)` params), not bare name — a handler with both an async
      `Task<TResult> ExecuteAsync` and a stream `ExecuteAsync` binds to the stream one with no
      `AmbiguousMatchException`
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `BuildStream` to `PipelineBuilder.cs` (ADR §6): factor the shared resolve/validate/order
      steps with `BuildAsync`; close handler + decorators over **`typeof(IStreamQuery<TResult>)`**
      (not `IQuery<TResult>`) to satisfy the `where TQuery : IStreamQuery<TResult>` constraint
    - Resolve the handler type from the stream registry; add signature-based stream-method resolution
    - Sink invokes the handler's stream `ExecuteAsync` returning the enumerable — **no**
      `TargetInvocationException` unwrap around enumeration (iterator defers, ADR §4/§6)

- [x] **TEST + IMPLEMENT: T008 — `ExecuteStream` runs a stream query end-to-end**
  - **USE COMMAND**: `/test-first when executing a stream query through the processor should yield all handler items via await foreach`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_executing_stream_query_should_yield_all_items.cs`
  - Test should verify:
    - `queryProcessor.ExecuteStream(query, ct)` returns an `IAsyncEnumerable<TResult>`
    - `await foreach` yields all items the handler produces, in order
    - Works with a processor built from `HandlerConfiguration` + `InMemoryQueryContextFactory`
      and the reused async handler/decorator factories
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `IAsyncEnumerable<TResult> ExecuteStream<TResult>(IStreamQuery<TResult> query, IQueryContext? queryContext = null, CancellationToken cancellationToken = default)`
      to `IQueryProcessor` (ADR §4) — **breaking interface change, permitted under V5/NFR2**
    - Implement it on `QueryProcessor` as an `async` iterator with `[EnumeratorCancellation]`,
      binding span **and** `PipelineBuilder.Dispose()` to enumeration lifetime via `try/finally`
      (ADR §4 code sketch)
    - Update **`FakeQueryProcessor`** (`src/Paramore.Darker.Testing/FakeQueryProcessor.cs`) and any
      other `IQueryProcessor` implementers to satisfy the new interface member

---

## Phase 3 — Core streaming correctness properties (each a distinct behaviour)

- [x] **TEST + IMPLEMENT: T009 — Items are produced lazily, not buffered**
  - **USE COMMAND**: `/test-first when consuming a stream query should observe the first item before the handler produces the last item`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_consuming_stream_query_should_produce_items_lazily.cs`
  - Test should verify (NFR1/FR6, ADR risk "accidental buffering"):
    - A handler that records each item's production (e.g. increments a counter / signals per `yield`)
      has produced **fewer** than all items at the moment the consumer observes the first item
    - The framework does not materialise the whole sequence before the first `MoveNextAsync`
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Confirm the `ExecuteStream` + `BuildStream` chain are `async` iterators end-to-end (no
      `ToListAsync`/eager await); fix any eager materialisation surfaced by the test

- [x] **TEST + IMPLEMENT: T010 — Cancellation stops enumeration promptly**
  - **USE COMMAND**: `/test-first when the cancellation token is cancelled mid-stream should stop enumeration and propagate OperationCanceledException`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_stream_cancelled_mid_enumeration_should_stop_and_throw_OperationCanceledException.cs`
  - Test should verify (FR5):
    - Cancelling the token during `await foreach` stops further item production promptly
    - `OperationCanceledException` (or `TaskCanceledException`) propagates to the caller
    - `await foreach (... .WithCancellation(ct))` flows the token into the handler's
      `[EnumeratorCancellation]` parameter
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Ensure `ExecuteStream` applies `[EnumeratorCancellation]` and passes `.WithCancellation(ct)`
      to the inner enumeration (ADR §4); no special-casing beyond token flow

- [x] **TEST + IMPLEMENT: T011 — Exceptions mid-stream surface unwrapped**
  - **USE COMMAND**: `/test-first when the handler throws during enumeration should surface the original exception to the caller with its stack trace`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_stream_handler_throws_during_enumeration_should_surface_original_exception.cs`
  - Test should verify:
    - A handler that yields some items then throws surfaces that exact exception type/message from
      `await foreach`, **not** wrapped in `TargetInvocationException`
    - Items yielded before the fault were still observed
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Confirm no `TargetInvocationException` unwrap is applied around enumeration (the iterator
      defers, so `Invoke` returns the enumerable without running the body — ADR §4)

- [x] **TEST + IMPLEMENT: T012 — Early `break` releases handler, decorators, and span**
  - **USE COMMAND**: `/test-first when the caller breaks out of the stream early should release the handler decorators and end the span exactly once`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_stream_consumer_breaks_early_should_release_pipeline_and_end_span_once.cs`
  - Test should verify (A2, ADR risk "span/handler leak"):
    - Breaking out of `await foreach` after the first item disposes the enumerator, so the
      processor's `finally` releases handler + decorators via the recording factory **exactly once**
    - The tracer's span is ended exactly once on early termination
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Rely on the `try/finally` in the `ExecuteStream` iterator running on enumerator `DisposeAsync`
      (ADR §4); use `RecordingHandlerFactory` / `RecordingDecoratorFactory` doubles to assert release

- [x] **TEST + IMPLEMENT: T013 — Stream query sent to `ExecuteAsync` fails cleanly**
  - **USE COMMAND**: `/test-first when a stream query is passed to ExecuteAsync should throw ConfigurationException for no async handler`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_stream_query_passed_to_ExecuteAsync_should_throw_ConfigurationException.cs`
  - Test should verify (ADR §1 consequence):
    - A query implementing `IStreamQuery<TResult>` compiles as an `IQuery<TResult>` argument to
      `ExecuteAsync`, but fails at handler lookup with a clear `ConfigurationException` ("no async
      handler registered"), because stream handlers live only in the stream registry
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - No new code expected beyond existing async-registry miss behaviour; test documents/locks the
      cross-path guarantee

- [x] **TEST + IMPLEMENT: T014 — Missing/mismatched stream handler surfaces on first `MoveNextAsync`**
  - **USE COMMAND**: `/test-first when no stream handler is registered should throw ConfigurationException on the first enumeration step`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_no_stream_handler_registered_should_throw_ConfigurationException_on_first_move_next.cs`
  - Test should verify (ADR §4 "deferred configuration errors"):
    - Calling `ExecuteStream` for an unregistered stream query does **not** throw at call time
    - The `ConfigurationException` surfaces from the caller's **first** `await foreach` iteration
      (deliberate behavioural difference vs eager `Execute`/`ExecuteAsync`)
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Confirm handler resolution/validation runs inside the iterator (first `MoveNextAsync`), per the
      single-iterator shape (ADR §4)

---

## Phase 4 — Stream decorator pipeline (behaviour)

- [x] **TEST + IMPLEMENT: T015 — Stream decorators run ordered by `Step` descending**
  - **USE COMMAND**: `/test-first when a stream handler has multiple stream decorators should execute them ordered by step descending around the handler`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_stream_handler_has_multiple_decorators_should_order_by_step_descending.cs`
  - Test should verify:
    - Two+ `StreamQueryHandlerAttribute`-derived decorators wrap the handler innermost→outermost by
      `Step` (higher `Step` executes first), asserted via a recording/step-event decorator double
    - Each decorator can observe/transform items as they stream through `next`
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - `BuildStream` orders `StreamQueryHandlerAttribute`s by `Step` descending and chains
      `IStreamQueryHandlerDecorator` instances via `next` (ADR §6), reusing the async decorator factory
    - Add step-event stream decorator + attribute test doubles in `TestDoubles/`

- [x] **TEST + IMPLEMENT: T016 — Mismatched decorator attributes are rejected**
  - **USE COMMAND**: `/test-first when a stream handler has a sync or async decorator attribute should throw ConfigurationException and vice versa`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_stream_handler_has_mismatched_decorator_attribute_should_throw_ConfigurationException.cs`
  - Test should verify (ADR §3/§6, risk "legacy attributes on a stream"):
    - `QueryHandlerAttribute` / `QueryHandlerAttributeAsync` (e.g. `RetryableQuery`, `FallbackPolicy`)
      on a stream handler's `ExecuteAsync` throws `ConfigurationException`
    - A `StreamQueryHandlerAttribute` on a sync/async handler throws `ConfigurationException`
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Reuse `ValidateNoMismatchedAttributes(MemberInfo, Type, string)` driven by attribute base type
      in `BuildStream` (and add the reciprocal guard in `Build`/`BuildAsync` for
      `StreamQueryHandlerAttribute`) — ADR §6

- [x] **TEST + IMPLEMENT: T017 — Re-enumeration re-executes the query**
  - **USE COMMAND**: `/test-first when a stream is enumerated twice should re-execute the handler with a fresh pipeline each time`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_stream_enumerated_twice_should_re_execute_with_fresh_pipeline.cs`
  - Test should verify (ADR §4 "multiple/concurrent enumeration"):
    - Two `await foreach` passes over the same returned `IAsyncEnumerable<TResult>` each start a fresh
      iterator → fresh `PipelineBuilder`/handler (recording factory shows two creations) and re-yield
      the items (cold, not cached)
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Confirm the processor owns a fresh context per enumeration on the null-context path (ADR §4
      context-ownership caveat); no caching of the enumerable

---

## Phase 5 — Logging / telemetry stream decorators (behaviour)

- [ ] **TEST + IMPLEMENT: T018 — Stream logging decorator wraps the stream lifecycle**
  - **USE COMMAND**: `/test-first when a stream query has the logging decorator should log start yield each item and log completion with item count and duration`
  - Test location: `test/Paramore.Darker.Core.Tests` (Logging folder)
  - Test file: `When_stream_query_logged_should_wrap_lifecycle_with_item_count.cs`
  - Test should verify (FR7 Logging):
    - Logs on stream start, yields each item through `next` unchanged, and logs completion with item
      count + elapsed duration
    - Laziness preserved (decorator does not buffer — items flow through as produced)
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add a stream logging decorator (`src/Paramore.Darker/Logging/Handlers/`) as an `async` iterator
      over `next`, modelled on `QueryLoggingDecoratorAsync`, plus a `StreamQueryHandlerAttribute`-derived
      logging attribute and a builder-extension registration

- [ ] **TEST + IMPLEMENT: T019 — Stream logging/telemetry records enumeration faults**
  - **USE COMMAND**: `/test-first when a stream faults during enumeration should record the exception in the logging decorator and rethrow`
  - Test location: `test/Paramore.Darker.Core.Tests` (Logging folder)
  - Test file: `When_stream_faults_during_enumeration_should_record_exception_in_logging_decorator.cs`
  - Test should verify (FR7 Logging):
    - When the handler throws mid-stream, the logging decorator records the exception (log/span event)
      and the exception still propagates to the caller
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Wrap the decorator's `await foreach`/`yield` in `try/catch` that records then rethrows

- [ ] **TEST + IMPLEMENT: T020 — `BuildStream` writes a span event per pipeline step**
  - **USE COMMAND**: `/test-first when building a stream pipeline with a span should write a query event per pipeline step`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_building_stream_pipeline_with_span_should_write_event_per_step.cs`
  - Test should verify (FR7 Telemetry, mirror `When_building_async_pipeline_with_span_should_write_event_per_step`):
    - With a span on the context, enumerating the stream writes one `DarkerTracer.WriteQueryEvent` per
      decorator + the handler sink (correct `isAsync`/`isSink` tags)
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Emit `WriteQueryEvent` at each step in `BuildStream` consistent with `BuildAsync` (ADR §6)

---

## Phase 6 — Resilience for streams (behaviour — highest risk)

- [ ] **STRUCTURAL: T021 — Stream resilience attribute**
  - Add `UseResiliencePipelineStreamAttribute(int step, string policy) : StreamQueryHandlerAttribute`
    (`src/Paramore.Darker/Policies/Attributes/`) returning the stream resilience decorator type;
    **no** `useTypePipeline` parameter (ADR §3a — untyped pipeline only). Must compile.

- [ ] **TEST + IMPLEMENT: T022 — Resilience decorator yields items on the happy path**
  - **USE COMMAND**: `/test-first when a stream uses the resilience pipeline decorator and establishment succeeds should yield all items`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_stream_resilience_establishment_succeeds_should_yield_all_items.cs`
  - Test should verify (ADR §3a):
    - With a no-op/succeeding named pipeline resolved from `IQueryContext.ResiliencePipeline`, the
      decorator establishes the stream, pulls the first item inside the pipeline, then yields all items
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `UseResiliencePipelineStreamHandler<TQuery,TResult> : IStreamQueryHandlerDecorator<TQuery,TResult>`
      (`src/Paramore.Darker/Policies/Handlers/`) per the ADR §3a code: untyped
      `GetPipeline(_policy)`, `Establish` callback returning `(IAsyncEnumerator<TResult>, bool)`,
      `[EnumeratorCancellation]`, yield only after the pipeline succeeds; reuse the async decorator's
      `InitializeFromAttributeParams` pipeline-resolution logic

- [ ] **TEST + IMPLEMENT: T023 — Retry before the first item does not duplicate emission**
  - **USE COMMAND**: `/test-first when establishment fails before the first item should retry a fresh stream with no duplicate emission`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_stream_establishment_fails_before_first_item_should_retry_without_duplicates.cs`
  - Test should verify (ADR §3a correctness property — key risk):
    - A retry pipeline + a handler that throws on the first attempt before yielding, then succeeds,
      produces the full item sequence **exactly once** (no re-emission of already-yielded items)
    - The handler body was re-run on retry (fresh enumerable)
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Rely on first-`MoveNextAsync`-inside-the-pipeline (ADR §3a); no item leaves the decorator until
      the pipeline succeeds

- [ ] **TEST + IMPLEMENT: T024 — Faults after the first item are not retried**
  - **USE COMMAND**: `/test-first when a stream faults after the first item should propagate without retry and without re-emitting items`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_stream_faults_after_first_item_should_not_retry.cs`
  - Test should verify (ADR §3a, risk "users assume resilience covers whole stream"):
    - A fault raised **after** the first item has been yielded propagates to the caller and is **not**
      retried; previously yielded items are not re-emitted
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Confirm the pipeline has exited once the first item is yielded (subsequent `MoveNextAsync`
      faults propagate) — ADR §3a

- [ ] **TEST + IMPLEMENT: T025 — Each failed establishment attempt disposes its enumerator**
  - **USE COMMAND**: `/test-first when establishment retries N times should dispose the enumerator from each failed attempt`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_stream_establishment_retries_should_dispose_each_failed_enumerator.cs`
  - Test should verify (ADR §3a caveat + risk "enumerator leak on retry"):
    - With N failed establishment attempts, the decorator disposes N enumerators (the failed ones),
      asserted via an enumerable double that counts `DisposeAsync`
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Ensure the in-pipeline `Establish` callback disposes the enumerator in its `catch` before
      rethrowing (the reference article omits this) — ADR §3a

- [ ] **TEST + IMPLEMENT: T026 — Fallback strategy substitutes an alternate stream at establishment**
  - **USE COMMAND**: `/test-first when the resilience pipeline has a fallback strategy should substitute an alternate stream at establishment`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_stream_establishment_fails_with_fallback_strategy_should_yield_alternate_stream.cs`
  - Test should verify (ADR §3a Strategy applicability — Fallback):
    - A pipeline whose fallback strategy supplies an alternate `(enumerator, moved)` yields the
      alternate stream's items when the primary establishment faults
    - No double-dispose / primary leak (primary enumerator disposed by `Establish`'s `catch` before
      fallback fires)
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Confirm the outer `await using` owns only the (possibly fallback-substituted) enumerator (ADR §3a
      "Fallback disposal is clean")

---

## Phase 7 — Dependency Injection & registration wiring (behaviour)

- [ ] **TEST + IMPLEMENT: T027 — `AddHandlersFromAssemblies` registers stream handlers for DI**
  - **USE COMMAND**: `/test-first when AddDarker scans assemblies should register stream handlers so ExecuteStream resolves them from the container`
  - Test location: `test/Paramore.Darker.Extensions.Tests`
  - Test file: `When_AddHandlersFromAssemblies_scans_assembly_should_register_stream_handlers.cs`
  - Test should verify (FR4, ADR §5.5):
    - After `services.AddDarker().AddHandlersFromAssemblies(asm)` and `BuildServiceProvider`, resolving
      `IQueryProcessor` and calling `ExecuteStream` for a scanned stream query yields the handler's items
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add a `ServiceCollectionStreamHandlerRegistry` (mirror `ServiceCollectionHandlerRegistryAsync`)
      and invoke the stream scan from `AddHandlersFromAssemblies` (ADR §5.1/§5.5)
    - Thread the stream registry into the `HandlerConfiguration` built in
      `ServiceCollectionExtensions.BuildQueryProcessor` (ADR §5.4)

- [ ] **TEST + IMPLEMENT: T028 — Explicit `AddStreamHandlers` registration**
  - **USE COMMAND**: `/test-first when registering stream handlers explicitly via the builder should resolve and execute the stream query`
  - Test location: `test/Paramore.Darker.Extensions.Tests`
  - Test file: `When_registering_stream_handlers_explicitly_should_execute_stream_query.cs`
  - Test should verify (FR4/NFR5):
    - `AddDarker().AddStreamHandlers(r => r.Register<TQuery, TResult, THandler>())` registers the
      handler and `ExecuteStream` yields its items
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `AddStreamHandlers(Action<IStreamQueryHandlerRegistry>)` to `IDarkerHandlerBuilder` /
      `ServiceCollectionDarkerHandlerBuilder` (mirror `AddAsyncHandlers`)

- [ ] **TEST + IMPLEMENT: T029 — Non-DI `QueryProcessorBuilder` wires the stream registry**
  - **USE COMMAND**: `/test-first when a processor is built via QueryProcessorBuilder with a stream registry should execute a stream query`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_QueryProcessorBuilder_configured_with_stream_registry_should_execute_stream_query.cs`
  - Test should verify:
    - A `QueryProcessor` built through the fluent `QueryProcessorBuilder` (with a stream registry +
      reused async factories) executes a stream query via `ExecuteStream`
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Extend the builder's handler-configuration wiring (`Builder/`) to accept/pass the stream registry
      into `HandlerConfiguration` (only if not already covered by T006/T008)

---

## Phase 8 — Cross-target build, AOT, docs (verification)

- [ ] **VERIFY: T030 — Green on all target frameworks + AOT**
  - `dotnet build Darker.Filter.slnf -c Release` and `dotnet test Darker.Filter.slnf -c Release --no-build`
    pass on `netstandard2.0;net8.0;net9.0` (NFR3).
  - `Paramore.Darker.Tests.AOT` still builds/publishes (NFR4 — no new trim/AOT-hostile reflection
    beyond existing `BuildAsync`).
  - Not a `/test-first` task — runs the existing suite; fix regressions surfaced.

- [ ] **DOCS: T031 — Document streaming usage and semantics**
  - Update README / user docs with: `ExecuteStream` + `await foreach` usage; DI registration; and the
    **documented semantics** — deferred config error on first `MoveNextAsync` (ADR §4); resilience
    covers **establishment + first item only**, `Timeout` bounds start-up, `Hedging` unsupported (ADR §3a);
    legacy `RetryableQuery`/`FallbackPolicy` do **not** apply to streams (use
    `UseResiliencePipelineStream`); re-enumeration re-executes; caller-supplied context is single-enumeration.
  - Update `specs/012-streaming_results/README.md` checklist (Tasks ✅ / Implementation).

---

## Dependencies

```
T001 (pkg) ─┐
T002 (contracts) ─┬─> T004,T005 (registry) ─┐
T003 (decorator contract) ─────────────────┤
                                            ├─> T006 (wiring) ─> T007 (BuildStream) ─> T008 (ExecuteStream)
                                            │                                             │
   T008 ─> T009..T014 (correctness: lazy, cancel, throw, lifetime, cross-path, deferred) │
   T007/T008 ─> T015..T017 (decorator chain, mismatch, re-enumeration)                   │
   T015 ─> T018,T019 (logging)   T007 ─> T020 (telemetry events)                         │
   T003 ─> T021 ─> T022 ─> T023,T024,T025,T026 (resilience)                              │
   T006/T008 ─> T027,T028,T029 (DI + builder wiring)                                     │
   everything ─> T030 (cross-target/AOT) ─> T031 (docs)
```

## Risk-mitigation coverage (traceability to ADR "Risks and Mitigations")

| ADR risk | Covered by |
|---|---|
| Accidental buffering defeats streaming | **T009** (laziness) |
| Span/handler leak on abandoned enumeration | **T012** (early-break lifetime) |
| Legacy `RetryableQuery`/`FallbackPolicy` on a stream | **T016** (mismatch validation) + **T031** (docs) |
| Enumerator leak on resilience retry | **T025** (N failures ⇒ N disposals) |
| Users assume resilience covers whole stream | **T024** (post-first-item fault not retried) + **T031** |
| Duplicate emission on retry | **T023** (fresh stream, no duplicates) |
| `BuildAsync`/`BuildStream` divergence | keep resolve/validate/order factored (**T007**, **T015**) |
| Deferred config error surprising | **T014** (documents first-`MoveNextAsync` error) + **T031** |
