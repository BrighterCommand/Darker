# Review: design — 014-Caching-Decorator

**Date**: 2026-07-21
**Threshold**: 60
**Verdict**: PASS (round 2)

_Round 2 re-review of `docs/adr/0021-caching-decorator-architecture.md` after the round-1 revision. Round 1 (2026-07-20) returned NEEDS WORK with two findings — a false "no metrics primitive exists" claim (85) and an undocumented stampede hit/miss miscount (62); both are verified genuinely resolved below. Every codebase claim the rewrite makes was re-verified against source; the metrics-reuse rewrite introduced no new above-threshold problem._

## Verification summary

**Metrics-from-traces reuse (the Finding-1 rewrite) — all accurate:**
- `DarkerSemanticConventions.MeterName == "paramore.darker"` lives in **core** (`src/Paramore.Darker/Observability/DarkerSemanticConventions.cs:96`); `QueryDurationMetricName` (`:99`), `QueryDurationAllowedTags` (`:125`), and `DbClientOperationDurationAllowedTags` (`:144`) all live there too. Adding `CacheOutcome`/`CacheRequestsMetricName`/`CacheRequestsAllowedTags` alongside them is consistent.
- `QueryMeter`/`DbMeter` are `(IMeterFactory meterFactory, MeterProvider meterProvider)` and build their instrument via `meterFactory.Create(DarkerSemanticConventions.MeterName)`, record `activity.TagObjects.Filter(<allowed set>)`, and expose `Enabled` (`QueryMeter.cs:40-59`, `DbMeter.cs:40-59`). The proposed `CacheMeter` (a `Counter<long>` created the same way, `RecordCacheOperation(Activity)`, `Enabled`) genuinely mirrors this — the only difference is `Counter<long>` vs `Histogram<double>`, correct since FR10 asks for a counter.
- `DarkerMetricsFromTracesProcessor` short-circuits on `if (!(queryMeter.Enabled || dbMeter.Enabled)) return;`, filters to the `paramore.darker` source, and dispatches `ActivityKind.Internal → queryMeter`, `Client → dbMeter` (`DarkerMetricsFromTracesProcessor.cs:53-72`). Adding a `cacheMeter.RecordCacheOperation` call on the `Internal` branch and `|| cacheMeter.Enabled` to the guard is feasible.
- `AddDarkerInstrumentation(this MeterProviderBuilder)` registers meters via `TryAddSingleton` and calls `AddMeter(MeterName)` (`DarkerMetricsBuilderExtensions.cs:31-42`). Registering `CacheMeter` there and adding an opt-out `bool` param is a non-breaking, consistent change. (Current signature is parameterless — the ADR correctly frames this as a proposed extension.)
- `IQueryContext.Span` exists and is typed `Activity?` in core (`IQueryContext.cs:22`); the caching package writes to it via `Context.Span?.SetTag(...)`. `Activity`/`SetTag` come from `System.Diagnostics.DiagnosticSource` (already a core dependency), **not** OpenTelemetry — so the "no OTel/metrics dependency in the caching package" claim is honest.

**Span ordering — no problem:** `QueryProcessor` creates the query span, sets `queryContext.Span = span`, then builds+invokes the pipeline, then `EndSpan(span)` in a `finally` (`QueryProcessor.cs:112-137`). The decorator's `SetTag` during pipeline execution lands on the still-open span; `OnEnd`/`RecordCacheOperation` reads it at span end. Correct.

**Previously-confirmed facts still hold:** `PipelineBuilder.cs:253` (sync, closes decorator over `typeof(IQuery<TResult>)`), `:263` (`InitializeFromAttributeParams` at build), `:404` (async) — all verified verbatim. `QueryHandlerAttribute`/`...Async` are `abstract` with `protected (int step)` ctors and abstract `GetAttributeParams`/`GetDecoratorType`; `IQueryHandlerDecorator<TQuery,TResult>.Execute(TQuery, Func, Func)` + `InitializeFromAttributeParams`; `Paramore.Darker.Exceptions.ConfigurationException` (sealed); `IQueryContext.Bag` is `IDictionary<string,object>`; `InstrumentationOptions` is `[Flags]`; validation `UseFluentValidation` `Use*` template — all confirmed.

**Layering:** No circular reference. Caching → core (for `Activity`, `DarkerSemanticConventions.CacheOutcome`, decorator interfaces). Diagnostics → core (already, via `DarkerSemanticConventions`). Core depends on neither. The three-package split is clean and honestly disclosed in Consequences.

## Prior findings status

- **Finding 1 (false "no metrics primitive" claim): RESOLVED.** The ADR now accurately describes the ADR 0018 subsystem and reuses it (decorator writes a span attribute; `CacheMeter` in diagnostics derives the counter). Every claim it makes about that subsystem checks out against source. The bespoke-`Meter` approach is correctly demoted to Alternatives (rejected). A genuine correction, not a reword.
- **Finding 2 (stampede hit/miss miscount): RESOLVED and correctly characterized.** Documented in both Consequences ("Metric hit/miss counts are approximate under stampede") and a dedicated Risk. The characterization — metrics-only, results correct, `ran` is a per-query method local so no cross-query corruption — is accurate: decorators are created per-query, `ran` is a method local, and `GetOrCreateAsync` dedup means joiners observe `ran == false` and are counted as hits while still receiving correct results.

## Findings

### 1. Cache counter requires tracing to be enabled — documented consequence, not a defect (Score: 35)

FR10 asks for a hit/miss counter with an independent opt-out toggle and says nothing about requiring tracing. Because the design reuses metrics-from-traces, the counter only materialises when a query span exists (a tracer is configured). This is a real limitation, but it is (a) inherent to "via the existing Observability support" — which FR10 and Resolved Decision 4 explicitly name, and that support *is* metrics-from-traces; (b) identical to how the existing query-duration and DB histograms already behave (both derive from spans in `QueryMeter`/`DbMeter`); and (c) honestly disclosed as the first Negative consequence and referenced in Alternatives. It does not undercut FR10 or introduce a hidden conflict.

**Evidence**: ADR Negative consequences: "Cache metrics require tracing + the metrics pipeline ... This is the same property the existing query-duration and DB metrics already have". Verified against `QueryMeter.RecordQueryOperation(Activity)` (`QueryMeter.cs:53`), which is only ever driven from `DarkerMetricsFromTracesProcessor.OnEnd` at span end.

**Recommendation**: None required; the trade-off is correctly documented. Optionally cross-link the Negative bullet to FR10's "existing Observability support" phrasing to make the requirement-alignment explicit.

---

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 0 |
| 0-49 (Low) | 1 |

**Total findings**: 1
**Findings at or above threshold (60)**: 0

Both prior findings are genuinely and correctly resolved, every codebase claim in the rewrite was verified accurate against source, and the metrics-reuse rewrite introduced no new above-threshold problem (no false claims, no wrong file/line, no API-shape mismatch, no layering/circular-reference issue). Verdict: **PASS**.
