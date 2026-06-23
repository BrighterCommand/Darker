# Review: design — 009-resilience_pipeline

**Date**: 2026-06-18
**Threshold**: 60
**Verdict**: PASS

> Reviewed: ADR 0015 (`docs/adr/0015-resilience-pipeline-integration.md`), status **Proposed**. This is an independent re-review (the prior round returned PASS after a fix cycle). The reviewer did not defer to the prior review: every Darker-local code citation was re-verified, and every load-bearing Polly 8.6.6 API claim was empirically confirmed by reflecting over and executing against the shipped `Polly.Core` 8.6.6 assembly. No findings at or above threshold.

## Findings

### 1. Imprecise claim that `ResiliencePipeline<T>.Execute` "only returns T" (Score: 38)

The ADR justifies two decisions with the statement that the typed pipeline's `Execute` can only return `T`:
- Line 40: "`ResiliencePipeline<T>.Execute` can only return `T`."
- Line 138: "`ResiliencePipeline<TResult>.Execute` returns `TResult` — no type gymnastics."
- Lines 316-317 (Alternatives): "`GetPipeline<TQuery>(key)` ... Rejected: cannot execute a `TResult`-returning handler — `ResiliencePipeline<T>.Execute` only returns `T`."

Verified against the shipped `Polly.Core` 8.6.6 assembly: `ResiliencePipeline<T>` has no `Execute` whose return type is the class parameter `T`. Every `Execute` is generic over a *method* type parameter, e.g. `TResult Execute<TResult>(Func<TResult> callback)`, and reflection shows that method parameter is **constrained `where TResult : T`**. So the precise statement is "`Execute<TResult>` returns the method's `TResult`, constrained to be assignable to `T`," not "only returns `T`."

This does not change any conclusion:
- The chosen §4 snippet `GetPipeline<TResult>(_policy).Execute(body)` (body: `Func<TResult>`) compiles because the constraint is identity-satisfied — the type-scoped path is sound.
- The rejected `GetPipeline<TQuery>` alternative is still correctly rejected, but the real reason is the constraint `where TResult : TQuery` (a query's result type is not generally a subtype of the query type), not "Execute only returns T."

**Evidence**: Reflection on `Polly.ResiliencePipeline\`1` (net8.0, Polly.Core 8.6.6): `TResult Execute<TResult>(Func<TResult> callback)` with generic-argument constraint `T`. The class parameter `T` appears only in constraints and in `ExecuteOutcomeAsync`'s `Outcome<TResult>` shaping, never as a bare `Execute` return type.

**Recommendation**: Reword to "`ResiliencePipeline<T>.Execute<TResult>` constrains `TResult : T`," and restate the `GetPipeline<TQuery>` rejection as a constraint mismatch (`TResult` is not assignable to `TQuery`). Conclusions stand; only the rationale text is loose.

---

### 2. Sync decorator silently ignores `ResilienceContext`; NFR2 parity nuance unstated (Score: 30)

§2 / Architecture Overview branch on `ResilienceContext` only on the async path (lines 102-106, 204); the sync snippets (lines 99-101) use `Execute(() => next(query))` with no `ResilienceContext` consideration. FR9 is explicitly scoped to "the async decorator," so this is requirement-consistent and not a defect. However, NFR2 (sync/async parity) and the Polly API (`Execute<TResult>(Func<ResilienceContext,TResult>, ResilienceContext)` exists and was verified present in 8.6.6) mean a sync caller who sets `IQueryContext.ResilienceContext` will have it silently ignored, unlike the async path. The ADR does not call this asymmetry out.

**Evidence**: ADR lines 99-101 (sync, no context branch) vs lines 102-106 (async, two-way branch); FR9 wording "The async decorator selects..."; verified `Polly.ResiliencePipeline.Execute<TResult>(Func<ResilienceContext,TResult>, ResilienceContext)` exists in 8.6.6.

**Recommendation**: Add one sentence noting that `ResilienceContext` propagation is intentionally async-only (per FR9), and that the sync decorator executes via the non-context overload by design, so readers do not expect sync `Properties` propagation.

---

### 3. "Verbatim" naming claim has a corner case for the result-type semantics (Score: 22)

The ADR repeatedly states the attribute/option naming follows Brighter "verbatim" (lines 112, 119, 343) including `UseTypePipeline`. The semantics deliberately diverge: Brighter's typed mode keys per `TRequest`; Darker's keys per `TResult` (lines 141-143). The ADR is honest about this divergence in the Negative consequences, but the same option name carries different isolation semantics between the two libraries — a developer relying on "verbatim" parity for behavior (not just spelling) could be surprised. Already mitigated in Negative/Risks, so sub-threshold.

**Evidence**: ADR line 119 "deliberately mirror Brighter's two-mode `UseTypePipeline` toggle (NFR4)" vs lines 141-143 "Independence is per result type, not per query type ... Brighter's is per `TRequest`; ours per `TResult`."

**Recommendation**: None required; the existing Negative-consequence and Risk entries adequately flag it. Optionally scope the word "verbatim" to spelling explicitly at first use.

---

## Verification performed (all confirmed unless noted)

**Darker-local citations — all accurate:**
- `RetryableQueryDecorator.cs` — `InitializeFromAttributeParams` at 19-25, sync resolve `Get<ISyncPolicy>(...).Execute` at 31, `GetPolicyRegistry()` guard at 34-35.
- `RetryableQueryDecoratorAsync.cs:36` async resolve.
- `QueryProcessor.cs:102-106` `InitQueryContext` fill-if-absent; `104-105` `Policies` null-guard.
- `QueryProcessorBuilderExtensions.cs:27-28` null-guard; `30-34` two-well-known-key content check.
- `IQueryContext.cs` / `QueryContext.cs` — `Bag` + `Policies` only; `QueryContext` is the **sole** `IQueryContext` implementation (grep confirmed).
- `TrackingQueryContextFactory.cs:3` is an `IQueryContextFactory` returning `QueryContext` (line 10); `InMemoryQueryContextFactory` returns `QueryContext`; no `InMemoryQueryContext` type exists. ADR's reworded Negative consequence is correct.
- `Constants.RetryPolicyName` / `CircuitBreakerPolicyName` exist; `QueryProcessorBuilder.PolicyRegistry` property + constructor threading exist.
- `Polly` `8.6.6` in `Directory.Packages.props`.

**Polly 8.6.6 API claims — empirically confirmed (reflection + live registry probe, Polly.Core 8.6.6, net8.0):**
- Provider exposes `GetPipeline(key)`, `GetPipeline<TResult>(key)`, `TryGetPipeline(key,out)`, `TryGetPipeline<TResult>(key,out)`.
- Registry derives from provider and exposes `TryAddBuilder(key,...)` and `TryAddBuilder<TResult>(key,...)`.
- Separate generic/non-generic namespaces: non-generic builder does NOT satisfy `GetPipeline<TResult>` → `KeyNotFoundException` with the exact message the ADR quotes; generic-only builder does NOT satisfy non-generic `GetPipeline` (both directions confirmed).
- Per-`(key,TResult)` caching: repeated `GetPipeline<int>("K")` returns the same instance (`ReferenceEquals` true).
- Unregistered `TResult` → `KeyNotFoundException`.
- `TryGetPipeline` / `TryGetPipeline<TResult>` return true/false as expected — supports FR7 build-time validation.
- `ResilienceContext` has `Properties` and `CancellationToken` (AC8/FR9).
- Async `ValueTask<TResult>` overloads exist for both `CancellationToken` and `ResilienceContext` forms (FR9).

**Brighter paths:** the ADR references Brighter type/shape names only (lines 343-344), not `src/Paramore.Brighter/...` file paths presented as Darker-local — no false local-path citations. Brighter is correctly treated as a separate (absent) repo.

**Requirements coverage:** FR1-FR9, NFR1-NFR5, RD1-RD4, AC1-AC11 all have corresponding ADR coverage. No unaddressed requirement found; no scope creep beyond the requirements (defaults/registration are in-scope per FR4-FR6). The "defaults do not support `UseTypePipeline`" limitation is stated consistently across FR6, FR8, §4, §5, and Negative.

**Concurrency:** the per-`(key,TResult)` cache and circuit-breaker shared state are Polly-owned; the ADR correctly attributes thread-safe caching to Polly's registry and introduces no Darker-owned mutable shared state. `ResilienceContext` is caller-owned (not processor-populated), avoiding a shared-lifetime hazard. Adequately addressed.

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 0 |
| 0-49 (Low) | 3 |

**Total findings**: 3
**Findings at or above threshold (60)**: 0

**Verdict: PASS.** Every Darker-local code citation re-verified (all accurate) and every load-bearing Polly 8.6.6 API claim empirically confirmed against the shipped `Polly.Core` 8.6.6 assembly — including the separate generic/non-generic registry namespaces, per-`(key,TResult)` caching, the exact `KeyNotFoundException` message, and the `TryGetPipeline[<TResult>]` validation hooks. The §4 native-`GetPipeline<TResult>` design is sound and internally consistent with the requirements. The only substantive note (Finding 1, score 38) is that the ADR repeats the imprecise "`ResiliencePipeline<T>.Execute` only returns `T`" claim; the real Polly contract is `Execute<TResult>(...) where TResult : T`. This is a wording/rationale defect, not a design defect — the conclusions it supports are correct — so it does not move the verdict.
