# Review: design — 007-use_simple_not_mocks (ADR 0013)

**Date**: 2026-06-05
**Threshold**: 60
**Verdict**: PASS

> Adversarial design review of `docs/adr/0013-migrate-legacy-tests-to-test-doubles.md`. Every load-bearing codebase claim was verified against the real tree; all hold. Only three sub-threshold presentational nits.

## Findings

### 1. Async recording double has no illustrative code; `FallbackAsync` overridability asserted but not shown (Score: 48)

Decision 3 gives a complete, compilable sync `RecordingQueryHandler<TQuery,TResult>` sample but only *names* the async sibling. The underlying claim is sound — `QueryHandlerAsync.cs:17` has `public virtual Task<TResult> FallbackAsync(TQuery, CancellationToken = default)` and `ExecuteAsync` is abstract — so an async double overrides both exactly like the sync one. The async double must thread `CancellationToken` through, which the `Func<TQuery,TResult>` ctor shape doesn't illustrate.

**Evidence**: ADR line 82 names `RecordingQueryHandlerAsync<TQuery,TResult>`; the only code block (86–102) is sync-only. `QueryHandlerAsync.cs:17`.

**Recommendation**: Add a one-line note: the async double overrides `ExecuteAsync`/`FallbackAsync` (both verified virtual/abstract) and takes a `Func<TQuery, Task<TResult>>` plus `CancellationToken` pass-through. Illustrative-only; a sentence suffices.

---

### 2. Sync/async `Release`-assertion asymmetry not called out (Score: 32)

The narrative implies a symmetric "handler released → assert `Release` count." In fact the async file asserts `Release` exactly once with `Times.Never` (`QueryProcessorAsyncTests.cs:53`), while sync `ExecutesQueries` asserts `Times.Once` (`QueryProcessorTests.cs:43`). The mapping table (`Times.Never → ShouldBe(0)`) already covers this correctly, so it's not a defect — just an undocumented asymmetry an implementer must preserve (NFR1).

**Evidence**: `QueryProcessorAsyncTests.cs:53` (`Times.Never`) vs `QueryProcessorTests.cs:43` (`Times.Once`).

**Recommendation**: Optionally note that the async case preserves as `ReleaseCount(handler).ShouldBe(0)`, not `1`.

---

### 3. Decision 2 doesn't note the decorator-factory interfaces are generic (Score: 22)

Decision 2 correctly says `SimpleHandlerDecoratorFactory` implements both interfaces on one class. Unlike the non-generic *handler* factory interfaces, the *decorator*-factory ones are generic (`T Create<T>(Type)`/`Release<T>(T)`). This affects no decision (the ADR reuses the real type, never hand-rolls a decorator double). Transparency only.

**Evidence**: `IQueryHandlerDecoratorFactoryAsync.cs`; ADR line 61.

**Recommendation**: None required.

---

## Verified-correct claims (no finding)

All checked specifically because they were the most likely defect sites; all hold:

- **`Fallback` overridable** (Decision 3's `override TResult Fallback` + `base.Fallback`): `QueryHandler.cs:15` `public virtual TResult Fallback(...)`; `Execute` abstract. Sync sample compiles.
- **`where TQuery : IQuery<TResult>`**: consistent — `QueryHandler<TQuery,TResult>` itself carries that constraint (`QueryHandler.cs:7`).
- **Both factory interfaces identical; `RecordingHandlerFactory` implements both with no missing async member**: `IQueryHandlerFactory.cs` and `IQueryHandlerFactoryAsync.cs` byte-for-byte identical (`Create(Type)` + `Release(IQueryHandler)`); no `CreateAsync`. `SimpleHandlerFactory.cs:32` already implements both on one class → a single `RecordingHandlerFactory` suffices for the async test.
- **`SimpleHandlerFactory.Release` not virtual** (Alternative 5 subclassing claim): `SimpleHandlerFactory.cs:49` non-virtual; disposes, matching "may still dispose."
- **"Exported = used by assembly scanning"** is real, not assumed: `When_AddHandlersFromAssemblies_scans_assembly...cs:21,40` and `QueryProcessorIntegrationTests.cs:26` call `AddHandlersFromAssemblies(typeof(TestQueryHandler[Async]).Assembly)`.
- **Four target files, "15 `.Verify` calls", three scenarios**: exact (sync 9 + async 6 = 15; `It.Is<>`, `Release Times.Once/Never`, `FormatException` all present).
- **Prior ADRs 0009/0008/0004**: exist with the cited roles; 0009 Accepted.
- **RDD vocabulary** matches `design_principles.md`; Service Provider + Information Holder justified as cohesive ("the handler under observation"), not an SRP violation.
- **No contradiction with requirements**: ADR's "prefer result, fall back to recording" mirrors the updated FR10/AC5; exception path addressed; no `src/` scope creep.

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 0 |
| 0-49 (Low) | 3 |

**Total findings**: 3
**Findings at or above threshold (60)**: 0

**Verdict logic**: No finding ≥ 60 → **PASS**.
