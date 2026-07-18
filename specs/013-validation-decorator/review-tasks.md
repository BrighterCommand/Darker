# Review: tasks (ralph) — 013-validation-decorator

**Date**: 2026-07-18
**Threshold**: 60
**Verdict**: NEEDS WORK

> Adversarial review of `ralph-tasks.md` (unattended TDD task list). Judged against the
> ralph-tasks contract (RALPH-VERIFY + References; no `/test-first`/`STOP HERE` gates).
> Finding 1 independently re-verified by the main agent against `PipelineBuilder.cs`.

## Findings

### 1. FluentValidation `IValidator<TQuery>` resolution is broken — Darker closes decorators over `IQuery<TResult>`, not the concrete query type (Score: 92) — CONFIRMED

The FluentValidation provider design rests on resolving a per-query `IValidator<TQuery>` from DI
(FR3; FV decorator tasks; ADR 0020: "lets DI close the generic per query and constructor-inject a
per-`TQuery` validator"). This assumes the decorator's `TQuery` is the concrete query type (e.g.
`FvTestQuery`). **It is not.**

`PipelineBuilder<TResult>` closes every decorator's open generic over the **interface**
`IQuery<TResult>`, never the concrete query:

```csharp
// src/Paramore.Darker/PipelineBuilder.cs:253  (sync) and :404 (async)
var decoratorType = attribute.GetDecoratorType()
    .MakeGenericType(typeof(IQuery<TResult>), typeof(TResult));
```

So the pipeline requests `FluentValidationQueryValidatorDecorator<IQuery<TResult>, TResult>`, whose
`TQuery` is `IQuery<TResult>`. A constructor-injected or `IServiceProvider`-resolved
`IValidator<TQuery>` becomes `IValidator<IQuery<TResult>>` — which nobody registers (the developer
registers `IValidator<FvTestQuery>`). Result: the fail-fast guard fires on **every** query (or DI
returns null); real per-query FluentValidation can never run through the pipeline.

The fix: resolve the validator by reflecting on the **runtime object type** inside
`Validate`/`ValidateAsync` — `serviceProvider.GetService(typeof(IValidator<>).MakeGenericType(query.GetType()))`
— because the actual object passed to `Execute` is the concrete query even though its static type is
`IQuery<TResult>`. (Existing Darker decorators confirm this model: `RetryableQueryDecoratorAsync`
pulls its dependency from `Context`, never from per-`TQuery` DI.) The DataAnnotations provider is
**unaffected** — `Validator.TryValidateObject(query, new ValidationContext(query), …)` reflects on
the runtime object.

**Evidence**: `src/Paramore.Darker/PipelineBuilder.cs:253` and `:404` (verified by main agent via
grep); FV decorator tasks specifying generic `IValidator<TQuery>` injection; ADR 0020 §Forces at
play / §Decision.

**Recommendation**:
1. Rework the FV provider tasks so the decorator injects `IServiceProvider` and resolves
   `IValidator<>` from `query.GetType()` at validate-time (not a generic `IValidator<TQuery>`).
2. **Correct ADR 0020**: replace "constructor-inject a per-`TQuery` validator" / "DI closes the
   generic per query" with the runtime-type resolution mechanism, and note that `TQuery` is
   `IQuery<TResult>` at pipeline runtime. (This is a design correction, so the ADR — already
   Accepted — needs a targeted amendment.)
3. Add an explicit task capturing the `TQuery == IQuery<TResult>` runtime fact so the unattended
   loop doesn't reintroduce generic injection.

---

### 2. No end-to-end coverage through the real QueryProcessor pipeline; the DI tests assert a resolution path the pipeline never uses (Score: 86) — CONFIRMED

Every decorator/provider test instantiates the decorator in isolation or resolves it directly from a
`ServiceProvider`. The `UseFluentValidation`/`UseDataAnnotations` tests assert that resolving the
**concrete-query-closed** abstract type (`ValidateQueryDecorator<FvTestQuery, FvTestQuery.Result>`)
yields the concrete decorator. But per Finding 1 the pipeline only ever requests
`ValidateQueryDecorator<IQuery<TResult>, TResult>`. Closing the test over `FvTestQuery` resolves
`IValidator<FvTestQuery>` and goes green — while the real pipeline (closing over `IQuery<TResult>`)
fails. Textbook false-green: the isolation tests validate a path production never exercises, hiding
the exact DI-wiring defect that is the riskiest part of this design. No task annotates a handler with
`[ValidateQuery]`, registers a validator, and runs a query through a real `QueryProcessor`.

**Evidence**: `UseFluentValidation`/`UseDataAnnotations` DI tasks close over `FvTestQuery`;
`PipelineBuilder.cs:253/404` closes over `IQuery<TResult>`; no task builds a `QueryProcessor` and
calls `Execute`/`ExecuteAsync`.

**Recommendation**: Add at least one end-to-end task per path: register a handler with
`[ValidateQuery]`/`[ValidateQueryAsync]` + a validator via `AddDarker(...).UseFluentValidation()`,
drive a valid and an invalid query through a real `QueryProcessor`, and assert pass-through +
`QueryValidationException`. This is the test that surfaces Finding 1. Add the equivalent for
DataAnnotations.

---

### 3. The dependency-free-core guarantee (FR10) and DataAnnotations "no FluentValidation reference" are asserted but never verified (Score: 62) — CONFIRMED

FR10 and an explicit acceptance criterion require the core to carry no
FluentValidation/DataAnnotations dependency; a DA task states its test project must have "No
FluentValidation package referenced." But the RALPH-VERIFY for these is `dotnet build`, which
succeeds whether or not such a reference exists. Nothing enforces the constraint as a regression
guard; a later task adding an offending reference would still pass.

**Evidence**: Core scaffold task ("references ONLY `Paramore.Darker`") + DA scaffold task ("No
FluentValidation package is referenced") both RALPH-VERIFY with `dotnet build`; requirements FR10 +
Acceptance Criteria.

**Recommendation**: Add a test that reflects over the core assembly's
`Assembly.GetReferencedAssemblies()` and asserts no `FluentValidation` /
`System.ComponentModel.DataAnnotations` reference — an executable guard for FR10.

---

### 4. No task verifies multiple validation errors are surfaced together (Score: 55)

FR6 defines `QueryValidationException.Errors` as a collection; a realistic failure produces several
errors. Every provider failure task asserts mapping "for at least one failure." None asserts that a
query with multiple failing rules produces multiple `QueryValidationError`s. A mapping bug returning
only the first failure would pass all listed tests.

**Evidence**: FV/DA failure tasks ("assert all four for at least one failure"); requirements FR6
(read-only *collection*).

**Recommendation**: Extend one FV and one DA failure task to assert two distinct property failures
map to two `QueryValidationError` entries.

---

### 5. Cancellation-token propagation is not asserted on the async path (Score: 40)

The async "valid" tasks assert `next` is invoked once but never that the `CancellationToken` is
threaded into `ValidateAsync`/`next`. Given the async NFR and that the FV async path awaits
`ValidateAsync(query, ct)`, a dropped token would go unnoticed.

**Evidence**: async valid-path tasks;
`src/Paramore.Darker/IQueryHandlerDecoratorAsync.cs:10-13` (`ExecuteAsync(..., CancellationToken)`).

**Recommendation**: Assert a supplied token reaches `ValidateAsync` (and is passed to `next`) on at
least one async path.

---

### 6. Cross-provider task bundles a build/reference change into an "assertion-only" test (Score: 30)

The cross-provider task is labelled "assertion-only" with "Implementation files: _(none)_", yet it
adds a `ProjectReference` from `...FluentValidation.Tests` to the DataAnnotations provider. That is a
real project-file change, and cross-linking the DA provider from the FV test project slightly erodes
the isolation the task demonstrates (the FV *package* stays out, but the projects now cross-link).

**Evidence**: cross-provider task "Implementation files: _(none — assertion-only test)_ Add a
ProjectReference …".

**Recommendation**: Relabel to acknowledge the reference change, or place the cross-provider shape
test in a dedicated test project that legitimately references both providers.

---

## Verified SOUND (not defects)

- All referenced existing paths exist; type signatures the tasks assume are correct
  (`QueryHandlerAttribute` abstract members; `IQueryHandlerDecorator` `Context` +
  `InitializeFromAttributeParams`; `Execute`/`ExecuteAsync(query, next, fallback, ct)` shapes).
- The abstract→concrete open-generic DI mapping added directly to `builder.Services` **is** the
  correct mechanism — `ServiceProviderComponentFactory` resolves via
  `serviceProvider.GetService(closedType)`, and `ServiceCollectionDecoratorRegistry.Register`
  (self-to-self) genuinely cannot express abstract→concrete. The task NOTE is accurate. (The mapping
  resolves; the flaw in Finding 1 is the *generic argument* it resolves under.)
- Test-project conventions correct (`net8.0;net9.0`, `xunit.v3` + `Shouldly`, CPM without versions);
  FluentValidation not yet pinned (the CPM-pin task is needed).
- Solution wiring correct (`Darker.slnx` per-project `<Configuration>` blocks; `Darker.Filter.slnf`
  escaped paths; both must be edited).
- Ordering/dependencies: no forward references (after the main agent's earlier attribute-reorder fix)
  — error/exception precede decorators; abstract decorators precede attributes and provider
  subclasses; FV CPM pin precedes the FV project; `Use*` follow their decorators.
- FR-to-task and Resolved-Decision-to-task mapping otherwise complete (FR1–FR10, Decisions 1–4).

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 1 |
| 70-89 (High) | 1 |
| 50-69 (Medium) | 2 |
| 0-49 (Low) | 2 |

**Total findings**: 6
**Findings at or above threshold (60)**: 3

**Blocking issue**: Finding 1 — as written, the FluentValidation provider cannot resolve a per-query
validator because Darker's pipeline closes all decorators over `IQuery<TResult>`, not the concrete
query type — and Finding 2 explains why the task list's own tests would not catch it. Both ADR 0020's
wording and the FV provider / `Use*` / DI-test tasks need reworking around runtime-type validator
resolution plus a real end-to-end pipeline test.

---

## Remediation applied (2026-07-18)

User chose "amend ADR + regenerate tasks". Actions taken:

- **Finding 1** — ADR 0020 amended (see its "Amendment (2026-07-18)" section + corrected Forces /
  diagram / Key Components / Implementation Approach). FV decorator tasks (sync/async valid,
  invalid, missing-validator) rewritten to inject `IServiceProvider` and resolve `IValidator<>` from
  `query.GetType()` via FluentValidation's non-generic `IValidator.Validate(IValidationContext)`. A
  CRITICAL note records that `TQuery` is `IQuery<TResult>` at runtime.
- **Finding 2** — `UseFluentValidation`/`UseDataAnnotations` DI-resolution tests corrected to resolve
  the abstract type closed over `IQuery<TResult>` (what `PipelineBuilder` actually requests). Added
  two **end-to-end tasks** (one per provider) driving `[ValidateQueryAsync]` handlers through a real
  `QueryProcessor`.
- **Finding 3** — added an FR10 executable guard task asserting the core assembly's
  `GetReferencedAssemblies()` has no FluentValidation / DataAnnotations reference.
- **Finding 4** — FV and DA invalid-query tasks now assert two failures → two `QueryValidationError`s.
- **Finding 5** — the FV async valid-path task now asserts `CancellationToken` propagation.
- **Finding 6** — cross-provider task relabelled to acknowledge the `ProjectReference` change.

Task count 26 → 29. A re-review is recommended before `/spec:approve tasks`.
