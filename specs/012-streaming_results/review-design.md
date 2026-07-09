# Review: design — 012-streaming_results (ADR 0019)

**Date**: 2026-07-09
**Threshold**: 60
**Verdict**: NEEDS WORK

## Findings

### 1. `IStreamQuery<TResult> : IQuery` cannot use the `Query<TResult>` base class, and breaks the `CreateQuerySpan<TResult>` call (Score: 90)

§1 asserts two things that the code contradicts. It declares `IStreamQuery<out TResult> : IQuery` (the **non-generic** marker) and simultaneously claims this "shares the `Query` base class (stable `Id`, tracing)" while "remaining distinct from `IQuery<TResult>` at the type level." Both cannot hold.

- The actual base class is `public abstract class Query<TResult> : IQuery<TResult>` — it derives from the **generic** `IQuery<TResult>`. A stream query deriving from `Query<TResult>` to get the stable `Id` would therefore also be an `IQuery<TResult>`, contradicting §1's "distinct from `IQuery<TResult>`."
- §4's processor calls `_tracer?.CreateQuerySpan(query, ...)`, but the signature is `Activity? CreateQuerySpan<TResult>(IQuery<TResult> query, ...)`. An `IStreamQuery<TResult>` implementing only non-generic `IQuery` does not satisfy the `IQuery<TResult>` parameter → does not compile.
- `CreateQuerySpan` also does `query is Query<TResult> q ? q.Id : null` (DarkerTracer.cs:70), which only yields the id if the stream query is a `Query<TResult>` (i.e. an `IQuery<TResult>`).

**Evidence**: ADR §1 "Derives from the **non-generic `IQuery`** marker so it shares the `Query` base class ... while remaining distinct from `IQuery<TResult>`". Code: `src/Paramore.Darker/Observability/Query.cs:16` `public abstract class Query<TResult> : IQuery<TResult>`; `src/Paramore.Darker/Observability/DarkerTracer.cs:47` `CreateQuerySpan<TResult>(IQuery<TResult> query, ...)` and line 70 `query is Query<TResult> q`. ADR §4 passes an `IStreamQuery<TResult>` to that method.

**Recommendation**: Resolve the type model explicitly. Either (a) make `IStreamQuery<out TResult> : IQuery<TResult>` (accepting stream queries are also `IQuery<TResult>`, and move the stream-vs-single dispatch to the handler registry rather than the marker), or (b) introduce a stream-specific span-creation overload / a shared non-generic id-bearing base and state stream queries do NOT reuse `Query<TResult>`. As written, §1 + §4 do not compile against the current tracer.

---

### 2. §4 span creation runs inside the deferred iterator, so configuration/resolution errors surface on first enumeration, not on the `ExecuteStream` call (Score: 62)

§4 places `CreateQuerySpan`, `InitQueryContext`, and `BuildStream` inside the `async IAsyncEnumerable` iterator body. An async iterator defers **all** body execution until the first `MoveNextAsync`, so the span/context/handler-resolution happen only when the caller starts enumerating. The ADR correctly leans on this for the "never-enumerated ⇒ no leak" property (which is sound), but does not acknowledge the flip side: `BuildStream` resolves the handler and can throw `ConfigurationException` (no handler registered, mismatched attributes) — with this shape that throws from the **first `MoveNextAsync`**, not from the `ExecuteStream` call, unlike `Execute`/`ExecuteAsync` which throw eagerly at build time.

**Evidence**: ADR §4 builds the pipeline and span inside the iterator body. `src/Paramore.Darker/QueryProcessor.cs:71`,`:110` build eagerly today; `ConfigurationException` from `PipelineBuilder` (e.g. PipelineBuilder.cs:196,:127) surfaces synchronously. The ADR discusses leak-on-abandon but never the deferred-exception semantics.

**Recommendation**: Document that stream configuration/resolution errors surface on first enumeration; decide whether that is acceptable or whether to eagerly validate (split a non-iterator outer method that resolves the handler, then an inner iterator that enumerates).

---

### 3. Multiple / concurrent enumeration of the returned `IAsyncEnumerable` is not addressed (Score: 60)

The ADR is silent on a stream query "executed once but enumerated twice." With the §4 shape, each `await foreach` spins up a fresh async-iterator state machine → fresh `PipelineBuilder`, handler, and span, so re-enumeration re-executes (and re-traces) the whole query. Defensible, but a sharp semantic difference from `ExecuteAsync` (materialised, re-readable `TResult`) and from any "the query ran once" expectation. Concurrent enumeration runs two independent pipelines/handlers/spans; per-enumeration span duplication has tracing implications.

**Evidence**: ADR §4 is a plain `async IAsyncEnumerable` iterator; nothing memoises or guards re-enumeration. No mention in Decision/Consequences/Risks.

**Recommendation**: Add an explicit paragraph: re-enumeration re-executes and re-traces; the returned enumerable is not cached/replayable; state the concurrent-enumeration behaviour. If single-execution is desired, design for it.

---

### 4. §6 glosses the generic-closing difference between `BuildStream` and `BuildAsync` (Score: 58)

§6 says `BuildStream` "follows the identical shape as `BuildAsync`." But existing decorator resolution closes the decorator generic with `IQuery<TResult>` as `TQuery`: `MakeGenericType(typeof(IQuery<TResult>), typeof(TResult))` (PipelineBuilder.cs:243,:279). Stream decorators are `IStreamQueryHandlerDecorator<TQuery,TResult> where TQuery : IStreamQuery<TResult>` — closing them with `IQuery<TResult>` violates the constraint. `BuildStream` must close with `typeof(IStreamQuery<TResult>)`, and the sink/`next` types differ. Not "identical shape"; the one part of the reuse that actually differs is under-specified.

**Evidence**: `src/Paramore.Darker/PipelineBuilder.cs:243`,`:279`; ADR §6 "It follows the identical shape as `BuildAsync`".

**Recommendation**: State that `BuildStream` closes stream decorators/handlers over `IStreamQuery<TResult>` (not `IQuery<TResult>`); the shared factored steps are resolve/order-by-Step only — close and sink differ.

---

### 5. §3a resilience code omits the `ResilienceContext` branch it claims to honour "exactly" (Score: 50)

§3a states "`ResilienceContext` is honoured exactly as in the async decorator," but the shown `Execute` body only calls `pipeline.ExecuteAsync(async ct => {...}, cancellationToken)` — it never branches on `Context.ResilienceContext != null`, unlike the real async decorator's four call-shapes (typed/untyped × context/no-context). As written the sample ignores any ambient `ResilienceContext`.

**Evidence**: `src/Paramore.Darker/Policies/Handlers/UseResiliencePipelineHandlerAsync.cs:82-109` branches on `resilienceContext != null`; ADR §3a passes only `cancellationToken`.

**Recommendation**: Add the `ResilienceContext` branch to the snippet or soften to "context-threading follows the async decorator's four-way branch."

---

### 6. Fallback described as both "supported" and "not supported" without one crisp statement (Score: 48)

FR7 says handler-method Fallback is "**Not supported**," §3a says a Polly fallback strategy "may substitute an alternate `(enumerator, moved)`," and Consequences (Positive) lists "retry / timeout / circuit-breaker / **fallback** ... available for streams." Individually reconcilable, but adjacent sections read as contradictory; the load-bearing distinction (handler-method fallback vs Polly fallback strategy) is only implied.

**Evidence**: ADR FR7 table row vs Consequences (Positive).

**Recommendation**: Add one up-front sentence: "Handler-method fallback: removed. Polly fallback strategy: supported at establishment only," and use it consistently.

---

## Notes on claims that VERIFIED correctly (not findings)

- `IQueryHandlerDecoratorAsync` `next`/`fallback` are `Func<TQuery, CancellationToken, Task<TResult>>` (IQueryHandlerDecoratorAsync.cs:10-13) — §3 mirroring accurate.
- `IQueryHandlerRegistryAsync` `Get`/`Register` exactly as §5 claims.
- Factory shapes: `IQueryHandlerFactoryAsync.Create(Type, IAmALifetime) : IQueryHandler` and generic `IQueryHandlerDecoratorFactoryAsync.Create<T>` — §5 "factories reused unchanged" holds given `IStreamQueryHandler : IQueryHandler` and `IStreamQueryHandlerDecorator : IQueryHandlerDecorator`.
- Resilience pipeline resolution API (`GetPipeline`, `GetPipeline<T>`, `TryGetPipeline`) verified (UseResiliencePipelineHandlerAsync.cs:62-108) — §3a reuse grounded.
- `PipelineBuilder<TResult>` is `internal sealed`, generic, `IDisposable`, uses reflection `Invoke` + `TargetInvocationException`/`ExceptionDispatchInfo` unwrap; `ValidateNoMismatchedAttributes` is a generalizable guard (PipelineBuilder.cs:15,81-88,184-189) — §6 accurate on these.
- Target frameworks `netstandard2.0;net8.0;net9.0` confirmed (Paramore.Darker.csproj:3); `Microsoft.Bcl.AsyncInterfaces` not yet in Directory.Packages.props, consistent with §7.
- §4 deferred-execution / leak-on-abandon reasoning is **correct** (not a defect): the whole method is one async iterator, so `BuildStream`/`Dispose` never run if never enumerated.
- All five open questions are resolved with rationale (Q1 §1, Q2 §4, Q3 §7/Decision, Q4 §7, Q5 FR7+§3a).

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 1 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 4 |
| 0-49 (Low) | 1 |

**Total findings**: 6
**Findings at or above threshold (60)**: 3 (Findings 1, 2, 3)

**Verdict**: NEEDS WORK — driven primarily by Finding 1 (the `IStreamQuery : IQuery` type model contradicts both the `Query<TResult>` base class and the `CreateQuerySpan<TResult>` signature the ADR calls in §4), plus the undocumented deferred-error and multiple-enumeration semantics.

---

## Resolution log (round 1 → ADR revised 2026-07-09)

All six findings addressed in `docs/adr/0019-streaming-query-pipeline.md`:

1. **[Critical] Type model** — `IStreamQuery<out TResult>` changed to derive from **`IQuery<TResult>`** (user decision). §1 rewritten: now compiles against `Query<TResult>` and `CreateQuerySpan<TResult>`; stream-vs-single dispatch moved to method + registry; documented that a stream query passed to `ExecuteAsync` fails cleanly with `ConfigurationException`.
2. **[Med] Deferred config errors** — §4 now documents that handler-resolution/attribute errors surface on first enumeration (not the call), accepted as the price of leak-on-abandon safety; eager type-only resolution added to Alternatives.
3. **[Med] Multiple/concurrent enumeration** — §4 now states the returned enumerable is cold/not cached; re-enumeration re-executes and re-traces; concurrent enumeration runs independent pipelines.
4. **[Med] Generic-close gloss** — §6 now states `BuildStream` closes over `IStreamQuery<TResult>` (not `IQuery<TResult>`) and calls out the sink/delegate difference; only resolve/validate/order are shared.
5. **[Med] `ResilienceContext` branch** — §3a code now shows the context-vs-token branch; prose corrected.
6. **[Low] Fallback wording** — a crisp up-front distinction (handler-method fallback removed vs Polly fallback strategy supported at establishment) added before the FR7 table.

**Status after revision**: round-2 review run (below).

---

# Review: design (round 2) — 012-streaming_results (ADR 0019)

**Date**: 2026-07-09
**Threshold**: 60
**Verdict**: NEEDS WORK (round 2) → all findings resolved (see resolution log)

## Round-1 fix verification
- #1 Type model → CONFIRMED FIXED (compiles against `Query<TResult>` / `CreateQuerySpan<TResult>`; no variance conflict; `ExecuteStream`/`ExecuteAsync` are distinct names, no overload ambiguity).
- #2 Deferred config errors → CONFIRMED FIXED (`QueryLifetimeScope` real; leak-on-abandon sound).
- #3 Multiple enumeration → CONFIRMED FIXED for the created-context path; exposed a residual race for caller-supplied context → **round-2 Finding 2**.
- #4 Generic close → CONFIRMED FIXED and reflection-valid.
- #5 ResilienceContext branch → FIX INTRODUCED NEW ISSUE → **round-2 Finding 1** (typed-pipeline branch doesn't compile).
- #6 Fallback wording → CONFIRMED FIXED (consistent everywhere).

## Round-2 findings

### 1. §3a `useTypePipeline` branch does not compile (Score: 78)
A `ResiliencePipeline<TResult>` (from `GetPipeline<TResult>`) can only execute `ValueTask<TResult>` callbacks; stream establishment executes a `(IAsyncEnumerator<TResult>, bool)`-returning callback, so the typed pipeline is type-incompatible, and the `?:` between `ResiliencePipeline<TResult>` and `ResiliencePipeline` does not type-unify.
**Evidence**: ADR §3a; `UseResiliencePipelineHandlerAsync.cs:84-109` keeps typed/untyped in disjoint `ValueTask<TResult>` branches.
**Recommendation**: Drop the typed branch for streams; untyped `GetPipeline(_policy)` with `ExecuteAsync<(enumerator,bool)>` only.

### 2. Caller-supplied `queryContext` is shared mutable state across re-enumerations (Score: 64)
§4 claims concurrent enumeration runs "independent pipelines," but a non-null supplied `queryContext` is mutated (`.Span`/`.Tracer`) on one shared instance → race between overlapping enumerations.
**Evidence**: ADR §4; single-valued mutable `IQueryContext.Span`/`.Tracer`.
**Recommendation**: Scope a supplied context to single enumeration; document; concurrent-safe only on the processor-created-context path.

### 3. `BuildStream` method resolution under-specified — `ExecuteAsync` name collision (Score: 55, below threshold)
`GetMethod("ExecuteAsync")` (no arg types) risks binding to the async overload / `AmbiguousMatchException`.
**Recommendation**: Resolve by signature (`IAsyncEnumerable<TResult>` return, `(TQuery, CancellationToken)`).

### 4. NFR4 (AOT) unaddressed (Score: 40, below threshold)
Design is reflection-based but introduces nothing beyond the existing (already `IsAotCompatible`) pipeline; just needed an explicit sentence.

## Requirements coverage
All FR1–FR9 covered (FR9 caveated for supplied-context concurrency by Finding 2). NFR1–NFR3, NFR5 covered; NFR4 was the only gap (Finding 4).

## Summary
| Score Range | Count |
|---|---|
| 90-100 | 0 |
| 70-89 | 1 |
| 50-69 | 2 |
| 0-49 | 1 |
**Total**: 4 — **At/above threshold (60)**: 2 (Findings 1, 2)

## Resolution log (round 2 → ADR revised 2026-07-09)
All four round-2 findings addressed in the ADR:
1. **[High] Typed-pipeline compile error** — §3a rewritten to use the **untyped** `GetPipeline(_policy)` only; added a note explaining a `ResiliencePipeline<TResult>` cannot wrap the tuple-returning establishment callback; removed `useTypePipeline` from the stream attribute and its caveat.
2. **[Med] Supplied-context race** — §4 now documents that a caller-supplied `queryContext` is single-enumeration-scoped; concurrent/repeat enumeration is safe only on the processor-created-context path.
3. **[Med] Method resolution** — §6 now requires signature-based resolution of the stream `ExecuteAsync` (return `IAsyncEnumerable<TResult>`, params `(TQuery, CancellationToken)`), avoiding name collision / `AmbiguousMatchException`.
4. **[Low] NFR4** — §7 now maps NFR4: same reflection as `BuildAsync`, existing `IsAotCompatible` posture carries over.

**Status after round-2 revision**: round-3 review run (below).

---

# Review: design (round 3) — 012-streaming_results (ADR 0019)

**Date**: 2026-07-09
**Threshold**: 60
**Verdict**: NEEDS WORK (round 3) → resolved (see resolution log)

## Round-2 fix verification
- #1 Typed-pipeline removal → **CONFIRMED FIXED** (reviewer probed Polly.Core 8.7.0 by reflection: non-generic `ResiliencePipeline.ExecuteAsync<TResult>` accepts the tuple as `TResult` for both the token and `ResilienceContext` overloads; ternary type-unifies).
- #2 Supplied-context single-enumeration → **CONFIRMED FIXED** (document-only decision, as accepted).
- #3 Signature-based resolution → **CONFIRMED FIXED**.
- #4 NFR4/AOT → **CONFIRMED FIXED** (`IsAotCompatible` set; existing `MakeGenericType`/`Invoke` confirmed).
- Also verified clean: `out TResult` variance, empty-stream disposal, no successful-establish leak window, type model.

## Round-3 findings

### 1. §5 "DI scan reads the same as async handlers" hid net-new plumbing (Score: 64)
The existing scan hard-matches `typeof(IQueryHandlerAsync<,>)` (`QueryHandlerRegistryAsync.cs:56`) and cannot pick up `IStreamQueryHandler<,>`; and §4's `new PipelineBuilder<TResult>(/* … */)` hand-waved a stream-registry ctor slot that `PipelineBuilder`'s ctor (`:35-49`) does not have. `QueryProcessor` (`:44-50`) must also read + thread a new `StreamHandlerRegistry`.
**Recommendation**: State the net-new wiring explicitly (new scan predicate, new registry type, new `PipelineBuilder` ctor slot, `QueryProcessor` threading, DI builder invoking the stream scan); "reads the same" applies to the *user*, not the internals.

### 2. §3a cancellation treated as retryable establishment failure (Score: 40, below threshold)
`Establish`'s undifferentiated `catch … throw` rethrows `OperationCanceledException` into the pipeline; a retry strategy without a cancellation-excluding `ShouldHandle` would retry a cancelled first pull.
**Recommendation**: Document the caveat.

### 3. §3a fallback/disposal interaction not walked through (Score: 35, below threshold)
Fallback-substitutes-alternate-stream is mechanically sound in Polly v8; the ADR didn't state that the primary enumerator is disposed by `Establish`'s catch before fallback fires (so no leak).
**Recommendation**: One clarifying sentence.

## Requirements coverage
All FR/NFR covered; FR4 was the item stressed by Finding 1 (asserted but under-specified) — now grounded.

## Summary
| Score Range | Count |
|---|---|
| 90-100 | 0 |
| 70-89 | 0 |
| 50-69 | 1 |
| 0-49 | 2 |
**Total**: 3 — **At/above threshold (60)**: 1 (Finding 1)

## Resolution log (round 3 → ADR revised 2026-07-09)
1. **[Med] §5 DI/registry plumbing** — §5 rewritten: registration mirrors async *for the user* but is net-new internally; lists the 5 explicit wiring changes (new stream `RegisterFromAssemblies` matching `typeof(IStreamQueryHandler<,>)`, `StreamHandlerRegistry` on `IHandlerConfiguration`, new `PipelineBuilder` ctor slot, `QueryProcessor` threading, DI builder invoking the stream scan). §4 ctor comment corrected to pass `_streamHandlerRegistry`.
2. **[Low] Cancellation caveat** — added to §3a caveats.
3. **[Low] Fallback disposal** — added to §3a caveats.

**Status after round-3 revision**: no known findings ≥ threshold remain. Three adversarial rounds complete (6 + 4 + 3 findings, all resolved). Ready for `/spec:approve design 0019`.
