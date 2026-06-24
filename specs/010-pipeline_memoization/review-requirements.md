# Re-Review: requirements — 010-pipeline_memoization

**Date**: 2026-06-24
**Threshold**: 60
**Verdict**: PASS

> This re-review supersedes the prior NEEDS WORK review of 2026-06-23 (preserved below under "Prior Review"). All six prior findings were resolved in the revised `requirements.md`.

## Findings

### 1. FR4 / validation-ordering boundary is under-specified vs. actual code (Score: 55)

FR4 says "If discovering **or validating** attributes throws a configuration error, nothing is cached." But in the current code `ValidateNoMismatchedAttributes` runs in `Build`/`BuildAsync` (`PipelineBuilder.cs:65` and `:118`) — physically separate from and *before* `GetDecorators`/`GetDecoratorsAsync` (`:67` and `:121`), which is where the cacheable `GetCustomAttributes`+`OrderByDescending` lives. The doc never states *where* the cache read/write sits relative to this validation. An implementer could plausibly (a) cache inside `GetDecorators` only — in which case the *separate* mismatch validation re-runs reflection every call and is never bypassed, or (b) hoist validation into the memoised path and accidentally cache past a mismatch error. The doc's intent (a) is inferable but not pinned.

**Evidence**: Code: `ValidateNoMismatchedAttributes(executeMethodInfo, typeof(QueryHandlerAttributeAsync), ...)` at `:65`, then `_decorators = GetDecorators(executeMethodInfo, queryContext)` at `:67`. Doc FR4: *"If discovering or validating attributes throws a configuration error, nothing is cached"* — but says nothing about validation being outside the cached region.

**Recommendation**: Add one sentence pinning the cache region to `GetDecorators`/`GetDecoratorsAsync`, with the pre-`GetDecorators` mismatch validation remaining outside the cache and continuing to run/throw on every call. **(Applied in revision.)**

### 2. "GetAttributeParams() per query" reuse-safety is asserted but not justified in the doc (Score: 40)

FR6 says cached attribute *instances* supply params while decorator instances are rebuilt per query. Verified safe — concrete attributes (`RetryableQueryAttribute`, `QueryLoggingAttribute`, etc.) hold only `readonly` fields, `Step` is get/private-set, and `GetAttributeParams()` returns a fresh `object[]` without mutating instance state, so sharing one attribute instance across concurrent queries is race-free. The doc *relies* on this immutability but never states it as the reason reuse is safe.

**Evidence**: `QueryHandlerAttribute.cs:8` `public int Step { get; private set; }`; `RetryableQueryAttribute.cs:9` `private readonly string _policyName;`; `GetAttributeParams()` returns `new object[] { _policyName }`. Doc NFR Thread-safety covers cache population races but not attribute-instance-sharing safety.

**Recommendation**: Add a Constraint noting cached attribute instances are immutable, so sharing across concurrent queries is safe. **(Applied in revision.)**

### 3. "without test failures" slightly overstates the Brighter 0003 evidence (Score: 30)

The doc states Brighter spec 0003 "removed ~201 cache-clearing calls and the associated `[Collection]` attributes **without test failures**." The cited spec is a *requirements* doc that sets this as the goal/acceptance criterion, not a post-hoc outcome report. Substance is fully supported (deterministic type-keyed caches, ~201 calls, `[Collection("CommandProcessor")]` removal); minor phrasing nit.

**Evidence**: Brighter `0003/requirements.md:24` "~201 files", `:50`/`:80` remove `[Collection("CommandProcessor")]`, `:32` "clearing them between tests has no correctness impact."

**Recommendation**: Soften to cite it as the design rationale/basis rather than an observed result. **(Applied in revision.)**

---

## Verified-correct claims (no finding)

- `PipelineBuilder.cs:213` sync `GetCustomAttributes(typeof(QueryHandlerAttribute), true)` + `OrderByDescending(attr => attr.Step)` — exact match.
- `PipelineBuilder.cs:245` async `QueryHandlerAttributeAsync` path — exact match.
- `GetDecorators` at `:211`, `GetDecoratorsAsync` at `:243` — exact match.
- `QueryProcessor.cs:52` and `:78` — `new PipelineBuilder<TResult>(...)` per execution — confirmed.
- ADR 0002 line 115 documents "Reflection cost ... (no caching of the pipeline structure)" as a Negative — confirmed.
- Brighter `s_preAttributesMemento`/`s_postAttributesMemento` are `static ConcurrentDictionary<Type, IOrderedEnumerable<RequestHandlerAttribute>>`, keyed by `implicitHandler.GetType()`, with `ClearPipelineCache()` at `:253` clearing both — confirmed.
- `QueryHandlerAttribute` exposes `Step`; `OrderByDescending(Step)` correct; `QueryHandlerAttributeAsync` is a distinct type — confirmed.
- No xUnit `[Collection]` test-serialization currently exists in Darker tests, consistent with the doc's "no `[Collection]` needed" stance.
- Every FR (1–8) maps to at least one AC. No orphan FRs.

The "memoisation verified by code inspection, not a dedicated test" stance is acceptable: the cache is private static state with no observation seam, and the genuinely testable risks (Brighter #4192 collision guard, thread safety, behaviour/exception preservation, config-errors-thrown-every-time) all have concrete behaviour-level ACs.

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 1 |
| 0-49 (Low) | 2 |

**Total findings**: 3
**Findings at or above threshold (60)**: 0

**Verdict: PASS.** All codebase and cross-repo references verified accurate (line numbers exact, Brighter implementation/spec claims correct, ADR 0002 citation correct). The single medium finding (55, below threshold) and two low findings were applied as small clarifications in the revision for implementer clarity.

---

## Prior Review (2026-06-23) — NEEDS WORK, resolved

The initial review returned NEEDS WORK with 5 findings at/above threshold (memoisation AC untestable; cache key under-specified; "keyed by Type" vs multi-field payload contradiction; config-error caching ambiguity; parallel-test/Collection concern) plus 1 low. All six were resolved per owner direction: scope narrowed to caching only ordered attributes (follow Brighter), separate sync/async caches keyed by handler `Type`, config-error builds not cached, memoisation treated as an inspection-verified implementation detail, and determinism-makes-clearing-harmless (no Collections needed). See git history of `requirements.md` and the Resolution log previously recorded.
