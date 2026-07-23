# Review: tasks — 014-Caching-Decorator

**Date**: 2026-07-21
**Threshold**: 60
**Verdict**: PASS

> Round 3. Reviews the unattended `ralph-tasks.md`. Both round-2 fixes landed correctly and the full forward-reference, coverage, and grounding sweep surfaced no defects. Absence of `STOP HERE`/`/test-first` gates and docs/scaffolding tasks without behavioral tests are by design for the ralph path.

## Findings

_No findings at or above threshold. Two round-2 fixes verified as landed correctly; the full forward-reference, coverage, and grounding sweep surfaced no defects worth flagging (not even sub-60 nits worth recording)._

---

## Round-2 fixes verified

1. **Forward-reference fix (round-2 #1, was ≥60) — LANDED correctly.**
   - (a) The step-ordering **doc task** lists only `CacheableQueryAttributeAsync.cs` + `README.md` as implementation files. The sync file `CacheableQueryAttribute.cs` appears in that task **only in prose** ("…is added in the sync-decorator task that creates that file — do NOT reference or create the sync attribute here"), never as an edit target.
   - (b) The **sync-decorator task** now folds in the `<remarks>` instruction: "Also add the same step-ordering/short-circuit `<remarks>` XML docs the async attribute carries … so both attributes document the footgun."
   - (c) Grep confirms `CacheableQueryAttribute.cs` (sync FILE) is an implementation-file bullet **only** in the sync-decorator task that creates it. The constant `CacheableQueryAttribute.CacheTag` is consumed only in the sync task itself or the tag tasks ordered after it. No file-edit or constant-use precedes creation.

2. **OTel-pointer fix (round-2 #2, low) — LANDED correctly.** The span-attribute task's References now point at the **core** `DarkerTracer` (`src/Paramore.Darker/Observability/DarkerTracer.cs`) + a BCL `ActivityListener` subscribed to `paramore.darker`, with an explicit "**no OpenTelemetry / Diagnostics dependency in the caching test project** (do NOT pull in `AddDarkerInstrumentation`/OTel here)." Verified coherent: `DarkerTracer : IAmADarkerTracer` lives in core and owns the `paramore.darker` ActivitySource; `QueryProcessor` construction resolves `IAmADarkerTracer` from DI, so a core-only tracer registration populates `Context.Span`.

## Full-sweep verification notes

- **Forward references (creation order rebuilt for every production type)** — clean:
  - `CacheOutcome` **enum**: first created and first used in the span-attribute task; the vertical-slice task explicitly excludes it. The earlier `DarkerSemanticConventions.CacheOutcome` **string** constant is a distinct construct created in the core-constants task and fine to reference thereafter.
  - `CachingOptions`: first created and first used in the opt-in task; vertical-slice `AddCaching` is parameterless and defers the overload.
  - `ICacheKeyGenerator`/`DefaultCacheKeyGenerator`/`IAmCacheable`: created in the default-key tasks before the vertical-slice task resolves `ICacheKeyGenerator`.
  - `DarkerSemanticConventions` cache constants: created in the core-constants task before the span and CacheMeter tasks consume them.
  - `IAmADarkerCacheMeter`/`CacheMeter`: created in the CacheMeter task before the processor-dispatch and AddDarkerInstrumentation tasks.
- **Grounding** — every touched reference exists and is named correctly: `DarkerTracer.cs`, `DarkerSemanticConventions.cs` (`MeterName`, `QueryDurationAllowedTags` FrozenSet pattern), `IQueryContext` (`Bag`, `Activity? Span`), `PipelineBuilder` (`OrderByDescending(attr => attr.Step)`, `GetDecoratorType().MakeGenericType`, `InitializeFromAttributeParams` — the cited 240/253/263/404 are accurate/approximate), `QueryMeter`/`IAmADarkerQueryMeter`, `DarkerMetricsFromTracesProcessor` (ctor params, `Enabled` guard, `Internal` branch — task edits map exactly), `DarkerTracerBuilderExtensions` (processor construction + meter gate), `RegisterDecorator`/`Services`/`IDarkerHandlerBuilder`, and the validation/FluentValidation csproj + real-processor test templates. No wrong or nonexistent reference found.
- **Coverage** — FR1–FR14 each map to ≥1 task (FR1→vertical-slice/step-order/sync; FR2→expiry/non-positive; FR3→vertical-slice; FR4→default-key/IAmCacheable/fail-fast; FR5→default-key/distinct-types; FR6→FusionCache switch; FR7→prereq/project/opt-in/hygiene; FR8→vertical-slice/sync fast-path/blocking fallback; FR9→tag-applied/absent-or-non-string; FR10→core-constants/span/CacheMeter/processor/toggle/e2e-metrics; FR11→negative-null; FR12→missing-cache; FR13→serialization; FR14→non-supporting-impl). ADR 0021 major decisions/components each map to a task.
- **Test-first framing / granularity** — each behavioral task's Test file + concrete assertions precede its implementation files, test names follow `When_[condition]_should_[expected_behavior]`, and each RALPH-VERIFY `FullyQualifiedName~…` filter matches its Test file name. Scaffolding/doc tasks correctly carry build-command verifies with no behavioral test (by design for the ralph path).

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 0 |
| 0-49 (Low) | 0 |

**Total findings**: 0
**Findings at or above threshold (60)**: 0

The task list is ready for implementation.
