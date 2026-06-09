# Review: tasks — 008-factory_component_lifetime

**Date**: 2026-06-08
**Threshold**: 60
**Verdict**: PASS *(round 3)*

> **Round history**
> - **Round 1** — NEEDS WORK: #1 (82) AC9 false "identical to current" premise; #2 (70)
>   tautological `/test-first` tasks B4/B5/B7/B9. Fixed.
> - **Round 2** — NEEDS WORK: #1 (74) B9's red trigger technically implausible, tracing to a
>   factual error in approved ADR 0014 Decision 4 §2. Fixed (ADR corrected; B9 removed; parity
>   audit renumbered B10→B9).
> - **Round 3 (this review)** — **PASS.** Both R1 and R2 fixes verified against source. No ≥60
>   findings. Two below-threshold nits, one of which is a reviewer miscount (see below).

## Findings

### 1. S4 "stale Create-site line numbers" — FALSE POSITIVE, no change made (Score: 35 → dismissed)

The round-3 reviewer claimed S4's `Create` call-site line refs (`:172/:191/:217/:253`) were stale and
should be `:162/:179/:202/:234`. **Verified against source: the reviewer confused the *method
declaration* lines with the *call-site* lines.** `PipelineBuilder.cs` lines 162/179/202/234 are the
`ResolveHandler` / `ResolveHandlerAsync` / `GetDecorators` / `GetDecoratorsAsync` *declarations*; the
actual `_…Factory.Create(...)` call sites are at **172/191/217/253**, exactly as tasks.md cites.

**Evidence**: `PipelineBuilder.cs:172` `var handler = _handlerFactory.Create(handlerType);`, `:191`
`_handlerFactoryAsync.Create`, `:217` `_decoratorFactory.Create<…>`, `:253`
`_decoratorFactoryAsync.Create<…>` — all match tasks.md S4. (`:162/:179/:202/:234` are the four
method-declaration lines.)

**Recommendation**: None — tasks.md is correct; the finding is the same declaration-vs-call-site
miscount the round-2 reviewer made on the `using` block. Left unchanged.

---

### 2. ADR "Decision 2" not cited by label in any task (Score: 20 — fixed)

Decisions 1, 3, 4, 5, 6 were cited by name; Decision 2 (thread the lifetime through the four factory
interfaces) was implemented verbatim by S2 but not labelled. A labelling gap, not a coverage gap.

**Evidence**: S2 implements the exact ADR Decision 2 signatures.

**Recommendation / action taken**: Added "(ADR Decision 2)" to the S2 heading for traceability.

---

## Prior-fix status
- **R1 (AC9 premise + tautological tasks): RESOLVED.** Verified `ServiceProviderHandlerFactory.Release`
  disposes only `handler as IDisposable` (no child scope, no injected-dependency disposal), so the
  "retained on master" premise is true; FR4/NFR2/AC9 and B8 now correctly separate the preserved
  invariant from the by-design change, and B8 is explicitly "NOT a no-op against master." B4/B5/B7/B8
  all carry sound red-on-master rationale.
- **R2 (ADR Decision 4 §2 + B9): RESOLVED.** The false "constructed twice / correctness contributor"
  claim is retracted and reconciled with Decision 3 (`grep "constructed twice|two caches"` → 0 hits;
  the only "correctness contributor" occurrence is inside the retraction sentence). Old B9 removed
  with a technically-correct MS DI removal note; singleton coverage retained via B2 (handler) + B7
  (decorator); parity audit renumbered to B9; ADR Implementation Approach now places the merge in the
  structural step, agreeing with tasks S5. No stale `B10` / `B1–B9` references remain.

### Additional verifications passed (per the reviewer)
- No dangling/duplicate task IDs after the B10→B9 renumber; B0–B9 contiguous.
- Dependency graph internally consistent (B3→S5; B4–B8→B1/B2; B9 audit→B1–B8).
- All real factory implementors enumerated in S3 (5 production + 2 test doubles); none missed.
- Test projects/dirs cited in tasks exist.
- AC re-map after old-B9 removal: AC1→B2, AC2→B2, AC3→B1, AC4→B3, AC5→B4, AC6→B5, AC7→B6, AC8→B7,
  AC9→B8, AC10→B9. No AC lost coverage.
- requirements.md (FR4/NFR2/AC9) and tasks.md B8 are consistent.

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 0 |
| 0-49 (Low) | 2 |

**Total findings**: 2 (one a verified false positive, one fixed)
**Findings at or above threshold (60)**: 0

**Verdict**: PASS — no findings at or above the threshold. Tasks are ready for approval.
</content>
