# Review: tasks — 007-use_simple_not_mocks

**Date**: 2026-06-05
**Threshold**: 60
**Verdict**: PASS

> Round 2. The prior round (verdict NEEDS WORK) found 3 blockers (scores 92/72/70)
> plus a low finding (48). tasks.md was revised and ADR 0013 amended. This round
> re-verified the fixes against the codebase and hunted for new problems.

## Findings

### 1. Task 5/6/7 offer the Task 4 generic recording handler as a bag-writer for `ExecutesQueries`, but its delegate signature cannot write to `Context.Bag` (Score: 38)

Both `ExecutesQueries` tests assert `handler.Context.Bag.ShouldContainKeyAndValue("id", id)` (`QueryProcessorTests.cs:42`, `QueryProcessorAsyncTests.cs:52`), mirroring `Exported.TestQueryHandler`, whose `Execute` writes `Context.Bag.Add("id", query.Id)`. Task 4 defines `RecordingQueryHandler` with a `Func<TQuery, TResult> execute` delegate (and the async sibling with `Func<TQuery, Task<TResult>>`). That delegate receives only the query — it has **no access to `Context`** — so it can echo `query.Id` as the *result* but cannot write `"id"` into the bag. Task 5 then says the `ExecutesQueries` bag-writer "can be the echoing recording handler from Task 4, or a small dedicated double." The first option is not actually viable given Task 4's signature; only the dedicated double (or a recording handler whose *override*, not its delegate, writes the bag) satisfies the bag assertion. An implementer following the "echoing recording handler from Task 4" branch would fail the bag assertion and have to course-correct mid-task.

**Evidence**: `QueryProcessorTests.cs:42`, `QueryProcessorAsyncTests.cs:52`; `Exported/TestQueryHandler.cs` (`Context.Bag.Add("id", query.Id)`); tasks.md Task 4 (`Func<TQuery, TResult> execute`) vs Task 5 ("This can be the echoing recording handler from Task 4, or a small dedicated double").

**Recommendation**: In Task 5, drop the "can be the echoing recording handler from Task 4" option for the bag-writing case and state that `ExecutesQueries` needs a dedicated double whose `Execute`/`ExecuteAsync` override writes `"id" → query.Id` to `Context.Bag` and returns `query.Id`. Not blocking — the correct path is already offered — but the misleading first option should be removed.

---

### 2. Task 9's registry conversion is described but functionally inert — harmless, worth a note (Score: 22)

Task 9 says the two `decoratorRegistry.Setup(Register(...))` calls "become real `Register(...)` calls on `InMemoryDecoratorRegistry`." Verified: those Moq `Setup` calls on a void method are no-ops today, and the pipeline attaches decorators from the handler-method **attributes** (`PipelineBuilder.GetDecorators` reads `[FallbackPolicy]`/`[DecoratorException]`, not the registry), with `_decoratorFactory.Create` supplying the instance. So whether or not the real `Register` calls are made does not affect these tests passing. The instruction is faithful and harmless, but the document implies the registration is load-bearing when it is not.

**Evidence**: `PipelineBuilderExceptionTests.cs:114-115` (Setup, no `.Verifiable()`); `PipelineBuilder.cs` `GetDecorators` (reads attributes); tasks.md Task 9.

**Recommendation**: Optional — note that the registry registration is preserved for fidelity but is not functionally required by these exception tests.

---

### 3. Prior findings resolved / verified-correct (Score: 0)

All three prior blockers are genuinely fixed, and the new references check out:

- **Finding 1 (was 92) — FIXED.** Task 7 now mandates **two distinct** `RecordingHandlerFactory` instances (sync slot + async slot) and asserts `asyncFactory.ReleaseCount(handler).ShouldBe(0)` on the **async** instance. Verified against `PipelineBuilder.cs:191` (async `Create`) and `:274` (`_handlerFactory?.Release(_handler)` — sync slot releases), so the async factory genuinely never sees `Release`. The other two async tests (`ExecutesTheMatchingHandler`, `ExceptionsDontCauseFallbackByDefault`) carry **no** `Release` assertion, so two instances do not perturb them. Task 6 (sync, single factory, `ShouldBe(1)`) remains correct.
- **Finding 2 (was 72) — FIXED.** Verified `PipelineBuilderExceptionTests.cs` has **zero** `_decoratorFactory.Verify(...)`. Task 9 and the summary/risk notes now correctly specify plain `SimpleHandlerDecoratorFactory`.
- **Finding 3 (was 70) — FIXED.** `RecordingDecoratorFactory` is now a concrete Task 5 deliverable. Verified `IQueryHandlerDecoratorFactory` and `IQueryHandlerDecoratorFactoryAsync` are **member-identical** (`T Create<T>(Type)` / `void Release<T>(T)`), so one class implementing both is valid. No generic-variance trap: `Release<T>(T handler)` records into a `List<IQueryHandlerDecorator>` and `ReleaseCount(decorator)` via reference-equality works because the test passes the **same** decorator instance to `Create`-return and the assertion. Confirmed 3 decorator `Release` verifies at `FallbackPolicyTests.cs:51,77,102` and that `SimpleHandlerDecoratorFactory.Release<T>` only disposes (`:50-54`).
- **Finding 4 (was 48) — ADDRESSED.** Task 4 now has the explicit "CancellationToken is behaviour-irrelevant post-migration / not recorded" paragraph.
- **ADR addendum.** The 2026-06-05 addendum is delimited, dated, attributed, and **extends** Decision 4 to the decorator factory by identical reasoning — it does **not contradict** the ADR body. Claims check out. Amending an Accepted ADR with a transparent, non-contradictory addendum is acceptable; status legitimately remains "Accepted."
- **Reference spot-checks.** Moq csproj line 22 ✓, props line 15 ✓. `grep -rl 'Mock<'` lists exactly the four target files ✓. FR3 closed list matches `FallbackPolicyTests.cs:105-156` ✓. FR4 closed list matches `PipelineBuilderExceptionTests.cs:22-104` ✓. Five `Exported/*.cs` (AC8) ✓. `[Fact]` counts 3/3/3/4, no `[Theory]` (AC13) ✓. `QueryHandler.Execute` abstract / `Fallback` virtual + async equivalents ✓. AC14's two-grep split correct (`Directory.Packages.props` at repo root, outside first grep's `test src` scope) ✓. No AC7 name collisions from planned additions ✓.

**Coverage cross-reference (re-checked fresh):** FR1→T6, FR2→T7, FR3→T2+T8, FR4→T3+T9, FR5→T2/T3, FR6→T5, FR7→T6-9, FR8→T10, FR9→T1, FR10→T4/T5/T6/T7. NFR1/NFR4 via tidy-first framing + green gate, NFR2→T0/T11 + per-task gates, NFR3 via AC1/2/3/5. AC1–AC14 all enumerated in Task 11 (+ AC11 in Task 0, AC12 per-task). ADR Decisions 1–4, the addendum, the nested-double extraction, and the sync/async `Release` asymmetry all map to tasks. **No unmapped requirement found.** The `/tidy-first` (not `/test-first`) framing is justified: no task changes asserted behaviour.

**Recommendation**: None.

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

All three prior blockers (92/72/70) are genuinely fixed, verified against `PipelineBuilder.cs`, the four target test files, the decorator-factory interfaces, and the ADR addendum — not merely asserted. The two remaining findings are low-severity documentation looseness; neither breaks the green-suite gate or blocks implementation. No finding reaches the 60 threshold. **Verdict: PASS.**
