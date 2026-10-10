# Review: design — 015-di_lifetime_conformance (round 2)

**Date**: 2026-10-10
**Threshold**: 60
**Verdict**: NEEDS WORK

1 finding at or above threshold 60. Address these before approving.

Round 1 findings are in git history (commit 6789f0d); all twelve are resolved — see the Verification log.

## Findings

### 1. 0024 now makes two decisions, but its decision sentence, its map row and its "unchanged" claims cover only one (Score: 62)

ADR 0024. The decorator registration change is the owner's decision and is not re-argued here. The problem is how the ADR records it. The change applies to every user with a Scoped `HandlerLifetime`, whether or not they opt in to an ambient scope, yet three places that define what 0024 decides leave it out.

- The bold Decision sentence (0024:119) covers adoption only. The decorator change appears only in the paragraph after it (0024:121).
- The `Where this ADR sits` row for 0024 (0024:79, and the identical rows at 0022:64 and 0023:67) says only "A Scoped handler may adopt an ambient scope … Darker disposes only scopes it created". A reader of 0022 or 0023 cannot learn from the map that 0024 changes the decorator registration lifetime, even though 0023:100 sends the reader to 0024 for exactly that.
- Two claims say behaviour is unchanged without the opt-in, and the ADR contradicts them:
  - The Positive bullet at 0024:553 says "Without the opt-in, or with `AlwaysNew`, behaviour and per-query cost are unchanged". The FR14 Scope bullet at 0024:45 says the factory "behaves as today".
  - 0024:396 and 0024:559 say that under an owned scope, a pipeline that applies the same decorator type twice "now gets two instances". The release notes list the change as behavioural (release_notes.md:25).
  - requirements.md:54 (FR14) has the same tension with FR25.

documentation.md (ADR readability) says: "If it will not fit in one sentence, it is not yet one decision".

**Evidence**:
- 0024:119: "**When the affinity is `JoinAmbient` and the handler lifetime is Scoped, the DI factory adopts … in every other case it creates and owns a scope as before.**"
- 0024:79: the map row does not mention decorators.
- 0024:553: "Without the opt-in, or with `AlwaysNew`, behaviour and per-query cost are unchanged".
- 0024:396: "Under an owned scope the change is visible only to a pipeline that applies the same decorator type twice: it now gets two instances".

**Recommendation**: State the decorator rule as part of the decision, either in the bold sentence or as an explicit second clause. Update the 0024 row of `Where this ADR sits` the same way in all three ADRs. Narrow 0024:553 and the FR14 Scope bullet to "adoption behaviour and per-query cost are unchanged", and name the duplicate-decorator exception once. Optionally, have FR14 carve out the decorator registration change.

---

### 2. Step 7's planned tests cannot be written as described (Score: 52)

ADR 0024, Implementation step 7.

- Its first bullet asks for "distinct `FallbackPolicyDecorator` instances, and each decorator's Scoped dependency is the ambient scope's instance". `FallbackPolicyDecorator` has no constructor dependencies, so there is no Scoped dependency to compare.
- The ADR gives no public-surface way to see which instance of a built-in decorator a pipeline received. The fourth bullet ("a decorator is the same instance across queries") has the same gap.
- `TrackedDecorator` records its dependency, not its own identity.

The acceptance criterion (requirements.md:124) can be reached, but only with a test double that does not exist yet.

**Evidence**: 0024:532 and 0024:535. `src/Paramore.Darker/Policies/Handlers/FallbackPolicyDecorator.cs:9-18` has no constructor, only `_exceptionTypes` and `Context`. In `test/Paramore.Darker.Extensions.Tests/TestDoubles/DecoratedTrackedQuery.cs`, `TrackedDecorator` records only `_dependency`.

**Recommendation**: Plan a recording decorator in `TestDoubles/` that takes a Scoped dependency and writes its own instance identity into the result. Use two query types that share one result type and the same decorator attribute. Name `FallbackPolicyDecorator` as the reason for the change, not as the subject under test.

---

### 3. The static, non-generic logger in `QueryLifetimeScope` makes the planned Error-log assertions depend on test order (Score: 50)

ADR 0022 (steps 2 and 3, and Risks) and ADR 0023 step 4. `QueryLifetimeScope` gets a static `ApplicationLogging.CreateLogger<QueryLifetimeScope>()` logger, and the tests assert that lifetime disposal faults are logged at Error.

- `PipelineBuilder<TResult>` is generic, so tests can isolate its logger by giving each test its own `TResult`.
- `QueryLifetimeScope` is not generic, so its logger is captured once per test process.
- If a test that registers `NullLoggerFactory` runs first, every later lifetime log goes to a null logger, and the assertion fails depending on test order.
- The Risk mitigation ("logger types are first used after registration") holds for one application, but not for a test process that builds many containers.

**Evidence**: 0022:323 and 0022:359. `src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionExtensions.cs:42-43`. `test/Paramore.Darker.Extensions.Tests/TestDoubles/TrackedDependencyScenario.cs:40`. The header comment in `test/Paramore.Darker.Extensions.Tests/Logging/LoggerCaptureFixture.cs` ("disjoint closed generic").

**Recommendation**: Say how the lifetime's logger stays testable. It could be created lazily per instance, passed in from `PipelineBuilder<TResult>`, or the DI-path assertion could use a dedicated `TResult`. Extend the Risk entry to cover test processes.

---

### 4. "No two queries share a decorator instance" is stated absolutely, but `TryAdd` keeps a user's own registration (Score: 35)

ADR 0024. A decorator type that the user registers as Scoped before Darker registers it keeps the user's registration, so it is shared across the request under `JoinAmbient`. The seventh invariant and the Positive bullet carry no qualification.

**Evidence**: `ServiceCollectionDecoratorRegistry.cs:21`; 0024:184 and 0024:559.

**Recommendation**: Narrow the claim to "decorators registered through Darker", or record the user-registration case under Negative.

---

### 5. The `AdoptionDiagnostics` latch has no concurrency contract (Score: 30)

ADR 0024. Rows 7 to 11 run per query, on concurrent threads, and must log "at most once per reason per container". Nothing says the latch is atomic.

**Evidence**: 0024:185, 0024:285 and 0024:518.

**Recommendation**: State that the latch is set atomically, and have the step 8 test run its queries concurrently.

---

### 6. "The processor calls `Dispose` only after `Build`" is false on the pre-build failure path (Score: 28)

ADR 0022. `using`, `await using` and the stream `finally` also dispose the builder when `_queryContextFactory.Create`, `InitQueryContext` or `CreateQuerySpan` throws before the build. In that case `_lifetime` and `_handler` are both null.

**Evidence**: 0022:275; `src/Paramore.Darker/QueryProcessor.cs:67-77`, `:104-116` and `:147-168`.

**Recommendation**: Replace the sentence with "teardown tolerates a builder that never built: no lifetime, no components", and add that case to row 1.

---

### 7. The release notes miss two smaller consumer-visible changes (Score: 28)

On the async paths, the Simple factories now await `DisposeAsync` on a component that implements `IAsyncDisposable`. A component that implements both interfaces previously received `Dispose` there. The DI package's NuGet `Description` also changes, which is minor.

**Evidence**: 0022:262; 0024:517; release_notes.md:15-32.

**Recommendation**: Add a behavioural line for the Simple factories' async disposal.

---

### 8. The migration advice for `IAmALifetime.Add` does not work for custom decorator factories (Score: 25)

ADR 0023 and release_notes.md. The advice is to return disposables inside the handle from `CreatePipelineScope()`, but only handler factories have that member.

**Evidence**: 0023:411; release_notes.md:23.

**Recommendation**: Say "a custom handler factory…", and add that decorator factories dispose their own per-query resources in `Release` or `ReleaseAsync`.

---

### 9. 0022's `Where each type is touched` omits the test doubles that its step 5 changes (Score: 15)

**Evidence**: 0022:326 against 0022:279-288; compare 0023:345.

**Recommendation**: Add a `test` row to 0022's table.

---

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 3 |
| 0-49 (Low) | 6 |

**Total findings**: 9
**Findings at or above threshold (60)**: 1

## Verification log

**Diagrams**: all 8 mermaid blocks rendered without errors:
- 0022: sequence and flowchart
- 0023: sequence, flowchart and classDiagram
- 0024: sequence, flowchart and classDiagram

0024's sequence diagram and flowchart were also rendered to PNG at 1600px and inspected, and both are legible. No flowchart encodes branching. 0024's adoption protocol is a 12-row decision table.

**Escaped markdown**: 0022 = 0, 0023 = 0, 0024 = 0.

**Structure and frontmatter**:
- Headings follow the canonical skeleton, in the same order and nesting, in all three ADRs.
- `Where this ADR sits` and the unifying sentence are identical in all three.
- Each frontmatter `id` matches its filename, `status` matches the body, tags come from the taxonomy, and each summary is 1–2 sentences.
- index.md matches the frontmatter.
- ADR 0014's status note is correct.

**Tone**: no references to the authoring conversation or to ephemeral state.

**Internal and code references**: all row, step and invariant references are correct, and every cited file:line matches HEAD.

**Testing plan**: no plan uses `InternalsVisibleTo` or signs a test project. Every planned test goes through the public surface. The testability gaps are Findings 2 and 3.

**Requirements coverage**: every FR, NFR and AC is claimed by at least one ADR.

**Round 1 findings**:

| # | Round 1 finding | Status |
|---|---|---|
| 1 | Teardown AC1 / NFR Reliability on the DI path | resolved |
| 2 | JoinAmbient vs FR11, decorator sharing | resolved (test feasibility: Finding 2) |
| 3 | FR17 / `ObjectDisposedException` wrap | resolved |
| 4 | Release notes gaps | resolved (residue: Finding 7) |
| 5 | FR15 narrowed silently; summary | resolved |
| 6 | 0024 Scope bullet for API no requirement names | resolved |
| 7 | ACs for partial build and creating factory | resolved |
| 8 | Invariants under the wrong `####` | resolved |
| 9 | Signed test project / CS8002 | resolved |
| 10 | 0014 status note under-lists amendments | resolved |
| 11 | `ReleaseAsync` null-handler contract | resolved |
| 12 | 0024 accuracy slips | resolved |
