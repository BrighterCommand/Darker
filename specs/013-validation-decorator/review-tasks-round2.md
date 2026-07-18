# Review (round 2): tasks (ralph) — 013-validation-decorator

**Date**: 2026-07-18
**Threshold**: 60
**Verdict (as reviewed)**: NEEDS WORK → **remediated (see bottom)**

Second adversarial pass over the revised `ralph-tasks.md` + amended ADR 0020, focused on (a)
confirming the Finding-1 remediation is correct and (b) hunting for regressions the remediation
introduced.

## Remediation verification (round-1 findings)

- **Finding 1 (runtime-type validator resolution) — CONFIRMED FIXED.** `PipelineBuilder.cs:253/404`
  still close decorators over `IQuery<TResult>`; the decorator is invoked with the concrete query
  *object*, so `query.GetType()` is the concrete type. `GetService(IValidator<>.MakeGenericType(query.GetType()))`
  matches the `IValidator<FvTestQuery>` DI key exactly. FluentValidation 11.x non-generic
  `IValidator.Validate(IValidationContext)` / `ValidateAsync(IValidationContext, ct)` with
  `new ValidationContext<object>(query)` is the correct API (validator dispatches on the runtime
  type of `InstanceToValidate`). Fail-fast via `GetService`→null→`ConfigurationException` is sound.
- **Finding 2 (end-to-end + DI generic argument) — FIXED IN CONCEPT**, but the added end-to-end
  tasks named a non-compiling registration method (New Finding 1, now fixed). The `Use*` DI tests
  correctly resolve over `IQuery<TResult>`; the abstract→concrete open-generic mapping constructs
  because `IQuery<TResult>` satisfies `where TQuery : IQuery<TResult>` (proven in production by
  `RetryableQueryDecorator`).
- **Finding 3 (FR10 guard) — PARTIALLY FIXED** (New Finding 2, now strengthened).
- **Finding 4 (multiple errors) — CONFIRMED FIXED.**
- **Finding 5 (cancellation token) — CONFIRMED FIXED.**
- **Finding 6 (cross-provider relabel) — CONFIRMED FIXED.**

## New findings (introduced by the remediation)

### 1. End-to-end tasks called `AddHandlers(assembly)` — won't compile; async needs `AddHandlersFromAssemblies` (Score: 66) — CONFIRMED

`IDarkerHandlerBuilder.AddHandlers` takes `Action<IQueryHandlerRegistry>` (sync registry only,
verified `IDarkerHandlerBuilder.cs:12`, `ServiceCollectionDarkerHandlerBuilder.cs:41`); passing an
`Assembly` does not compile, and it would not register the async handler even via a lambda. The
correct method is `AddHandlersFromAssemblies(params Assembly[])` which registers sync + async
(`:30`), as every existing async end-to-end test uses (`QueryProcessorIntegrationTests.cs:26`).

**Evidence**: main agent re-verified via grep. **Fix applied**: both end-to-end tasks now use
`AddHandlersFromAssemblies(typeof(...HandlerAsync).Assembly)`, with a References note that
`AddHandlers` is sync-only.

### 2. FR10 guard weaker than it appears — trimming blind spot + wrong DataAnnotations assembly name (Score: 62) — CONFIRMED

`GetReferencedAssemblies()` only lists assemblies used in IL, so a declared-but-unused provider
package reference would pass. Additionally, on net8/net9 the `System.ComponentModel.DataAnnotations.*`
types live in the **`System.ComponentModel.Annotations`** assembly, so matching only the
`System.ComponentModel.DataAnnotations` string would miss a DataAnnotations use.

**Evidence**: reviewer knowledge of BCL layout (flagged as not repo-verifiable) + reflection
semantics. **Fix applied**: the FR10 guard now (a) also matches `System.ComponentModel.Annotations`,
and (b) adds a csproj-graph assertion that the core project declares no FluentValidation/DataAnnotations
`PackageReference`/`ProjectReference` — the two checks cover each other's blind spots.

## Summary (as reviewed)

| Score Range | Count |
|---|---|
| 90-100 | 0 |
| 70-89 | 0 |
| 50-69 | 2 |
| 0-49 | 0 |

**Findings ≥ 60**: 2 — both remediated below. The Finding-1 blocking defect remains fixed and was
NOT reopened.

---

## Remediation applied (2026-07-18, round 2)

- **New Finding 1** — both end-to-end tasks switched to
  `AddHandlersFromAssemblies(typeof(<Handler>Async).Assembly)`; References updated to warn that
  `AddHandlers(Action<IQueryHandlerRegistry>)` is sync-only. The two `Use*` DI-resolution tasks
  (which resolve the decorator directly, not through the pipeline) were simplified to
  `AddDarker(...).UseX()` to remove the ambiguous `AddHandlers(...)`.
- **New Finding 2** — FR10 guard broadened to also match `System.ComponentModel.Annotations` and
  augmented with a csproj-reference assertion.

Task count unchanged at 29. No findings above threshold remain.
