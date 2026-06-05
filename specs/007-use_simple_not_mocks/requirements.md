# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: [#306](https://github.com/BrighterCommand/Darker/issues/306) — Migrate legacy tests to use Simple*/InMemory* factories and TestDoubles directory

## Problem Statement

As a **Darker maintainer/contributor**, I would like the legacy test files to use the project's preferred test-double patterns (real/Simple/InMemory implementations and a shared `TestDoubles/` directory) instead of Moq mocks and inline nested test-double classes, **so that** the test suite is consistent with [ADR 0009](../../docs/adr/0009-simple-and-inmemory-factory-implementations.md) and the testing guidelines, is easier to read and maintain, and reuses test doubles rather than duplicating them.

Several legacy test files still rely on `Mock<IQueryHandlerFactory>`, `Mock<IQueryHandlerDecoratorFactory>`, `Mock<IQueryHandlerDecoratorRegistry>`, `Mock<IQueryHandler<,>>` (and their `Async` counterparts), plus inline nested test-double classes. This is inconsistent with the rest of the suite (e.g. the `When_*` test files) which already follow the preferred patterns.

> **Path correction (verified against the codebase, 2026-06-04)**: The issue lists the target files under `test/Paramore.Darker.Tests/`. That project has since been renamed to **`test/Paramore.Darker.Core.Tests/`** (namespace `Paramore.Darker.Core.Tests`), most likely as part of #304. All four target files exist there and still use Moq. The corrected paths are used throughout this document.

## Proposed Solution

Migrate the four remaining legacy test files away from Moq and inline nested test doubles, towards the preferred patterns, **without changing the behaviour being tested**:

1. Replace `Mock<IQueryHandlerFactory>` / `Mock<IQueryHandlerFactoryAsync>` with **`SimpleHandlerFactory`**.
2. Replace `Mock<IQueryHandlerDecoratorFactory>` / `Mock<IQueryHandlerDecoratorFactoryAsync>` with **`SimpleHandlerDecoratorFactory`**.
3. Replace `Mock<IQueryHandlerDecoratorRegistry>` / `Mock<IQueryHandlerDecoratorRegistryAsync>` with **`InMemoryDecoratorRegistry`**.
4. Replace mocked handlers (`Mock<IQueryHandler<,>>` / `Mock<IQueryHandlerAsync<,>>`) with real, concrete test-double handler classes that **record their own invocations** (see step 6).
5. Extract nested test-double classes (handlers, queries, decorators, attributes) into `test/Paramore.Darker.Core.Tests/TestDoubles/` under namespace `Paramore.Darker.Core.Tests.TestDoubles`, reusing existing `TestDoubles/` doubles where one already covers the need.
6. Re-express Moq **interaction verification** (`.Verify(... Times.Once/Never)`) as **state-based verification**. The QueryProcessor tests currently assert, via Moq, *which* handler's `Execute`/`Fallback` (or `ExecuteAsync`/`FallbackAsync`) ran and how many times, and whether the handler factory `Release`d each handler. After migration these same behaviours are asserted against state recorded by the doubles: invocation-recording handler doubles and a **tracking handler-factory double** (a `SimpleHandlerFactory`-style factory that also records `Release` calls), both in `TestDoubles/`.

### Two test-double directories — `Exported/` vs `TestDoubles/`

The test project deliberately keeps **two** directories of doubles, which serve different purposes:

- **`test/Paramore.Darker.Core.Tests/Exported/`** (namespace `Paramore.Darker.Core.Tests.Exported`): the *public* doubles intended to be discovered by assembly/handler **scanning** (e.g. `AddHandlersFromAssemblies`). Currently: `TestQueryA`, `TestQueryB`, `TestQueryC`, `TestQueryHandler`, `TestQueryHandlerAsync`. These MUST remain as-is — scanning relies on them.
- **`test/Paramore.Darker.Core.Tests/TestDoubles/`** (namespace `Paramore.Darker.Core.Tests.TestDoubles`): the *internal* doubles used directly by individual tests.

**Decision (resolves the `Exported/` ambiguity):** this migration does **not** reuse the `Exported/` doubles for the four target files. Where a target test needs a double (query, handler, or decorator) that an `Exported/` type resembles, a **new** double is created in `TestDoubles/` with a **distinct name** (copy-and-rename, not just a namespace difference) to avoid same-name/different-namespace confusion between the two directories. This applies to **both query types and handler types**: after migration the four target files reference **no** `Exported` types at all. In particular `QueryProcessorTests.cs` and `QueryProcessorAsyncTests.cs` — which today consume `Exported.TestQueryA`/`TestQueryB`/`TestQueryHandler`/`TestQueryHandlerAsync` — get distinctly-named query + handler doubles in `TestDoubles/`.

The supporting helpers already exist in `src/Paramore.Darker/` (`SimpleHandlerFactory`, `SimpleHandlerDecoratorFactory`, `InMemoryDecoratorRegistry`) and a `TestDoubles/` directory already exists in the test project, so this is a migration of existing tests onto existing infrastructure.

## Requirements

### Functional Requirements

- **FR1**: `test/Paramore.Darker.Core.Tests/QueryProcessorTests.cs` MUST be migrated to use a tracking `SimpleHandlerFactory`-style factory (FR10), `SimpleHandlerDecoratorFactory`, and `InMemoryDecoratorRegistry` in place of `Mock<IQueryHandlerFactory>`, `Mock<IQueryHandlerDecoratorFactory>`, and `Mock<IQueryHandlerDecoratorRegistry>`, and to use real, invocation-recording handler test doubles in place of `Mock<IQueryHandler<,>>`. After migration the file MUST reference **no** `Exported` types: the `Exported.TestQueryA`/`TestQueryB`/`TestQueryHandler` it currently consumes are replaced by distinctly-named query + handler doubles in `TestDoubles/` (FR6).
- **FR2**: `test/Paramore.Darker.Core.Tests/QueryProcessorAsyncTests.cs` MUST be migrated to use the Simple*/InMemory* equivalents (including the `Async` factory/registry variants and the tracking factory of FR10) in place of all `Mock<...>` usages, and to use real, invocation-recording async handler test doubles in place of `Mock<IQueryHandlerAsync<,>>`. After migration the file MUST reference **no** `Exported` types (FR6).
- **FR3**: `test/Paramore.Darker.Core.Tests/Decorators/FallbackPolicyTests.cs` MUST be migrated to use `SimpleHandlerFactory`, `SimpleHandlerDecoratorFactory`, and `InMemoryDecoratorRegistry` in place of its `Mock<...>` usages, and its inline nested test-double classes MUST be extracted into the `TestDoubles/` directory. The complete, closed set to extract is: `TestQuery` (and its nested `Result`), `TestQueryHandlerWithCatchAllFallback`, `TestQueryHandlerWithFormatExceptionFallback`, `TestQueryHandlerWithoutFormatExceptionFallback`.
- **FR4**: `test/Paramore.Darker.Core.Tests/PipelineBuilderExceptionTests.cs` MUST be migrated to use `SimpleHandlerFactory`, `SimpleHandlerDecoratorFactory`, and `InMemoryDecoratorRegistry` in place of its `Mock<...>` usages, and its inline nested test-double classes MUST be extracted into the `TestDoubles/` directory. The complete, closed set to extract is: `ExceptionQuery` (+ `Result`), `ExceptionQueryHandler`, `FallbackExceptionQuery` (+ `Result`), `FallbackExceptionQueryHandler`, `NullInnerExceptionQuery`, `NullInnerExceptionQueryHandler`, `DecoratorExceptionQuery` (+ `Result`), `DecoratorExceptionQueryHandler`, `TestExceptionDecorator<TQuery, TResult>`, `DecoratorExceptionAttribute`. Each `Result` type MUST move with its owning query.
- **FR5**: All extracted test-double classes MUST live under `test/Paramore.Darker.Core.Tests/TestDoubles/` with namespace `Paramore.Darker.Core.Tests.TestDoubles`, consistent with the existing doubles in that directory. The `Exported/` directory and its doubles MUST NOT be moved or modified (other than FR9).
- **FR6**: Reuse rules:
  - Where an existing double in **`TestDoubles/`** already satisfies a test's need, the migrated test MUST reuse it rather than introducing a duplicate.
  - The migration MUST NOT reuse the **`Exported/`** doubles for the four target files. This covers **query types, handler types, and decorators alike**. Where a target test needs a double resembling an `Exported/` type, a **new** double MUST be created in `TestDoubles/` with a **distinct name** (not the same name under a different namespace).
  - After migration, none of the four target files may reference any `Exported` type or import the `Paramore.Darker.Core.Tests.Exported` namespace.
  - No newly created `TestDoubles/` type may share a simple type name with an `Exported/` type (no `Exported.TestQueryA` vs `TestDoubles.TestQueryA` collisions).
- **FR7**: After migration, none of the four target files may reference Moq (`using Moq;` or `Mock<...>`). In particular, mocked handlers (`Mock<IQueryHandler<,>>` / `Mock<IQueryHandlerAsync<,>>`) MUST be replaced by concrete, named handler test-double classes — not by mocks of any kind.
- **FR8**: Once FR1–FR4 are complete, Moq becomes unused across the entire repository (verified 2026-06-04: the four target files are the only Moq consumers, and `Paramore.Darker.Core.Tests.csproj` is the only project referencing the package). The dead Moq dependency MUST therefore be removed: the `<PackageReference Include="Moq" />` from `test/Paramore.Darker.Core.Tests/Paramore.Darker.Core.Tests.csproj` and the central `<PackageVersion Include="Moq" ... />` from `Directory.Packages.props`.
- **FR9**: A `README.md` MUST be added to `test/Paramore.Darker.Core.Tests/Exported/` explaining the directory's purpose: it holds the *public* test doubles that exist to be discovered by assembly/handler **scanning** (e.g. `AddHandlersFromAssemblies`), and is distinct from `TestDoubles/` (internal doubles used directly by tests). The README MUST state that doubles here must not be renamed/removed without checking scanning-based tests.
- **FR10 (interaction → state verification)**: The QueryProcessor tests currently use Moq **interaction verification** — `_handlerFactory.Verify(x => x.Release(handler), Times.Once/Never)` and `handler.Verify(x => x.Execute/ExecuteAsync/Fallback/FallbackAsync(...), Times.Once/Never)` (15 such calls across the two files). These behaviours MUST be **preserved** but re-expressed as **state-based** assertions, since `SimpleHandlerFactory` and plain handler doubles do not record interactions. Specifically:
  - A **tracking handler-factory double** MUST be provided in `TestDoubles/` (a `SimpleHandlerFactory`-equivalent that also records which handlers it `Release`d, and how many times), so that `Release(...) Times.Once/Never` assertions become state assertions over the recorded releases.
  - The handler doubles in `TestDoubles/` MUST be **able to record their own invocations** (which method ran — `Execute`/`Fallback` and async variants — how many times, and the query argument). Because a Darker query handler returns a result, the **preferred** re-expression asserts on the **returned result** wherever it can demonstrate the behaviour (e.g. a double whose `Execute` echoes `query.Id` lets a test assert `result.ShouldBe(id)`, proving the matching handler ran with the expected query — no recorded state needed). Recorded invocation state is used only where the result cannot show the behaviour: a handler/`Fallback` that must **not** run (a negative), an **exception** path (no result), or a factory `Release` count. The `CancellationToken` argument of the async verifications is invariantly `default` and need NOT be recorded or asserted.
  - The set of behaviours asserted MUST be identical to the pre-migration tests (same handler-ran / not-ran / released expectations); only the *mechanism* of assertion changes (Moq interaction verification → state verification).

### Non-functional Requirements

- **NFR1 (Behaviour preservation)**: This is a structural-only (tidy-first) change. No production code in `src/` may be modified. The set of behaviours asserted by each test MUST remain identical before and after migration (same scenarios, same expected outcomes). "Same assertions" means **same asserted behaviour**, not identical assertion syntax: where a test used Moq interaction verification, that behaviour is re-expressed as state-based verification per FR10 — this re-expression is permitted and expected, and is not considered a behaviour change.
- **NFR2 (Green suite)**: The full solution MUST build and all existing tests MUST continue to pass after migration (`dotnet build Darker.Filter.slnf -c Release` and `dotnet test Darker.Filter.slnf -c Release`).
- **NFR3 (Consistency)**: Migrated tests MUST follow the style of the existing `When_*` exemplar tests and the testing guidelines in `.agent_instructions/testing.md` (Test Doubles section), honouring the Real > Simple > InMemory > Mock preference order. This is a **review-time/narrative** quality attribute with no dedicated mechanical AC; its substantive, checkable part (no mocks; doubles constructed via Simple*/InMemory*/concrete classes) is covered by AC1, AC2, AC3, and AC5.
- **NFR4 (No scope creep)**: No new test scenarios and no renames of test methods. Assertion changes are limited to (a) referencing the new doubles and (b) the interaction→state re-expression required by FR10; no new behaviours are asserted.

### Constraints and Assumptions

- **Constraint**: Governed by existing **ADR 0009** (`docs/adr/0009-simple-and-inmemory-factory-implementations.md`); this migration does not introduce new architecture, so a new ADR is likely unnecessary (see Additional Context).
- **Constraint**: `SimpleHandlerFactory`, `SimpleHandlerDecoratorFactory`, and `InMemoryDecoratorRegistry` each implement both the sync and async contracts, so a single instance can serve both sides of `QueryProcessorAsyncTests`.
- **Assumption**: The four files listed are the only remaining legacy mock-based test files in scope. Verified on 2026-06-04: `grep -rl 'Mock<' test/Paramore.Darker.Core.Tests/` returns exactly these four files.
- **Assumption**: #304 (split handler interfaces) has already landed and renamed the project to `Paramore.Darker.Core.Tests`; this spec migrates whatever legacy tests remain.
- **Assumption**: This work proceeds on the existing `use_simple` branch (already checked out; not `master`).

### Out of Scope

- Migrating mock usage in any project other than `Paramore.Darker.Core.Tests` (e.g. `Paramore.Darker.Extensions.Tests`) — none was found in scope, but no broader sweep is promised here.
- Adding new test coverage, new behaviours, or new assertions.
- Any change to production code under `src/`.
- Refactoring the existing `TestDoubles/` files beyond what is needed to host newly extracted doubles.
- Moving, renaming, deleting, or otherwise changing the `Exported/` doubles. The only permitted change to `Exported/` is adding the `README.md` required by FR9.

## Acceptance Criteria

How we'll know this is working correctly:

- **AC1 (FR7)**: `grep -rl 'Mock<' test/Paramore.Darker.Core.Tests/` returns **no files**, and `grep -rl 'using Moq' test/Paramore.Darker.Core.Tests/` returns **no files**.
- **AC2 (FR1–FR4)**: Each of the four target files constructs its `QueryProcessor` using `SimpleHandlerDecoratorFactory` and `InMemoryDecoratorRegistry` (and the async variants where applicable); the two QueryProcessor files use the tracking handler-factory double (FR10) and the other two use `SimpleHandlerFactory`.
- **AC3 (FR1, FR2, FR7)**: Neither `QueryProcessorTests.cs` nor `QueryProcessorAsyncTests.cs` contains `Mock<IQueryHandler` / `Mock<IQueryHandlerAsync`; each instead references concrete, named handler test-double types for the handlers under test.
- **AC4 (FR1, FR2, FR6)**: Neither `QueryProcessorTests.cs` nor `QueryProcessorAsyncTests.cs` imports the `Exported` namespace or references any `Exported` type. Concretely: no `using Paramore.Darker.Core.Tests.Exported;` line, and a **whole-word** search `grep -nwE 'TestQueryA|TestQueryB|TestQueryC|TestQueryHandler|TestQueryHandlerAsync'` over the two files returns no matches. (Whole-word `-w` matching is required so that legitimately-named new doubles such as `RecordingTestQueryHandler` do not false-fail; exact same-name collisions are independently prevented by AC7.)
- **AC5 (FR10)**: The migrated QueryProcessor tests preserve every pre-migration behavioural expectation (which handler's `Execute`/`Fallback`/async variant ran, how many times, with the expected query argument, and whether the factory `Release`d each handler). Each expectation is re-expressed as an assertion on the **returned result** where the result can demonstrate the behaviour, and otherwise as a **state** assertion over recorded state on the handler doubles (negatives, exception paths) or the tracking handler-factory double (`Release` counts) in `TestDoubles/`. No Moq tokens (`.Verify(`, `Times.`, `It.`) remain in either file.
- **AC6 (FR3, FR4, FR5)**: No `class` declarations for test-double handlers, queries, decorators, or attributes remain nested inside the four target test files; every class named in the FR3 and FR4 closed lists (including the `Result` types) exists under `test/Paramore.Darker.Core.Tests/TestDoubles/` with namespace `Paramore.Darker.Core.Tests.TestDoubles`.
- **AC7 (FR6)**: No type declared under `TestDoubles/` shares a simple type name with a type declared under `Exported/` (no name collisions).
- **AC8 (FR5, Exported untouched)**: `git diff` shows no changes to the five existing `Exported/*.cs` files (the only permitted `Exported/` change is the new README).
- **AC9 (NFR1)**: `git diff` shows no changes under `src/`.
- **AC10 (FR9)**: `test/Paramore.Darker.Core.Tests/Exported/README.md` exists and explains the directory's scanning purpose and its distinction from `TestDoubles/`.
- **AC11 (baseline)**: Before any migration change, the current passing-test count for `Paramore.Darker.Core.Tests` (and the filtered solution) is recorded in the spec (e.g. captured in `tasks.md` or a `.baseline-test-count` file), so AC12 has a concrete number to compare against.
- **AC12 (NFR2)**: `dotnet build Darker.Filter.slnf -c Release` succeeds and `dotnet test Darker.Filter.slnf -c Release` reports a passing count **equal to the AC11 baseline**, with zero failures.
- **AC13 (NFR4)**: In each target file the number of `[Fact]`/`[Theory]` methods is unchanged, and none are added, removed, or renamed.
- **AC14 (FR8)**: After migration, both of these return **nothing**:
  - `grep -rliE 'moq' --include='*.cs' --include='*.csproj' --include='*.props' --exclude-dir=bin --exclude-dir=obj test src`
  - `grep -liE 'moq' Directory.Packages.props`

  The check is scoped to source files (`.cs`/`.csproj`/`.props`) with `bin/`/`obj/` excluded, because git-ignored build artifacts (`*.deps.json`, `project.assets.json`) can name Moq transitively and are regenerated by builds — they are not in scope and would otherwise guarantee a false failure. Today the first command lists exactly the four target files plus `Paramore.Darker.Core.Tests.csproj`, and the second lists `Directory.Packages.props`; after migration both are empty.

**Testing approach**: This is a refactor of test code itself, so correctness is demonstrated by the unchanged, still-passing test suite (NFR2) rather than by new tests. Apply `/tidy-first` framing: structural change only, verified green before and after. The interaction→state re-expression (FR10) is the one place where assertion *syntax* changes; the asserted behaviour does not.

**Definition of done**: All fourteen acceptance criteria (AC1–AC14) pass; the four target files are Moq-free, free of handler mocks, and (for the two QueryProcessor files) free of any `Exported` reference; the FR3/FR4 closed-list doubles live in `TestDoubles/`; interaction verification is re-expressed as state verification (FR10); `Exported/` is unchanged except for its new README; Moq is fully removed from the repository; the suite is green at the recorded baseline count; no `src/` changes.

## Additional Context

- **Reference ADR**: ADR 0009 — `docs/adr/0009-simple-and-inmemory-factory-implementations.md` (establishes the Simple*/InMemory* implementations and the preference order).
- **Testing guidelines**: `.agent_instructions/testing.md` (Test Doubles section) and the project `CLAUDE.md` "Test Double Preference (Real > Simple > InMemory > Mock)".
- **Exemplar style**: `test/Paramore.Darker.Core.Tests/When_sync_query_executed_should_resolve_from_sync_registry_and_build_sync_decorator_chain.cs` (and sibling `When_*` tests) with doubles in `TestDoubles/`.
- **Brighter precedent**: `tests/Paramore.Brighter.Core.Tests/CommandProcessors/TestDoubles/`.
- **ADR note**: Because this migration is fully governed by ADR 0009 and adds no new architecture, a dedicated ADR may not be warranted. If the workflow requires a design artifact, the next available number is **0013** (`docs/adr/0013-...`); otherwise the design phase can simply reference ADR 0009. Confirm during `/spec:design`.
