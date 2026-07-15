# Tasks — Agreement Dispatcher (Content- and Context-Based Handler Routing)

**Spec**: 013-agreement_dispatcher
**Issue**: [#349](https://github.com/BrighterCommand/Darker/issues/349)
**ADR**: [`docs/adr/0020-agreement-dispatch-handler-routing.md`](../../docs/adr/0020-agreement-dispatch-handler-routing.md)
**Design status**: Approved (`.design-approved`)

## How to work this list

- This feature is sequenced **Tidy First**: a **structural** commit (Phase 1) that changes shape
  with *no new behaviour* and leaves all existing tests green, then a **behavioural** commit
  (Phases 2–5) that introduces routing and its tests.
- **Structural tasks (Phase 1)** use **`/tidy-first`**. They introduce no new observable behaviour,
  so there is no failing test to write first — the *existing* suite is the safety net. Run the full
  suite before and after; it MUST stay green with **no *behavioural* test changes**. Note: widening
  `Get`'s signature is a breaking API change, so existing tests that call the old one-arg `Get(Type)`
  get a **mechanical** call-site update (1-arg → 3-arg) to keep compiling — that is part of the
  structural change, not a behaviour change. The affected tests are enumerated in the Phase 1 gate.
- **Behavioural tasks (Phases 2–5)** are `TEST + IMPLEMENT` and MUST use **`/test-first`**. Write
  the test, **STOP for IDE approval**, then implement. Do not write a behavioural test by hand and
  proceed.
- Do the phases in order. Within Phase 1, all sub-tasks land in one commit because widening `Get`
  is a breaking signature change that must compile across the registry + `PipelineBuilder`
  simultaneously.

### Key files (verified)

| Concern | File |
|---|---|
| Sync registry | `src/Paramore.Darker/QueryHandlerRegistry.cs`, `IQueryHandlerRegistry.cs` |
| Async registry | `src/Paramore.Darker/QueryHandlerRegistryAsync.cs`, `IQueryHandlerRegistryAsync.cs` |
| Stream registry | `src/Paramore.Darker/StreamQueryHandlerRegistry.cs`, `IStreamQueryHandlerRegistry.cs` |
| Pipeline resolution | `src/Paramore.Darker/PipelineBuilder.cs` (`ResolveHandler` :201, `ResolveHandlerAsync` :218, `ResolveStreamHandler` :367; Build call sites :67, :128, :282) |
| Existing exception | `src/Paramore.Darker/Exceptions/ConfigurationException.cs` |
| DI overrides | `src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionHandlerRegistry.cs`, `...RegistryAsync.cs`, `...StreamHandlerRegistry.cs` |
| Core tests | `test/Paramore.Darker.Core.Tests/` (TestDoubles in `TestDoubles/`) |
| DI tests | `test/Paramore.Darker.Extensions.Tests/` |

---

## Phase 1 — Structural (Tidy First): the `IResolveHandlers` seam

> **One commit.** No new behaviour. Existing tests pass unmodified. Use `/tidy-first`.
> Message the commit as structural (e.g. `refactor: model handler resolution as a route (IResolveHandlers)`).

- [x] **STRUCTURAL: Introduce the `IResolveHandlers` role and `FixedHandlerRoute`**
  - **USE COMMAND**: `/tidy-first introduce IResolveHandlers role and FixedHandlerRoute encapsulating today's type-based resolution`
  - Create `src/Paramore.Darker/IResolveHandlers.cs`:
    - Single method `Type ResolveHandlerType(IQuery query, IQueryContext context)`.
    - XML docs describing it as the routing role (decides which handler type serves a query execution).
  - Create `src/Paramore.Darker/FixedHandlerRoute.cs`:
    - Implements `IResolveHandlers`, holds one `Type handlerType`, returns it and **ignores** `query`/`context`.
    - Encapsulates today's fixed type-to-handler behaviour.
  - No test change: nothing constructs these yet.

- [x] **STRUCTURAL: Migrate the three registries' storage to `Dictionary<Type, IResolveHandlers>` and widen `Get`**
  - **USE COMMAND**: `/tidy-first change registry storage to Dictionary of IResolveHandlers, route type-based Register through FixedHandlerRoute, and widen Get to accept query and context`
  - For **each** of `QueryHandlerRegistry`, `QueryHandlerRegistryAsync`, `StreamQueryHandlerRegistry`:
    - Change `_registry`/`_routes` to `Dictionary<Type, IResolveHandlers>`.
    - `Register(Type,Type,Type)` keeps its existing duplicate-key + `HasMatchingResultType` validation, then stores a `FixedHandlerRoute` (was: stores the `Type`).
    - Replace `Get(Type queryType)` with `Get(Type queryType, IQuery query, IQueryContext context)`:
      `_routes.TryGetValue(queryType, out var route) ? route.ResolveHandlerType(query, context) : null`.
    - `RegisterFromAssemblies` and `Register<TQuery,TResult,THandler>()` are unchanged (they funnel through `Register(Type,Type,Type)`).
  - Update the three interfaces (`IQueryHandlerRegistry`, `IQueryHandlerRegistryAsync`, `IStreamQueryHandlerRegistry`):
    - `Get` signature widened to `(Type queryType, IQuery query, IQueryContext context)`.
    - Rewrite the `Get` XML docs to describe the **three** outcomes: absent type → `null`; present resolvable → handler `Type`; present routed entry that fails → *throws* `RoutingException` (forward-reference; the throw path arrives in Phase 3, but document it now so the contract is stated once).
  - **No new behaviour**: `FixedHandlerRoute` ignores `query`/`context`, so results are identical to today.

- [x] **STRUCTURAL: Thread `(query, context)` through `PipelineBuilder` resolution helpers**
  - **USE COMMAND**: `/tidy-first thread query and context into PipelineBuilder handler-resolution helpers so they call the widened registry Get`
  - Change the three private helpers to take the query + context already in scope at their callers:
    - `ResolveHandler` (`PipelineBuilder.cs:201`) — called from `Build` (:67), which holds `query` + `queryContext`.
    - `ResolveHandlerAsync` (:218) — called from `BuildAsync` (:128); its sync fallback (:238) forwards the same args.
    - `ResolveStreamHandler` (:367) — called from `BuildStream` (:282), which holds `query` + `queryContext`.
  - Each helper calls the widened `Get(queryType, query, context)`. The `null` → `ConfigurationException` guards (:205, :224, :373) are **unchanged** — "no handler registered" still throws exactly as today.
  - **Mechanical call-site updates (structural, expected).** Widening `Get` breaks every existing caller of the one-arg form; update each to pass the query + context in scope (or `null` where a unit test asserts pure lookup — the resolved handler type is unchanged). Verified callers to update:
    - `test/Paramore.Darker.Core.Tests/QueryHandlerRegistryTests.cs` (13 call sites, e.g. :21, :35, :54-56)
    - `test/Paramore.Darker.Core.Tests/When_stream_query_registered_should_resolve_stream_handler_type.cs` (:18, :32)
    - `test/Paramore.Darker.Core.Tests/When_scanning_assemblies_for_stream_handlers_should_register_only_stream_handlers.cs` (:19, :32, :46)
    - Plus any further callers the compiler surfaces (run the build to enumerate). These edits are behaviour-preserving: the assertions (which handler `Type` is returned) do not change.
  - **Gate**: `dotnet build Darker.Filter.slnf -c Release` then `dotnet test Darker.Filter.slnf -c Release --no-build` — full suite green. The **only** test edits permitted in this commit are the mechanical `Get` call-site signature updates above; **no test assertion or expected behaviour may change**.

---

## Phase 2 — Behavioural: `RoutingException` + `RoutingFailure`

> First behavioural commit begins here. Use `/test-first`.

- [x] **TEST + IMPLEMENT: RoutingException carries a distinct Reason and a message that differs per failure mode**
  - **USE COMMAND**: `/test-first when RoutingException constructed should expose RoutingFailure reason and compose a message distinct from ConfigurationException`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_routing_exception_constructed_should_expose_reason_and_compose_message.cs`
  - Test should verify:
    - `new RoutingException(RoutingFailure.NoHandlerResolved, typeof(SomeQuery))` has `Reason == NoHandlerResolved`, message names the query type, and `resolvedHandlerType` is not required.
    - `new RoutingException(RoutingFailure.UnregisteredCandidate, typeof(SomeQuery), typeof(SomeHandler))` has `Reason == UnregisteredCandidate` and a message that reads **differently** (names the resolved handler type).
    - `RoutingException` is **not** a `ConfigurationException` (a `catch (ConfigurationException)` must not catch it) — assert it does not derive from `ConfigurationException` / derives from `Exception`.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `src/Paramore.Darker/Exceptions/RoutingFailure.cs`: `enum RoutingFailure { NoHandlerResolved, UnregisteredCandidate }`.
    - Add `src/Paramore.Darker/Exceptions/RoutingException.cs : Exception` (NOT `: ConfigurationException`), canonical ctor `RoutingException(RoutingFailure reason, Type queryType, Type resolvedHandlerType = null)`, public `RoutingFailure Reason { get; }`, message composed from `reason` so the two sub-cases read differently.

---

## Phase 3 — Behavioural: `RoutedHandlers` and the sync routing `Register` overload

> Prove agreement dispatch end-to-end on the **sync** registry first; async/stream mirror it in Phase 4.

- [x] **TEST + IMPLEMENT: A routing function selects the handler by query content**
  - **USE COMMAND**: `/test-first when query registered with routing function should dispatch to handler chosen from query content`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_query_registered_with_routing_function_should_route_by_content.cs`
  - Test should verify:
    - Register one query type with a router `(q, ctx) => q.Date < cutover ? typeof(LegacyHandler) : typeof(NewHandler)` and candidate list `{ LegacyHandler, NewHandler }`.
    - Two instances of the **same** query type differing only in the `Date` field resolve (via `QueryProcessor.Execute`) to **different** handlers, in the **same** registry.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `src/Paramore.Darker/RoutedHandlers.cs : IResolveHandlers` — holds `_queryType`, a type-erased `Func<IQuery,IQueryContext,Type>`, and a `HashSet<Type> _candidates`. `ResolveHandlerType` runs the routing algorithm from ADR §"Implementation Approach" (see the error-path tasks for the throw branches).
    - Add the sync routing overload to `QueryHandlerRegistry`/`IQueryHandlerRegistry`:
      `Register<TQuery,TResult>(Func<TQuery,IQueryContext,Type> router, params Type[] candidateHandlerTypes) where TQuery : IQuery<TResult>`.
    - Store the generic router type-erased via a cast wrapper `(q,ctx) => router((TQuery)q, ctx)` (safe: keyed on `typeof(TQuery)`, `Get` only reached with `query.GetType() == queryType`).
    - Add `TestDoubles` as needed (a dated query + two handlers).

- [x] **TEST + IMPLEMENT: A routing function selects the handler from IQueryContext**
  - **USE COMMAND**: `/test-first when routing function reads IQueryContext bag should route by context not query content`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_routing_function_reads_context_should_route_by_context.cs`
  - Test should verify:
    - Router decides on `context.Bag["..."]` (query content identical across both calls).
    - Executing the same query with two different context bag values reaches two different handlers.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - No new production code beyond Phase 3's `RoutedHandlers`/overload if already general; if the router wasn't receiving `context`, thread it. (Expected: already satisfied — this test guards the context path.)

- [x] **TEST + IMPLEMENT: Routing to null throws RoutingException(NoHandlerResolved), distinct from unregistered-query-type**
  - **USE COMMAND**: `/test-first when routing function returns null should throw RoutingException NoHandlerResolved distinct from unregistered query type`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_routing_function_returns_null_should_throw_RoutingException_NoHandlerResolved.cs`
  - Test should verify:
    - Router returns `null` → `Execute` throws `RoutingException` with `Reason == NoHandlerResolved`.
    - It is **not** the `ConfigurationException` thrown for an unregistered query type (contrast with the existing `When_execute_called_with_no_sync_handler_should_throw_configuration_exception` behaviour).
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - In `RoutedHandlers.ResolveHandlerType`: `if (handlerType is null) throw new RoutingException(RoutingFailure.NoHandlerResolved, _queryType);`.

- [x] **TEST + IMPLEMENT: Routing to a non-candidate type throws RoutingException(UnregisteredCandidate)**
  - **USE COMMAND**: `/test-first when routing function returns type outside candidate set should throw RoutingException UnregisteredCandidate`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_routing_function_returns_non_candidate_should_throw_RoutingException_UnregisteredCandidate.cs`
  - Test should verify:
    - Router returns a handler `Type` not in the registered candidate set → `Execute` throws `RoutingException` with `Reason == UnregisteredCandidate` and the resolved handler type surfaced.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - In `RoutedHandlers.ResolveHandlerType`: `if (!_candidates.Contains(handlerType)) throw new RoutingException(RoutingFailure.UnregisteredCandidate, _queryType, handlerType);` else return it.

- [x] **TEST + IMPLEMENT: Registering a candidate that does not implement the handler interface is rejected at registration time**
  - **USE COMMAND**: `/test-first when routing registered with candidate not implementing handler interface should throw ConfigurationException at registration`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_routing_candidate_does_not_implement_handler_interface_should_throw_ConfigurationException.cs`
  - Test should verify:
    - Passing a candidate type that is not an `IQueryHandler<TQuery,TResult>` throws `ConfigurationException` **from `Register`** (registration-time), *not* `RoutingException` (which is execution-time).
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - In the routing `Register` overload, validate each candidate with `IsAssignableFrom` against `IQueryHandler<TQuery,TResult>` (async/stream use their own handler interface in Phase 4); throw `ConfigurationException` naming the offending candidate.

- [x] **TEST + IMPLEMENT: An exception thrown inside the routing function surfaces to the caller unwrapped**
  - **USE COMMAND**: `/test-first when routing function itself throws should surface original exception not wrapped in RoutingException`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_routing_function_throws_should_surface_original_exception.cs`
  - Test should verify:
    - A router `(q, ctx) => throw new InvalidOperationException("boom")` → `Execute` throws that **same** `InvalidOperationException` (message preserved), **not** a `RoutingException` and **not** a `ConfigurationException`.
    - The registry does not swallow or wrap exceptions thrown *inside* the func — only the *decision outcomes* (null / non-candidate) become `RoutingException`.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Confirm `RoutedHandlers.ResolveHandlerType` invokes `_router(...)` directly with no surrounding try/catch, so an in-func throw propagates with its stack trace intact. Expected: already satisfied by the Phase 3 implementation — this is a guard test pinning the ADR Risks contract ("does not swallow exceptions thrown *inside* the func").

- [x] **TEST + IMPLEMENT: A routed handler still runs its decorator pipeline**
  - **USE COMMAND**: `/test-first when handler selected by routing function should still apply its decorator pipeline`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_handler_selected_by_routing_should_apply_decorator_pipeline.cs`
  - Test should verify:
    - The handler chosen by the router carries a decorator attribute (e.g. an existing step/logging/recording decorator from TestDoubles) on its `Execute` method, and executing the routed query runs that decorator — proving hand-off to the existing pipeline-building/decorator-resolution machinery is unchanged (requirements.md:65-67).
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - No new production code expected — routing returns a `Type` that re-enters `PipelineBuilder` exactly as type-based resolution does. This test guards that the routing seam did not bypass decorator discovery.

- [x] **TEST + IMPLEMENT: Agreement dispatch cannot be combined with type-based/auto-scan registration for the same query type**
  - **USE COMMAND**: `/test-first when routing function registered for a query type already registered should throw ConfigurationException duplicate`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_routing_registered_for_already_registered_query_type_should_throw_ConfigurationException.cs`
  - Test should verify:
    - `Register<TQuery,TResult,THandler>()` (or `RegisterFromAssemblies`) then routing `Register` for the **same** query type → `ConfigurationException` (duplicate key), and vice-versa.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - The routing overload performs the **same** duplicate-key guard as `Register(Type,Type,Type)` before storing `RoutedHandlers` — the single dictionary makes the constraint fall out automatically (no special case).

---

## Phase 4 — Behavioural: mirror routing on the Async registry

> Same behaviours as Phase 3, proven on the async registry per the acceptance criteria
> ("sync, async, and streaming"). One `/test-first` task per behaviour, matching Phase 3's granularity.
> The first task adds the async routing overload (production code); the rest are behaviour tests over it.

- [x] **TEST + IMPLEMENT: Async routing function selects the handler by query content**
  - **USE COMMAND**: `/test-first when async query registered with routing function should route by content`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_async_query_registered_with_routing_function_should_route_by_content.cs`
  - Test should verify:
    - Via `ExecuteAsync`, two same-type queries differing only in a routed field reach different async handlers in one registry.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add the routing `Register` overload to `QueryHandlerRegistryAsync`/`IQueryHandlerRegistryAsync` with `where TQuery : IQuery<TResult>`, validating candidates against `IQueryHandlerAsync<TQuery,TResult>`; reuse `RoutedHandlers`/`RoutingException`.

- [x] **TEST + IMPLEMENT: Async routing function selects the handler from IQueryContext**
  - **USE COMMAND**: `/test-first when async routing function reads IQueryContext should route by context`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_async_routing_function_reads_context_should_route_by_context.cs`
  - Test should verify: context-bag-based routing to two different async handlers with identical query content.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should: no new production code beyond the async overload (guard test).

- [x] **TEST + IMPLEMENT: Async routing to null throws RoutingException(NoHandlerResolved)**
  - **USE COMMAND**: `/test-first when async routing function returns null should throw RoutingException NoHandlerResolved`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_async_routing_function_returns_null_should_throw_RoutingException_NoHandlerResolved.cs`
  - Test should verify: `ExecuteAsync` throws `RoutingException` with `Reason == NoHandlerResolved`, distinct from the unregistered-query-type `ConfigurationException`.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should: reuse `RoutedHandlers` throw path (guard test).

- [x] **TEST + IMPLEMENT: Async routing to a non-candidate throws RoutingException(UnregisteredCandidate)**
  - **USE COMMAND**: `/test-first when async routing function returns non-candidate should throw RoutingException UnregisteredCandidate`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_async_routing_function_returns_non_candidate_should_throw_RoutingException_UnregisteredCandidate.cs`
  - Test should verify: `ExecuteAsync` throws `RoutingException` with `Reason == UnregisteredCandidate`, resolved type surfaced.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should: reuse `RoutedHandlers` throw path (guard test).

- [x] **TEST + IMPLEMENT: Async candidate-validation and duplicate-registration guards**
  - **USE COMMAND**: `/test-first when async routing registered with bad candidate or duplicate query type should throw ConfigurationException`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_async_routing_registration_invalid_should_throw_ConfigurationException.cs`
  - Test should verify (registration-time): a candidate not implementing `IQueryHandlerAsync<TQuery,TResult>` → `ConfigurationException`; and routing `Register` for an already-registered query type → `ConfigurationException` (duplicate key).
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should: async overload performs the same `IsAssignableFrom` candidate check and duplicate-key guard as sync.

## Phase 4b — Behavioural: mirror routing on the Stream registry

> Streaming resolves the handler type **before** enumeration, so routing errors surface at
> build/first-resolve (consistent with ADR 0019 ordering). One `/test-first` task per behaviour.

- [x] **TEST + IMPLEMENT: Stream routing function selects the handler by query content**
  - **USE COMMAND**: `/test-first when stream query registered with routing function should route by content`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_stream_query_registered_with_routing_function_should_route_by_content.cs`
  - Test should verify: two same-type stream queries differing in a routed field enumerate through different stream handlers.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add the routing `Register` overload to `StreamQueryHandlerRegistry`/`IStreamQueryHandlerRegistry` with `where TQuery : IStreamQuery<TResult>`, validating candidates against `IStreamQueryHandler<TQuery,TResult>`; reuse `RoutedHandlers`/`RoutingException`.

- [x] **TEST + IMPLEMENT: Stream routing function selects the handler from IQueryContext**
  - **USE COMMAND**: `/test-first when stream routing function reads IQueryContext should route by context`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_stream_routing_function_reads_context_should_route_by_context.cs`
  - Test should verify: context-bag-based routing to two different stream handlers with identical query content.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should: no new production code beyond the stream overload (guard test).

- [x] **TEST + IMPLEMENT: Stream routing to null throws RoutingException(NoHandlerResolved) before enumeration**
  - **USE COMMAND**: `/test-first when stream routing function returns null should throw RoutingException NoHandlerResolved at resolve time`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_stream_routing_function_returns_null_should_throw_RoutingException_NoHandlerResolved.cs`
  - Test should verify: `RoutingException` with `Reason == NoHandlerResolved` surfaces at handler resolution (before any item is yielded), distinct from the unregistered-query-type `ConfigurationException`.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should: reuse `RoutedHandlers` throw path (guard test).

- [x] **TEST + IMPLEMENT: Stream routing to a non-candidate throws RoutingException(UnregisteredCandidate)**
  - **USE COMMAND**: `/test-first when stream routing function returns non-candidate should throw RoutingException UnregisteredCandidate`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_stream_routing_function_returns_non_candidate_should_throw_RoutingException_UnregisteredCandidate.cs`
  - Test should verify: `RoutingException` with `Reason == UnregisteredCandidate`, resolved type surfaced, at resolve time.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should: reuse `RoutedHandlers` throw path (guard test).

- [x] **TEST + IMPLEMENT: Stream candidate-validation and duplicate-registration guards**
  - **USE COMMAND**: `/test-first when stream routing registered with bad candidate or duplicate query type should throw ConfigurationException`
  - Test location: `test/Paramore.Darker.Core.Tests`
  - Test file: `When_stream_routing_registration_invalid_should_throw_ConfigurationException.cs`
  - Test should verify (registration-time): a candidate not implementing `IStreamQueryHandler<TQuery,TResult>` → `ConfigurationException`; and routing `Register` for an already-registered stream query type → `ConfigurationException` (duplicate key).
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should: stream overload performs the same `IsAssignableFrom` candidate check and duplicate-key guard as sync.

---

## Phase 5 — Behavioural: DI integration

- [ ] **TEST + IMPLEMENT: Sync DI routing registration registers every candidate handler in the container**
  - **USE COMMAND**: `/test-first when sync routing registration used via ServiceCollection should register all candidate handler types in the container`
  - Test location: `test/Paramore.Darker.Extensions.Tests`
  - Test file: `When_sync_routing_registration_used_should_register_all_candidate_handlers.cs`
  - Test should verify:
    - After registering a routed query with candidates `{ A, B }` through the DI builder, resolving the built `IQueryProcessor` and calling `Execute` routes to both `A` and `B` — i.e. both candidate handler types were added to the container and are creatable by the factory.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Override the routing `Register` overload in `ServiceCollectionHandlerRegistry` to `TryAdd` each candidate handler type (symmetric with the existing single-handler override at `ServiceCollectionHandlerRegistry.cs:18-23`), then delegate to base.

- [ ] **TEST + IMPLEMENT: Async DI routing registration registers every candidate handler in the container**
  - **USE COMMAND**: `/test-first when async routing registration used via ServiceCollection should register all candidate handler types in the container`
  - Test location: `test/Paramore.Darker.Extensions.Tests`
  - Test file: `When_async_routing_registration_used_should_register_all_candidate_handlers.cs`
  - Test should verify: routed query with candidates `{ A, B }` via the DI builder; `ExecuteAsync` reaches both async candidates.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Override the routing `Register` overload in `ServiceCollectionHandlerRegistryAsync` to `TryAdd` each candidate, then delegate to base.

- [ ] **TEST + IMPLEMENT: Stream DI routing registration registers every candidate handler in the container**
  - **USE COMMAND**: `/test-first when stream routing registration used via ServiceCollection should register all candidate handler types in the container`
  - Test location: `test/Paramore.Darker.Extensions.Tests`
  - Test file: `When_stream_routing_registration_used_should_register_all_candidate_handlers.cs`
  - Test should verify: routed stream query with candidates `{ A, B }` via the DI builder; the stream pipeline enumerates through both stream candidates.
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Override the routing `Register` overload in `ServiceCollectionStreamHandlerRegistry` to `TryAdd` each candidate, then delegate to base.

---

## Phase 6 — Verification & wrap-up

- [ ] **Full regression**: `dotnet build Darker.Filter.slnf -c Release` && `dotnet test Darker.Filter.slnf -c Release --no-build` — all green, including the AOT test project (routing adds no new reflection).
- [ ] **Backwards-compat check**: confirm the Phase 1 structural commit changed existing tests **only** via the mechanical `Get` call-site signature updates (no assertion/behaviour changes); all pre-existing type-based dispatch behaviour is unchanged. (Satisfies the revised requirements AC: "behaviour and assertions unchanged; mechanical source-level updates required by a deliberate breaking API change are permitted at call sites" — here the ADR's `Get`-widen; see ADR "Breaking API surface (accepted for V5)".)
- [ ] **XML docs**: verify the three `Get` interface docs now describe the three outcomes and the `RoutingException` throw path (per ADR Negative consequence).
- [ ] **Update spec status**: tick Task Breakdown / Implementation in `README.md`; update `docs/adr/0020-*` status if implementation reveals any deviation.

---

## Risk mitigation tasks

- [ ] **Router that throws is not swallowed** — covered by the Phase 3 test-first task *"An exception thrown inside the routing function surfaces to the caller unwrapped"* (ADR Risks: "a routing Func that throws leaks... the registry only wraps the decision outcomes"). Listed here for traceability; no separate work.
- [ ] **Decorator hand-off preserved** — covered by the Phase 3 test-first task *"A routed handler still runs its decorator pipeline"* (requirements.md:65-67). Listed here for traceability; no separate work.
- [ ] **Candidate/DI drift guard** — the DI tests in Phase 5 double as the mitigation: candidates come from the same declared array used for the allow-list, so container and allow-list cannot drift.
- [ ] **Hot-path cost** — sanity-check (optionally via `test/Paramore.Darker.Benchmarks`) that routed resolution is one delegate call + one `HashSet.Contains`, no reflection/allocation beyond type-based resolution (NFR: "no overhead beyond invoking a `Func`").
