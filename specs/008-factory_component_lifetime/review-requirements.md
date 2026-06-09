# Review: requirements — 008-factory_component_lifetime (re-review)

**Date**: 2026-06-06
**Threshold**: 60
**Verdict**: PASS

## Prior Findings Status
1. "Same/shared instance" undefined — **RESOLVED**: "Key Terms and Observability" fixes it to `ReferenceEquals` + an `ITrackedDependency` construction counter, asserted on the injected dependency.
2. AC4 ambiguous about what is shared — **RESOLVED**: AC4 now tests a Scoped `ITrackedDependency` injected into both handler and decorator yielding the same reference (counter == 1), and clarifies it is not a shared handler/decorator object.
3. Failure/exception path disposal unspecified — **RESOLVED**: New FR9 and AC7 cover thrown handler/decorator and `OperationCanceledException`, with `try/finally` scope disposal.
4. NFR1 "Brighter documented semantics" non-verifiable — **RESOLVED**: B1–B3 baseline semantics inlined as explicit, observable assertions; Brighter source demoted to reference-only.
5. Concurrency test not deterministic — **RESOLVED**: AC6 specifies a barrier/`TaskCompletionSource` forcing provable overlap, distinct references, and selective disposal (A disposed, B not).
6. NFR2 regression guard missing — **RESOLVED**: AC9 added as an explicit default-path regression guard pinning NFR2.
7. Transient-via-child-scope latent contradiction — **RESOLVED**: FR4 gives the rationale (deterministic disposal, baseline B3) and ties the observable outcome to NFR2/AC9.

## Findings

### 1. AC9 / NFR2 "unchanged from current implementation" not pinned to the exact invariant (Score: 48)

NFR2/AC9 promise default Transient behaviour is "unchanged," while FR4 mandates Transient now resolve via a per-execution child scope (today it resolves from the captured provider and is disposed directly by `Release`). The externally observable outcome is plausibly equivalent, but AC9 states the invariant qualitatively rather than as the precise pair (per-query construction counter increments by 1; injected dependency `IsDisposed == true` after pipeline).

**Evidence**: FR4: "externally observable disposal outcome for the default Transient path must remain equivalent to today's behaviour"; AC9: "unchanged from the current implementation."

**Recommendation**: State in AC9 the exact preserved invariant (counter increments by 1 per query; `IsDisposed == true` after pipeline), matching AC3, to remove residual interpretation room.

---

### 2. "Transient new-instance-per-resolution" (B3) not pinned by an AC for multiple resolutions within one pipeline (Score: 42)

B3/FR4 describe Transient as "new instance per resolution," but a pipeline resolves the handler once and each decorator once, so "per resolution" is never exercised as multiple resolutions of the same Transient type within one pipeline. Minor completeness gap.

**Evidence**: B3: "a new instance per resolution"; AC3 asserts "counter increments by 1 each execution" (per-query).

**Recommendation**: Either add an AC for two Transient resolutions of one type within a pipeline being distinct, or note explicitly that Darker resolves each Transient type once per pipeline so per-resolution and per-execution coincide.

---

### 3. AC6 exercises only the Scoped case; Transient concurrency isolation not concretely pinned (Score: 38)

AC6 forces concurrency with a Scoped dependency. FR6 also claims Transient instances must not be shared/disposed across concurrent pipelines, but AC6 only exercises Scoped. Low impact since the per-execution scope mechanism is shared.

**Evidence**: AC6 names "injected Scoped `ITrackedDependency`"; FR6 covers "scoped/transient instances."

**Recommendation**: State that AC6 also applies with Transient lifetime, or that the shared child-scope mechanism makes the Scoped case representative.

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
