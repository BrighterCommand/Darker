# Review: tasks — 009-resilience_pipeline

**Date**: 2026-06-18
**Threshold**: 60
**Verdict**: PASS

> Re-review of the CURRENT `tasks.md` after a fix cycle, against `requirements.md` and ADR 0015. Both prior ≥60 findings independently verified as FIXED; all three sub-threshold nits fixed. Every Darker-local file/line citation re-verified against the working tree. Hunted for new defects introduced by the fixes (dependency cycles, B2↔B3/B5 contradiction, wrong edges) — none found. One new sub-threshold nit (B2 shell omits a generic constraint). No finding at or above threshold.

## Findings

### 1. B2 shell declaration omits the `where TQuery : IQuery<TResult>` constraint (Score: 35)

B2 tells the implementer to create the shell as `UseResiliencePipelineHandler<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>` (and the async counterpart) but does not state the generic constraint. The interface `IQueryHandlerDecorator<TQuery, TResult>` carries `where TQuery : IQuery<TResult>` (verified), so a shell written literally as quoted will not compile — defeating the point of the B2-first "make it compile" shell. The implementer is told to "mirror `RetryableQueryDecorator.cs`", which has the constraint, so this is a low-risk wording gap rather than a blocker. Same omission for the async shell vs `RetryableQueryDecoratorAsync` (`where TQuery : IQuery<TResult>`, verified).

**Evidence**: `src/Paramore.Darker/IQueryHandlerDecorator.cs:12-14` — `interface IQueryHandlerDecorator<TQuery, TResult> : IQueryHandlerDecorator where TQuery : IQuery<TResult>`. `src/Paramore.Darker/Policies/Handlers/RetryableQueryDecoratorAsync.cs:12-13` declares `where TQuery : IQuery<TResult>`. tasks.md B2/B3/B5 quote the shell type without the constraint.

**Recommendation**: In B2, add `where TQuery : IQuery<TResult>` to both shell declarations (or state "mirror the class header of `RetryableQueryDecorator[Async]` including its `where TQuery : IQuery<TResult>` constraint"). Optional to fix before implementation.

---

## Verification of prior findings

| Prior finding | Status | Evidence |
|---|---|---|
| 1 (90) DI path never threads `ResiliencePipelineProvider<string>` into `QueryProcessor` via `BuildQueryProcessor` → AC11/FR5 unachievable | **FIXED** | B13 now has an explicit "CRITICAL (Finding 1…)" step: edit `ServiceCollectionExtensions.BuildQueryProcessor` (`ServiceCollectionExtensions.cs:43-49`) to `provider.GetService<ResiliencePipelineProvider<string>>()` and pass it as the new ctor param, the same way as `policyRegistry`. Verified: `BuildQueryProcessor` resolves `IPolicyRegistry<string>` at `:43`, constructs `new QueryProcessor(...)` at `:45-49`. B13 now `Depends on: B11, B1`; dependency-summary note records the B1→B13 DI edge. Sufficient for AC11/FR5, acyclic. |
| 2 (78) B2 ordered before B3/B5 but hard compile-time dependency on the decorator types | **FIXED** | B2 now creates "empty handler shells … bodies left to B3/B5 (e.g. `Execute`/`ExecuteAsync` throwing `NotImplementedException`)"; B3/B5 now "Fill in the … shell created in B2". Direction coherent (B2→B3/B5), no cycle (B2 `Depends on: S1` only). B2's test asserts only `GetAttributeParams`/`GetDecoratorType`, which the shells satisfy. Verified `RetryableQueryAttribute.cs:22-24` returns `typeof(RetryableQueryDecorator<,>)` (mirror pattern). |
| Nit 3 (55) test-doubles namespace wrong | **FIXED** | tasks.md now states `Paramore.Darker.Core.Tests.TestDoubles`, matching `TrackingQueryContextFactory.cs:1`. |
| Nit 4 (50) stale `/test-first` path | **FIXED** | tasks.md adds a "⚠️ Stale skill path" note; verified `test/Paramore.Darker.Tests` does not exist and `.claude/commands/tdd/test-first.md:164,180` still reference it. |
| Nit 5 (30) B14 mirror wording | **FIXED** | B14 now distinguishes the delegating DI `AddDefaultPolicies` (`PolicyDIExtensions.cs:39`) from the inlined builder `AddDefaultPolicies<TBuilder>` (`QueryProcessorBuilderExtensions.cs:66-92`). |

## Re-verified production citations (all accurate)

`QueryProcessor.cs:25,27-30,44,102-106`; `QueryProcessorBuilder.cs:12,93` (path `src/Paramore.Darker/Builder/`); `RetryableQueryDecorator.cs:23-24,34-35`; `RetryableQueryAttribute.cs:22-24`; `QueryProcessorBuilderExtensions.cs:27-28,30-34,66-92`; `PolicyDIExtensions.cs:39`; `ServiceCollectionExtensions.cs:43,45-49`; `Constants.cs` legacy keys (S2's new keys distinct); `IQueryContext.cs`/`QueryContext.cs` (sole impl, import instructions correct). Legacy template tests for B1/B15 all exist; `Decorators/` subdir exists in Core.Tests.

## Dependency graph re-check

S1, S2 structural (`/tidy-first`), behaviour-free, before all behavioural — correct. Edges acyclic; the two repaired edges (B2→B3/B5 now one-directional; B1→B13 DI thread) both correct. Graph text and per-task `Depends on:` lines match.

## Coverage — FR/NFR/RD/AC → task (verified, no gaps)

FR1 (S1,B1) · FR2 (B3,B5) · FR3 (B2) · FR4 (B11, no content check) · FR5 (B13, now achievable) · FR6 (B12,B14,S2) · FR7 (B4,B7) · FR8 (B10) · FR9 (B5/B6). NFR1 (B15) · NFR2 (B3/B5,B8,B9,B15) · NFR3 (B15) · NFR4 (B2) · NFR5 (all test tasks). RD1/RD2 (S1) · RD3 (B2) · RD4 (S2). AC1 (B3,B5) · AC2 (B8) · AC3 (B9) · AC4 (B10) · AC5 (B4,B7) · AC6 (B12,B14) · AC7 (B5) · AC8 (B6) · AC9 (B15) · AC10 (B11) · AC11 (B13, now achievable) · DoD (B15).

**ADR decision → task**: §1 (S1,B1) · §2 incl. async FR9 branches + sync no-context (B3-B7) · §3 (B2) · §4 type-scoped + generic-registration precondition + defaults-no-UseTypePipeline (B10,B12) · §5 builder/DI/defaults (B11-B14, DI thread gap closed). No gaps.

**Scope-creep check**: none. S2 constant values illustrative; no telemetry, legacy removal, timing changes, or new deps. B15 guards NFR3 and AC9.

**Test-first framing**: B1-B14 each carry a `/test-first` command + explicit STOP/approval gate; S1-S2 use `/tidy-first` and are behaviour-free; B15 is a non-`/test-first` verification gate. Granularity one-session per task.

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 0 |
| 0-49 (Low) | 1 |

**Total findings**: 1
**Findings at or above threshold (60)**: 0

**Verdict: PASS.** Both prior ≥60 findings (DI provider threading; B2 compile-gap/ordering) are genuinely resolved, and all three sub-threshold nits are fixed. The fixes introduced no dependency cycle, no B2↔B3/B5 contradiction, and no wrong edge. The only remaining issue is a sub-threshold wording nit: B2's shell declaration omits the `where TQuery : IQuery<TResult>` generic constraint the decorator interface requires (mitigated by the "mirror `RetryableQueryDecorator`" instruction).
