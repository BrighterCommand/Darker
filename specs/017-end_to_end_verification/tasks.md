# Tasks

This is the task list for Spec 017, End-to-End Verification. It implements [requirements.md](requirements.md) as designed in [ADR 0027](../../docs/adr/0027-end-to-end-verification-outside-developer-tests.md) (Accepted).

**How the phases depend on each other.** The phases follow ADR 0027's Implementation Approach in order.
- **Phase 1** makes the one structural change to the sample and adds the package version. It then creates both projects, adds them to the solution files, and sets up the two guards against the static logger factory.
- **Phase 2** characterises the sample's four endpoints and its startup.
- **Phase 3** adds the harness scenarios, one commit each.
- **Phase 4** adds the CI step and the documentation, and ends with the final checkpoint.

No phase starts until the phase before it is green on net8.0 and net9.0.

### Conventions for every task in this file

- **Public surface only (NFR1).** Neither project uses `InternalsVisibleTo`, `Simple*` factories, `InMemory*` registries or `Paramore.Darker.Testing`. Tests reach Darker only through `AddDarker`, the builder's `Add*Handlers` calls, `AddJsonQueryLogging`, `AddResiliencePipelines`, `ValidatePipelines()`, `ValidateAll`, `IAmAPipelineValidator`, `IQueryProcessor`, `PipelineValidationException` and a real host.
- **The system under test (FR5).** The sample tests use the sample's own `Program` through `WebApplicationFactory<Program>`. They override no services and repeat no registration.
- **CHARACTERISE tasks.** The behaviour already exists, so these tests are green when they arrive. Observe RED by applying the named mutation to **production or sample code, never to the test**. The mutation may sit under `src/` or `samples/`. The test must fail on the assertion it exists for. Then:
  - revert the mutation and confirm green;
  - check that `git status` shows no modified production or sample file;
  - run the full suite;
  - commit the test alone as `test:`, naming the mutation in the message.

  A mutation is never committed, so AC8 still holds. If a named mutation fails in another way (a compile error, or an unrelated exception before the check), stop and ask. Do not improvise another mutation.
- **Harness scenarios are not xunit tests.** Each one is a class in `test/Paramore.Darker.Tests.EndToEnd/Scenarios/` with `public static async Task<int> RunAsync()`. It returns 0, or prints what differed to `Console.Error` and returns non-zero. This follows `test/Paramore.Darker.Tests.AOT/Scenarios/`. Scenario files have descriptive PascalCase names, as in the AOT harness, not `When_` names. They are still written test-first through `/tdd:test-first`.
- **Harness handlers** go in `test/Paramore.Darker.Tests.EndToEnd/Handlers/`, one type per file.
  - They are **`public`**, because `ValidatePipelines()` reports a non-public handler (`HandlerPublic`).
  - Each scenario registers its handlers explicitly with `AddHandlers`, `AddAsyncHandlers` or `AddStreamHandlers`. **Never use `AddHandlersFromAssemblies`**, or the deliberately invalid handler from T3.4 would be scanned into every scenario's host.
- **Hosts and logging.**
  - `AddDarker` stores the container's `ILoggerFactory` in the static `ApplicationLogging.LoggerFactory` (`src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionExtensions.cs:76-77`). Static loggers in `QueryProcessor`, `PipelineBuilder`, `QueryHandlerAsync` and the decorators capture it once.
  - Never dispose a provider whose `ILoggerFactory` is real while later code in the same process can still use Darker's static loggers. T1.4 puts this in place for the sample tests. Each harness scenario puts it in place for itself, as the note at the head of Phase 3 describes.
  - Pass `TestContext.Current.CancellationToken` to every HTTP call and every content read in the sample tests.
- **Naming.**
  - Sample test files and methods are named `When_[condition]_should_[behaviour]`, and classes `[Behavior]Tests`.
  - Use one test per file.
  - Write `// Arrange`, `// Act` and `// Assert` comments, and use Evident Data.
- **Source files.** Use `<Nullable>enable</Nullable>` in both new projects. Follow `.agent_instructions/code_style.md`.
- **Commits.**
  - STRUCTURAL tasks commit as `refactor:`, with the suite green before and after.
  - SETUP tasks commit as `build:` (or `ci:` for the workflow), and the build must succeed.
  - Each behavioural task produces one `test:` commit, then a separate `docs:` commit that ticks the box.
  - DOC tasks commit as `docs:`.
  - Never mix a structural change and a behavioural change in one commit.

---

## Phase 1 — Scaffolding (setup)

- [x] **STRUCTURAL: T1.1 — Let tests name the sample's `Program` type (ADR 0027 Implementation Approach step 1; NFR6; AC8)**
  - Add one line, `public partial class Program;`, after `app.Run();` at the end of `samples/SampleMinimalApi/Program.cs`. On net8.0 and net9.0 the compiler generates `Program` as internal, so without this line `WebApplicationFactory<Program>` would need `InternalsVisibleTo`.
  - Change nothing else in the sample. AC8 allows exactly one changed line.
  - `dotnet build Darker.Filter.slnf -c Release` succeeds. `dotnet test Darker.Filter.slnf -c Release` is green before and after. Commit as `refactor:`.

- [x] **SETUP: T1.2 — Add a central package version for `Microsoft.AspNetCore.Mvc.Testing` (ADR 0027 "Where each type is touched", `Directory.Packages.props` row; Constraints, central package version)**
  - Add `PackageVersion` entries for `Microsoft.AspNetCore.Mvc.Testing` to `Directory.Packages.props`.
  - Version choice:
    - The package's major version must match the ASP.NET Core shared framework of each target framework: 8.0.x for net8.0 and 9.0.x for net9.0.
    - Use two entries with `Condition="'$(TargetFramework)' == 'net8.0'"` and `Condition="'$(TargetFramework)' == 'net9.0'"`, each pinned to the latest patch of its line. `Directory.Packages.props` has no TFM-conditional entries today, so this is the first.
    - First check on nuget.org whether one version supports both frameworks. If one does, use a single unconditional entry instead, and record the choice in the commit message.
  - Use no floating versions. Central Package Management does not have floating versions enabled.
  - Build success is checked in T1.3, the first consumer. This commit changes only `Directory.Packages.props`.

- [x] **SETUP: T1.3 — Create `test/Paramore.Darker.SampleMinimalApi.Tests` and add it to both solution files (ADR 0027 step 2; FR12; FR14; NFR1; NFR5 separate assembly)**
  - New `test/Paramore.Darker.SampleMinimalApi.Tests/Paramore.Darker.SampleMinimalApi.Tests.csproj`. Model it on `test/Paramore.Darker.Extensions.AspNetCore.Tests/Paramore.Darker.Extensions.AspNetCore.Tests.csproj`:
    - `Microsoft.NET.Sdk`, `<TargetFrameworks>net8.0;net9.0</TargetFrameworks>`, `<IsPackable>false</IsPackable>`, `<Nullable>enable</Nullable>`;
    - `<FrameworkReference Include="Microsoft.AspNetCore.App" />`;
    - `PackageReference`s to `Microsoft.NET.Test.Sdk`, `xunit.v3`, `xunit.analyzers`, `xunit.runner.visualstudio` (with the same `PrivateAssets`/`IncludeAssets`), `Shouldly` and `Microsoft.AspNetCore.Mvc.Testing`;
    - one `ProjectReference` to `..\..\samples\SampleMinimalApi\SampleMinimalApi.csproj`.
  - Do not reference any `src/` project directly; the sample brings them. Add no `InternalsVisibleTo` anywhere.
  - Add the project to `Darker.slnx` under `/test/`, with the same four `<Configuration …>` mappings as its siblings. Add `test\\Paramore.Darker.SampleMinimalApi.Tests\\Paramore.Darker.SampleMinimalApi.Tests.csproj` to `Darker.Filter.slnf`.
  - `dotnet build Darker.Filter.slnf -c Release` succeeds for net8.0 and net9.0. `dotnet test Darker.Filter.slnf -c Release` stays green; the new project has no tests yet.

- [x] **SETUP: T1.4 — Risk mitigation: one shared sample host for the whole assembly, which protects the static logger factory (NFR5; ADR 0027 Risks, "Disposing the factory disposes a real `ILoggerFactory`")**
  - Add `test/Paramore.Darker.SampleMinimalApi.Tests/SampleMinimalApiCollection.cs`:
    - `[CollectionDefinition("SampleMinimalApi", DisableParallelization = true)] public sealed class SampleMinimalApiCollection : ICollectionFixture<WebApplicationFactory<Program>>`;
    - an XML summary that explains the static `ApplicationLogging.LoggerFactory` hazard, following `test/Paramore.Darker.Extensions.AspNetCore.Tests/DarkerHostBootstrapCollection.cs`.
  - Every sample test class in Phase 2 carries `[Collection("SampleMinimalApi")]` and takes `WebApplicationFactory<Program> factory` through its primary constructor. The result is one host for all tests in the assembly, disposed once at the end, with no other test assembly in the process.
  - Do not subclass `WebApplicationFactory` and do not override `ConfigureWebHost` (FR5). `WebApplicationFactory` starts the host lazily, on the first access to `Services` or `CreateClient()`. So a failed start, which is a validation error, surfaces inside the first test that touches the factory, with its message (FR1).
  - Why a collection fixture and not a class fixture: one test per file means several test classes. A class fixture would build one host per class, which breaks NFR5's "one shared host".
  - Risk check: if the content root does not resolve (the repo uses `.slnx`, and older `WebApplicationFactory` fallbacks search for `*.sln`), stop and ask. Do not add a content-root override without approval.
  - The build succeeds and the suite stays green.

- [x] **SETUP: T1.5 — Create the `test/Paramore.Darker.Tests.EndToEnd` console harness with an empty runner, and add it to both solution files (ADR 0027 step 3; FR6; FR11 runner; FR14; AC9)**
  - New `test/Paramore.Darker.Tests.EndToEnd/Paramore.Darker.Tests.EndToEnd.csproj`. Model it on `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj`:
    - `Microsoft.NET.Sdk`, `<OutputType>Exe</OutputType>`, `<TargetFrameworks>net8.0;net9.0</TargetFrameworks>`, `<IsPackable>false</IsPackable>`, `<Nullable>enable</Nullable>`;
    - **no** `PublishAot` or `TrimMode`, because AOT is out of scope;
    - `ProjectReference`s to `..\..\src\Paramore.Darker.Extensions.DependencyInjection\…csproj` and `..\..\src\Paramore.Darker\…csproj`;
    - `PackageReference Include="Microsoft.Extensions.Hosting"`, which already has a central version.
  - **No** reference to `Paramore.Darker.Testing` (NFR1). **No** `FrameworkReference` to `Microsoft.AspNetCore.App`, no `Microsoft.NET.Sdk.Web`, and no `Microsoft.AspNetCore.*` package (FR6, AC9). No xunit package.
  - Add `Program.cs`:
    - a header comment in the style of `test/Paramore.Darker.Tests.AOT/Program.cs` that states the contract: run scenarios in order, stop at the first non-zero result and exit with it, and exit 0 when all pass;
    - with no scenarios yet, it ends `return 0;`.
  - Create empty `Scenarios/` and `Handlers/` folders when their first files arrive in T3.1.
  - Add the project to `Darker.slnx` under `/test/` (with the four configuration mappings) and to `Darker.Filter.slnf`.
  - `dotnet build Darker.Filter.slnf -c Release` succeeds. On each TFM, `dotnet run --project test/Paramore.Darker.Tests.EndToEnd -c Release --no-build -f net8.0` exits 0, and the same with `-f net9.0`. `dotnet test Darker.Filter.slnf` stays green, because the console project is not a test project.

## Phase 2 — Sample endpoints (characterise)

- [x] **CHARACTERISE: T2.1 — The sample starts with pipeline validation enabled, and validation finds no errors (FR1; FR5; AC1)**
  - **USE COMMAND**: `/tdd:test-first when the sample starts should have registered pipeline validation and report no validation errors`
  - Test location: "test/Paramore.Darker.SampleMinimalApi.Tests/"
  - Test file: `When_the_sample_starts_should_validate_its_pipelines_and_find_no_errors.cs` (class `SampleStartupTests`, `[Collection("SampleMinimalApi")]`)
  - Test should verify:
    - Act: read `factory.Services`, which starts the sample's own host, so `DarkerValidationHostedService` runs. Resolve `GetServices<IAmAPipelineValidator>()` and call `factory.Services.ValidateAll()`.
    - Assert: the validators are not empty, which shows `ValidatePipelines()` is in the sample's chain. `result.IsValid.ShouldBeTrue(…)`, with the joined `result.Errors` as the custom message so that a failure shows the findings.
    - The test registers nothing and overrides nothing (FR5).
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation (two runs, revert between them):
    1. In `samples/SampleMinimalApi/Program.cs`, remove `.ValidatePipelines()` from the chain, so the line reads `.AddResiliencePipelines(DarkerSettings.ConfigureResiliencePipelines());`. The test must fail on the assertion that the validators are not empty.
    2. (AC1, FR1) In `samples/SampleMinimalApi/Program.cs`, delete the line `.AddJsonQueryLogging()`. That leaves the `QueryLoggingAttributeAsync` decorator unregistered. Host start in the Act must throw `PipelineValidationException`. The test failure must show a message containing `[DecoratorResolvable]` and naming a sample handler, for example `GetPeopleQueryHandler`. Here a start failure that carries the validation message is the failure FR1 requires.
  - Implementation should:
    - Need no production or sample change. T1.1, T1.3 and T1.4 deliver this.

- [x] **CHARACTERISE: T2.2 — `GET /people` returns every person in the repository (FR2; AC2)**
  - **USE COMMAND**: `/tdd:test-first when getting people should return success and every person in the sample repository`
  - Test location: "test/Paramore.Darker.SampleMinimalApi.Tests/"
  - Test file: `When_getting_people_should_return_every_person_in_the_repository.cs` (class `PeopleEndpointTests`, `[Collection("SampleMinimalApi")]`)
  - Test should verify:
    - `GET /people` returns `HttpStatusCode.OK`.
    - The JSON body, read as `Dictionary<int, string>`, has 8 entries, including the Evident Data `1 → "Alan Turing"` and `8 → "Claude Shannon"` (see `samples/SampleMinimalApi/Domain/PersonRepository.cs`).
    - Pass `TestContext.Current.CancellationToken` to the request and the read.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `samples/SampleMinimalApi/QueryHandlers/GetPeopleQueryHandler.cs`, replace `return await repository.GetAll(cancellationToken);` with `return new Dictionary<int, string>();`. The test must fail on the count or content assertion, with status still OK.
  - Implementation should:
    - Need no production or sample change.

- [x] **CHARACTERISE: T2.3 — `GET /people/{id}` returns the name for a known id (FR2; AC2)**
  - **USE COMMAND**: `/tdd:test-first when getting a known person by id should return success and that person's name`
  - Test location: "test/Paramore.Darker.SampleMinimalApi.Tests/"
  - Test file: `When_getting_a_known_person_should_return_their_name.cs` (class `PersonNameEndpointTests`, `[Collection("SampleMinimalApi")]`)
  - Test should verify:
    - `GET /people/1` returns `HttpStatusCode.OK`.
    - The body, read as a string, equals `Alan Turing` (Evident Data: id 1).
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `samples/SampleMinimalApi/QueryHandlers/GetPersonQueryHandler.cs`, replace `return await repository.GetNameById(query.PersonId, cancellationToken);` with `return "Grace Hopper";`. The test must fail on the body equality assertion.
  - Implementation should:
    - Need no production or sample change.

- [x] **CHARACTERISE: T2.4 — `GET /flaky` returns the success text after the retry pipeline recovers the first failure (FR3; AC2)**
  - **USE COMMAND**: `/tdd:test-first when getting the flaky result should return success and the text saying it succeeded on attempt 2 after a transient failure`
  - Test location: "test/Paramore.Darker.SampleMinimalApi.Tests/"
  - Test file: `When_getting_the_flaky_result_should_return_the_success_text_after_a_retry.cs` (class `FlakyEndpointTests`, `[Collection("SampleMinimalApi")]`)
  - Test should verify:
    - `GET /flaky` returns `HttpStatusCode.OK`.
    - The body equals `Succeeded on attempt 2 after a transient failure`. Attempt 2 is the evidence that the first attempt failed and was retried.
    - Each request gets its own Scoped handler, so its own `_attempts`. The result therefore does not depend on test order in the shared host.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation (two runs, revert between them):
    1. In `samples/SampleMinimalApi/QueryHandlers/GetFlakyResultQueryHandler.cs`, delete `[UseResiliencePipelineAttributeAsync(2, DarkerSettings.RetryPipelineName)]`. The first attempt's `SomethingWentTerriblyWrongException` escapes. The test must fail on the status-code assertion: the factory's Development environment returns 500 through the developer exception page. If the client call throws instead, stop and ask.
    2. (AC2) Replace `$"Succeeded on attempt {_attempts} after a transient failure"` with `$"Succeeded on attempt {_attempts}"`. The test must fail on the body equality assertion.
  - Implementation should:
    - Need no production or sample change.

- [x] **CHARACTERISE: T2.5 — `GET /request-scope` returns the same id for the endpoint and the handler (FR4; AC3)**
  - **USE COMMAND**: `/tdd:test-first when getting the request scope should return the same correlation id for the endpoint and the handler`
  - Test location: "test/Paramore.Darker.SampleMinimalApi.Tests/"
  - Test file: `When_getting_the_request_scope_should_return_the_same_id_for_endpoint_and_handler.cs` (class `RequestScopeEndpointTests`, `[Collection("SampleMinimalApi")]`)
  - Test should verify:
    - `GET /request-scope` returns `HttpStatusCode.OK`.
    - The body is the camel-cased JSON of the sample's anonymous `{ Endpoint, Handler }`. Read it as a response record: add `test/Paramore.Darker.SampleMinimalApi.Tests/TestDoubles/RequestScopeResponse.cs`, `public sealed record RequestScopeResponse(Guid Endpoint, Guid Handler)`, in namespace `Paramore.Darker.SampleMinimalApi.Tests.TestDoubles`.
    - `Endpoint` is not `Guid.Empty`, and `Handler` equals `Endpoint`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `samples/SampleMinimalApi/Program.cs`, delete `builder.Services.AddDarkerRequestScope();`. The handler then resolves `RequestCorrelation` in its own scope. The test must fail on `Handler.ShouldBe(Endpoint)`. If the removal makes host start fail validation instead, stop and ask.
  - Implementation should:
    - Need no production or sample change.

## Phase 3 — Console harness (scenarios)

> Every scenario builds its own host and calls the public `AddDarker` chain, ending with `.ValidatePipelines()` (FR6). The harness runs four hosts one after another in one process, and Darker's static loggers capture the first host's `ILoggerFactory`. So each scenario creates its host with `Host.CreateApplicationBuilder()` and registers `builder.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)` after the builder's defaults. The container does not dispose a supplied instance, so later scenarios never log through a disposed factory. The three lines are repeated in each scenario, not shared, so a scenario reads on its own. A scenario reports through `Console.Error`, not through logging. It then calls `await host.StartAsync()`, runs its queries through `host.Services.GetRequiredService<IQueryProcessor>()`, calls `await host.StopAsync()` and disposes the host. Each scenario catches exceptions around its own checks and turns them into a printed difference and a non-zero return. One scenario per commit (ADR 0027 step 3).

- [x] **CHARACTERISE: T3.1 — A valid configuration starts the Generic Host and runs a sync, an async and a stream query (FR6; FR7; FR11; AC5; AC9)**
  - **USE COMMAND**: `/tdd:test-first harness scenario: when a Generic Host with no web server is configured through AddDarker ending with ValidatePipelines should start and return the expected results for a sync, an async and a stream query`
  - Test location: "test/Paramore.Darker.Tests.EndToEnd/Scenarios/"
  - Test file: `ValidConfigurationRunsSyncAsyncAndStreamQueries.cs`
  - Test should verify:
    - The host starts. Validation passes, so `StartAsync` does not throw.
    - `Execute` of the sync query returns its fixed value, for example `"sync"`.
    - `ExecuteAsync` of the async query returns its fixed value, for example `"async"`.
    - `ExecuteStream` of the stream query yields exactly `1, 2, 3` in order.
    - On any difference the scenario prints `expected …, got …` (or the exception) to `Console.Error` and returns 1. Otherwise it returns 0.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionDarkerHandlerBuilder.cs`, delete `registerHandlers(_registryStream);` in `AddStreamHandlers`. The stream check must report a difference: no handler is found for the stream query. `dotnet run --project test/Paramore.Darker.Tests.EndToEnd -c Release -f net9.0` must exit non-zero.
  - Implementation should:
    - Add the public query and handler types in `test/Paramore.Darker.Tests.EndToEnd/Handlers/`: one sync, one async and one stream query, each with its handler. They carry no decorator attributes.
    - Register them with `AddHandlers`, `AddAsyncHandlers` and `AddStreamHandlers`. Do not use assembly scanning.
    - In `Program.cs`, call `ValidConfigurationRunsSyncAsyncAndStreamQueries.RunAsync()` and return its result when it is non-zero, before `return 0;`.

- [x] **CHARACTERISE: T3.2 — A retry pipeline recovers a handler that fails on its first call (FR8; NFR4; AC5)**
  - **USE COMMAND**: `/tdd:test-first harness scenario: when an async handler fails on its first call and is decorated with a retry resilience pipeline should return the handler's result after exactly two calls`
  - Test location: "test/Paramore.Darker.Tests.EndToEnd/Scenarios/"
  - Test file: `RetryPipelineRecoversATransientFailure.cs`
  - Test should verify:
    - `ExecuteAsync` returns the handler's success value.
    - A Singleton call counter, resolved from the host, reads exactly 2: one failure, then one success.
    - The scenario has no delay and no wait. The retry strategy has `Delay = TimeSpan.Zero`. Failure is decided by call count only (NFR4).
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `src/Paramore.Darker/Policies/Handlers/UseResiliencePipelineHandlerAsync.cs`, insert `return await next(query, cancellationToken).ConfigureAwait(false);` as the first statement of `ExecuteAsync`, which bypasses the pipeline. The first call's exception escapes. The scenario must report that the retry did not recover, and the harness must exit non-zero.
  - Implementation should:
    - Add, in `Handlers/`:
      - a public async query and handler whose `ExecuteAsync` carries `[UseResiliencePipelineAttributeAsync(1, "EndToEnd.Retry")]`;
      - a public Singleton call counter. The handler throws a dedicated public transient exception when the counter's first increment returns 1.
    - In the scenario, build a `ResiliencePipelineRegistry<string>` with `"EndToEnd.Retry"`: `AddRetry` with `ShouldHandle` for that exception only, `MaxRetryAttempts = 1` and `Delay = TimeSpan.Zero`. Pass it to `.AddResiliencePipelines(…)` before `.ValidatePipelines()`.
    - Append the scenario to `Program.cs` after T3.1's.

- [x] **CHARACTERISE: T3.3 — A Scoped dependency is provided to the handler and disposed when the query ends (FR9; AC5)**
  - **USE COMMAND**: `/tdd:test-first harness scenario: when a Scoped handler takes a Scoped disposable dependency should receive an undisposed instance during the query and the host should dispose it after the query ends`
  - Test location: "test/Paramore.Darker.Tests.EndToEnd/Scenarios/"
  - Test file: `ScopedDependencyIsProvidedAndDisposed.cs`
  - Test should verify:
    - The query result shows that the handler received a dependency instance (not null), and that the instance was not disposed while the handler ran.
    - After `ExecuteAsync` returns, the same instance reports `IsDisposed == true`.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `src/Paramore.Darker.Extensions.DependencyInjection/ServiceProviderLifetimeScope.cs`, change `public void Dispose() => _scope.Dispose();` to `public void Dispose() { }`, and `public ValueTask DisposeAsync() => _scope.DisposeAsync();` to `public ValueTask DisposeAsync() => default;`. The scenario must fail on the "disposed after the query" check and return non-zero. If it stays green, the scenario is not on the owned-scope path: stop and ask.
  - Implementation should:
    - Add, in `Handlers/`:
      - a public `ScopedDependency : IDisposable` that exposes `IsDisposed`;
      - a public async query whose result carries the received instance and a `WasDisposedDuringQuery` flag;
      - its public handler, which takes `ScopedDependency` through its constructor.
    - Register with `AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)` and `services.AddScoped<ScopedDependency>()`, before `.ValidatePipelines()`, so `CaptiveDependency` raises no finding.
    - Append the scenario to `Program.cs`.

- [x] **CHARACTERISE: T3.4 — An invalid configuration fails at host start with a message that names the fault, and the harness treats that as success (FR10; AC5)**
  - **USE COMMAND**: `/tdd:test-first harness scenario: when a handler's decorator is not registered and the chain ends with ValidatePipelines should fail host start with a PipelineValidationException naming DecoratorResolvable and the handler`
  - Test location: "test/Paramore.Darker.Tests.EndToEnd/Scenarios/"
  - Test file: `InvalidConfigurationFailsAtHostStart.cs`
  - Test should verify:
    - `await host.StartAsync()` throws `PipelineValidationException`.
    - Its message contains `[DecoratorResolvable]` and the invalid handler's type name.
    - That outcome returns 0. If start succeeds, or throws anything else, or the message lacks either name, the scenario prints what differed and returns 1.
    - The host is disposed either way, safely, because it uses the supplied `NullLoggerFactory` described at the head of this phase.
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - 🔁 RED mutation: in `src/Paramore.Darker.Extensions.DependencyInjection/PipelineValidationExtensions.cs`, delete `services.AddHostedService<DarkerValidationHostedService>();` in `RegisterValidation`. The host then starts despite the invalid registration. The scenario must report "host started although the configuration is invalid" and the harness must exit non-zero.
  - Implementation should:
    - Add, in `Handlers/`, a public async query and handler whose `ExecuteAsync` carries `[QueryLoggingAttributeAsync(1)]`.
    - Register it with `AddAsyncHandlers`, and deliberately omit `.AddJsonQueryLogging()` from the chain, which still ends with `.ValidatePipelines()`.
    - Append the scenario to `Program.cs` as the last scenario.

- [ ] **VERIFY: T3.5 — The harness exits 0 on both frameworks, and exits with the first non-zero result (FR11; AC5)**
  - Run `dotnet build Darker.Filter.slnf -c Release`. Then, for `-f net8.0` and `-f net9.0`, run `dotnet run --project test/Paramore.Darker.Tests.EndToEnd -c Release --no-build`. Each exits 0 (`echo $?`).
  - Apply T3.1's mutation. The harness exits non-zero, and no output from T3.2–T3.4 appears, which shows it stops at the first failure. Revert. `git status` shows no modified `src/` file.
  - Apply T3.4's mutation. The harness exits non-zero after T3.1–T3.3 pass. Revert.
  - Commit nothing, unless you tick the box.

## Phase 4 — CI and docs

- [ ] **SETUP: T4.1 — Run the harness for net8.0 and net9.0 in CI, after the test step (FR13; AC6; ADR 0027 step 4)**
  - In `.github/workflows/dotnet-core.yml`, add this step immediately after `- name: Core Tests`:
    ```yaml
    - name: End-to-end harness (Paramore.Darker.Tests.EndToEnd)
      run: |
        dotnet run --project test/Paramore.Darker.Tests.EndToEnd/Paramore.Darker.Tests.EndToEnd.csproj -c Release --no-build -f net8.0
        dotnet run --project test/Paramore.Darker.Tests.EndToEnd/Paramore.Darker.Tests.EndToEnd.csproj -c Release --no-build -f net9.0
    ```
  - The default shell on `ubuntu-latest` is `bash -e`, so a non-zero exit from either line fails the step and the build.
  - `--no-build` relies on the `Build` step building `Darker.Filter.slnf` in Release, which includes the harness (T1.5).
  - The step name names the harness (ADR 0027 Risks).
  - Commit as `ci:`.

- [ ] **DOC: T4.2 — Document both projects in `.agent_instructions/project_structure.md` (ADR 0027 step 5)**
  - Add two entries.
    - `test/Paramore.Darker.SampleMinimalApi.Tests/` is an xunit v3 test that starts `samples/SampleMinimalApi` in memory through `WebApplicationFactory<Program>`, with one shared host per assembly.
    - `test/Paramore.Darker.Tests.EndToEnd/` is a console harness that runs scenarios in a Generic Host with no ASP.NET Core and reports through its exit code. CI runs it as its own step.
  - State that both use only public API and project references.
  - Commit as `docs:`.

- [ ] **VERIFY: T4.3 — Final checkpoint (AC4, AC6, AC7, AC8, AC9; NFR1, NFR2, NFR3, NFR6)**
  - **AC4 / FR12.** `dotnet test Darker.Filter.slnf -c Release` is green. The output shows `Paramore.Darker.SampleMinimalApi.Tests` running its 5 tests on both net8.0 and net9.0.
  - **AC6 / FR13.** Locally, run the T4.1 step script under `bash -e` with T3.2's mutation applied. It exits non-zero. Revert. Then, on the PR, confirm that the CI log shows the "End-to-end harness" step running both frameworks green.
  - **AC7 / NFR1.**
    - `grep -rn "InternalsVisibleTo" src test` finds nothing that names either new project.
    - `grep -rn "Simple\|InMemory\|Paramore.Darker.Testing" test/Paramore.Darker.SampleMinimalApi.Tests test/Paramore.Darker.Tests.EndToEnd` finds nothing.
  - **AC8 / NFR6.**
    - `git diff master --stat -- src/` is empty.
    - `git diff master -- samples/` shows exactly one added line, `public partial class Program;`.
  - **AC9 / FR6.**
    - The harness `.csproj` has no `Microsoft.AspNetCore` reference, no `FrameworkReference` and no `Sdk.Web`.
    - `dotnet list test/Paramore.Darker.Tests.EndToEnd package --include-transitive` lists no `Microsoft.AspNetCore.*`.
    - `test/Paramore.Darker.Tests.EndToEnd/obj/project.assets.json` lists no `Microsoft.AspNetCore.App` framework for either TFM.
  - **NFR2.** No test or scenario opens a port, uses a network address or database, or starts a process. The sample test uses only `TestServer`.
  - **NFR3.** Time the sample test project on both TFMs, and time both harness runs. Together they add no more than a few seconds over the previous CI run. Record the times in the PR description.
  - **NFR5.** One `WebApplicationFactory<Program>` exists in the sample test assembly, owned by the `SampleMinimalApi` collection. All five test classes carry `[Collection("SampleMinimalApi")]`.
  - Finally, `dotnet build Darker.Filter.slnf -c Release` and `dotnet test Darker.Filter.slnf -c Release` are green, and `git status` is clean.
