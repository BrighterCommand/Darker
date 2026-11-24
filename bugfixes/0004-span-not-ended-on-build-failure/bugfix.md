# Bugfix: Query span not ended, exception not recorded, and (sync only) Activity.Current left on the orphaned span when the pipeline fails to build

**Linked Issue**: #415
**Status**: Verified

## Symptom
On all three paths (`Execute`, `ExecuteAsync`, `ExecuteStream`), `QueryProcessor` starts the query span before it builds the pipeline.
The `try` that records exceptions and calls `EndSpan` starts only after the build. So when the build throws (a `ConfigurationException`
for no handler registered, a wrong-kind decorator attribute, a decorator or handler that cannot be created, a handler with no public
`Execute`, and so on):

- `EndSpan` never runs. The span is never stopped, gets no duration and never reaches `ActivityStopped` listeners or exporters.
- `AddExceptionToSpan` never runs. The span gets no exception event, no `Error` status and no `error.type` tag.
- On the sync path, `Activity.Current` is left pointing at the orphaned span in the caller's flow. Later work on that flow becomes a
  child of a span that is never ended.

The exception itself still reaches the caller unchanged.

**Expected:** a build failure behaves like a handler failure. The span is stopped, the exception and `Error` status are recorded, and
`Activity.Current` goes back to its previous value.

Found by code trace; not yet reproduced at runtime.

**Suggested repro (not run):**
1. Register an `ActivityListener` on `paramore.darker` that samples `AllData` and collects stopped activities.
2. Build a `QueryProcessor` with a real `DarkerTracer` and a registry with no handler for the query.
3. Call `Execute` and catch the `ConfigurationException`.
4. Assert one stopped span with `Status == Error` and an exception event, and `Activity.Current` equal to its value before the call.
5. Repeat for `ExecuteAsync` (await) and `ExecuteStream` (first `MoveNextAsync`).

## Suspected Location
Verified at master `9d36676` (the issue's `fefa746` line numbers have not moved).

- `src/Paramore.Darker/QueryProcessor.cs`
  - **Sync `Execute`:** span created at `:73`. `pipelineBuilder.Build(...)` at `:77` is outside the `try` at `:79`. Catches at `:83-94`
    call `AddExceptionToSpan` (`:85`, `:91`); `EndSpan` is in the `finally` at `:95-98`.
  - **Async `ExecuteAsync`:** span created at `:113`. `BuildAsync` at `:117` is outside the `try` at `:119`. Catches at `:124-135`
    (`AddExceptionToSpan` at `:126`, `:132`); `EndSpan` in the `finally` at `:136-139`.
  - **Stream `ExecuteStream`:** an `async IAsyncEnumerable` iterator (`:143`). Span created at `:157`. `BuildStream` at `:161` is outside
    the inner `try` at `:162`, whose `finally` calls `EndSpan` at `:167`. The inner block is `try`/`finally` only, so this path records
    no exceptions at all, not even handler failures.
- `src/Paramore.Darker/Observability/DarkerTracer.cs`
  - `CreateQuerySpan` (`:47`) returns `null` when nothing is listening (`:50-51`); captures the previous current (`:55`), sets
    `Activity.Current = activity` (`:64`) and stores the previous value as a custom property (`:65`).
  - `AddExceptionToSpan` (`:131`) sets `Error` status, the exception event and `error.type`. Only the `QueryProcessor` catches call it.
  - `EndSpan` (`:156`): sets `Ok` if unset (`:161-162`), `Stop()` (`:168`), restores `Activity.Current` (`:171`) — the only restore.
- `src/Paramore.Darker/PipelineBuilder.cs`: `Build` `:60`, `BuildAsync` `:122`, `BuildStream` `:346`. Example throw sites: no handler
  `:273`/`:292`/`:446`/`:450`; handler cannot be created `:280`/`:299`/`:459`; decorator cannot be created `:329`/`:426`/`:490`;
  missing path method `:78`/`:140`/`:364`.

## Root-Cause Hypothesis
**Primary (falsifiable):** in all three `QueryProcessor` methods, `Build`/`BuildAsync`/`BuildStream` sits between `CreateQuerySpan` and
the `try` that holds `AddExceptionToSpan` (catches) and `EndSpan` (`finally`). A build-time exception leaves the method without either
call, so the span is never stopped and carries no exception or `Error` status.

**`Activity.Current`: the caller-visible leak is likely sync only.** `Activity.Current` is backed by an `AsyncLocal`.
- **Sync:** the write at `DarkerTracer.cs:64` happens in the caller's execution context; with `EndSpan` skipped, the caller sees the
  orphaned span as current after the exception. **Expected to leak to the caller.**
- **Async:** the async method builder's `Start` saves and restores the caller's execution context, including when the method throws
  synchronously before its first `await` (as here: `BuildAsync` is synchronous). The caller's `Activity.Current` should be unchanged.
  Inside the method, the `await using` teardown (`:107`) runs with the orphaned span still current, so any activity started during
  release would parent to it. **Probably not visible to the caller.**
- **Stream:** the span is created lazily on the first `MoveNextAsync` (see the existing test
  `When_no_stream_handler_registered_should_not_throw_at_call_time`), and each `MoveNextAsync` runs under the same save/restore.
  The outer `finally` teardown (`:169`) runs with the orphaned span current. **Probably not visible to the caller.**

The issue's "stays `Activity.Current`" claim is therefore likely true for sync only. On async and stream the observable defect is the
un-ended span with no exception. The regression tests should assert caller-side `Activity.Current` on all three paths to settle this.

**When the bug does not show:** `_tracer` is null; no listener on the source (`CreateQuerySpan` returns null at `:50-51`); a listener
samples `None` (`StartActivity` returns null, so `:64` never runs). Only a sampling listener triggers it.

**Adjacent, not affected (for confirm to check):** pipeline teardown is not skipped. The `using` (`:67`), `await using` (`:107`) and
stream outer `try`/`finally` (`:152`/`:169`) all wrap the build; each `Build*` creates `_lifetime` before anything can throw
(`PipelineBuilder.cs:66`, `:128`, `:351`). Covered by the existing
`When_no_sync_handler_is_registered_on_execute_should_surface_the_lookup_exception_and_dispose_the_scope_once` tests (sync/async/stream).

**Other adjacent points, out of scope unless confirm says otherwise:**
- `queryContext.Span` is set to the orphaned span (`:74`/`:114`/`:158`) and left there; a reused caller-supplied context would make it
  the parent of the next query span.
- The stream path never records exceptions, even during enumeration — a parity gap beyond the build-time case. Note that C# forbids
  `yield return` inside a `try` that has a `catch`, which is why the stream block is `try`/`finally` only; any stream fix must respect that.

**Fix shape the issue suggests (UNVERIFIED — to be proven or refuted in /bugfix:confirm):** move `Build`/`BuildAsync`/`BuildStream`
inside the same `try`/catch/`finally` that records exceptions and calls `EndSpan`, keeping sync, async and stream in step.

**Tests to follow:** `When_executing_sync_query_with_tracer_should_create_and_end_query_span.cs` and the async equivalent
(`CreateListener`, `CreateProcessorWithTracer`, `[Collection("DarkerActivitySource")]`),
`When_stream_consumer_breaks_early_should_release_pipeline_and_end_span_once.cs`,
`When_no_stream_handler_registered_should_throw_ConfigurationException_on_first_move_next.cs`, `DarkerActivitySourceCollection.cs`.

## Confirmed Root Cause
In `QueryProcessor`, each path creates the query span with `_tracer?.CreateQuerySpan(...)` (which sets `Activity.Current`,
`DarkerTracer.cs:64`) and then calls `Build`/`BuildAsync`/`BuildStream` **outside** the `try` that guards span completion:

| Path | Span created | Build call | `try` starts | Catches (`AddExceptionToSpan`) | `finally` (`EndSpan`) |
|---|---|---|---|---|---|
| Sync | `:73` | `:77` | `:79` | `:83-94` | `:95-98` |
| Async | `:113` | `:117` | `:119` | `:124-135` | `:136-139` |
| Stream | `:157` | `:161` | `:162` | none (`try`/`finally` only) | `:167` |

`EndSpan` is the only code that stops the query span and restores `Activity.Current` (`DarkerTracer.cs:168`, `:171`). A build-time
exception leaves before both the catches and the `finally`, so the span is never stopped, keeps status `Unset`, and gets no exception.

`Activity.Current` leaks **to the caller on the sync path only**. On async and stream, the async method / async iterator builder
restores the caller's `ExecutionContext` (`BuildAsync` throws before the first `await`, so the exception is stored in the Task). Inside
those methods the orphaned span is still current while `await using` / the outer `finally` dispose the pipeline.

## Evidence
- [x] **Red repro (runtime probe, run).** Scratch console app (net10.0, `ProjectReference` to `src/Paramore.Darker`), real
  `DarkerTracer`, `AllData` listener, empty registries, so each build throws `ConfigurationException` (no handler):
  ```
  sync:   threw ConfigurationException; started=1 stopped=0 spanStopped=False status=Unset events=0 Current=ORPHAN
  async:  threw ConfigurationException; started=2 stopped=0 spanStopped=False status=Unset events=0 Current=null
  stream: threw ConfigurationException; started=3 stopped=0 spanStopped=False status=Unset events=0 Current=null
  async-unawaited: syncThrow=False Current=null taskFaulted=True
  ```
  (`Activity.Current` was reset to null between paths.) The span is started but never stopped on all three paths; the caller's
  `Activity.Current` is left on the orphan on sync only; `ExecuteAsync` does not throw synchronously, it returns a faulted Task.
- [x] **Code-trace (opus sub-agent).**
  1. `CreateQuerySpan`: `DarkerTracer.cs:50-51` (null with no listeners), `:55` previous captured, `:57` `StartActivity`, `:64` sets current,
     `:65` stores previous.
  2. Build throws after the span is current: sync `PipelineBuilder.cs:273`/`:280`/`:78`/`:263-265`/`:329`; async `:292`/`:299`/`:140`/`:490`;
     stream `:446`/`:450`/`:459`/`:364`/`:426`. User code (handler/decorator factories, `InitializeFromAttributeParams` `:338`) also runs
     inside Build and can throw anything.
  3. Build touches no span state: it only reads `queryContext.Span` into closures (`:86`, `:149`, `:369`) and creates no child spans.
  4. Nothing else stops the query span: `.Stop(` and `Activity.Current =` appear only in `DarkerTracer.cs:64/168/171`; `EndSpan` is called
     only at `QueryProcessor.cs:97/138/167` and by the DB decorators for their own span; `AddExceptionToSpan` only at `QueryProcessor.cs:85/91/126/132`;
     pipeline/scope disposal never touches an `Activity`.
  5. The exception still reaches the caller unchanged; teardown runs (`:67`, `:107`, `:169`).

## Scope Notes
**Suggested fix (move Build inside the `try`): PARTIAL.**
- **Sync / async:** moving `Build`/`BuildAsync` inside the existing `try` gives `EndSpan`, `Error` status, the exception event and the
  `Activity.Current` restore. Two observable side effects the fix must own (or avoid with a narrower build-only `try`/`catch`):
  - build failures start being logged by `LogInformation(..., "An exception was thrown during [async] pipeline execution")` (`:92`, `:133`);
  - a `TargetInvocationException` thrown from user code during Build (e.g. a factory using `Activator.CreateInstance` on a throwing
    constructor) is today passed to the caller as-is; inside the `try` the `:83`/`:124` catches would unwrap it.
- **Stream:** moving `BuildStream` into the existing `try`/`finally` (`:162-167`) ends the span and restores `Activity.Current`, but
  `EndSpan` would then mark the failed build `Ok` (`DarkerTracer.cs:161-162`). Recording `Error` needs a catch, and C# forbids `yield
  return` inside a `try` with a `catch`, so it must be a separate build-only `try`/`catch` (legal: no `yield` in it).
- **Parity:** same defect on all three paths; the fix and regression tests cover sync, async and stream.
- **Tests:** assert the stopped span (count 1, `Error`, exception event) and caller-side `Activity.Current` on all three paths. On sync,
  reset `Activity.Current` in a `finally` so the leak cannot contaminate later tests in the `DarkerActivitySource` collection.

**Out of scope (pre-existing, separate issues):**
- The stream path never records exceptions thrown *during enumeration* (`:162-167` has no catch); needs a manual `MoveNextAsync` loop.
- `queryContext.Span` (`:74`/`:114`/`:158`) keeps pointing at the span after it ends — true on the success path too.
- **DB spans lose the ambient query span (verified by reading):** `CreateDbSpan` (`DarkerTracer.cs:94-127`) never sets
  `darker.previous_current`, so `EndSpan` on a DB span sets `Activity.Current = null` (`:166`, `:171`) instead of restoring the query
  span. The DB decorators (`QueryDbSpanDecorator.cs:43-47`, async `:51`) also never call `AddExceptionToSpan`, so a failing DB step is `Ok`.

**Not affected:** pipeline teardown — each `Build*` creates `_lifetime` first (`PipelineBuilder.cs:66`, `:128`, `:351`) and the
`using`/`await using`/outer `finally` wrap the build (existing `…dispose_the_scope_once` tests cover it). No other site creates a span
before a throwing call outside its `try` (`CreateQuerySpan` only at `QueryProcessor.cs:73/113/157`; `CreateDbSpan` is followed directly by `try`/`finally`).

## Regression Test
In `test/Paramore.Darker.Core.Tests/`, one per path, all `[Collection("DarkerActivitySource")]`:
- `When_the_sync_pipeline_fails_to_build_should_end_the_span_with_error_and_restore_activity_current.cs` (`SyncPipelineBuildFailureTracingTests`)
- `When_the_async_pipeline_fails_to_build_should_end_the_span_with_error_and_restore_activity_current.cs` (`AsyncPipelineBuildFailureTracingTests`)
- `When_the_stream_pipeline_fails_to_build_should_end_the_span_with_error_and_restore_activity_current.cs` (`StreamPipelineBuildFailureTracingTests`)

Each runs a query with no registered handler, under a real `DarkerTracer`, an `AllData` listener and a started caller `Activity`;
asserts `ConfigurationException`, then one stopped span with `Error` status, an `exception` event and `error.type = ConfigurationException`,
then `Activity.Current` is the caller's activity. The `finally` stops the caller activity and resets `Activity.Current`.

RED (net8/9/10/11, user-approved 2026-11-23): all three fail on `completed.ShouldHaveSingleItem()` — "should have single item but had 0 items".
The `Activity.Current` assertion is expected to hold already on async/stream (probe); it is kept for parity.

## Fix
`src/Paramore.Darker/QueryProcessor.cs` only. On each of `Execute`, `ExecuteAsync` and `ExecuteStream`, the `Build`/`BuildAsync`/`BuildStream`
call is wrapped in its own `try`/`catch (Exception)` that calls `_tracer?.AddExceptionToSpan(span, ex)` and `_tracer?.EndSpan(span)`, then
rethrows. So a build failure stops the span with `Error` status and the exception, and restores `Activity.Current`.

Why a narrow build-only `try`/`catch` instead of moving the build into the existing invocation `try`:
- **No other behaviour change.** Build failures are not newly logged by the invocation catch's `LogInformation`, and a
  `TargetInvocationException` thrown from user code during build still reaches the caller as it did before.
- **Stream.** It is the only legal shape there: C# forbids `yield return` inside a `try` that has a `catch`, and this block has no `yield`.

The three paths are kept in step (written inline, matching the deliberate sync/async/stream triplication). Regression tests green on
net8.0/9.0/10.0/11.0.
