# Review: design — 008-factory_component_lifetime (ADR 0014, re-review)

**Date**: 2026-06-08
**Threshold**: 60
**Verdict**: PASS

## Prior Findings Status
1. Decision 4 understates starting point (55) — **RESOLVED.** Decision 4 now opens with an explicit "Starting point" para: two DI classes, each already dual-interface, already shared across sync/async slots. Matches `ServiceProviderHandlerFactory.cs:5`, `ServiceCollectionExtensions.cs:46-49`.
2. Decision 3↔4 singleton-cache link under-stated (45) — **RESOLVED.** Decision 4 item 2 now states "two separate factory classes there would be two caches… One class → one cache → one instance."
3. Decision 4 "not a correctness requirement" contradiction (62, ≥threshold) — **RESOLVED.** Replaced with "a correctness contributor for the shared-Singleton case, not merely a tidiness choice." Consistent with B1/AC1/AC4.
4. Singleton "root provider" under-specified, Scoped case ignored (50) — **RESOLVED.** Decision 3 now says "captured provider," explains container-owned Singleton disposal, and adds a `QueryProcessorLifetime = Scoped` interaction paragraph.
5. Wrong path `src/Paramore.Darker/Builder/PipelineBuilder.cs` (60, ≥threshold) — **RESOLVED.** `requirements.md` now reads `src/Paramore.Darker/PipelineBuilder.cs`. Verified file exists; no `Builder/` dir.
6. Lifetime created at start of Build before any Create (48) — **RESOLVED.** Decision 5 states the lifetime is created "before any `Create` call" and explains partial-build safety.
7. Singleton cache needs thread-safe get-or-create (35) — **RESOLVED.** Decision 3 mandates "thread-safe get-or-create (e.g. `ConcurrentDictionary<Type, …>` with `Lazy<>`)"; Consequences updated.

## Findings

### 1. Line-number citation for the `using` block is slightly off (Score: 25)

Decision 5 cites the failure-path guarantee at "`QueryProcessor.cs:47-71`". Line 47 is the `Execute` signature; the `using` begins at line 49. The async `using` (75-99) is not cited. Claim is substantively correct; citation imprecise.

**Evidence**: `QueryProcessor.cs:49` (`using (var pipelineBuilder = …)`), `:54` (`Build` inside), `:75-99` (async `using`/`BuildAsync`).

**Recommendation**: Cite `QueryProcessor.cs:49-71` (sync) and `:75-99` (async), or write "the `using` blocks in `Execute`/`ExecuteAsync`."

---

### 2. New `Release(handler, lifetime)` null-tolerance contract is implied but unstated (Score: 30)

Decision 5 says `Release` "tolerates a null handler." Verified `PipelineBuilder.Dispose()` calls `_handlerFactory?.Release(_handler)` with possibly-null `_handler` (`PipelineBuilder.cs:274`, field `:29`). The ADR states the tolerance as behaviour to preserve, but not as an explicit contract on the new signature.

**Evidence**: `PipelineBuilder.cs:274` `_handlerFactory?.Release(_handler);`, `:29`.

**Recommendation**: State that the new `Release(handler, lifetime)` must be null-tolerant on both `handler` and the not-yet-attached-scope case.

---

### 3. Architecture diagram/table reuses the handler-only class name for the merged class (Score: 20)

The diagram and Key Components table name the merged all-four-roles class `ServiceProviderHandlerFactory`. Decision 4 collapses the handler and decorator factories into one class but doesn't name the survivor; reusing the handler-only name for a class that also creates decorators is mildly misleading.

**Evidence**: ADR diagram / Key Components vs Decision 4; existing `ServiceProviderHandlerDecoratorFactory.cs:5`.

**Recommendation**: Name the merged class explicitly (e.g. `ServiceProviderComponentFactory`) or note in Decision 4 which name survives.

---

## Summary
| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 0 |
| 0-49 (Low) | 3 |

**Total findings**: 3
**Findings at or above threshold (60)**: 0
