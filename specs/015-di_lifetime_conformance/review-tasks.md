# Review: tasks — 015-di_lifetime_conformance

**Date**: 2026-10-10
**Threshold**: 60
**Verdict**: NEEDS WORK

7 findings at or above threshold 60. Address these before approving.

## Findings

### 1. T1.3's RED mutation does not fail the test: `Clear()` already makes a second Dispose a no-op (Score: 75)

T1.3 is a CHARACTERISE task, and its only mutation deletes the `_disposed` guard. But `QueryLifetimeScope.Dispose()` also empties the tracked list after the first pass. With the guard deleted, a second call walks an empty list, so the recorder is still disposed only once and the test stays green. T1.2 keeps the flag and says nothing about removing `Clear()`.

**Evidence**:
- tasks.md:64: "delete the `if (_disposed) return;` guard. The test must fail on `disposeCount.ShouldBe(1)`".
- `src/Paramore.Darker/QueryLifetimeScope.cs:51-59` shows the guard, the loop, then `_trackedDisposables.Clear();`.
- tasks.md:53.

**Recommendation**: Make the mutation delete both the guard and `Clear()`, or have T1.2 drop `Clear()` so the flag is the only guard. Do the same in the post-T2.10 single-handle version.

---

### 2. T4.8's mutation cannot fail, and as written the test duplicates T4.4 (Score: 72)

`ExecuteStream` builds lazily, so the provider is first asked at the first `MoveNextAsync`. The test clears `HttpContext` before enumerating, so the provider is never called while a request is current. The "remember the last `RequestServices`" mutation therefore has nothing to remember, and the test stays green. What the test then shows is what T4.4 already pins. Nothing pins the lazy read itself.

**Evidence**:
- tasks.md:943-947.
- `src/Paramore.Darker/QueryProcessor.cs:141-168`: `BuildStream` runs inside the iterator.
- ADR 0024:581.

**Recommendation**: Use a mutation that breaks laziness: ask for the pipeline scope eagerly in `ExecuteStream`, so the test fails. Alternatively, run one query inside the request first, so the "remember" mutation has something to remember.

---

### 3. ADR 0022 row 1 has no task, and the appendix misses it (Score: 68)

ADR 0022 row 1 says teardown skips the releases when no component was created and tolerates a builder that never built. `ReleaseAsync` is called only for a handler the builder created. No task implements any of this.

- **Sync path today:** `Dispose` releases with a null `_handler`.
- **Async path as planned:** T1.12 and T1.16 await `_lifetime.DisposeAsync()` with no null check. That throws `NullReferenceException` when context or span creation throws, and when `CreatePipelineScope` throws (T2.8).
- **T2.8's mutation** assumes a `_lifetime?.` that no task creates on the async path.
- **T2.8 and T2.9** do not assert that release is skipped.
- **The appendix** (tasks.md:1136) lists only two untasked decisions.

**Evidence**: ADR 0022:143, :259 and :275; `PipelineBuilder.cs:428`; tasks.md:195, :250 and :391.

**Recommendation**: Add a Phase 1 TEST + IMPLEMENT on all three paths: when the context factory or the handler lookup throws, release is never called with a null handler, and teardown does not throw. Have T1.12 and T1.16 tolerate a null lifetime. Correct the appendix.

---

### 4. T1.16's planned RED relies on T1.14 and T1.15 removing isolation that T1.12 already built (Score: 65)

T1.12's `DisposeAsync` "performs the existing isolated releases". T1.14 and T1.15 switch to awaited `ReleaseAsync` without saying whether the fault boundary stays. That leaves two outcomes:

- If the boundary stays, T1.16 is green on arrival and is mis-tagged.
- If it goes, async isolation regresses silently between T1.14 and T1.16.

**Evidence**: tasks.md:195, :224, :236 and :249.

**Recommendation**: Have T1.14 and T1.15 keep each awaited release in its fault boundary, and retag T1.16 as CHARACTERISE with the mutation "remove the try/catch around the awaited handler release". Alternatively, move T1.16 before T1.14 and T1.15.

---

### 5. T2.10 is tagged STRUCTURAL but writes renamed tests that duplicate T2.6 and are never seen RED (Score: 62)

The rewritten T1.2 and the async single-handle T1.13 assert what T2.6 already asserts: a throwing or faulting handle is logged at Error with the scope type, and the result returns. The STRUCTURAL tag means no new test, and CLAUDE.md says to stop on a duplicate test.

**Evidence**: tasks.md:418-421 against :356-362; ralph-implement.md:187.

**Recommendation**: Delete the T1.2 and T1.13 tests in T2.10 and let T2.6 own that behaviour. For the remaining migrated tests, name one mutation per file that must still fail, or retag the migration so it records RED evidence.

---

### 6. Padded CHARACTERISE tasks: T2.14 writes no test, and T3.13 (owned fact) and T3.17 repeat an existing test with the same mutation (Score: 62)

- **T2.14** has "Test file: none new", yet it opens an approval gate. Its commit instruction ("Commit nothing unless a test needed no edit") is garbled and breaks the two-commit shape.
- **T3.13's owned fact and T3.17** repeat `When_handler_lifetime_is_scoped_should_share_dependency_across_handler_and_decorator.cs`.
- **T2.14(b), T3.13 and T3.17** all use one mutation.

**Evidence**: tasks.md:466, :474, :472, :671, :725 and :669.

**Recommendation**: Fold T2.14 into T2.13's guard as a checklist, or make it a VERIFY. Drop T3.13's owned fact and T3.17, or give T3.17 an assertion that only the Transient-decorator change can break: distinct decorator instances across owned-scope queries, with T3.18's inverse mutation.

---

### 7. STRUCTURAL T3.3 adds public behaviour that no test covers (Score: 60)

`AddDarkerScopeProvider` throws `ArgumentNullException`, registers the provider as a singleton, and replaces any earlier registration. ADR 0024:208 also says a later call replaces an earlier one. No task tests the replacement or the null guards, and T4.2's guard on `AddDarkerRequestScope` is untested too.

**Evidence**: tasks.md:535 and :871; ADR 0024:208 and :411.

**Recommendation**: Keep T3.3 to plumbing with no observable effect. Add a task asserting that a second `AddDarkerScopeProvider` replaces the first, with the mutation "use `TryAddSingleton`". Add small tests for the two null guards.

---

### 8. Implementation notes ask for more code than their tests need; the guarded log on the async paths is never tested (Score: 55)

T1.12's notes require fault boundaries, a guarded log and a shared `_disposed` flag, but its test needs only an awaited `DisposeAsync`. T1.16 adds the guarded log to `PipelineBuilder.DisposeAsync`, yet no async test uses a throwing logging provider, because T1.9 covers `Execute` only.

**Evidence**: tasks.md:193, :145-146 and :249; testing.md:195-196.

**Recommendation**: Trim T1.12's notes to what its test needs and retag T1.13 as TEST + IMPLEMENT. Extend T1.9 to `ExecuteAsync` and `ExecuteStream`, or add a CHARACTERISE task for the async guarded log.

---

### 9. The DI-factory "Scoped service Dispose throws" acceptance criterion is only tested on the sync path (Score: 50)

**Evidence**: tasks.md:126-133 (T1.8 is `Execute` only); ADR 0022:329.

**Recommendation**: Add `ExecuteAsync` and `ExecuteStream` facts to T1.8, or add a CHARACTERISE task after T1.12 that uses a Scoped dependency whose `DisposeAsync` faults.

---

### 10. T2.4's FR12 assertion cannot be observed through `SimpleHandlerDecoratorFactory` (Score: 45)

`SimpleHandlerDecoratorFactory` never sees the lifetime, so a test cannot observe it through that factory.

**Evidence**: tasks.md:335; `src/Paramore.Darker/SimpleHandlerDecoratorFactory.cs:40-47`.

**Recommendation**: Pair `SimpleHandlerFactory` with `LifetimeRecordingDecoratorFactory` for the assertion, and keep a plain Simple+Simple fact for "the query still runs".

---

### 11. T3.24's design is fragile, and nothing checks that the latch is per reason (Score: 45)

A 100-participant `Barrier` blocks 100 thread-pool threads. Every probe test has only one reason, so a latch on a single boolean would still pass.

**Evidence**: tasks.md:812; FR15.

**Recommendation**: Gate with a `TaskCompletionSource` or `SemaphoreSlim`, or use fewer queries. Add a fact with two reasons in one container that expects two Warnings, using the mutation "latch on a single flag".

---

### 12. T3.5 lists its test doubles under "Implementation should" (Score: 35)

**Evidence**: tasks.md:565.

**Recommendation**: Move them under "Test should verify".

---

### 13. T2.12's mutation text is out of date (Score: 30)

The text says "Before T1.18 removes the async `Release`…", but T2.12 runs after T1.18.

**Evidence**: tasks.md:448.

**Recommendation**: Say "make both the sync `Release` and `ReleaseAsync` dispose an `IDisposable` handler".

---

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 2 |
| 50-69 (Medium) | 7 |
| 0-49 (Low) | 4 |

**Total findings**: 13
**Findings at or above threshold (60)**: 7

Checks that passed:
- No `InternalsVisibleTo`, no internal types driven directly, and no Moq.
- Every checkbox is tag-first.
- All 54 behavioural tasks carry the gate line.
- Every CHARACTERISE task names a mutation.
- Cited files, test doubles and line references are correct.

## Coverage check

**Gaps**:
1. ADR 0022 row 1: no task skips releases when nothing was created, or tolerates a null lifetime (#3).
2. The DI Scoped-service teardown fault is not tested on `ExecuteAsync` or `ExecuteStream` (#9).
3. The guarded log on the async paths is not tested (#8).
4. Provider-registration replacement and the `ArgumentNullException` guards are not tested (#7).
5. FR15's "once per reason" is not distinguished from "once in total" (#11).
6. The lazy, first-`MoveNextAsync` read of the ambient scope is not pinned (#2).

There are no untraced tasks.

Every FR, from FR1 to FR25 and including FR5a, maps to at least one task. The partial cases are listed in the gaps above. The reviewer's full FR → tasks, AC → tasks and ADR-step → tasks tables match the coverage appendix in tasks.md, except for the gaps above.
