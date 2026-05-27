# Review: design — 005-merge_builtin_decorators (round 7)

**Date**: 2026-05-27
**Threshold**: 60
**Verdict**: PASS

## Findings

No threshold-crossing findings. The round-6 finding (F1, score 65 — stale "precise-count assertions" wording in the Risks table at line 419) has been correctly fixed. Line 419 now reads:

> "...would re-register the now-`internal` local TestDoubles, causing `RegisterFromAssemblies` to throw `ConfigurationException` on the duplicate `SyncTestQuery`/`AsyncTestQuery` handlers (see §10) and breaking `When_AddHandlersFromAssemblies_*` and `QueryHandlerRegistryTests` at handler-discovery time"

This mirrors §10's framing (line 308) exactly and references the correct exception type, the correct failure mode (handler-discovery-time throw, not silent count drift), and the right tests. The two paragraphs are now internally consistent.

### Round-5 fixes spot-checked — all hold

- §5 line 245 cross-references "Implementation Approach **Step 9**" (correct — the deletion/grep-safety-net step is now Step 9 after the Step 7/8 expansion). Verified.
- §10 line 301 describes `<PrivateAssets>all</PrivateAssets>` as a belt-and-braces transitive control with xunit's entry-assembly-only discoverer behaviour as the load-bearing mechanism. Verified.
- §10 line 303 documents the "extract `Exported/` into a separate non-test csproj" contingency as immediately actionable ("It is no longer deferred to a follow-up — it is the documented contingency action for the implementer if the count check fails"). Verified.
- ADR Architecture Overview line 117 cites "12 files, 14 public class decls". Verified (12 files counted in `test/Paramore.Darker.Tests/TestDoubles/`).
- §10 line 308 "Why `internal`" rationale cites `ConfigurationException("Registry already contains an entry...")` from `RegisterFromAssemblies`. Verified.
- Step 7 line 364 says "the AOT project's two source files — `Base/AOTTestClassBase.cs` and `QueryProcessor/AOTQueryProcessorTests.cs`". Verified (those are exactly the two `.cs` files outside `obj/`).

### Codebase grounding — all factual claims verified

- §9 claim: both `QueryHandlerRegistry.RegisterFromAssemblies` and `QueryHandlerRegistryAsync.RegisterFromAssemblies` use `assemblies.SelectMany(a => a.ExportedTypes)` — verified at `src/Paramore.Darker/QueryHandlerRegistry.cs:51` and `src/Paramore.Darker/QueryHandlerRegistryAsync.cs:49`.
- §10 / FR12 claim of 12 files in `TestDoubles/` — verified (12 files listed).
- §11 / Step 8 claim of "43 source files at spec time" with `namespace Paramore.Darker.Tests` declarations — verified (`grep -rln` returns 43).
- §10 "Why internal" rationale names 5 handlers bound to `SyncTestQuery`: `LoggingQueryHandler`, `RetryableQueryHandler`, `SyncHandlerWithFallback`, `SyncHandlerWithAsyncAttribute`, and `ContextCapturingHandler` — verified, all five files declare `QueryHandler<SyncTestQuery, SyncTestQuery.Result>`.
- §5 enumeration of 5 csprojs holding side-package references (`Paramore.Darker.Tests`, `Paramore.Darker.Tests.AOT`, `SampleMauiTestApp`, `SampleMinimalApi`, `Paramore.Darker.Extensions.DependencyInjection`) — verified exactly matches `grep -rln` output.
- §11 claim of 4 files in `test/Paramore.Darker.Tests/Integrations/` — verified (4 files listed).

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 0 |
| 0-49 (Low) | 0 |

**Total findings**: 0
**Findings at or above threshold (60)**: 0
