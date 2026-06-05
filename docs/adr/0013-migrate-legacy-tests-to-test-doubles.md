# 13. Migrate Legacy Tests to Simple/InMemory Doubles and a TestDoubles Directory

Date: 2026-06-05

## Status

Accepted

## Context

**Parent Requirement**: [specs/007-use_simple_not_mocks/requirements.md](../../specs/007-use_simple_not_mocks/requirements.md)

**Scope**: This ADR addresses *how* four legacy test files are migrated off Moq and inline nested test doubles onto the patterns established in [ADR 0009](0009-simple-and-inmemory-factory-implementations.md). It is a test-only, structural (tidy-first) change. It introduces **no** production types and **no** new architecture in `src/`; the only new types are *test doubles* in the test project.

### The Problem

ADR 0009 added public `SimpleHandlerFactory`, `SimpleHandlerDecoratorFactory`, and `InMemoryDecoratorRegistry` so tests could stop depending on Moq. Four files were never migrated (the project was since renamed `Paramore.Darker.Tests` → `Paramore.Darker.Core.Tests`, likely via #304):

- `QueryProcessorTests.cs`, `QueryProcessorAsyncTests.cs` — mock the factories, registries, **and** the handlers.
- `Decorators/FallbackPolicyTests.cs`, `PipelineBuilderExceptionTests.cs` — mock the factories/registries and declare nested test-double classes inline.

They are the only remaining Moq consumers in the repository, so finishing the migration also lets us delete the Moq dependency entirely.

Replacing the *factory/registry* mocks is mechanical (ADR 0009 already provides the doubles). The architectural difficulty is concentrated in two places:

1. **The two QueryProcessor files assert interactions, not just outcomes.** They use Moq interaction verification — `_handlerFactory.Verify(x => x.Release(handler), Times.Once/Never)` and `handler.Verify(x => x.Execute(It.Is<TestQueryA>(q => q.Id == id)), Times.Once/Never)` (15 such calls). `SimpleHandlerFactory` disposes on `Release` but records nothing, and a plain handler double records nothing either. So a faithful migration cannot be a one-for-one type swap; the *assertion mechanism* must change from interaction verification to **state** verification, which requires doubles that **know** what happened to them.

2. **There are two distinct families of handler double, with different reasons to exist.** The test project already keeps `Exported/` doubles (`TestQueryA/B/C`, `TestQueryHandler`, `TestQueryHandlerAsync`) that exist to be discovered by **assembly/handler scanning** (e.g. `AddHandlersFromAssemblies`). `QueryProcessorTests` currently reuses those `Exported` types directly. Reusing the same types as plain in-test doubles blurs the two roles and invites same-name/different-namespace confusion.

### Forces

- **Faithful migration (NFR1/NFR4)**: behaviour asserted must be identical before/after; only the *mechanism* may change. We must not silently drop the `Release`/invocation assertions.
- **ADR 0009 philosophy**: prefer asserting *behaviour/outcome* over *implementation/interaction*. The legacy assertions lean on interaction counts — in tension with this — but the requirement is a structural migration, not a redesign of what the tests assert.
- **Don't add types without necessity** (design principles): minimise new doubles; prefer one reusable, cohesive double over many bespoke ones.
- **Reveal intention**: a reader must immediately see which doubles are "public/scannable" vs "internal to a test".

### Constraints

- Test-only. No `src/` change. No new production type.
- The new doubles live in `test/Paramore.Darker.Core.Tests/TestDoubles/` (namespace `Paramore.Darker.Core.Tests.TestDoubles`).
- `Exported/` doubles must not move or change (scanning depends on them); only a `README.md` may be added.
- Doubles must implement the real handler/factory interfaces (no Moq).

## Decision

Complete the migration with four decisions. The factory/registry swap follows ADR 0009 unchanged; the novel parts are the **recording doubles** and the **two-directory role split**.

### Decision 1 — Two directories, two roles (make the existing split explicit)

Keep two directories, each with a single, cohesive role, and document the distinction (a `README.md` in `Exported/`):

| Directory | Role (stereotype) | Reason to exist |
|-----------|-------------------|-----------------|
| `Exported/` | **Service Provider**, discovered by *scanning* | Public handlers that assembly-scanning tests (`AddHandlersFromAssemblies`) must find by reflection. Renaming/removing them breaks those tests. |
| `TestDoubles/` | **Service Provider / Information Holder**, used *directly* | Internal doubles a test news-up and hands to the system under test. |

The four target files use **only** `TestDoubles/`. Where a `TestDoubles/` double resembles an `Exported/` one, it is **copied and renamed** (distinct simple name), never reused under a different namespace. After migration the two QueryProcessor files reference no `Exported` type.

### Decision 2 — Factory/registry mocks → ADR 0009 doubles

Replace `Mock<IQueryHandlerFactory>` → `SimpleHandlerFactory` (or the recording factory of Decision 4), `Mock<IQueryHandlerDecoratorFactory>` → `SimpleHandlerDecoratorFactory`, `Mock<IQueryHandlerDecoratorRegistry>` → `InMemoryDecoratorRegistry`, and the `Async` variants likewise. A single `SimpleHandlerDecoratorFactory`/`InMemoryDecoratorRegistry` instance serves both sync and async sides (both interfaces are implemented on one class — ADR 0009).

### Decision 3 — Prefer the query result; fall back to a recording handler double where it cannot prove the behaviour

Darker handlers differ from Brighter's command handlers: a query handler **returns a result**. That return value is itself observable evidence that the handler ran, with the right input — and asserting on it is *outcome* verification, which ADR 0009 prefers over interaction verification. So the preferred translation of a Moq `handler.Verify(...)` is, **wherever the result can carry the proof, to assert on the returned result** rather than on recorded invocation state.

Concretely, a double whose `Execute` echoes something derived from the query (e.g. returns `query.Id`) lets a test assert `result.ShouldBe(id)` — which simultaneously proves the *matching* handler ran *and* received the expected query, replacing `handler.Verify(x => x.Execute(It.Is<…>(q => q.Id == id)), Times.Once)` with no recording at all.

That path does not cover every legacy assertion. The result cannot demonstrate a **negative** (a handler/`Fallback` that must *not* run), an **exception** path (which produces no result), or a **factory `Release`** count. For those — and only those — fall back to a double that **knows about its own invocations**.

**Role**: Service Provider (it *is* the handler — runs `Execute`/`Fallback`) **+** Information Holder (it records what was done to it). These responsibilities are cohesive: both describe "the handler under observation".

> **Mapping the three QueryProcessor scenarios:**
> - *Matching handler executes* → assert the **returned result** (no recording needed for the positive case); recorded `ExecuteCount == 0` proves the *non*-matching handler did not run.
> - *Exception does not trigger fallback* → no result is produced; assert recorded `FallbackCount == 0`.
> - *Handler is released* → assert the recording **factory** (Decision 4), not the handler.

- **Knowing**: how many times `Execute`/`Fallback` (and async variants) ran; the last query passed.
- **Doing**: `Execute(query)` records the call and returns a configured result — or throws a configured exception (needed for `ExceptionsDontCauseFallbackByDefault`).
- **Deciding**: whether to return or throw, per its configuration.

A single **generic** double — illustratively `RecordingQueryHandler<TQuery, TResult>` (and `RecordingQueryHandlerAsync<TQuery, TResult>`) — covers all three QueryProcessor scenarios, avoiding bespoke per-test classes:

```csharp
// illustrative — final names/shape are an implementation detail
internal class RecordingQueryHandler<TQuery, TResult>(Func<TQuery, TResult> execute)
    : QueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public int ExecuteCount { get; private set; }
    public int FallbackCount { get; private set; }
    public TQuery? LastQuery { get; private set; }

    public override TResult Execute(TQuery query)
    {
        ExecuteCount++;
        LastQuery = query;
        return execute(query);   // delegate may throw, for the exception scenario
    }

    public override TResult Fallback(TQuery query) { FallbackCount++; return base.Fallback(query); }
}
```

The async sibling `RecordingQueryHandlerAsync<TQuery, TResult>` mirrors this against `QueryHandlerAsync<TQuery, TResult>`: `ExecuteAsync` is `abstract` and `FallbackAsync` is `virtual` (both verified in `src/Paramore.Darker/QueryHandlerAsync.cs`), so it overrides both the same way, takes a `Func<TQuery, Task<TResult>>` (or a sync `Func` wrapped in `Task.FromResult`), and threads the `CancellationToken` through to the delegate. No other shape difference.

The Moq assertions translate as follows — **result first**, recorded state only for what the result cannot show:

| Legacy (Moq interaction) | Migrated | Mechanism |
|--------------------------|----------|-----------|
| `handlerA.Verify(x => x.Execute(It.Is<…>(q => q.Id == id)), Times.Once)` | `result.ShouldBe(id)` (double echoes `query.Id`) | **outcome** — preferred, no recording |
| `handlerB.Verify(x => x.Execute(…), Times.Never)` | `handlerB.ExecuteCount.ShouldBe(0)` | state (negative — result can't show it) |
| `handlerA.Verify(x => x.Fallback(…), Times.Never)` | `handlerA.FallbackCount.ShouldBe(0)` | state (negative) |

The `CancellationToken` argument of the async verifications is invariantly `default`, so it is not recorded (behaviour-equivalent). Recording fields on the double are present for the negative/exception cases; tests that can assert on the result alone need not read them.

### Decision 4 — A recording (tracking) handler-factory double

The legacy tests also assert `_handlerFactory.Verify(x => x.Release(handler), Times.Once/Never)`. `SimpleHandlerFactory.Release` disposes but does not record, and `Release` is not virtual, so we add a sibling double in `TestDoubles/` rather than subclass.

**Role**: Service Provider (creates via a delegate, exactly like `SimpleHandlerFactory`) **+** Information Holder (records which handlers it released, and how many times).

- **Knowing**: the set/multiset of handlers passed to `Release`.
- **Doing**: `Create(Type)` delegates to a `Func<Type, IQueryHandler>`; `Release(handler)` records the handler (and may still dispose, preserving `SimpleHandlerFactory` semantics).

```csharp
// illustrative
internal class RecordingHandlerFactory(Func<Type, IQueryHandler> create)
    : IQueryHandlerFactory, IQueryHandlerFactoryAsync
{
    private readonly List<IQueryHandler> _released = new();
    public IReadOnlyList<IQueryHandler> Released => _released;
    public int ReleaseCount(IQueryHandler h) => _released.Count(r => ReferenceEquals(r, h));

    public IQueryHandler Create(Type handlerType) => create(handlerType);
    public void Release(IQueryHandler handler) => _released.Add(handler);
}
```

`_handlerFactory.Verify(x => x.Release(handlerA), Times.Once)` → `factory.ReleaseCount(handlerA).ShouldBe(1)`; `Times.Never` → `ShouldBe(0)`.

> **Addendum (2026-06-05, from tasks review):** `FallbackPolicyTests` also asserts the
> **decorator** factory's `Release` — `_decoratorFactory.Verify(x => x.Release<…>(decorator), Times.Once)`
> (×3, `FallbackPolicyTests.cs:51,77,102`). `SimpleHandlerDecoratorFactory.Release<T>`
> disposes but records nothing (`SimpleHandlerDecoratorFactory.cs:50-54`), so by the
> exact same reasoning as the handler factory above, a sibling **recording
> decorator-factory** double (`RecordingDecoratorFactory`, implementing both
> `IQueryHandlerDecoratorFactory` and `IQueryHandlerDecoratorFactoryAsync`) is added to
> `TestDoubles/`, exposing `ReleaseCount(decorator)`. The original Decision 4 named only
> the *handler* factory; this addendum extends the same pattern to the decorator factory
> to keep the `Release` assertions faithful (NFR1). Confirmed with the user that these
> decorator-`Release` assertions are **preserved as state**, not dropped.
> `PipelineBuilderExceptionTests` has **no** decorator-`Release` assertion and continues
> to use the plain `SimpleHandlerDecoratorFactory`.

Note an asymmetry to preserve faithfully (NFR1): the sync `ExecutesQueries` asserts `Release ... Times.Once` (`QueryProcessorTests.cs:43`), but the **async** `ExecutesQueries` asserts `Release ... Times.Never` (`QueryProcessorAsyncTests.cs:53`). The async case therefore migrates to `ReleaseCount(handler).ShouldBe(0)`, not `1` — do not "tidy" the two into matching expectations.

### Architecture Overview

```
                 Paramore.Darker.Core.Tests
                 ┌───────────────────────────────────────────────┐
                 │                                               │
   scanning ───► │  Exported/   (public, discovered by reflection)│  ← README explains role
   tests         │    TestQueryA/B/C, TestQueryHandler[Async]      │     (untouched otherwise)
                 │                                               │
   four target ─►│  TestDoubles/ (internal, used directly)        │
   files         │    • RecordingQueryHandler<,> / …Async<,>  (D3) │  Service Provider
                 │    • RecordingHandlerFactory               (D4) │  + Information Holder
                 │    • extracted FallbackPolicy / Pipeline doubles│
                 │    • reuse: SimpleHandlerDecoratorFactory,      │
                 │      InMemoryDecoratorRegistry (from src, ADR 9)│
                 └───────────────────────────────────────────────┘
```

### Nested-double extraction (FallbackPolicyTests, PipelineBuilderException Tests)

The nested classes in these two files are behaviour doubles (handlers, queries, a decorator, an attribute), not recorders. They move verbatim to `TestDoubles/` (each `Result` with its owning query), per the closed lists in the requirements (FR3/FR4). No recording behaviour is added to them.

## Consequences

### Positive

- **Moq fully removed** — the dependency and its `Directory.Packages.props` entry are deleted; one fewer test dependency to maintain.
- **Behaviour over plumbing where it matters** — outcome assertions (`result.ShouldBe(id)`, context bag contents) stay; interaction assertions become explicit, readable state reads.
- **One reusable recording double** instead of N bespoke mocks/classes — fewer types, higher cohesion (RDD).
- **Roles are legible** — `Exported/` vs `TestDoubles/` is documented; future contributors won't accidentally rename a scanned handler or reuse a scanned type as an in-test double.
- **Consumer benefit** — the recording doubles model a pattern users can copy for their own handler tests.

### Negative

- **New test-only types** — a generic recording handler (sync + async) and a recording factory. Mitigated by keeping them generic/reusable and `internal`.
- **State doubles are slightly more code than a one-line `SimpleHandlerFactory`** for the cases that need release/invocation tracking. Justified only where the legacy test actually asserts those interactions.
- **Preserves interaction-style assertions** that ADR 0009 would rather avoid (see Risks).

### Risks and Mitigations

- **Risk**: The migration faithfully preserves `Release`/invocation-count assertions that are arguably implementation detail, perpetuating interaction-style testing ADR 0009 discourages.
  - **Mitigation**: This ADR's mandate is a *structural* migration (NFR1/NFR4) — changing what is asserted is out of scope. Recorded as a candidate future cleanup: once on state doubles, a follow-up could drop low-value `Release`-count assertions in favour of pure outcome assertions.
- **Risk**: A recording double drifts from real handler semantics (e.g. forgets to call `base.Fallback`).
  - **Mitigation**: Derive from `QueryHandler<TQuery,TResult>`/`QueryHandlerAsync<…>` so default behaviour is inherited; record by overriding.
- **Risk**: Name collision/confusion between `Exported/` and `TestDoubles/` doubles.
  - **Mitigation**: Distinct simple names enforced (requirements AC4/AC7); whole-word checks.

## Alternatives Considered

### Alternative 1 — Keep Moq for the two QueryProcessor files
Migrate only the two files with nested doubles; leave the interaction-heavy files on Moq.
**Rejected**: Moq would remain a dependency (defeating the cleanup), and the suite stays inconsistent. The interaction assertions are exactly what we want to express as readable state.

### Alternative 2 — Drop the `Release`/invocation assertions, assert outcomes only
Most aligned with ADR 0009's "behaviour not implementation".
**Rejected for now**: changes *what* the tests assert, violating NFR1/NFR4 (faithful structural migration). Captured as a future cleanup instead.

### Alternative 3 — Bespoke hand-written handler double per test
A separate non-generic double for each scenario.
**Rejected**: more types, duplicated recording logic; violates "don't add types without necessity" and lowers cohesion versus one generic recorder.

### Alternative 4 — Reuse the `Exported/` doubles directly (status quo for QueryProcessorTests)
Keep consuming `Exported.TestQueryA`/`TestQueryHandler` as in-test doubles.
**Rejected**: conflates the scannable-public role with the internal-double role and risks same-name/different-namespace confusion. The two-directory split (Decision 1) exists precisely to keep these roles distinct.

### Alternative 5 — Enhance `SimpleHandlerFactory` itself to record create/release per handler
Give the production `SimpleHandlerFactory` a recording data type that tracks, per handler, its create/release calls — so every test gets recording "for free" without a separate double.
**Rejected**: this pushes a test-only concern (interaction recording) into a production type, and adds a per-handler recording data structure that the overwhelming majority of uses neither need nor want — complexity that does not earn its weight. It would also be a `src/` change, which this tidy-first migration explicitly forbids. A specialized test double (Decision 4), scoped to the handful of tests that assert `Release`, keeps `SimpleHandlerFactory` minimal and the recording concern in the test project. (Subclassing is not even available as a shortcut: `Release` is not virtual.)

## References

- Requirements: [specs/007-use_simple_not_mocks/requirements.md](../../specs/007-use_simple_not_mocks/requirements.md)
- Related ADRs:
  - [ADR 0009: Simple and InMemory Factory Implementations](0009-simple-and-inmemory-factory-implementations.md) — provides the factory/registry doubles and the "behaviour not implementation" stance this migration extends.
  - [ADR 0008: Split Handler Interfaces](0008-split-handler-interfaces.md) — the sync/async split these doubles span.
  - [ADR 0004: Factory and Registry Abstractions](0004-factory-registry-abstractions.md) — the interfaces the doubles implement.
- Testing guidelines: `.agent_instructions/testing.md` (Test Doubles section); `CLAUDE.md` "Test Double Preference (Real > Simple > InMemory > Mock)".
- Brighter precedent: `tests/Paramore.Brighter.Core.Tests/CommandProcessors/TestDoubles/`.
- Target files: `test/Paramore.Darker.Core.Tests/{QueryProcessorTests,QueryProcessorAsyncTests,PipelineBuilderExceptionTests}.cs`, `…/Decorators/FallbackPolicyTests.cs`.
