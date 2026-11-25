# Bugfix: DB span end clears Activity.Current (sync), and a failing DB step is recorded as Ok

**Linked Issue**: #419
**Status**: Verified

## Symptom
Not yet reproduced. The issue was written from a code trace; every claim below was re-checked against `master` `6e9a163`.

**Claim A — the ambient activity is lost.**
- Observed (hypothesis): when a `[QueryDbSpan]` step's span ends, `Activity.Current` becomes `null`.
- Expected: `Activity.Current` goes back to what was current before the DB span started, normally the query span.
- Who would see it (sync): code in an outer decorator (lower `Step`) that runs after `next()` returns or throws, before
  `QueryProcessor` ends the query span. For example, a fallback run by an outer `FallbackPolicyDecorator`. Any `Activity` that
  third-party instrumentation (HttpClient, EF Core) starts there would become a new root instead of a child of the query span.

**Claim B — DB failures are not recorded.**
- Observed (hypothesis): a DB step that throws is exported with `Status == Ok`, no `exception` event and no `error.type`.
- Knock-on: `db.client.operation.duration` is derived from DB (`Client`) spans
  (`src/Paramore.Darker.Extensions.Diagnostics/Observability/DarkerMetricsFromTracesProcessor.cs:69-70`, `DbMeter.cs:53-56`), so a failing
  DB step also gets no `error.type` dimension there.
- Expected (per the issue): the DB span ends with `Error`, the exception event and `error.type`, as the query span does.
- **Caveat:** this behaviour is a documented design decision, not an oversight (see Root-Cause Hypothesis, Claim B).

**Suggested repro (not run):**
1. Use a real `DarkerTracer` and an `AllData` listener, as in the existing DB-span tests.
2. Put `[QueryDbSpan(step: 2, …)]` on the handler and an outer test decorator at `step: 1` that captures `Activity.Current`
   after `next()` returns.
3. Run it with a handler that succeeds, then with one that throws.
4. Repeat with `[QueryDbSpanAsync]` and `ExecuteAsync`.

## Suspected Location
- `src/Paramore.Darker/Observability/DarkerTracer.cs`
  - `CreateQuerySpan` (`:47`): captures `previous = Activity.Current` (`:55`), sets `Activity.Current = activity` (`:64`) and
    `SetCustomProperty("darker.previous_current", previous)` (`:65`).
  - `CreateDbSpan` (`:93-128`): `StartActivity(name, ActivityKind.Client, parentActivity?.Id)` (`:103-106`). It never sets
    `darker.previous_current`.
  - `EndSpan` (`:156-172`): promotes `Unset` to `Ok` (`:161-162`), reads `darker.previous_current` (`:166`), calls `span.Stop()`
    (`:168`), then sets `Activity.Current = previous` unconditionally (`:171`).
- `src/Paramore.Darker/Observability/Handlers/QueryDbSpanDecorator.cs:37-48`: `CreateDbSpan(_spanInfo!, Context.Span)` (`:39`),
  then `try { next } finally { EndSpan(dbSpan) }`, with no catch. The remarks at `:13-14` say: "Exception recording is left to the
  `QueryProcessor` (the span owner) to avoid duplicate exception events."
- `src/Paramore.Darker/Observability/Handlers/QueryDbSpanDecoratorAsync.cs:39-53`: the same shape (`:44`, `:47`, `:51`), with the
  same remarks at `:15-16`.
- `src/Paramore.Darker/QueryProcessor.cs`: the sync catches at `:95`/`:101` and `EndSpan` at `:107`, and the async ones at
  `:146`/`:152` and `:158`, act on the **query** span only.
- Attribute and info: `Observability/Attributes/QueryDbSpanAttribute.cs:31-46` (async: `QueryDbSpanAttributeAsync.cs:31-46`).
  `InitializeFromAttributeParams` (`QueryDbSpanDecorator.cs:27-34`, async `:29-36`) builds the `DbSpanInfo`, and
  `PipelineBuilder.cs:345-348` calls it.
- Ordering: `PipelineAttributeReader.cs:78` sorts by `OrderByDescending(Step)`, and `PipelineBuilder.cs:106-118` / `:170-181`
  wrap in that order, so the **lowest Step is outermost**.
- Design record: `docs/adr/0017-query-tracing-and-database-spans.md`:
  - `:229-230`: "the exception is recorded once by the processor (the span owner) to avoid duplicate exception events"
  - `:243-244`: the DB decorator "lets the processor record any exception"
  - `:304`: risk "Double exception events" → "centralise exception recording in the processor"
  - `:154-155`: `CreateQuerySpan` sets `Activity.Current` "so downstream steps and DB spans nest; `EndSpan` reverts it"
- Scope: only the two decorators call `CreateDbSpan` in `src/`. `Paramore.Darker.Caching` only tags `Context.Span`.

## Root-Cause Hypothesis

### Claim A — ambient loss (predicted: sync path only)
**Mechanism:**
- `CreateDbSpan` passes the parent as a string id. The DB span therefore has `ParentId`/`ParentSpanId` set, but
  `Activity.Parent == null`.
- `Activity.Start()` still records the activity that was current at start (the query span) as its previous active activity,
  and makes the DB span current.
- In current System.Diagnostics.DiagnosticSource (10.0.14, `Directory.Packages.props:32`), `Stop()` restores that previous
  activity. After `span.Stop()` (`:168`), `Activity.Current` should therefore be the query span.
  - Sub-agent confidence about 80%, from memory of the runtime source. Older versions restored `Parent`, which would be `null` here.
- `EndSpan` then overwrites it with `Activity.Current = null` (`:171`), because the DB span has no `darker.previous_current`.
- Either way the net result is `null`.

**Predictions for a runtime probe — sync (`Execute`):**
- In an outer decorator, straight after `next()` returns or throws: `Activity.Current == null`. Expected: the query span.
- Inside the handler: the DB span is current, which is correct. The DB decorator wraps the whole handler invocation, so there is
  no "after the DB step" point inside the handler.
- In the caller after `Execute`: its prior value, which is correct. The query span's `EndSpan` restores from its own property.
  So the leak is confined to the window between the DB span ending and the query span ending.

**Predictions for a runtime probe — async (`ExecuteAsync`):**
- The `null` write happens inside the `async` `QueryDbSpanDecoratorAsync.ExecuteAsync`. The async builder or the awaiter's
  captured `ExecutionContext` keeps it from reaching the awaiting outer decorator.
- Predicted: **no observable ambient loss on the async path.** This is the same `AsyncLocal` behaviour #415 showed.

### Claim B — DB failure recorded as Ok (both paths)
- When `next` throws, the decorator's `finally` runs `EndSpan(dbSpan)` with `Unset` status, which is promoted to `Ok`. There is
  no exception event or `error.type`.
- The exception reaches `QueryProcessor`'s catch, which records it on the **query** span only.
- Predicted on both paths: the DB span is `Ok` with no event; the query span is `Error` with one exception event.
- **This is the behaviour ADR 0017 decided** (`:229-230`, `:243-244`, `:304`), and the decorator remarks restate it. It is not a
  bug in the sense of code diverging from design.
  - The OTel convention is that each span reports its own failure. The ADR's "duplicate exception events" risk concerned the
    same exception recorded at several steps, not on two different spans. So there is a case for changing it.
  - That case is a **design change to an accepted ADR**. It needs the user's decision (and an ADR amendment), not a `/bugfix`
    fix. Raise it at the Confirm gate.

### Suggested fixes from the issue — **UNVERIFIED — to be proven or refuted in /bugfix:confirm**
- (A) Make `CreateDbSpan` record `darker.previous_current` the same way `CreateQuerySpan` does. Alternative: have `EndSpan` not
  overwrite `Activity.Current` when no previous was recorded, and rely on `Activity.Stop()`. That alternative depends on the
  DiagnosticSource `Stop()` behaviour above, so confirm must check it at runtime.
- (B) Have the DB decorators call `AddExceptionToSpan(dbSpan, ex)` in a catch. This contradicts ADR 0017 and the decorator remarks
  (see above).

### Existing tests
- `test/Paramore.Darker.Core.Tests/When_executing_sync_handler_with_db_span_attribute_should_create_child_db_span.cs:53-84` and
  the async variant (`:55-87`) check count, `ParentId`, the `db.*` tags and `Kind`. They do not check `Activity.Current`, status
  or a failing step.
- `When_creating_db_span_should_nest_under_parent_with_db_attributes.cs:29-38` calls `dbSpan.Stop()` directly, not `EndSpan`.
- `When_ending_span_should_set_ok_and_restore_previous_current.cs:21-56` covers query spans only.
- No test encodes the buggy behaviour. If B changes, check
  `test/Paramore.Darker.Extensions.Diagnostics.Tests/When_executing_query_with_metrics_wired_should_record_query_and_db_duration_metrics.cs:192-209`.

### Side note (not part of this bug)
`QueryDbSpanAttribute.cs:26` documents `step` as "higher executes first, outermost". This contradicts the code, where the lowest
Step is outermost.

## Confirmed Root Cause
**Claim A (defect, confirmed).**
- `DarkerTracer.EndSpan` (`src/Paramore.Darker/Observability/DarkerTracer.cs:166-171`) reads `darker.previous_current` and then
  always assigns `Activity.Current = previous`.
- Only `CreateQuerySpan` writes that property (`:55`, `:65`). `CreateDbSpan` (`:93-128`) never does.
- So for a DB span `previous` is `null`, and `:171` clears the ambient activity straight after `span.Stop()`. `Stop()` itself had
  already restored the query span.
- This breaks the documented contract: ADR 0017 `:137` says `EndSpan` "sets Ok if unset, restores previous Activity.Current".
  The interface doc (`IAmADarkerTracer.cs:41-45`) does not mention the restore at all.

Who sees the `null`:
- **`[QueryDbSpan]` (sync):** outer decorators (lower `Step`) after `next()` returns or throws, until `QueryProcessor` ends the
  query span. The caller is unaffected, because the query span's `EndSpan` restores from its own property.
- **`[QueryDbSpanAttributeAsync]` (async):** no one. The write happens inside an `async` method, and the awaiting frame keeps its
  own `ExecutionContext` (the same `AsyncLocal` behaviour as #415).
- **Direct `CreateDbSpan` + `EndSpan` in a handler:** the rest of that method, on sync **and** async, plus outer decorators on
  sync. ADR 0017 `:110`, `:239`, `:244-245` documents this direct use for "handlers needing finer scoping". No sample shows it.

**Claim B (not a defect).** A failing DB step leaves its span `Ok` with no exception event, because the decorators use
`try`/`finally` and `EndSpan` promotes `Unset` to `Ok`. This is what ADR 0017 decided (`:229-230`, `:243-244`, `:304`), and the
decorator remarks restate it (`QueryDbSpanDecorator.cs:13-14`, `QueryDbSpanDecoratorAsync.cs:15-16`).

## Evidence
- [x] **Red repro (runtime probe, run).** Scratch console app (net10.0, `ProjectReference` to `src/Paramore.Darker`; DiagnosticSource
  10.0.14). Real `DarkerTracer`, `AllData` listener, a started `caller` activity. In the pipeline, an outer probe decorator
  (step 1) captures `Activity.Current` after `next()`, around `[QueryDbSpan(2, …)]` / `[QueryDbSpanAttributeAsync(2, …)]`:
  ```
  direct: db.Parent=null db.ParentId==q.Id=True current-before-end=select orders
  direct: after raw db.Stop() Current=Q query
  direct: after EndSpan(db) Current=null   <-- expected Q query
  direct: after EndSpan(query) Current=caller (expected caller)
  sync  fail=False: outer-after-next Current=null     caller-after=caller  dbStatus=Ok dbEvents=0
  async fail=False: outer-after-next Current=QA query caller-after=caller  dbStatus=Ok dbEvents=0
  sync  fail=True:  outer-after-next Current=null     caller-after=caller  dbStatus=Ok dbEvents=0   [threw InvalidOperationException]
  async fail=True:  outer-after-next Current=QA query caller-after=caller  dbStatus=Ok dbEvents=0   [threw InvalidOperationException]
  ```
  - A bare `Activity.Stop()` on the DB span restores the query span. Its `Parent` is null (the parent is passed as a string id),
    so `Stop()` restores the previous active activity, not `Parent`.
  - `EndSpan`'s assignment at `:171` is what clears it.
  - Sync outer decorators see `null`; async ones see the query span.
  - The caller is unaffected on both paths.
  - The DB span is `Ok` with no events, even when the step throws.
- [x] **Code-trace (opus sub-agent).**
  - In `src/`, `Activity.Current` is written only at `DarkerTracer.cs:64` and `:171`.
  - The `PipelineBuilder` wrapper lambdas (`:107-117`, `:176-180`) only call `WriteQueryEvent` (`AddEvent`).
  - The `TargetInvocationException` catches only unwrap and rethrow.
  - `EndSpan` is called on query spans (`QueryProcessor.cs`) and on DB spans (the two decorators) only.
  - `When_creating_db_span_should_nest_under_parent_with_db_attributes.cs` ends its DB span with `Stop()` rather than `EndSpan`,
    which is why no test caught this.

## Scope Notes
**Suggested fixes for Claim A:**
- **A1 — CONFIRMED, minimal.** In `CreateDbSpan`, capture `previous = Activity.Current` before `StartActivity` and
  `SetCustomProperty("darker.previous_current", previous)` when the activity is not null, mirroring `CreateQuerySpan`.
  - One change in `DarkerTracer` fixes all three call shapes: the sync decorator, the async decorator and direct use.
  - It does not depend on how `Stop()` behaves. Query spans are unchanged.
- **A2 — PARTIAL.** Have `EndSpan` assign only when the property was recorded, and otherwise rely on `Stop()`.
  - The probe shows `Stop()` restores correctly on 10.0.14, but this relies on runtime behaviour.
  - It cannot tell "recorded null" (a root query span) from "not recorded".
  - It is a wider behaviour change. It would fit as defence in depth, but it is not the minimal fix.
- **Ending out of order** (`db1` start, `db2` start, `db1` end): A1, A2 and `Stop()` behave the same, because each restores what
  was current at start. Guarding with "restore only if `Current == span`" would be separate hardening, outside this fix.

**Tests:**
- **Red:** a `DarkerTracer` unit test: `CreateQuerySpan`, `CreateDbSpan`, `EndSpan(db)`, then assert `Activity.Current` is the
  query span. It fails today with `null`.
- **Pipeline (sync):** an outer decorator at step 1 captures `Activity.Current` after `next()` around `[QueryDbSpan(2, …)]`;
  assert it is the query span. It fails today with `null`.
- **Pipeline (async):** the twin passes today. Keep it only as a parity guard, if at all.
- A tracing test needs `SampleUsingParentId`, because `CreateDbSpan` passes a string parent id.

**Claim B: out of scope for this bugfix (user decision at the gate).** Changing it amends ADR 0017.
- **For changing it:** OTel expects each span to report its own failure. The ADR's "duplicate events" risk was about one span
  being recorded at several steps. `db.client.operation.duration` gets no `error.type` today.
- **For keeping it:** it is the accepted design, and the query span already carries the error. A change would alter exported
  telemetry and the metrics test `When_executing_query_with_metrics_wired_should_record_query_and_db_duration_metrics.cs:192-209`.

**Related, in scope with the fix (docs only):** `IAmADarkerTracer.EndSpan` (`:41-45`) should say it restores `Activity.Current`,
as ADR 0017 `:137` already says.

**Out of scope, separate finding (confirmed):** some docs give the step order backwards. The code
(`PipelineAttributeReader.cs:78` `OrderByDescending(Step)` plus the wrap loop) makes the **lowest Step outermost**, and
`PipelineStepDescription.cs:34` and `DecoratorOrderingRule.cs:42` agree. These say "higher executes first":
- `QueryDbSpanAttribute(.Async).cs:26`
- `UseResiliencePipelineAttribute(.Async/.Stream)`
- `CacheableQueryAttribute(.Async)`
- `ValidateQueryAttribute(.Async)`
- `CLAUDE.md:163-167`, which also contradicts itself

**Not affected:** the stream path (there is no stream DB-span decorator). `Paramore.Darker.Caching` only tags `Context.Span`.

### Decisions at the Confirm gate (user, 2026-11-25)
- **Claim A:** the diagnosis is approved. Fix with **A1**: record `darker.previous_current` in `CreateDbSpan`.
- **Claim B: include in this fix** (scope widened).
  - Amend ADR 0017 so that each span records its own failure. The DB span records the exception, while the query span still
    records it once as its owner.
  - Add `AddExceptionToSpan(dbSpan, ex)` in both DB decorators, and update their remarks.
  - Check the metrics test `When_executing_query_with_metrics_wired_should_record_query_and_db_duration_metrics.cs`.
  - B needs its own red test (sync and async).
- **Step-order docs: fix on this branch**, as a separate `docs:` commit correcting the XML docs that say "higher executes
  first". For `CLAUDE.md`, only propose wording; the user edits it.

## Regression Test
In `test/Paramore.Darker.Core.Tests/`, all `[Collection("DarkerActivitySource")]`. Red approved by the user on 2026-11-25; RED on
net8, 9, 10 and 11.

**Claim A (ambient restore):**
- `When_ending_a_db_span_should_restore_the_query_span_as_current.cs` (`DbSpanEndingTests`): `CreateQuerySpan`, then
  `CreateDbSpan`, then `EndSpan(db)`. Asserts `Activity.Current` is the query span. RED: it was `null`.
- `When_a_db_span_step_ends_should_leave_the_query_span_current_for_outer_decorators.cs` (`QueryDbSpanAmbientActivityTests`): an
  outer `[CurrentActivityCapturing(1)]` around `[QueryDbSpan(2, …)]` records `Activity.Current` after `next()`, and the test
  asserts it is the query span. RED: it was `null`.
- No async pipeline twin: the probe shows async is already correct, so the twin would pass on arrival. Direct use on any path
  is covered by the first test.

**Claim B (the DB span records its own failure):**
- `When_a_sync_db_span_step_throws_should_record_the_failure_on_the_db_span.cs` (`QueryDbSpanFailureSyncTests`)
- `When_an_async_db_span_step_throws_should_record_the_failure_on_the_db_span.cs` (`QueryDbSpanFailureAsyncTests`)
- Each asserts the DB span has `Error`, exactly one `exception` event and `error.type`, and that the query span has `Error` and
  exactly one `exception` event. RED: the DB span status was `Ok`.

New doubles in `TestDoubles/`:
- `CurrentActivityCapturingDecorator<,>`
- `CurrentActivityCapturingAttribute`
- `SyncHandlerWithDbSpanInsideCapturingDecorator`
- `ThrowingSyncHandlerWithDbSpanAttribute`
- `ThrowingAsyncHandlerWithDbSpanAttribute` (`Task.FromException`)

## Fix
**Design:** ADR 0017 is amended (approved by the user). The status stays Accepted, with the line "Amended 2026-11-25 (#419)…".
- §1 `CreateDbSpan` records the previous current activity.
- §7 Each span records its own failure; the query span records it once.
- §8 The DB decorator records the failure on the DB span, then rethrows.
- Risks: the DB decorator is the one exception to centralised recording.

**Code (the `fix:` commit):**
- `src/Paramore.Darker/Observability/DarkerTracer.cs` (Claim A, fix A1): `CreateDbSpan` captures `Activity.Current` before
  `StartActivity` and sets `darker.previous_current` on the new activity. `EndSpan` therefore restores the query span. Query
  spans are unchanged.
- `src/Paramore.Darker/Observability/Handlers/QueryDbSpanDecorator.cs` and `QueryDbSpanDecoratorAsync.cs` (Claim B):
  `catch (Exception ex) { Context.Tracer?.AddExceptionToSpan(dbSpan, ex); throw; }` before the existing `finally`. The remarks now
  say the failure is recorded on the DB span and then rethrown for the query span.
- `src/Paramore.Darker/Observability/IAmADarkerTracer.cs`: the `EndSpan` doc now says it restores `Activity.Current`.

**Metrics:** `error.type` was already on `DbClientOperationDurationAllowedTags` (`DarkerSemanticConventions.cs:166`), so
`db.client.operation.duration` now carries it with no metrics change. `Paramore.Darker.Extensions.Diagnostics.Tests` is green
(27/27 on each of net8–11).

**Regression tests:** all four are green on net8, 9, 10 and 11.

**Step-order docs (separate `docs:` commit; scope approved by the user):** the lowest `Step` is outermost, and has been since
2017 (`OrderByDescending(Step)` plus the wrap loop). Fixed:
- XML docs on `QueryDbSpanAttribute(.Async)`, `UseResiliencePipelineAttribute(.Async/.Stream)`, `ValidateQueryAttribute(.Async)`
  and `CacheableQueryAttribute(.Async)`, including both caching examples
- `src/Paramore.Darker.Caching/README.md`
- ADR 0002 (§3 and Step Ordering)

`CLAUDE.md` and `.agent_instructions/skills_overview.md`: wording proposed to the user, who edits those files.
