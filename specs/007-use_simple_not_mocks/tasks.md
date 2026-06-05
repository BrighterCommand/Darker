# Tasks — Spec 007: use_simple_not_mocks

> Migrate four legacy test files off Moq and inline nested doubles onto ADR 0009
> Simple\*/InMemory\* doubles + new recording doubles (ADR 0013), then delete Moq.
>
> **Nature of this work — read before starting.** This is a **structural-only,
> test-only, tidy-first** change (NFR1, NFR4). No `src/` changes. There are **no
> new behaviours** to drive out, so the normal `/test-first` TDD gate does **not**
> apply: correctness is proven by the **existing suite staying green** before and
> after each step (requirements "Testing approach"). Each task therefore uses
> **`/tidy-first`** framing and a **green-suite gate**, not `/test-first`.
>
> **Tidy First discipline:** never mix structural and behavioural change in one
> commit, and here *every* commit is structural. Keep the suite green at every
> step; commit in the small, ordered units below (structural-before-behavioural
> ordering = extract/add doubles before rewiring the consuming tests).

Linked issue: [#306](https://github.com/BrighterCommand/Darker/issues/306) ·
ADR: `docs/adr/0013-migrate-legacy-tests-to-test-doubles.md` (Accepted) ·
Requirements: `requirements.md` (FR1–FR10, NFR1–NFR4, AC1–AC14)

## Build / test commands

```bash
dotnet build Darker.Filter.slnf -c Release
dotnet test  Darker.Filter.slnf -c Release
```

## Conventions for every task below

- New/extracted doubles live in `test/Paramore.Darker.Core.Tests/TestDoubles/`,
  namespace `Paramore.Darker.Core.Tests.TestDoubles`, one type per file, with the
  standard MIT licence header used by sibling files in that directory.
- `Exported/` is **read-only** except for its new `README.md` (FR5, AC8).
- After each task: build + full test run must be **green at the AC11 baseline
  count** (AC12). Commit that task before starting the next (Tidy First).
- Doubles must be reused where one already covers the need (FR6); no
  `TestDoubles/` type may share a simple name with an `Exported/` type (FR6, AC7).

---

## Task 0 — Record the green-suite baseline (AC11)

- [x] **STRUCTURAL PRE-STEP: capture the passing-test baseline before any change**
  - Run `dotnet build Darker.Filter.slnf -c Release` then
    `dotnet test Darker.Filter.slnf -c Release`.
  - Record the **passing test count** for `Paramore.Darker.Core.Tests` and for the
    full filtered solution into `specs/007-use_simple_not_mocks/.baseline-test-count`
    (and note it here in tasks.md). This is the number AC12 compares against.
  - Confirm the pre-migration Moq footprint matches the spec's assumption:
    `grep -rl 'Mock<' test/Paramore.Darker.Core.Tests/` lists exactly the four
    target files.
  - **Gate**: suite green; baseline number written down.
  - **Satisfies**: AC11.

---

## Task 1 — Document the `Exported/` directory role (FR9) — structural

- [x] **STRUCTURAL: add `Exported/README.md` explaining the scanning role**
  - Create `test/Paramore.Darker.Core.Tests/Exported/README.md`.
  - Content must state: this directory holds the **public** test doubles that exist
    to be discovered by assembly/handler **scanning** (e.g. `AddHandlersFromAssemblies`);
    it is distinct from `TestDoubles/` (internal doubles used directly by tests);
    doubles here MUST NOT be renamed/removed without checking scanning-based tests.
  - No `.cs` change in `Exported/` (AC8 must still pass).
  - **Gate**: build + full test run green at baseline.
  - **Satisfies**: FR9, AC10. **Commit** (e.g. `docs: add Exported/ README (#306)`).

---

## Task 2 — Extract `FallbackPolicyTests` nested doubles to `TestDoubles/` (FR3) — structural

- [x] **STRUCTURAL: move FallbackPolicyTests inline doubles into `TestDoubles/`**
  - **USE**: `/tidy-first extract FallbackPolicyTests nested doubles to TestDoubles`
  - Move **verbatim** (closed list, FR3) — one file each, namespace
    `Paramore.Darker.Core.Tests.TestDoubles`:
    - `TestQuery` (with its nested `Result`)
    - `TestQueryHandlerWithCatchAllFallback`
    - `TestQueryHandlerWithFormatExceptionFallback`
    - `TestQueryHandlerWithoutFormatExceptionFallback`
  - Name check: `TestQuery` must not collide with any `Exported/` simple name
    (Exported has `TestQueryA/B/C`, `TestQueryHandler[Async]` — no collision; AC7).
  - In `FallbackPolicyTests.cs`: delete the nested class declarations and add
    `using Paramore.Darker.Core.Tests.TestDoubles;`. **Do not** touch the Moq usage
    yet — factory/registry swap is Task 6. This task is pure extraction; assertions
    and `[Fact]` count unchanged (AC13).
  - **Gate**: build + full test run green at baseline (the still-mocked test now
    references the extracted doubles). **No** `class` declarations for doubles remain
    nested in the file (AC6, partial).
  - **Satisfies**: FR3 (extraction half), FR5, AC6, AC7. **Commit**.

---

## Task 3 — Extract `PipelineBuilderExceptionTests` nested doubles to `TestDoubles/` (FR4) — structural

- [x] **STRUCTURAL: move PipelineBuilderExceptionTests inline doubles into `TestDoubles/`**
  - **USE**: `/tidy-first extract PipelineBuilderExceptionTests nested doubles to TestDoubles`
  - Move **verbatim** (closed list, FR4) — each `Result` moves with its owning query:
    - `ExceptionQuery` (+ `Result`), `ExceptionQueryHandler`
    - `FallbackExceptionQuery` (+ `Result`), `FallbackExceptionQueryHandler`
    - `NullInnerExceptionQuery`, `NullInnerExceptionQueryHandler`
    - `DecoratorExceptionQuery` (+ `Result`), `DecoratorExceptionQueryHandler`
    - `TestExceptionDecorator<TQuery, TResult>`
    - `DecoratorExceptionAttribute`
  - These are currently `private` nested types — promote to `internal` (or `public`
    as needed by `DecoratorExceptionAttribute`/decorator generics) so they resolve
    from `TestDoubles/`. Keep behaviour identical; do not add recording.
  - Name check: none of these collide with `Exported/` simple names (AC7).
  - In `PipelineBuilderExceptionTests.cs`: delete the nested declarations, add the
    `using ...TestDoubles;`. **Do not** touch Moq yet (factory/registry swap is Task 7).
  - **Gate**: build + full test run green at baseline; no nested double `class`
    declarations remain in the file (AC6, partial).
  - **Satisfies**: FR4 (extraction half), FR5, AC6, AC7. **Commit**.

---

## Task 4 — Add the recording handler doubles to `TestDoubles/` (FR10, Decision 3) — structural

- [x] **STRUCTURAL: add generic recording query-handler doubles (sync + async)**
  - **USE**: `/tidy-first add RecordingQueryHandler sync and async test doubles`
  - Add to `TestDoubles/` (illustrative names per ADR 0013 Decision 3 — final names
    are an implementation detail, but must not collide with `Exported/` simple names):
    - `RecordingQueryHandler<TQuery, TResult> : QueryHandler<TQuery, TResult>` —
      ctor takes `Func<TQuery, TResult> execute`; overrides `Execute` to record
      `ExecuteCount++`, `LastQuery = query`, then return/throw via the delegate;
      overrides `Fallback` to record `FallbackCount++` then `return base.Fallback(query)`.
      Exposes `ExecuteCount`, `FallbackCount`, `LastQuery`.
    - `RecordingQueryHandlerAsync<TQuery, TResult> : QueryHandlerAsync<TQuery, TResult>` —
      mirrors against the async base; overrides `ExecuteAsync` (abstract) and
      `FallbackAsync` (virtual); ctor takes a delegate producing the result (sync
      `Func` wrapped in `Task.FromResult`, or `Func<TQuery, Task<TResult>>`); threads
      the `CancellationToken` to the delegate. The token is invariantly `default`
      and is **not** recorded (FR10 — behaviour-equivalent).
  - **The `CancellationToken` is behaviour-irrelevant post-migration:** no migrated
    test asserts on it (the pre-migration `default(CancellationToken)` arguments in
    `QueryProcessorAsyncTests.cs:75-78,88,97` are dropped per FR10, and the migrated
    tests prove behaviour via the awaited **result** and `FallbackCount` instead). Do
    **not** record or re-assert the token — doing so would reintroduce an interaction
    check this migration is removing.
  - These compile against `src/` base classes (`QueryHandler`/`QueryHandlerAsync`);
    no `src/` change (AC9). Not yet referenced by any test — this task only adds the
    doubles.
  - **Gate**: build + full test run green at baseline (new unused types compile).
  - **Satisfies**: FR10 (handler-recording double), groundwork for FR1/FR2. **Commit**.

---

## Task 5 — Add the recording handler-factory, recording decorator-factory + distinct query/handler doubles (FR10 D4, FR6) — structural

- [x] **STRUCTURAL: add `RecordingHandlerFactory`, `RecordingDecoratorFactory`, and the renamed QueryProcessor query/handler doubles**
  - **USE**: `/tidy-first add RecordingHandlerFactory, RecordingDecoratorFactory and distinct QueryProcessor doubles`
  - Add to `TestDoubles/`:
    - `RecordingHandlerFactory : IQueryHandlerFactory, IQueryHandlerFactoryAsync`
      (one class implements **both** — interfaces are identical, no `CreateAsync`):
      ctor takes `Func<Type, IQueryHandler> create`; `Create(Type)` delegates;
      `Release(handler)` records the handler (list of released handlers). Expose
      `ReleaseCount(IQueryHandler h)` (reference-equality count) and `Released`
      (the recorded list, so e.g. `Released.OfType<ExceptionQueryHandler>().Count()`
      works for Task 9) for the `Times.Once/Never` → `ShouldBe(1)/ShouldBe(0)`
      translation. May still dispose on `Release` to preserve `SimpleHandlerFactory`
      semantics.
    - `RecordingDecoratorFactory : IQueryHandlerDecoratorFactory, IQueryHandlerDecoratorFactoryAsync`
      — a `SimpleHandlerDecoratorFactory`-equivalent that **also records `Release`**.
      Needed because FallbackPolicyTests asserts `_decoratorFactory.Verify(Release<T>(decorator), Times.Once)`
      ×3 and `SimpleHandlerDecoratorFactory.Release<T>` records nothing
      (`SimpleHandlerDecoratorFactory.cs:50-54`). Mirror `SimpleHandlerDecoratorFactory`'s
      generic `Create<T>(Type)`/`Release<T>(T)` shape (verify the exact signatures in
      that file), delegate `Create` to a ctor `Func`, and record released decorators so
      `ReleaseCount(decorator).ShouldBe(1)` re-expresses the three `Verify`s as state.
      This fills a gap in ADR 0013 (whose Decision 4 names only a recording **handler**
      factory); see the ADR addendum recorded for this spec. **Only FallbackPolicyTests
      (Task 8) needs it** — PipelineBuilderExceptionTests (Task 9) has **no**
      decorator-`Release` assertion and uses plain `SimpleHandlerDecoratorFactory`.
    - **Distinct-named query + handler doubles** to replace the `Exported` types the
      two QueryProcessor files currently consume (FR6 — copy-and-rename, distinct
      simple names so AC4 whole-word search and AC7 collision check both pass). Need
      (per the migrated assertions):
      - a query carrying an `Id` (analogue of `Exported.TestQueryA`, returns `Guid`)
        whose recording handler **echoes `query.Id`** (so `result.ShouldBe(id)` proves
        the matching handler ran with the right query — preferred outcome assertion);
      - a second query/result type (analogue of `TestQueryB`, returns `int`) used as
        the non-matching handler in `ExecutesTheMatchingHandler`.
      - For `ExecutesQueries`, a handler that adds `"id" → query.Id` to the context
        bag **and** returns `query.Id` (matches current `TestQueryHandler` behaviour so
        the `Context.Bag` assertions on lines 41–42 / 51–52 still hold). **This needs a
        dedicated double whose `Execute`/`ExecuteAsync` override writes the bag** — the
        Task 4 `RecordingQueryHandler` cannot serve here: its `Func<TQuery, TResult>`
        delegate receives only the query and has **no access to `Context`**, so it can
        echo `query.Id` as the result but cannot write `"id"` to the bag. Use a small
        dedicated handler double (overriding `Execute`/`ExecuteAsync` to write the bag
        and return `query.Id`) for the two `ExecutesQueries` tests; the Task 4 generic
        recorder is for the `ExecutesTheMatchingHandler` / exception scenarios.
    - Confirm **no** new simple name equals any `Exported/` name `TestQueryA/B/C`,
      `TestQueryHandler`, `TestQueryHandlerAsync` (AC4 whole-word, AC7).
  - **Gate**: build + full test run green at baseline (new types unused so far).
  - **Satisfies**: FR10 (recording factory), FR6 (distinct doubles). **Commit**.

---

## Task 6 — Migrate `QueryProcessorTests.cs` off Moq (FR1, FR7, FR10) — behaviour-preserving rewire

- [ ] **STRUCTURAL (test-rewire): replace all Moq in QueryProcessorTests with doubles, result-first**
  - **USE**: `/tidy-first migrate QueryProcessorTests off Moq to recording doubles`
  - Constructor: replace `Mock<IQueryHandlerFactory>` → `RecordingHandlerFactory`
    (delegate returns the per-test handler); `Mock<IQueryHandlerDecoratorFactory>` →
    `SimpleHandlerDecoratorFactory`; `Mock<IQueryHandlerDecoratorRegistry>` →
    `InMemoryDecoratorRegistry`.
  - Per `[Fact]`, translate each Moq assertion **result-first**, recorded-state only
    where the result can't show it (ADR 0013 Decision 3/4 table):
    - `ExecutesQueries`: keep `result.ShouldBe(id)` and the two `Context.Bag`
      assertions; `Verify(Release(handler), Times.Once)` → `factory.ReleaseCount(handler).ShouldBe(1)`.
    - `ExecutesTheMatchingHandler`: `handlerA.Execute(... q.Id==id) Times.Once` →
      assert the **returned result** equals `id` (echoing handler);
      `handlerA.Fallback Times.Never` → `handlerA.FallbackCount.ShouldBe(0)`;
      `handlerB.Execute Times.Never` → `handlerB.ExecuteCount.ShouldBe(0)`;
      `handlerB.Fallback Times.Never` → `handlerB.FallbackCount.ShouldBe(0)`;
      `Release(handlerA) Times.Once` → `ReleaseCount(handlerA).ShouldBe(1)`;
      `Release(handlerB) Times.Never` → `ReleaseCount(handlerB).ShouldBe(0)`.
    - `ExceptionsDontCauseFallbackByDefault`: recording handler whose `execute`
      delegate **throws** `FormatException`; keep `Assert.Throws<FormatException>`;
      `Fallback Times.Never` → `handlerA.FallbackCount.ShouldBe(0)`;
      `Release(handlerA) Times.Once` → `ReleaseCount(handlerA).ShouldBe(1)`.
  - Remove `using Moq;` and `using Paramore.Darker.Core.Tests.Exported;`; add the
    `TestDoubles` using. Reference only the renamed doubles (no `Exported` type; AC4).
  - **No** new/removed/renamed `[Fact]` (AC13); no `.Verify(` / `Times.` / `It.`
    tokens remain (AC5).
  - **Gate**: build + full test run green at baseline. `grep 'Mock<\|using Moq'`
    on this file returns nothing (AC1); whole-word `Exported` check clean (AC4).
  - **Satisfies**: FR1, FR7, FR10, AC2, AC3, AC4, AC5. **Commit**.

---

## Task 7 — Migrate `QueryProcessorAsyncTests.cs` off Moq (FR2, FR7, FR10) — behaviour-preserving rewire

- [ ] **STRUCTURAL (test-rewire): replace all Moq in QueryProcessorAsyncTests with async doubles, result-first**
  - **USE**: `/tidy-first migrate QueryProcessorAsyncTests off Moq to recording doubles`
  - Constructor: replace the mocks as follows. **CRITICAL — two distinct handler-factory
    instances, not one.** The async `ExecutesQueries` `Release ... Times.Never`
    assertion passes today **only because** the sync slot (`_handlerFactory`) and the
    async slot (`_handlerFactoryAsync`) are *separate* instances: the async pipeline
    **creates** the handler via the async factory (`PipelineBuilder.cs:191`) but
    **releases** it via the **sync** slot (`PipelineBuilder.cs:274` —
    `_handlerFactory?.Release(_handler)`). So the *async* factory's `Release` is
    genuinely never called. Wire a **separate** `RecordingHandlerFactory` into each of
    the sync and async `HandlerConfiguration` slots — do **not** collapse to one shared
    instance (a shared instance would record `ReleaseCount == 1` via the sync slot and
    fail the `ShouldBe(0)` gate). For the decorator factory and registry, a **single**
    `SimpleHandlerDecoratorFactory` + `InMemoryDecoratorRegistry` instance *can* serve
    both sync and async slots (ADR 0009 — those carry no per-instance assertion). Keep
    the dual sync/async `HandlerConfiguration` ctor shape.
  - Translate assertions with `RecordingQueryHandlerAsync` doubles, result-first:
    - `ExecutesQueries`: keep `result.ShouldBe(id)` + the two `Context.Bag`
      assertions. **Preserve the asymmetry** (ADR 0013 D4 watch-out) — it is a
      slot/instance distinction, not just a count value:
      `Verify(Release(handler), Times.Never)` → assert on the **async** factory instance:
      `asyncFactory.ReleaseCount(handler).ShouldBe(0)` (async asserts **Never**, unlike
      the sync file's **Once** — do NOT tidy to match, and do NOT assert on the sync
      instance, which does see the release).
    - `ExecutesTheMatchingHandler`: `ExecuteAsync(... q.Id==id) Times.Once` →
      assert the awaited **result** equals `id`; the three `Times.Never` checks →
      `FallbackCount`/`ExecuteCount`/`FallbackCount` `.ShouldBe(0)` on the respective
      doubles. `CancellationToken default` is not recorded/asserted (FR10).
    - `ExceptionsDontCauseFallbackByDefault`: async recording handler whose delegate
      throws `FormatException`; keep `Assert.ThrowsAsync<FormatException>`;
      `FallbackAsync Times.Never` → `FallbackCount.ShouldBe(0)`.
  - Remove `using Moq;` and the `Exported` using; reference only renamed doubles (AC4).
  - **No** `[Fact]` count change (AC13); no Moq tokens remain (AC5).
  - **Gate**: build + full test run green at baseline; AC1/AC4 greps clean on this file.
  - **Satisfies**: FR2, FR7, FR10, AC2, AC3, AC4, AC5. **Commit**.

---

## Task 8 — Migrate `FallbackPolicyTests.cs` off Moq (FR3 factory swap) — behaviour-preserving rewire

- [ ] **STRUCTURAL (test-rewire): swap FallbackPolicyTests factory/registry mocks for Simple\*/InMemory\***
  - **USE**: `/tidy-first migrate FallbackPolicyTests factories off Moq`
  - **Depends on Task 5** (`RecordingHandlerFactory` + `RecordingDecoratorFactory`) and
    Task 2 (extracted doubles).
  - Replace `Mock<IQueryHandlerFactory>` → `RecordingHandlerFactory` (its three
    tests assert `Release(handler) Times.Once` → `factory.ReleaseCount(handler).ShouldBe(1)`).
  - Replace `Mock<IQueryHandlerDecoratorFactory>` → **`RecordingDecoratorFactory`**
    (Task 5). The three `_decoratorFactory.Verify(Release<T>(decorator), Times.Once)`
    assertions (`FallbackPolicyTests.cs:51,77,102`) → `decoratorFactory.ReleaseCount(decorator).ShouldBe(1)`.
    **Decision (resolved with user, 2026-06-05):** preserve these decorator-`Release`
    assertions as state via the recording decorator factory — do **not** drop them
    (NFR1). This fills the ADR 0013 gap; see the ADR addendum for this spec.
  - Replace `Mock<IQueryHandlerDecoratorRegistry>` → `InMemoryDecoratorRegistry`.
  - Keep all `result`/`Context.Bag` outcome assertions exactly as-is (they already
    assert behaviour, not interactions). Only the `.Verify(Release...)` lines change
    to state reads. Uses the doubles extracted in Task 2.
  - Remove `using Moq;`. No `[Fact]` count change (AC13); no Moq tokens remain.
  - **Gate**: build + full test run green at baseline; AC1 grep clean on this file.
  - **Satisfies**: FR3 (factory-swap half), FR7, AC1, AC2. **Commit**.

---

## Task 9 — Migrate `PipelineBuilderExceptionTests.cs` off Moq (FR4 factory swap) — behaviour-preserving rewire

- [ ] **STRUCTURAL (test-rewire): swap PipelineBuilderExceptionTests factory/registry mocks for Simple\*/InMemory\***
  - **USE**: `/tidy-first migrate PipelineBuilderExceptionTests factories off Moq`
  - Replace `Mock<IQueryHandlerFactory>` → `RecordingHandlerFactory` (the
    `ShouldPreserveOriginalException...` test asserts
    `Release(It.IsAny<ExceptionQueryHandler>()) Times.Once` → assert exactly one
    released handler of that type, e.g. `factory.Released.OfType<ExceptionQueryHandler>().Count().ShouldBe(1)`
    or the `ReleaseCount` of the created instance — keep the created handler in a
    local so it can be asserted on);
    `Mock<IQueryHandlerDecoratorFactory>` → **plain `SimpleHandlerDecoratorFactory`**.
    Note: this file has **no** `_decoratorFactory.Verify(...)` calls (verified — only
    `Create` setups and `decoratorRegistry.Setup(Register(...))`), so it does **not**
    need `RecordingDecoratorFactory`; the decorator-`Release` recording concern is
    exclusive to FallbackPolicyTests (Task 8).
    `Mock<IQueryHandlerDecoratorRegistry>` → `InMemoryDecoratorRegistry`. The two
    `decoratorRegistry.Setup(Register(...))` calls become real `Register(...)` calls
    on `InMemoryDecoratorRegistry`.
  - Keep the exception-message outcome assertions exactly as-is. Uses the doubles
    extracted in Task 3.
  - Remove `using Moq;`. No `[Fact]` count change (AC13); no Moq tokens remain.
  - **Gate**: build + full test run green at baseline; AC1 grep clean on this file.
  - **Satisfies**: FR4 (factory-swap half), FR7, AC1, AC2. **Commit**.

---

## Task 10 — Remove the dead Moq dependency (FR8) — structural

- [ ] **STRUCTURAL: delete Moq package references now that no test uses it**
  - **USE**: `/tidy-first remove Moq package references`
  - Delete `<PackageReference Include="Moq" />` from
    `test/Paramore.Darker.Core.Tests/Paramore.Darker.Core.Tests.csproj` (line ~22).
  - Delete `<PackageVersion Include="Moq" Version="4.20.72" />` from
    `Directory.Packages.props` (line ~15).
  - **Gate**: `dotnet build Darker.Filter.slnf -c Release` succeeds and
    `dotnet test Darker.Filter.slnf -c Release` is green at the AC11 baseline (AC12).
  - **Satisfies**: FR8. **Commit**.

---

## Task 11 — Final acceptance verification (AC1–AC14)

- [ ] **VERIFY: run the full acceptance-criteria checklist and record the result**
  - Build + full test run green at the **AC11 baseline** count, zero failures (AC12, NFR2).
  - `grep -rl 'Mock<' test/Paramore.Darker.Core.Tests/` → empty;
    `grep -rl 'using Moq' test/Paramore.Darker.Core.Tests/` → empty (AC1).
  - AC14 both empty:
    - `grep -rliE 'moq' --include='*.cs' --include='*.csproj' --include='*.props' --exclude-dir=bin --exclude-dir=obj test src`
    - `grep -liE 'moq' Directory.Packages.props`
  - No `.Verify(` / `Times.` / `It.` tokens in either QueryProcessor file (AC5).
  - Whole-word `grep -nwE 'TestQueryA|TestQueryB|TestQueryC|TestQueryHandler|TestQueryHandlerAsync'`
    over the two QueryProcessor files → no matches; no `using ...Exported;` in them (AC4).
  - No nested double `class` decls remain in the four target files; every FR3/FR4
    closed-list type (incl. `Result` types) exists under `TestDoubles/` (AC6).
  - No `TestDoubles/` type shares a simple name with an `Exported/` type (AC7).
  - `git diff` shows **no** change to the five `Exported/*.cs` files (AC8) and **no**
    change under `src/` (AC9).
  - `Exported/README.md` exists and explains the scanning role + `TestDoubles/`
    distinction (AC10).
  - In each target file the `[Fact]`/`[Theory]` count is unchanged vs pre-migration
    (AC13).
  - Record pass/fail of AC1–AC14 in the spec (and tick the Definition of Done).
  - **Satisfies**: AC1–AC14 verification, Definition of Done.

---

## Dependency / ordering summary

```
Task 0 (baseline)
   │
   ├─ Task 1 (Exported README)            ── independent
   ├─ Task 2 (extract FallbackPolicy)     ──┐
   ├─ Task 3 (extract PipelineException)  ──┤ structural extractions, suite stays green
   ├─ Task 4 (recording handler doubles)  ──┤
   └─ Task 5 (recording factory + renamed doubles)
            │
            ├─ Task 6 (QueryProcessorTests)      needs 4 + 5
            ├─ Task 7 (QueryProcessorAsyncTests) needs 4 + 5 (two factory instances)
            ├─ Task 8 (FallbackPolicyTests)      needs 2 + 5 (RecordingDecoratorFactory)
            └─ Task 9 (PipelineBuilderException) needs 3 (plain SimpleHandlerDecoratorFactory)
                     │
                     └─ Task 10 (remove Moq)  needs 6,7,8,9 (last Moq consumer gone)
                              │
                              └─ Task 11 (verify AC1–AC14)
```

## Risk-mitigation notes (from ADR 0013 / PROMPT.md watch-outs)

- **Async/sync `Release` asymmetry** (Task 6 vs 7): the async pipeline releases via
  the **sync** factory slot (`PipelineBuilder.cs:274`), so use **two separate**
  `RecordingHandlerFactory` instances in Task 7 and assert on the **async** one
  (`ReleaseCount == 0`); sync (Task 6, one slot) → `ReleaseCount == 1`. Do **not**
  collapse to one instance or tidy the counts to match (NFR1).
- **Decorator-`Release` is FallbackPolicyTests-only** (Task 8): its three
  `_decoratorFactory.Verify(Release<T>(decorator), Times.Once)` calls are preserved as
  state via `RecordingDecoratorFactory` (Task 5) — resolved with the user 2026-06-05,
  not dropped (NFR1). PipelineBuilderExceptionTests (Task 9) has **no** such assertion
  and uses plain `SimpleHandlerDecoratorFactory`. This fills a gap in ADR 0013, whose
  Decision 4 named only a recording **handler** factory (see ADR addendum).
- **Result-first, record only the gaps** (FR10): prefer `result.ShouldBe(id)`;
  use recorded counts only for negatives, exception paths, and `Release` counts.
- **`Exported/` is sacred** (AC8): touch only its new README.
- **No `src/` change** (AC9); **no new/renamed `[Fact]`** (AC13).
- Never commit `docs/.DS_Store` (untracked macOS junk).
```
