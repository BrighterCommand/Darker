# Review: tasks — agreement_dispatcher (re-review)

**Date**: 2026-07-14
**Threshold**: 60
**Verdict**: PASS

> Re-review after the prior six findings (3 above threshold) were addressed, and after the
> requirements AC (requirements.md:122) was softened to permit mechanical call-site updates from the
> ADR's deliberate `Get`-widen. All six prior findings are genuinely resolved (not merely reworded),
> verified against the revised docs and the actual code. Two minor granularity/presentation nits
> remain, both below threshold.

## Findings

### 1. Phase 1's three `/tidy-first` sub-tasks cannot each reach a green build in isolation (Score: 48)

Phase 1 lists three separate `/tidy-first` commands (introduce role/route; migrate storage + widen
`Get`; thread through `PipelineBuilder`), yet the header concedes "all sub-tasks land in one commit
because widening `Get` is a breaking signature change that must compile across the registry +
`PipelineBuilder` simultaneously." Only sub-task 1 (introduce `IResolveHandlers`/`FixedHandlerRoute`)
is independently green. Sub-task 2 (widen `Get`, replace not overload — `Get(Type)` verified as the
sole method at `QueryHandlerRegistry.cs:18`, `QueryHandlerRegistryAsync.cs:18`,
`StreamQueryHandlerRegistry.cs:17`) breaks every caller until sub-task 3 completes. Mild tension with
independent-verifiability and the one-tidy-per-commit spirit of `/tidy-first`, though the list
documents it honestly rather than hiding it.

**Evidence**: tasks.md header ("all sub-tasks land in one commit … must compile across the registry +
`PipelineBuilder` simultaneously"); three separate `/tidy-first` USE COMMANDs in Phase 1.

**Recommendation**: Either collapse sub-tasks 2+3 into one `/tidy-first` task (they share a single
atomic breaking change), or keep three but state up front that the green-build gate applies to the
aggregate, not each sub-task.

---

### 2. Phase 4/4b bundle candidate-validation and duplicate-registration into one task, inconsistent with Phase 3 (Score: 42)

Phase 3 splits registration-time guards into two separate `/test-first` tasks
(candidate-not-implementing-interface; duplicate-registration). Phase 4 (async) and Phase 4b (stream)
each collapse both into a single "candidate-validation and duplicate-registration guards" task,
asserting two distinct registration behaviours behind one STOP gate. A residual (much reduced) version
of the prior finding #2 granularity inconsistency; minor, but the sync vs async/stream asymmetry is
avoidable.

**Evidence**: Phase 4/4b guard tasks ("a candidate not implementing … → `ConfigurationException`;
**and** routing `Register` for an already-registered query type → `ConfigurationException`") — two
behaviours, one `/test-first`, one STOP gate; contrast Phase 3's two separate tasks.

**Recommendation**: Either split Phase 4/4b guards to mirror Phase 3, or add a one-line note that
async/stream deliberately combine the two registration guards already proven separately on sync.

---

## Prior findings status

1. **"Zero test edits" invariant impossible — RESOLVED.** Replaced with "no *behavioural* test
   changes"; the mechanical 1-arg→3-arg call-site updates are named with exact files/lines. Confirmed
   by grep those three files are the *complete* set of test callers of `Get` (QueryHandlerRegistryTests
   13 sites; the two stream tests) — no async-registry test calls `Get` directly.
2. **Phase 4 bundled four behaviours — RESOLVED** (largely). Phase 4/4b split
   content/context/null/non-candidate into separate `/test-first` tasks; only the two registration
   guards remain combined (Finding 2, below threshold).
3. **Router-throws mitigation lacked `/test-first` — RESOLVED.** Now a full TEST+IMPLEMENT task with
   `/test-first` and STOP gate; risk section references it for traceability only.
4. **Coverage gap: decorator hand-off + async/stream validation — RESOLVED.** Phase 3 adds "routed
   handler still runs its decorator pipeline" (cites requirements.md:65-67); Phase 4/4b add
   candidate-validation + duplicate guards.
5. **Phase 5 DI bundled three registries vaguely — RESOLVED.** Now three separate DI tasks
   (sync/async/stream), each with its own named test file and STOP gate.
6. **Fictitious "Testing/Fake registries" example — RESOLVED.** Removed; Key-files table and Phase 1
   cite the actual affected test files. Confirmed no registry/`Get` caller exists in
   `Paramore.Darker.Testing`.

**Revised-AC consistency check (requirements.md:122-126)**: Now internally consistent with tasks.md
Phase 1. The AC permits "mechanical source-level updates required purely by a deliberate breaking API
change" but closes the loophole with "no test's expected behaviour or assertions may change";
tasks.md mirrors this. Passing `null, null` at pure-lookup call sites is genuinely inert because
`FixedHandlerRoute` ignores query/context — no behaviour can be smuggled in under "mechanical."

**Grounding**: All codebase citations in the revised tasks.md verify against source — `PipelineBuilder`
helpers (`ResolveHandler`:201, `ResolveHandlerAsync`:218, `ResolveStreamHandler`:367), Build call
sites (:67,:128,:282), sync fallback (:238), null-guards (:205,:224,:373),
`ServiceCollectionHandlerRegistry.cs:18-23`, the test call-site line numbers, the async/stream generic
constraints, and the `Paramore.Darker.Tests.AOT` / `Paramore.Darker.Benchmarks` projects all exist as
cited.

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 0 |
| 0-49 (Low) | 2 |

**Total findings**: 2
**Findings at or above threshold (60)**: 0

**Verdict**: PASS. All three prior above-threshold findings (and all six total) are genuinely
resolved — not merely reworded. Full FR/AC/ADR coverage confirmed; all grounding accurate. The two
remaining issues are minor granularity/presentation nits below threshold.
