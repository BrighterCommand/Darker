# Ralph Tasks: 013-validation-decorator

> Auto-generated from the approved design for unattended TDD execution.
> Each task is self-contained with all context a fresh Claude session needs.

## Spec Context

- **Spec**: 013-validation-decorator
- **Requirements**: specs/013-validation-decorator/requirements.md
- **ADRs**: docs/adr/0020-validation-decorator-architecture.md

## Tasks

- [x] **Scaffold the dependency-free core `Paramore.Darker.Validation` project**
  - **Behavior**: A new class library `Paramore.Darker.Validation` exists, targets `netstandard2.0;net8.0;net9.0`, references ONLY `Paramore.Darker` (no third-party packages), and is added to both `Darker.slnx` and `Darker.Filter.slnf`. It compiles cleanly (empty of code beyond a placeholder namespace is fine).
  - **Test file**: _(none — scaffolding task, build verification only)_
  - **Test should verify**:
    - `dotnet build` of the new project succeeds on all three target frameworks
    - The project appears in `Darker.Filter.slnf` `projects` array
  - **Implementation files**:
    - `src/Paramore.Darker.Validation/Paramore.Darker.Validation.csproj` - new SDK-style csproj; `<TargetFrameworks>netstandard2.0;net8.0;net9.0</TargetFrameworks>`, a `<Description>`, a single `<ProjectReference Include="..\Paramore.Darker\Paramore.Darker.csproj" />`. Mirror `src/Paramore.Darker.Extensions.DependencyInjection/Paramore.Darker.Extensions.DependencyInjection.csproj`.
    - `Darker.slnx` - add a `<Project Path="src\Paramore.Darker.Validation\Paramore.Darker.Validation.csproj">` entry under the `/src/` folder with the same `<Configuration>` block as the other src projects.
    - `Darker.Filter.slnf` - add `"src\\Paramore.Darker.Validation\\Paramore.Darker.Validation.csproj"` to the `projects` array.
  - **RALPH-VERIFY**: `dotnet build src/Paramore.Darker.Validation/Paramore.Darker.Validation.csproj -c Release`
  - **References**: ADR 0020 (Key Components → Core); `src/Paramore.Darker.Extensions.DependencyInjection/Paramore.Darker.Extensions.DependencyInjection.csproj` (csproj shape); `Darker.slnx`; `Darker.Filter.slnf`

- [x] **Scaffold the `Paramore.Darker.Validation.Tests` project**
  - **Behavior**: A new xUnit test project `Paramore.Darker.Validation.Tests` exists, targets `net8.0;net9.0`, `IsPackable=false`, references `Paramore.Darker.Validation`, and is added to `Darker.slnx` and `Darker.Filter.slnf`. It contains an empty `TestDoubles/` folder convention (no doubles yet). It builds and reports zero tests.
  - **Test file**: _(none — scaffolding task, build verification only)_
  - **Test should verify**:
    - `dotnet build` of the test project succeeds
    - Project is present in `Darker.Filter.slnf`
  - **Implementation files**:
    - `test/Paramore.Darker.Validation.Tests/Paramore.Darker.Validation.Tests.csproj` - mirror `test/Paramore.Darker.Extensions.Tests/Paramore.Darker.Extensions.Tests.csproj` (xunit.v3, Shouldly, Microsoft.NET.Test.Sdk, analyzers). Reference `..\..\src\Paramore.Darker.Validation\Paramore.Darker.Validation.csproj`.
    - `Darker.slnx` - add the test project under `/test/`.
    - `Darker.Filter.slnf` - add the test project path.
  - **RALPH-VERIFY**: `dotnet build test/Paramore.Darker.Validation.Tests/Paramore.Darker.Validation.Tests.csproj -c Release`
  - **References**: `test/Paramore.Darker.Extensions.Tests/Paramore.Darker.Extensions.Tests.csproj`; `Directory.Packages.props` (CPM — reference packages WITHOUT version attributes); `Darker.slnx`; `Darker.Filter.slnf`

- [x] **`QueryValidationError` record**
  - **Behavior**: A sealed record `QueryValidationError(string PropertyName, string ErrorMessage, object? AttemptedValue = null, string? ErrorCode = null)` exists in the core package. It is value-equatable; two errors with the same four values are equal; `AttemptedValue` and `ErrorCode` default to null.
  - **Test file**: `test/Paramore.Darker.Validation.Tests/When_creating_query_validation_error_should_expose_supplied_values.cs`
  - **Test should verify**:
    - A `QueryValidationError("Name", "must not be empty")` exposes `PropertyName`/`ErrorMessage` and leaves `AttemptedValue`/`ErrorCode` null
    - Two records constructed with identical arguments (incl. `AttemptedValue`, `ErrorCode`) are `.ShouldBe` equal (value semantics)
  - **Implementation files**:
    - `src/Paramore.Darker.Validation/QueryValidationError.cs` - `public sealed record QueryValidationError(string PropertyName, string ErrorMessage, object? AttemptedValue = null, string? ErrorCode = null);` in namespace `Paramore.Darker.Validation`. Enable nullable context for this file/project.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.Tests/ --filter "FullyQualifiedName~When_creating_query_validation_error_should_expose_supplied_values"`
  - **References**: ADR 0020 (Key Components → `QueryValidationError`); requirements FR6 + Resolved Decision 4

- [x] **`QueryValidationException`**
  - **Behavior**: A `QueryValidationException : Exception` exists carrying `IReadOnlyCollection<QueryValidationError> Errors`. Constructing it with a collection of errors exposes exactly those errors; it has a message summarising the failure count.
  - **Test file**: `test/Paramore.Darker.Validation.Tests/When_creating_query_validation_exception_should_expose_errors.cs`
  - **Test should verify**:
    - The exception exposes the exact `QueryValidationError` collection passed in (count and contents)
    - It is an `Exception` (assignable), so the pipeline propagates it normally
  - **Implementation files**:
    - `src/Paramore.Darker.Validation/QueryValidationException.cs` - `public sealed class QueryValidationException : Exception` with `public IReadOnlyCollection<QueryValidationError> Errors { get; }` set from a constructor taking `IReadOnlyCollection<QueryValidationError>`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.Tests/ --filter "FullyQualifiedName~When_creating_query_validation_exception_should_expose_errors"`
  - **References**: ADR 0020 (Key Components → `QueryValidationException`); requirements FR6

- [x] **Core package is dependency-free (FR10 executable guard)**
  - **Behavior**: The `Paramore.Darker.Validation` core assembly depends on neither FluentValidation nor DataAnnotations. Enforced by a test so a later accidental reference is caught as a regression. Two complementary checks, because each alone has a blind spot: the IL-reference check misses a *declared-but-unused* package reference (the compiler omits references to assemblies whose types aren't used in IL), while the csproj check misses a *transitive* leak — together they cover FR10.
  - **Test file**: `test/Paramore.Darker.Validation.Tests/When_inspecting_core_assembly_should_have_no_provider_dependencies.cs`
  - **Test should verify**:
    - **IL-reference check**: `typeof(QueryValidationError).Assembly.GetReferencedAssemblies()` contains no assembly whose name is `FluentValidation`, `System.ComponentModel.DataAnnotations`, **or `System.ComponentModel.Annotations`** (on net8.0/net9.0 the `System.ComponentModel.DataAnnotations.*` types live in the `System.ComponentModel.Annotations` assembly — matching only the `DataAnnotations` string would let a DataAnnotations use slip through; keep both names for netstandard2.0 + modern targets)
    - **csproj check** (closes the metadata-trimming blind spot): read the core project file `src/Paramore.Darker.Validation/Paramore.Darker.Validation.csproj` (locate it by walking up from `AppContext.BaseDirectory`, or via a path relative to the test assembly) and assert it declares no `PackageReference`/`ProjectReference` whose Include contains `FluentValidation` or `DataAnnotations` — its only reference is `Paramore.Darker`
  - **Implementation files**:
    - _(no production code — assertion-only guard over the built core assembly + its project file)_
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.Tests/ --filter "FullyQualifiedName~When_inspecting_core_assembly_should_have_no_provider_dependencies"`
  - **References**: requirements FR10 + Acceptance Criteria ("Core validation abstractions carry no dependency on FluentValidation or DataAnnotations"); ADR 0020 (Consequences → Dependency-free core); review round 2 Finding 2 (assembly-name + trimming caveats)

- [x] **Abstract `ValidateQueryDecorator<TQuery,TResult>` sync — valid query calls `next`**
  - **Behavior**: An abstract `ValidateQueryDecorator<TQuery,TResult> : IQueryHandlerDecorator<TQuery,TResult>` implements the template method `Execute`: it calls the abstract member `IReadOnlyCollection<QueryValidationError> Validate(TQuery query)`; when it returns zero errors, `Execute` invokes `next(query)` and returns its result unchanged. Implements `Context { get; set; }` and a no-op `InitializeFromAttributeParams` (attribute has no params).
  - **Test file**: `test/Paramore.Darker.Validation.Tests/When_query_is_valid_should_call_next.cs`
  - **Test should verify**:
    - Using a test-double subclass whose `Validate` returns an empty collection, `Execute` returns the exact result produced by the `next` delegate
    - The `next` delegate is invoked exactly once
  - **Implementation files**:
    - `src/Paramore.Darker.Validation/ValidateQueryDecorator.cs` - abstract class; `Execute(query, next, fallback)` runs template method; declares `protected abstract IReadOnlyCollection<QueryValidationError> Validate(TQuery query);`
    - `test/Paramore.Darker.Validation.Tests/TestDoubles/StubValidateQueryDecorator.cs` - concrete subclass returning a caller-supplied error collection (used across sync abstract-decorator tests)
    - `test/Paramore.Darker.Validation.Tests/TestDoubles/ValidationTestQuery.cs` - a simple `IQuery<TResult>` test double (with a `Result` nested type), mirroring `SyncTestQuery`
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.Tests/ --filter "FullyQualifiedName~When_query_is_valid_should_call_next"`
  - **References**: ADR 0020 (Architecture Overview + template-method seam); `src/Paramore.Darker/IQueryHandlerDecorator.cs`; `src/Paramore.Darker/Policies/Handlers/RetryableQueryDecoratorAsync.cs` (Context/InitializeFromAttributeParams shape); `test/Paramore.Darker.Core.Tests/TestDoubles/SyncTestQuery.cs`

- [x] **Abstract `ValidateQueryDecorator<TQuery,TResult>` sync — invalid query throws and short-circuits**
  - **Behavior**: When the abstract member returns one or more `QueryValidationError`s, `Execute` throws `QueryValidationException` carrying those errors and does NOT invoke `next`.
  - **Test file**: `test/Paramore.Darker.Validation.Tests/When_query_is_invalid_should_throw_and_not_call_next.cs`
  - **Test should verify**:
    - `Execute` throws `QueryValidationException` whose `Errors` equal the errors returned by `Validate`
    - The `next` delegate is never invoked (assert via a flag/counter that stays false/zero)
  - **Implementation files**:
    - `src/Paramore.Darker.Validation/ValidateQueryDecorator.cs` - (behavior already implemented in the template method; this task adds the failing-then-passing test proving short-circuit; add code only if the previous task left the throw path unimplemented)
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.Tests/ --filter "FullyQualifiedName~When_query_is_invalid_should_throw_and_not_call_next"`
  - **References**: ADR 0020 (Architecture Overview: "validation runs before `next`"); requirements FR4, FR5; reuse `TestDoubles/StubValidateQueryDecorator.cs` + `ValidationTestQuery.cs`

- [x] **Abstract `ValidateQueryDecoratorAsync<TQuery,TResult>` — valid query calls `next`**
  - **Behavior**: An abstract `ValidateQueryDecoratorAsync<TQuery,TResult> : IQueryHandlerDecoratorAsync<TQuery,TResult>` implements `ExecuteAsync`: it awaits the abstract member `Task<IReadOnlyCollection<QueryValidationError>> ValidateAsync(TQuery query, CancellationToken)`; on zero errors it awaits `next(query, ct)` and returns the result. Implements `Context` and no-op `InitializeFromAttributeParams`.
  - **Test file**: `test/Paramore.Darker.Validation.Tests/When_async_query_is_valid_should_call_next.cs`
  - **Test should verify**:
    - With a stub whose `ValidateAsync` returns empty, `ExecuteAsync` returns the awaited `next` result
    - `next` invoked exactly once
  - **Implementation files**:
    - `src/Paramore.Darker.Validation/ValidateQueryDecoratorAsync.cs` - abstract class; `protected abstract Task<IReadOnlyCollection<QueryValidationError>> ValidateAsync(TQuery query, CancellationToken cancellationToken);`
    - `test/Paramore.Darker.Validation.Tests/TestDoubles/StubValidateQueryDecoratorAsync.cs` - concrete async subclass returning a caller-supplied error collection
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.Tests/ --filter "FullyQualifiedName~When_async_query_is_valid_should_call_next"`
  - **References**: ADR 0020 (Architecture Overview); `src/Paramore.Darker/IQueryHandlerDecoratorAsync.cs`; `src/Paramore.Darker/Policies/Handlers/RetryableQueryDecoratorAsync.cs` (ExecuteAsync signature incl. `fallback` + `CancellationToken`)

- [x] **Abstract `ValidateQueryDecoratorAsync<TQuery,TResult>` — invalid query throws and short-circuits**
  - **Behavior**: When `ValidateAsync` returns errors, `ExecuteAsync` throws `QueryValidationException` carrying them and does NOT await `next`.
  - **Test file**: `test/Paramore.Darker.Validation.Tests/When_async_query_is_invalid_should_throw_and_not_call_next.cs`
  - **Test should verify**:
    - `ExecuteAsync` throws `QueryValidationException` with the expected `Errors` (use `Should.ThrowAsync`)
    - `next` never invoked
  - **Implementation files**:
    - `src/Paramore.Darker.Validation/ValidateQueryDecoratorAsync.cs` - (template method already throws; add code only if unimplemented)
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.Tests/ --filter "FullyQualifiedName~When_async_query_is_invalid_should_throw_and_not_call_next"`
  - **References**: requirements FR4, FR5, NFR async; reuse `TestDoubles/StubValidateQueryDecoratorAsync.cs`

- [x] **`ValidateQueryAttribute` (sync)**
  - **Behavior**: A sealed `ValidateQueryAttribute : QueryHandlerAttribute` exists. Constructed with a `step`, `GetDecoratorType()` returns the **abstract** open generic `typeof(ValidateQueryDecorator<,>)`, `GetAttributeParams()` returns an empty `object[]`, and `Step` is preserved. `[AttributeUsage(AttributeTargets.Method)]`.
  - **Test file**: `test/Paramore.Darker.Validation.Tests/When_validate_query_attribute_created_should_return_abstract_decorator_type.cs`
  - **Test should verify**:
    - `GetDecoratorType()` returns `typeof(ValidateQueryDecorator<,>)` (the abstract core generic, NOT a provider type)
    - `GetAttributeParams()` returns an empty array
    - `Step` equals the value passed to the constructor
  - **Implementation files**:
    - `src/Paramore.Darker.Validation/ValidateQueryAttribute.cs` - subclass `QueryHandlerAttribute`; mirror `src/Paramore.Darker/Policies/Attributes/RetryableQueryAttribute.cs` but with no policy-name param. (The abstract `ValidateQueryDecorator<,>` already exists from the earlier decorator tasks, so `GetDecoratorType()` compiles directly — no stub needed.)
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.Tests/ --filter "FullyQualifiedName~When_validate_query_attribute_created_should_return_abstract_decorator_type"`
  - **References**: ADR 0020 (Key Components + provider-switchability decision); `src/Paramore.Darker/QueryHandlerAttribute.cs`; `src/Paramore.Darker/Policies/Attributes/RetryableQueryAttribute.cs`; `src/Paramore.Darker.Validation/ValidateQueryDecorator.cs`

- [x] **`ValidateQueryAttributeAsync` (async)**
  - **Behavior**: A sealed `ValidateQueryAttributeAsync : QueryHandlerAttributeAsync` exists. `GetDecoratorType()` returns `typeof(ValidateQueryDecoratorAsync<,>)`, `GetAttributeParams()` returns empty, `Step` preserved.
  - **Test file**: `test/Paramore.Darker.Validation.Tests/When_validate_query_attribute_async_created_should_return_abstract_async_decorator_type.cs`
  - **Test should verify**:
    - `GetDecoratorType()` returns `typeof(ValidateQueryDecoratorAsync<,>)`
    - `GetAttributeParams()` returns an empty array
    - `Step` equals the constructor value
  - **Implementation files**:
    - `src/Paramore.Darker.Validation/ValidateQueryAttributeAsync.cs` - mirror `src/Paramore.Darker/Policies/Attributes/RetryableQueryAttributeAsync.cs`. (The abstract `ValidateQueryDecoratorAsync<,>` already exists from the earlier decorator tasks.)
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.Tests/ --filter "FullyQualifiedName~When_validate_query_attribute_async_created_should_return_abstract_async_decorator_type"`
  - **References**: `src/Paramore.Darker/QueryHandlerAttributeAsync.cs`; `src/Paramore.Darker/Policies/Attributes/RetryableQueryAttributeAsync.cs`; `src/Paramore.Darker.Validation/ValidateQueryDecoratorAsync.cs`

- [x] **Pin FluentValidation 11.11.0 in CPM**
  - **Behavior**: `Directory.Packages.props` gains `<PackageVersion Include="FluentValidation" Version="11.11.0" />` (11.x retains netstandard2.0 support). No project references it yet; the build still restores/compiles.
  - **Test file**: _(none — CPM entry, build verification only)_
  - **Test should verify**:
    - The props file parses and an existing project still builds (proves the entry is well-formed)
  - **Implementation files**:
    - `Directory.Packages.props` - add the `<PackageVersion>` entry to the main `<ItemGroup>` (alphabetical placement near the `Microsoft.*`/`Polly` entries is fine; keep the file's existing style — no version on references, version only here).
  - **RALPH-VERIFY**: `dotnet build src/Paramore.Darker.Validation/Paramore.Darker.Validation.csproj -c Release`
  - **References**: `Directory.Packages.props`; Resolved Decision (FluentValidation 11.11.0, own task)

- [x] **Scaffold `Paramore.Darker.Validation.FluentValidation` project + its test project**
  - **Behavior**: A new library `Paramore.Darker.Validation.FluentValidation` (targets `netstandard2.0;net8.0;net9.0`) references `Paramore.Darker.Validation`, `Paramore.Darker.Extensions.DependencyInjection`, and the CPM-managed `FluentValidation` package. A matching test project `Paramore.Darker.Validation.FluentValidation.Tests` (`net8.0;net9.0`) references the FV provider and the FluentValidation package (so tests can declare validators). Both are added to `Darker.slnx` and `Darker.Filter.slnf`. Both build.
  - **Test file**: _(none — scaffolding task, build verification only)_
  - **Test should verify**:
    - Both projects build on their targets
    - Both appear in `Darker.Filter.slnf`
  - **Implementation files**:
    - `src/Paramore.Darker.Validation.FluentValidation/Paramore.Darker.Validation.FluentValidation.csproj` - ProjectReferences to `..\Paramore.Darker.Validation\...` and `..\Paramore.Darker.Extensions.DependencyInjection\...`; `<PackageReference Include="FluentValidation" />` (no version — CPM).
    - `test/Paramore.Darker.Validation.FluentValidation.Tests/Paramore.Darker.Validation.FluentValidation.Tests.csproj` - xunit/Shouldly like the other test csproj; ProjectReference to the FV provider; `<PackageReference Include="FluentValidation" />`.
    - `Darker.slnx` - add both projects (src + test folders).
    - `Darker.Filter.slnf` - add both project paths.
  - **RALPH-VERIFY**: `dotnet build src/Paramore.Darker.Validation.FluentValidation/Paramore.Darker.Validation.FluentValidation.csproj -c Release`
  - **References**: Resolved Decision (FV package deps; FV isolated to its own test project); `src/Paramore.Darker.Extensions.DependencyInjection/Paramore.Darker.Extensions.DependencyInjection.csproj`; `test/Paramore.Darker.Extensions.Tests/Paramore.Darker.Extensions.Tests.csproj`; `Darker.slnx`; `Darker.Filter.slnf`

- [x] **`FluentValidationQueryValidatorDecorator<,>` sync — valid query passes to `next`**
  - **Behavior**: A concrete `FluentValidationQueryValidatorDecorator<TQuery,TResult> : ValidateQueryDecorator<TQuery,TResult>` injects `IServiceProvider` and, inside `Validate`, resolves the FluentValidation validator from the **runtime query type**: `serviceProvider.GetService(typeof(IValidator<>).MakeGenericType(query.GetType()))` (NOT generic `IValidator<TQuery>` — at pipeline runtime `TQuery` is `IQuery<TResult>`, see the CRITICAL note below). It runs the resolved validator's `Validate(query)`; when valid, returns an empty `IReadOnlyCollection<QueryValidationError>` so the base calls `next`.
  - **Test file**: `test/Paramore.Darker.Validation.FluentValidation.Tests/When_fluent_validator_passes_should_call_next.cs`
  - **Test should verify**:
    - With an `IValidator<FvTestQuery>` registered in a real `ServiceProvider` (an `AbstractValidator<FvTestQuery>` test double whose rules pass) handed to the decorator, and a concrete `FvTestQuery` object passed to `Execute`, the decorator resolves the validator by the object's runtime type, returns the `next` result, and invokes `next` once
  - **Implementation files**:
    - `src/Paramore.Darker.Validation.FluentValidation/FluentValidationQueryValidatorDecorator.cs` - inject `IServiceProvider`; override `Validate`; resolve `IValidator<>` via `query.GetType()`; run `validator.Validate((IValidationContext)new ValidationContext<object>(query))` or cast the resolved `IValidator` and call its non-generic `Validate(IValidationContext)` (FluentValidation's `IValidator` exposes `Validate(IValidationContext)`), since the static type is not `IValidator<FvTestQuery>`.
    - `test/Paramore.Darker.Validation.FluentValidation.Tests/TestDoubles/FvTestQuery.cs` - `IQuery<TResult>` double
    - `test/Paramore.Darker.Validation.FluentValidation.Tests/TestDoubles/FvTestQueryValidator.cs` - `AbstractValidator<FvTestQuery>` with a rule (e.g. `RuleFor(x => x.Name).NotEmpty()`)
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.FluentValidation.Tests/ --filter "FullyQualifiedName~When_fluent_validator_passes_should_call_next"`
  - **References**: ADR 0020 (**Amendment** + Provider → FluentValidation); requirements FR3, FR4; `src/Paramore.Darker/PipelineBuilder.cs:253` (decorators closed over `IQuery<TResult>`); `src/Paramore.Darker/Policies/Handlers/RetryableQueryDecoratorAsync.cs` (DI-resolved decorator pattern). **CRITICAL**: `PipelineBuilder` closes every decorator over `typeof(IQuery<TResult>)`, so `TQuery` is `IQuery<TResult>` at runtime — you CANNOT inject `IValidator<TQuery>` (it would resolve to `IValidator<IQuery<TResult>>`, which nobody registers). Resolve from `query.GetType()` at validate-time. Use FluentValidation's non-generic `IValidator.Validate(IValidationContext)` API since the compile-time type is not the concrete validator.

- [x] **`FluentValidationQueryValidatorDecorator<,>` sync — invalid query maps failures and throws**
  - **Behavior**: When the FluentValidation `Validate` result is invalid, the override maps each `ValidationFailure` → `QueryValidationError(PropertyName, ErrorMessage, AttemptedValue, ErrorCode)`; the base then throws `QueryValidationException` and `next` is not invoked. A query that fails **two distinct rules** produces **two** `QueryValidationError`s (proves the whole collection is surfaced, not just the first failure — FR6).
  - **Test file**: `test/Paramore.Darker.Validation.FluentValidation.Tests/When_fluent_validator_fails_should_map_failures_and_throw.cs`
  - **Test should verify**:
    - `Execute` throws `QueryValidationException`
    - Each mapped `QueryValidationError` carries the FluentValidation `PropertyName`, `ErrorMessage`, `AttemptedValue`, and `ErrorCode` (assert all four for at least one failure)
    - A query violating two rules yields `Errors.Count == 2` with both property names present (multiple-error coverage)
    - `next` not invoked
  - **Implementation files**:
    - `src/Paramore.Darker.Validation.FluentValidation/FluentValidationQueryValidatorDecorator.cs` - implement the `ValidationFailure` → `QueryValidationError` mapping (map the full failure list)
    - `test/Paramore.Darker.Validation.FluentValidation.Tests/TestDoubles/FvTestQueryValidator.cs` - ensure the validator has at least two rules on distinct properties so an invalid query can fail both
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.FluentValidation.Tests/ --filter "FullyQualifiedName~When_fluent_validator_fails_should_map_failures_and_throw"`
  - **References**: ADR 0020 (Provider → FluentValidation, `ValidationFailure` → `QueryValidationError`); requirements FR6 (read-only *collection*), FR7

- [ ] **`FluentValidationQueryValidatorDecorator<,>` sync — missing validator fails fast**
  - **Behavior**: When `serviceProvider.GetService(typeof(IValidator<>).MakeGenericType(query.GetType()))` returns null (no validator registered for the runtime query type), the decorator throws `Paramore.Darker.Exceptions.ConfigurationException` (fail-fast) rather than silently skipping validation.
  - **Test file**: `test/Paramore.Darker.Validation.FluentValidation.Tests/When_no_fluent_validator_registered_should_throw_configuration_exception.cs`
  - **Test should verify**:
    - With an empty `ServiceProvider` (no validator registered) and a concrete `FvTestQuery`, `Execute` throws `ConfigurationException`
    - The message names the runtime query type / indicates a missing validator
  - **Implementation files**:
    - `src/Paramore.Darker.Validation.FluentValidation/FluentValidationQueryValidatorDecorator.cs` - null-service guard on the runtime-type lookup, throwing `ConfigurationException`
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.FluentValidation.Tests/ --filter "FullyQualifiedName~When_no_fluent_validator_registered_should_throw_configuration_exception"`
  - **References**: ADR 0020 (**Amendment** + fail-fast); requirements FR9 + Resolved Decision 1; `src/Paramore.Darker/Exceptions/ConfigurationException.cs`; `src/Paramore.Darker/Policies/Handlers/RetryableQueryDecoratorAsync.cs` (ConfigurationException usage)

- [ ] **`FluentValidationQueryValidatorDecoratorAsync<,>` — valid query passes to `next`**
  - **Behavior**: A concrete `FluentValidationQueryValidatorDecoratorAsync<TQuery,TResult> : ValidateQueryDecoratorAsync<TQuery,TResult>` injects `IServiceProvider`, resolves the validator from the **runtime query type** (`query.GetType()`, as the sync decorator does), awaits the non-generic `IValidator.ValidateAsync(IValidationContext, ct)` (honouring async rules and threading the `CancellationToken`), and on success returns empty so the base awaits `next`.
  - **Test file**: `test/Paramore.Darker.Validation.FluentValidation.Tests/When_async_fluent_validator_passes_should_call_next.cs`
  - **Test should verify**:
    - With a passing validator registered in the `ServiceProvider` and a concrete `FvTestQuery`, `ExecuteAsync` returns the `next` result and `next` runs once
    - The supplied `CancellationToken` is threaded into the FluentValidation `ValidateAsync` call and into `next` (assert the token observed downstream equals the one passed in)
  - **Implementation files**:
    - `src/Paramore.Darker.Validation.FluentValidation/FluentValidationQueryValidatorDecoratorAsync.cs` - override `ValidateAsync`; resolve `IValidator` from `query.GetType()`; call `IValidator.ValidateAsync(context, cancellationToken)`
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.FluentValidation.Tests/ --filter "FullyQualifiedName~When_async_fluent_validator_passes_should_call_next"`
  - **References**: ADR 0020 (**Amendment** + async path); requirements NFR async; `src/Paramore.Darker/IQueryHandlerDecoratorAsync.cs` (CancellationToken on `ExecuteAsync`); reuse FV `TestDoubles`

- [ ] **`FluentValidationQueryValidatorDecoratorAsync<,>` — invalid query maps failures and throws**
  - **Behavior**: On an invalid async result, map each `ValidationFailure` → `QueryValidationError`; base throws `QueryValidationException`; `next` not awaited.
  - **Test file**: `test/Paramore.Darker.Validation.FluentValidation.Tests/When_async_fluent_validator_fails_should_map_failures_and_throw.cs`
  - **Test should verify**:
    - `ExecuteAsync` throws `QueryValidationException` (via `Should.ThrowAsync`)
    - Mapped errors carry all four fields
    - `next` not invoked
  - **Implementation files**:
    - `src/Paramore.Darker.Validation.FluentValidation/FluentValidationQueryValidatorDecoratorAsync.cs` - failure mapping (may share a private mapping helper with the sync decorator)
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.FluentValidation.Tests/ --filter "FullyQualifiedName~When_async_fluent_validator_fails_should_map_failures_and_throw"`
  - **References**: requirements FR6, FR7, NFR async

- [ ] **`FluentValidationQueryValidatorDecoratorAsync<,>` — missing validator fails fast**
  - **Behavior**: When the runtime-type validator lookup (`query.GetType()`) returns null, `ExecuteAsync` throws `ConfigurationException`.
  - **Test file**: `test/Paramore.Darker.Validation.FluentValidation.Tests/When_no_async_fluent_validator_registered_should_throw_configuration_exception.cs`
  - **Test should verify**:
    - With an empty `ServiceProvider` and a concrete `FvTestQuery`, `ExecuteAsync` throws `ConfigurationException` (via `Should.ThrowAsync`)
  - **Implementation files**:
    - `src/Paramore.Darker.Validation.FluentValidation/FluentValidationQueryValidatorDecoratorAsync.cs` - null-service guard on the runtime-type lookup
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.FluentValidation.Tests/ --filter "FullyQualifiedName~When_no_async_fluent_validator_registered_should_throw_configuration_exception"`
  - **References**: ADR 0020 (**Amendment** + fail-fast); requirements FR9; `src/Paramore.Darker/Exceptions/ConfigurationException.cs`

- [ ] **`UseFluentValidation()` DI extension registers the abstract→concrete mapping**
  - **Behavior**: A `UseFluentValidation(this IDarkerHandlerBuilder builder)` extension registers open-generic service descriptors mapping the abstract core decorators to the FluentValidation concretes — `typeof(ValidateQueryDecorator<,>)` → `typeof(FluentValidationQueryValidatorDecorator<,>)` and the async pair — so DI closes the generic per query and the pipeline resolves the concrete decorator. Registered as `Transient` (mirroring Brighter). Returns the builder for chaining; throws `ArgumentNullException` on null builder.
  - **Test file**: `test/Paramore.Darker.Validation.FluentValidation.Tests/When_UseFluentValidation_called_should_resolve_concrete_decorator_for_abstract_type.cs`
  - **Test should verify**:
    - After `AddDarker(...).UseFluentValidation()` (no handler registration needed — this test resolves the decorator type directly from the container, not through the pipeline), building the provider and resolving the closed abstract type **as the pipeline requests it** — `ValidateQueryDecorator<IQuery<FvTestQuery.Result>, FvTestQuery.Result>` (closed over `IQuery<TResult>`, NOT the concrete query — see the note) — yields a `FluentValidationQueryValidatorDecorator<...>` instance
    - The async closed abstract (`ValidateQueryDecoratorAsync<IQuery<FvTestQuery.Result>, FvTestQuery.Result>`) resolves to the async concrete
    - `UseFluentValidation(null)` throws `ArgumentNullException`
  - **Implementation files**:
    - `src/Paramore.Darker.Validation.FluentValidation/FluentValidationDarkerBuilderExtensions.cs` - static class with `UseFluentValidation`; add two open-generic `ServiceDescriptor`s to `builder.Services` (abstract → concrete, sync + async).
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.FluentValidation.Tests/ --filter "FullyQualifiedName~When_UseFluentValidation_called_should_resolve_concrete_decorator_for_abstract_type"`
  - **References**: ADR 0020 (Implementation Approach step 5 + DI seam + **Amendment**); requirements FR7; `src/Paramore.Darker/PipelineBuilder.cs:253` (the closed type the pipeline resolves); `src/Paramore.Darker.Extensions.DependencyInjection/PolicyDIExtensions.cs`; `src/Paramore.Darker/Policies/QueryProcessorBuilderExtensions.cs` (RegisterDecorator pattern); `src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionDecoratorRegistry.cs`; `src/Paramore.Darker.Extensions.DependencyInjection/IDarkerHandlerBuilder.cs`; requirements Resolved Decision 3. NOTE: `ServiceCollectionDecoratorRegistry.Register` maps a type to itself and CANNOT express abstract→concrete; add the open-generic `ServiceDescriptor`s to `builder.Services` directly (as the Brighter snippet does). Resolve the abstract type closed over `IQuery<TResult>` — that is exactly what `PipelineBuilder` asks DI for; closing over the concrete query would test a path production never exercises.

- [ ] **End-to-end: FluentValidation validates through the real `QueryProcessor` pipeline**
  - **Behavior**: A handler whose async execute method carries `[ValidateQueryAsync(step)]`, wired via `AddDarker(...).AddHandlersFromAssemblies(...).UseFluentValidation()` with an `IValidator<FvTestQuery>` registered, validates the query when executed through a **real `QueryProcessor`**: a valid query returns the handler result; an invalid query throws `QueryValidationException` and the handler never runs. This is the test that proves the runtime-type validator resolution + abstract→concrete DI mapping actually work end-to-end (isolation tests close over the wrong generic argument and would false-green — see review Finding 2).
  - **Test file**: `test/Paramore.Darker.Validation.FluentValidation.Tests/When_validated_query_executed_through_processor_should_validate.cs`
  - **Test should verify**:
    - Build a `ServiceProvider` from `AddDarker(o => {...}).AddHandlersFromAssemblies(typeof(FvTestQueryHandlerAsync).Assembly).UseFluentValidation()` plus `services.AddScoped<IValidator<FvTestQuery>, FvTestQueryValidator>()`; resolve `IQueryProcessor`
    - A valid `FvTestQuery` returns the handler's result (handler executed)
    - An invalid `FvTestQuery` throws `QueryValidationException` (via `Should.ThrowAsync`) and the handler's side-effect flag proves it did NOT run
  - **Implementation files**:
    - `test/Paramore.Darker.Validation.FluentValidation.Tests/TestDoubles/FvTestQueryHandlerAsync.cs` - `QueryHandlerAsync<FvTestQuery, FvTestQuery.Result>` whose `ExecuteAsync` carries `[ValidateQueryAsync(1)]` and sets a "handler ran" flag
    - _(no production code — this exercises the assembled pipeline; if it fails, the defect is in the FV decorator/DI wiring, fix there)_
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.FluentValidation.Tests/ --filter "FullyQualifiedName~When_validated_query_executed_through_processor_should_validate"`
  - **References**: ADR 0020 (Implementation Approach step 7); review Finding 1 + 2; `src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionExtensions.cs` (`AddDarker`) + `IDarkerHandlerBuilder.cs` (`AddHandlersFromAssemblies(params Assembly[])` — registers BOTH sync and async handlers; `AddHandlers` takes `Action<IQueryHandlerRegistry>` and is sync-only, so it will NOT work for an async handler); `src/Paramore.Darker/QueryProcessor.cs`; existing async end-to-end style in `test/Paramore.Darker.Extensions.Tests/QueryProcessorIntegrationTests.cs` (uses `AddHandlersFromAssemblies`)

- [ ] **Scaffold `Paramore.Darker.Validation.DataAnnotations` project + its test project**
  - **Behavior**: A new library `Paramore.Darker.Validation.DataAnnotations` (targets `netstandard2.0;net8.0;net9.0`) references `Paramore.Darker.Validation` and `Paramore.Darker.Extensions.DependencyInjection` and uses in-box `System.ComponentModel.DataAnnotations` (add a framework/BCL reference if netstandard2.0 requires it — no external package). A matching test project `Paramore.Darker.Validation.DataAnnotations.Tests` (`net8.0;net9.0`) references the DA provider. Both added to `Darker.slnx` and `Darker.Filter.slnf`. Both build. NO FluentValidation reference anywhere here.
  - **Test file**: _(none — scaffolding task, build verification only)_
  - **Test should verify**:
    - Both projects build; both appear in `Darker.Filter.slnf`
    - No FluentValidation package is referenced (keeps FV dependency isolated)
  - **Implementation files**:
    - `src/Paramore.Darker.Validation.DataAnnotations/Paramore.Darker.Validation.DataAnnotations.csproj`
    - `test/Paramore.Darker.Validation.DataAnnotations.Tests/Paramore.Darker.Validation.DataAnnotations.Tests.csproj`
    - `Darker.slnx` - add both projects
    - `Darker.Filter.slnf` - add both project paths
  - **RALPH-VERIFY**: `dotnet build src/Paramore.Darker.Validation.DataAnnotations/Paramore.Darker.Validation.DataAnnotations.csproj -c Release`
  - **References**: Resolved Decision (DA deps, in-box); ADR 0020 (Provider → DataAnnotations); `Darker.slnx`; `Darker.Filter.slnf`

- [ ] **`DataAnnotationsQueryValidatorDecorator<,>` sync — valid query passes to `next`**
  - **Behavior**: A concrete `DataAnnotationsQueryValidatorDecorator<TQuery,TResult> : ValidateQueryDecorator<TQuery,TResult>` runs `Validator.TryValidateObject(query, new ValidationContext(query), results, validateAllProperties: true)`; when valid, returns an empty error collection so the base calls `next`. NO fail-fast (constraints live on the query type; nothing per-query to register).
  - **Test file**: `test/Paramore.Darker.Validation.DataAnnotations.Tests/When_data_annotations_pass_should_call_next.cs`
  - **Test should verify**:
    - A query whose `[Required]`/`[Range]` etc. constraints are satisfied passes through; `next` invoked once and its result returned
  - **Implementation files**:
    - `src/Paramore.Darker.Validation.DataAnnotations/DataAnnotationsQueryValidatorDecorator.cs` - override `Validate` using `Validator.TryValidateObject(..., validateAllProperties: true)`
    - `test/Paramore.Darker.Validation.DataAnnotations.Tests/TestDoubles/DaTestQuery.cs` - `IQuery<TResult>` double with DataAnnotations attributes (e.g. `[Required] string Name`)
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.DataAnnotations.Tests/ --filter "FullyQualifiedName~When_data_annotations_pass_should_call_next"`
  - **References**: ADR 0020 (Provider → DataAnnotations); requirements FR4, FR8

- [ ] **`DataAnnotationsQueryValidatorDecorator<,>` sync — invalid query maps results and throws**
  - **Behavior**: On failed `TryValidateObject`, map each `ValidationResult` → `QueryValidationError(memberName, ErrorMessage, AttemptedValue: null, ErrorCode: null)` (DataAnnotations has no attempted-value/error-code concept); base throws `QueryValidationException`; `next` not invoked. A query violating **two** constraints (on distinct members) yields **two** `QueryValidationError`s (FR6 collection coverage).
  - **Test file**: `test/Paramore.Darker.Validation.DataAnnotations.Tests/When_data_annotations_fail_should_map_results_and_throw.cs`
  - **Test should verify**:
    - `Execute` throws `QueryValidationException`
    - Each mapped `QueryValidationError` has the member name as `PropertyName`, the DataAnnotations message as `ErrorMessage`, and `AttemptedValue`/`ErrorCode` both null
    - A query violating two constraints yields `Errors.Count == 2` with both member names present (multiple-error coverage)
    - `next` not invoked
  - **Implementation files**:
    - `src/Paramore.Darker.Validation.DataAnnotations/DataAnnotationsQueryValidatorDecorator.cs` - `ValidationResult` → `QueryValidationError` mapping (use `MemberNames.FirstOrDefault()`)
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.DataAnnotations.Tests/ --filter "FullyQualifiedName~When_data_annotations_fail_should_map_results_and_throw"`
  - **References**: ADR 0020 (Provider → DataAnnotations, `AttemptedValue`/`ErrorCode` left null); requirements FR6, FR8

- [ ] **`DataAnnotationsQueryValidatorDecoratorAsync<,>` — valid query passes to `next`**
  - **Behavior**: A concrete async subclass overrides `ValidateAsync`, runs the same `Validator.TryValidateObject` synchronously and wraps the result in a completed task; on success the base awaits `next`. (DataAnnotations has no async validation API.)
  - **Test file**: `test/Paramore.Darker.Validation.DataAnnotations.Tests/When_async_data_annotations_pass_should_call_next.cs`
  - **Test should verify**:
    - A satisfying query passes through; `ExecuteAsync` returns the `next` result; `next` invoked once
  - **Implementation files**:
    - `src/Paramore.Darker.Validation.DataAnnotations/DataAnnotationsQueryValidatorDecoratorAsync.cs` - override `ValidateAsync`; share a private mapping/validation helper with the sync decorator
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.DataAnnotations.Tests/ --filter "FullyQualifiedName~When_async_data_annotations_pass_should_call_next"`
  - **References**: ADR 0020 (async path); requirements NFR async; reuse DA `TestDoubles`

- [ ] **`DataAnnotationsQueryValidatorDecoratorAsync<,>` — invalid query maps results and throws**
  - **Behavior**: On failure, map `ValidationResult` → `QueryValidationError` (as sync) and the base throws `QueryValidationException`; `next` not awaited.
  - **Test file**: `test/Paramore.Darker.Validation.DataAnnotations.Tests/When_async_data_annotations_fail_should_map_results_and_throw.cs`
  - **Test should verify**:
    - `ExecuteAsync` throws `QueryValidationException` (via `Should.ThrowAsync`)
    - Mapped errors have null `AttemptedValue`/`ErrorCode`
    - `next` not invoked
  - **Implementation files**:
    - `src/Paramore.Darker.Validation.DataAnnotations/DataAnnotationsQueryValidatorDecoratorAsync.cs` - mapping via shared helper
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.DataAnnotations.Tests/ --filter "FullyQualifiedName~When_async_data_annotations_fail_should_map_results_and_throw"`
  - **References**: requirements FR6, FR8, NFR async

- [ ] **`UseDataAnnotations()` DI extension registers the abstract→concrete mapping**
  - **Behavior**: A `UseDataAnnotations(this IDarkerHandlerBuilder builder)` extension registers `typeof(ValidateQueryDecorator<,>)` → `typeof(DataAnnotationsQueryValidatorDecorator<,>)` and the async pair as open-generic `Transient` descriptors so DI resolves the DataAnnotations concrete for the abstract type. Returns the builder; throws `ArgumentNullException` on null.
  - **Test file**: `test/Paramore.Darker.Validation.DataAnnotations.Tests/When_UseDataAnnotations_called_should_resolve_concrete_decorator_for_abstract_type.cs`
  - **Test should verify**:
    - After `AddDarker(...).UseDataAnnotations()` (no handler registration needed — resolves the decorator type directly from the container), resolving the closed abstract sync type **as the pipeline requests it** — closed over `IQuery<DaTestQuery.Result>`, not the concrete query — yields a `DataAnnotationsQueryValidatorDecorator<...>` and the async closed type yields the async concrete
    - `UseDataAnnotations(null)` throws `ArgumentNullException`
  - **Implementation files**:
    - `src/Paramore.Darker.Validation.DataAnnotations/DataAnnotationsDarkerBuilderExtensions.cs` - static class with `UseDataAnnotations`; add two open-generic `ServiceDescriptor`s (abstract → concrete, sync + async).
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.DataAnnotations.Tests/ --filter "FullyQualifiedName~When_UseDataAnnotations_called_should_resolve_concrete_decorator_for_abstract_type"`
  - **References**: ADR 0020 (Implementation Approach step 5 + **Amendment**); requirements FR8 + Resolved Decision 3; `src/Paramore.Darker/PipelineBuilder.cs:253` (closed type the pipeline resolves); `src/Paramore.Darker.Extensions.DependencyInjection/PolicyDIExtensions.cs`; `src/Paramore.Darker.Extensions.DependencyInjection/IDarkerHandlerBuilder.cs`

- [ ] **End-to-end: DataAnnotations validates through the real `QueryProcessor` pipeline**
  - **Behavior**: A handler whose async execute method carries `[ValidateQueryAsync(step)]`, wired via `AddDarker(...).AddHandlersFromAssemblies(...).UseDataAnnotations()`, validates the query through a **real `QueryProcessor`**: a query satisfying its DataAnnotations constraints returns the handler result; a violating query throws `QueryValidationException` and the handler never runs.
  - **Test file**: `test/Paramore.Darker.Validation.DataAnnotations.Tests/When_validated_query_executed_through_processor_should_validate.cs`
  - **Test should verify**:
    - Resolve `IQueryProcessor` from `AddDarker(...).AddHandlersFromAssemblies(typeof(DaTestQueryHandlerAsync).Assembly).UseDataAnnotations()`
    - A satisfying `DaTestQuery` returns the handler result (handler executed)
    - A violating `DaTestQuery` throws `QueryValidationException` (via `Should.ThrowAsync`) and the handler's "ran" flag stays false
  - **Implementation files**:
    - `test/Paramore.Darker.Validation.DataAnnotations.Tests/TestDoubles/DaTestQueryHandlerAsync.cs` - `QueryHandlerAsync<DaTestQuery, DaTestQuery.Result>` with `[ValidateQueryAsync(1)]` and a "ran" flag
    - _(no production code — exercises the assembled pipeline)_
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.DataAnnotations.Tests/ --filter "FullyQualifiedName~When_validated_query_executed_through_processor_should_validate"`
  - **References**: ADR 0020 (Implementation Approach step 7); review Finding 2; `src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionExtensions.cs` (`AddDarker`) + `IDarkerHandlerBuilder.cs` (use `AddHandlersFromAssemblies(params Assembly[])` — registers sync+async; `AddHandlers(Action<IQueryHandlerRegistry>)` is sync-only and will NOT register the async handler); `src/Paramore.Darker/QueryProcessor.cs`; `test/Paramore.Darker.Extensions.Tests/QueryProcessorIntegrationTests.cs`

- [ ] **Cross-provider identical `QueryValidationError` shape**
  - **Behavior**: For an equivalent invalid query, both the FluentValidation and DataAnnotations decorators produce `QueryValidationException.Errors` of the same shape — same `QueryValidationError` type, `PropertyName` and `ErrorMessage` populated for both (values may differ; `AttemptedValue`/`ErrorCode` may be null for DataAnnotations). This proves the provider-agnostic contract (identical shape, not identical values).
  - **Test file**: `test/Paramore.Darker.Validation.FluentValidation.Tests/When_both_providers_fail_should_produce_identical_error_shape.cs`
  - **Test should verify**:
    - The FV decorator and the DA decorator each throw `QueryValidationException` whose `Errors` are `IReadOnlyCollection<QueryValidationError>`
    - For a failure on the same property, both produce a `QueryValidationError` with non-null `PropertyName` and `ErrorMessage`
    - The DataAnnotations error leaves `AttemptedValue`/`ErrorCode` null (documented shape difference)
  - **Implementation files**:
    - `test/Paramore.Darker.Validation.FluentValidation.Tests/Paramore.Darker.Validation.FluentValidation.Tests.csproj` - **project-file change**: add a `ProjectReference` to `src/Paramore.Darker.Validation.DataAnnotations` so this one test can run both providers. Safe for isolation: the DataAnnotations provider is BCL-only, and the FluentValidation *package* still never enters the DataAnnotations test project. (This is a deliberate, acknowledged cross-link for the shared-contract test — not "no implementation".)
    - `test/Paramore.Darker.Validation.FluentValidation.Tests/When_both_providers_fail_should_produce_identical_error_shape.cs` - the assertion-only test itself
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Validation.FluentValidation.Tests/ --filter "FullyQualifiedName~When_both_providers_fail_should_produce_identical_error_shape"`
  - **References**: ADR 0020 (Risks → "identical shape, not identical values"); requirements FR6 + Acceptance Criteria; Resolved Decision (FV isolation constraint)
