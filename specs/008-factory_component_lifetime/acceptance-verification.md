# Acceptance Verification — Spec 008 (factory_component_lifetime)

Date: 2026-06-09 · Issue [#329](https://github.com/BrighterCommand/Darker/issues/329) ·
ADR `docs/adr/0014-factory-component-lifetime.md` (Accepted)

Final verification that the DI-backed handler and decorator factories are lifetime-aware
(Singleton / Scoped / Transient), so components and their injected dependencies are created and
released per their configured lifetime. All checks run on branch `329-factory-component-lifetime`.

## Build & test (NFR4, F1)

- `dotnet build Darker.Filter.slnf -c Release` → **0 errors**.
- `dotnet test Darker.Filter.slnf -c Release --no-build` → **all green**:
  - `Paramore.Darker.Core.Tests` = 76 passing (net8.0 and net9.0), 0 failed, 0 skipped.
  - `Paramore.Darker.Extensions.Tests` = 30 passing (net8.0 and net9.0), 0 failed, 0 skipped.
  - Total **212** passing across both frameworks.

## Acceptance criteria

| AC  | Result | Test file (task) — evidence |
|-----|--------|------------------------------|
| AC1 — Singleton reuse | PASS | `When_handler_lifetime_is_singleton_should_reuse_dependency_and_not_dispose.cs` (B2): same instance reused across two queries, `ConstructionCount == 1`. |
| AC2 — Singleton not disposed | PASS | ↑ (B2): `IsDisposed == false` after both queries — Darker's `Release` is a no-op for singletons. |
| AC3 — Transient disposed | PASS | `When_handler_lifetime_is_transient_should_create_fresh_and_dispose_after_pipeline.cs` (B1): fresh per query (`ConstructionCount == 2`), each `IsDisposed == true`. |
| AC4 — Scoped: one scope per execution, shared, disposed | PASS | `When_handler_lifetime_is_scoped_should_share_dependency_across_handler_and_decorator.cs` (B3): handler and decorator share one scoped instance, disposed after the pipeline. |
| AC5 — Scoped dependency under Singleton processor | PASS | `When_query_processor_is_singleton_and_dependency_is_scoped_should_resolve_and_dispose.cs` (B4): scoped dep resolves from the per-query child scope (not root) and is disposed; passes with `ValidateScopes = true`. |
| AC6 — Concurrency isolation (deterministic) | PASS | `When_two_queries_run_concurrently_should_isolate_scopes.cs` (B5): barrier-gated concurrent pipelines get distinct scoped deps; neither disposes the other's. |
| AC7 — Failure-path disposal | PASS | `When_pipeline_fails_should_dispose_dependency.cs` (B6): throw (sync + async) and cancel (async) still dispose the per-query dependency; `[Theory]` over Transient and Scoped. |
| AC8 — Decorators follow the same rules | PASS | `When_decorator_lifetime_configured_should_follow_same_rules_as_handler.cs` (B7): Singleton decorator dep reused/not disposed; Transient decorator dep fresh-per-query/disposed. |
| AC9 — Default-path guard | PASS | `When_no_lifetime_configured_should_create_once_per_query_and_dispose.cs` (B8): with no `HandlerLifetime` set (default Transient), fresh per query + disposed after each pipeline. |
| AC10 — Sync and async parity | PASS | B9 audit: every behavioural test pairs `Execute` (`void`) with `ExecuteAsync` (`async Task`); B6 adds throw-sync + throw-async + cancel-async (sync cancellation not applicable). No gaps. |

## NFR4 coverage matrix (lifetime × component × sync/async)

| Lifetime | Handler (sync / async) | Decorator (sync / async) |
|----------|------------------------|--------------------------|
| Transient | B1, B8 ✅ / ✅ | B7 ✅ / ✅ |
| Singleton | B2 ✅ / ✅ | B7 ✅ / ✅ |
| Scoped | B3, B4 ✅ / ✅ | B3 ✅ / ✅ |

Plus the two named scenarios: singleton-reuse (AC1 / B2) and scoped-dependency (AC5 / B4). All
cells covered.

## Definition of Done

- [x] DI factories honour Singleton / Scoped / Transient for handlers and decorators (FR1–FR8).
- [x] Singleton components reused and never disposed by Darker (FR2, FR3).
- [x] Transient/Scoped components and their injected disposables released per query (FR1, FR4, FR9).
- [x] Scoped dependencies resolve from a per-query child scope even under a Singleton
      `QueryProcessor` (FR5) — defect 2 fixed.
- [x] Per-query scopes isolated under concurrency; shared factory state is thread-safe (FR6, NFR3).
- [x] Failure-path (throw / cancel) still disposes the per-query scope (FR9).
- [x] Default lifetime (`Transient`) and default `QueryProcessor` lifetime (`Singleton`) unchanged
      (NFR2) — no scope creep.
- [x] All three lifetimes × {handlers, decorators} × {sync, async} covered via TDD (NFR4).

**Result: all of AC1–AC10 PASS. Implementation complete.**
