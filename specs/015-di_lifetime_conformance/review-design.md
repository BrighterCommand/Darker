# Review: design — 015-di_lifetime_conformance

**Date**: 2026-10-10
**Threshold**: 60
**Verdict**: NEEDS WORK

3 findings at or above threshold 60. Address these before approving.

## Findings

### 1. Teardown cannot meet AC1 or NFR Reliability on the DI path, but 0022 claims it does (Score: 75)

ADR 0022. On the DI path the DI factory's `Release`/`ReleaseAsync` does nothing. The Scoped or Transient handler is disposed by Microsoft DI inside the scope's `Dispose`/`DisposeAsync`. `ServiceProviderEngineScope.Dispose` loops over its tracked disposables in reverse creation order and has no per-item try/catch. A handler's constructor dependencies (for example a `DbContext`) are created before the handler, so the container disposes the handler first. If that `Dispose` throws, the loop stops and the `DbContext` is never disposed. After ADR 0023 the lifetime tracks exactly one item, so 0022's per-item isolation in `QueryLifetimeScope` cannot help either.

So the first teardown AC ("A handler whose `Dispose` throws: decorators are still released and the scoped dependency is disposed") cannot hold on the path that matters most. The same goes for NFR Reliability ("no scope or `DbContext` leaks on any failure path in teardown"). The ADR's Negative section admits the limit in general terms. Its Positive section, its requirement mapping and its test plan still claim the guarantee.

**Evidence**:
- 0022:335: "The per-query scope is disposed on every teardown, so a teardown fault no longer leaks a `DbContext` (FR2, NFR Reliability)."
- 0022:165: "Acceptance criteria for teardown — rows 2, 3 and 6, on each path."
- 0022:321, step 3 test: "a handler whose `Dispose` throws still sees its decorators released and its Scoped dependency disposed". With Simple factories there is no Scoped dependency (0023 makes `CreatePipelineScope()` return null). With the DI factory the test fails for the reason above.
- 0022:346 (Negative) concedes "the container may leave the rest of that scope's services undisposed". This contradicts the two claims above.
- 0022:76: problem-table row 3 ("the lifetime's other tracked items") is moot once 0023 lands.

**Recommendation**: State the guarantee that actually holds. Darker's own sequence is isolated, and the scope's `Dispose` is always called. Isolation inside one Microsoft DI scope belongs to the container. Rewrite the Positive bullet and the AC mapping to match. Make the step 3 test name its factory: Simple factory for "decorators still released", DI factory only for "a Scoped service's `Dispose` throws, and the result still returns". Have the requirements AC1 and NFR Reliability reworded (or explicitly scoped to the Simple-factory path) before approval. Otherwise the spec carries an AC that the design knowingly cannot meet.

---

### 2. JoinAmbient breaks FR11 isolation, and the ADR understates which instances are shared (Score: 72)

ADR 0024. FR11 says "concurrent queries remain isolated from one another", without qualification. Under `JoinAmbient`, concurrent queries in one request share the request's Scoped instances, so FR11 no longer holds. Neither 0023 nor 0024 reconciles FR11 with adoption. 0023:186 claims FR11 through "two queries never share a lifetime, so they never share a scope", which is false for a borrowed scope.

The sharing is also wider than the ADR says. Decorators are registered with the handler lifetime (`ServiceCollectionDecoratorRegistry.cs:21`). They are closed over `IQuery<TResult>, TResult`, not over the concrete query type (`PipelineBuilder.cs:253`). So one Scoped `FallbackPolicyDecorator<IQuery<string>, string>` instance is shared by every query that returns `string` in the request, whatever the query type. That decorator is registered by default. It stores per-query state: `_exceptionTypes` from `InitializeFromAttributeParams` and `Context`, into whose `Bag` it writes. Two concurrent queries of different types can therefore silently swap exception filters and write the fallback cause into the wrong query's context. This is silent misbehaviour, not the loud EF Core concurrency exception the ADR describes. The only mitigation is documentation.

**Evidence**:
- 0024:538: "A Scoped handler or decorator instance is shared by every query of the same type in one request". For decorators the sharing key is `TResult`, not the query type.
- 0024:548, mitigation: "the documentation states ... concurrent queries need `AlwaysNew`".
- `src/Paramore.Darker/Policies/Handlers/FallbackPolicyDecorator.cs:16,18`: per-instance state.
- requirements.md:48: FR11.

**Recommendation**:
- Correct the Negative bullet to say that sharing is keyed on the decorator's closed type (`TResult`), and name the default `FallbackPolicyDecorator`.
- Add a Risk entry for silent cross-query state corruption, separate from the EF Core failure.
- Have FR11 reworded to "concurrent queries never share an owned scope" (matching the Concurrency NFR), and update 0023:186 to match.
- Consider a design mitigation beyond documentation, for example XML-doc warnings on `ScopeAffinity.JoinAmbient` and `AddDarkerRequestScope`, and a test that pins the shared-instance behaviour so it is a known contract.

---

### 3. FR17 is claimed as met, contradicted in Consequences, and the `ObjectDisposedException` wrap is too broad (Score: 65)

ADR 0024. The Scope and requirements-satisfied lists say that resolving from a borrowed scope that its owner has disposed throws `ConfigurationException`. The ADR itself then says the case it covers is almost unreachable:

- Every disposed Microsoft DI scope fails the probe with `ObjectDisposedException` and is declined (row 11).
- "The factory resolves nothing after the build" (0024:396), so disposal after the build surfaces as the user's own `ObjectDisposedException`.

That leaves a race between probe and `Create`, or a custom provider whose singleton resolution succeeds while handler resolution throws. Step 8's test needs a contrived double to reach it. Step 8 also catches `ObjectDisposedException` in "the factory's resolution" without limiting this to borrowed handles. A handler constructor that throws `ObjectDisposedException` for its own reasons, even on an owned scope, would then be re-typed as `ConfigurationException`. That is a behavioural change the ADR says it does not make (0024:58, "This ADR adds no breaking change").

**Evidence**:
- 0024:48: "Resolution from a borrowed scope that its owner has already disposed throws `ConfigurationException`".
- 0024:207: "FR17: the first two invariants, and the `ConfigurationException` in *Key Components*".
- 0024:542: "surfaces as `ObjectDisposedException` from the user's own services, not as Darker's `ConfigurationException`".
- 0024:511: step 8.

**Recommendation**: Accept the design and have FR17's second sentence reworded, for example: "A borrowed scope that is already disposed when the pipeline is built is declined and logged; disposal after the build is the owner's responsibility." Then either drop step 8 and the `ConfigurationException(string, Exception)` constructor, or limit the catch to resolution through a `BorrowedServiceProviderScope`. Make the Scope bullet and the requirements-satisfied line say exactly what holds.

---

### 4. Release notes miss the 0024 items its own step 11 promises, and two behavioural changes (Score: 48)

The spec 015 section exists with the right marker, and it covers every 0022 and 0023 breaking change. It does not list several things:

- what 0024 step 11 commits to recording: `DefaultScopeAffinity`, `AddDarkerScopeProvider`, `IAmAScopeProvider`/`ScopeAffinity`/`AmbientScopeSourceException`, the new `ConfigurationException` constructor, and the `JoinAmbient` concurrency caveat (Finding 2)
- 0022's behavioural change that Simple decorator factories now dispose decorators created before a failed build
- 0024 step 8's re-typing of `ObjectDisposedException` (if it stays)

**Evidence**: release_notes.md:10-23 has only a summary paragraph and "Breaking changes". 0024:520 lists the items to record.

**Recommendation**: Add an "Added" list for the 0024 surface, the concurrency caveat, and the partial-build release as a behavioural line, then regenerate through `/spec:write_release_notes`.

---

### 5. FR15 is narrowed without saying so, and the 0024 summary repeats the narrowing (Score: 45)

ADR 0024. FR15 says "Otherwise Darker falls back to an owned scope and logs a Warning once". Row 6 (provider returns null) logs nothing, and the Scope bullet quietly restates FR15 as "a fallback that signals a misconfiguration logs one Warning". The rationale is sound: null is the normal answer outside a request, so a Warning would be noise. But the requirement is not met as written. The frontmatter summary goes the other way and says every fallback warns: "otherwise it falls back to an owned scope with a once-per-container Warning". That is false for row 4 (`AlwaysNew`), row 6 (null) and row 1 (Singleton handlers get no scope). It is also three sentences, against the schema's one or two.

**Evidence**: 0024:46, 0024:163, 0024:8; requirements.md:55.

**Recommendation**: Have FR15 reworded to exempt "no ambient scope offered". Fix the summary to say that only misconfiguration fallbacks warn, and fit it in one or two sentences.

---

### 6. 0024 Scope has no bullet for new public API that no requirement names (Score: 42)

The skeleton requires a Scope bullet "for any scope no tagged requirement carries"; 0022 and 0023 both provide one. 0024 adds three things no FR names: the public `AddDarkerScopeProvider<TProvider>(affinity)`, the public `ConfigurationException(string, Exception)` constructor, and a change to the DI package's csproj `Description` (step 2). Only the first is justified, in Technology Choices. On open point 4: `AddDarkerScopeProvider` is necessary, because the affinity carrier is internal and FR19 needs a non-ASP.NET opt-in. The constructor exists only for step 8 (Finding 3).

**Evidence**: 0024:43-58, the In-scope list. 0024:469-471, 0024:496.

**Recommendation**: Add an In-scope bullet naming these items and why each is needed. Remove the constructor if Finding 3 drops step 8.

---

### 7. Partial-build release and release-through-the-creating-factory have no acceptance criteria (Score: 40)

ADR 0022 discloses both defect fixes honestly (Scope bullets at 0022:47-48, problem table rows 5-6). The "creating factory" change flips an existing test (`QueryProcessorAsyncTests.cs:24-29, :61`) and is a user-visible behaviour change that release_notes.md already records. Neither change is traceable to a requirement, so "All FRs covered by tests" will not catch a regression in either.

**Evidence**: requirements.md:98-103 has no AC for either case.

**Recommendation**: Add two ACs to requirements.md: "a decorator whose initialisation throws leaves earlier decorators released" and "on `ExecuteAsync`/`ExecuteStream` the handler is released by the factory that created it".

---

### 8. 0024 puts its invariants and requirement map under the wrong `####` heading (Score: 40)

The "load-bearing invariants" and "Requirements satisfied" blocks (0024:191-212) are the core of *The mechanism, end to end*. They sit under `#### Where the ambient scope is read`, a sub-topic about `IHttpContextAccessor`. `#### The corrected guidance` is documentation guidance, not mechanism, yet also sits there. A reader navigating by heading will not find the invariants.

**Evidence**: 0024:173, 182, 191, 214.

**Recommendation**: Move the invariants and requirement map up, directly after the decision-ladder table and its row-evaluation note, ahead of the `####` sub-sections. Move *The corrected guidance* to the end of Key Components or into Implementation step 11.

---

### 9. 0023's signed-test-project plan ignores the Core.Tests reference (Score: 38)

0023 step 5 says to sign `Paramore.Darker.Extensions.Tests` "as `test/Paramore.Darker.Extensions.Diagnostics.Tests` does". Diagnostics.Tests references no unsigned test project. Extensions.Tests references the unsigned `Paramore.Darker.Core.Tests`, which grants it `InternalsVisibleTo` by simple name (Core.Tests csproj:7). A signed assembly referencing an unsigned one raises CS8002. There is no `TreatWarningsAsErrors`, so it does not break the build, but the ADR's Negative bullet does not mention it. On open point 8: the approach is acceptable. Rows 3 and 4 cannot be reached through `AddDarker`, and the DI package is signed, so a signed friend is the only `InternalsVisibleTo` route.

**Evidence**: 0023:393; 0023:415. `test/Paramore.Darker.Extensions.Tests/*.csproj` (ProjectReference to Core.Tests); `test/Paramore.Darker.Core.Tests/*.csproj:7`.

**Recommendation**: In step 5, either also sign Core.Tests (and add the public key to its `InternalsVisibleTo`), or accept and suppress CS8002 in Extensions.Tests. Say which in the Negative bullet.

---

### 10. ADR 0014's partial-supersession note under-lists the amendments (Score: 35)

The note in 0014 says that 0022 amends Decision 5 and 0023 amends Decisions 1 and 3. The siblings' own References say more:

- 0022 also extends Decision 1 (`IAmALifetime : IAsyncDisposable`) and changes Decision 2's async release signature.
- 0023 also adds `CreatePipelineScope` to Decision 2's interfaces.

On open point 9, the handling is otherwise correct. 0014's Decision 4 claim ("independent of how many factory objects exist") is false today, because the table sits on each factory instance (0023:87). 0023 makes it true by moving the scope onto the lifetime, so no false claim survives implementation and 0014 can stay Accepted.

**Evidence**: 0014:23 against 0022:372 and 0023:440.

**Recommendation**: Make 0014's status note list Decisions 1, 2 and 5 for 0022 and Decisions 1, 2 and 3 for 0023. Optionally, add a clause to 0023's References saying it makes 0014 Decision 4 point 1 hold.

---

### 11. 0022's `ReleaseAsync` null-handler contract contradicts its own row 1 (Score: 25)

Row 1 says teardown skips the releases when no component was created. `_handler` is null only in that case (`PipelineBuilder.cs:69` assigns it only after a successful resolve). Yet the contract requires factories to "tolerate a null handler".

**Evidence**: 0022:143 against 0022:256.

**Recommendation**: Pick one rule. Either the builder never passes null (and the contract says so), or row 1 releases with null.

---

### 12. Minor accuracy slips in 0024 (Score: 20)

- 0024:86: "Default: `QueryProcessorLifetime = Singleton`, any handler lifetime → a new scope from the root". Singleton handlers get no scope.
- 0024:459: the adoption path "costs one small allocation". It costs two: the provider's `HttpRequestScope` and Darker's `BorrowedServiceProviderScope`.

**Recommendation**: Say "Scoped or Transient handler lifetime", and "two small allocations".

---

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 2 |
| 50-69 (Medium) | 1 |
| 0-49 (Low) | 9 |

**Total findings**: 12
**Findings at or above threshold (60)**: 3

## Verification log

**Diagrams**: All 8 mermaid blocks were extracted to the scratchpad and rendered with `npx -y -p @mermaid-js/mermaid-cli@11 mmdc`. Every one exited 0 and produced a non-empty SVG:
- 0022: sequence and flowchart
- 0023: sequence, flowchart and classDiagram
- 0024: sequence, flowchart and classDiagram

The two most complex (the 0024 sequence and the 0024 flowchart) were rendered to PNG at 1600 px and inspected. Both are legible. No flowchart exceeds about 4 decision points, and the 0024 adoption protocol is already a decision-ladder table.

**Escaped-markdown grep** (`&lt;|&gt;|&amp;`): 0022 = 0, 0023 = 0, 0024 = 0.

**Structure and frontmatter**: All three use the same canonical heading sequence. `### Where this ADR sits` is identical in all three, with the right row bolded and marked *(this one)*. The unifying sentence is identical in all three. Frontmatter `id` matches the filename stem, `status: Proposed` matches the body, tags come from the taxonomy (1–4 each), and docs/adr/index.md is in sync. No references to authoring-conversation participants or ephemeral state were found.

**Code references**: Every file:line reference checked matches the code at HEAD (PipelineBuilder, QueryProcessor, QueryLifetimeScope, IAmALifetime, async factory interfaces, ServiceProviderComponentFactory, ServiceCollectionExtensions, ServiceCollectionDecoratorRegistry, DarkerOptions, DI and core csproj, ApplicationLogging, FactoryFuncWrapper, RecordingHandlerFactory, QueryProcessorAsyncTests, CLAUDE.md, README, CONTRIBUTING, LoggerCaptureFixture, Caching csproj, Diagnostics.Tests signing, ADR 0014 Alternative D). The inventory of factory and lifetime implementers is complete.

**Open points**:
1. Provider not asked under `AlwaysNew`: acceptable. FR13 mandates the parameter; the deviation from Brighter is recorded in Technology Choices, Alternative 6 and Negative; the contract table tells provider authors they are asked only with `JoinAmbient`.
2. No Warning when the provider returns null: design right, requirement silently narrowed — Finding 5.
3. FR17: Finding 3 — accept the design, reword FR17, narrow or drop step 8.
4. New public API: `AddDarkerScopeProvider` necessary; the `ConfigurationException` constructor depends on Finding 3; both missing from Scope — Finding 6.
5. Concurrency caveat: not adequately addressed — Finding 2.
6. Probe's root test relies on MS DI internals: acceptable. Root and scope both answer `IServiceScopeFactory` with the root engine scope while `IServiceProvider` answers with the current scope; the planned tests fail the build if a container upgrade changes it.
7. Extra defects fixed in 0022: disclosed properly; add ACs — Finding 7.
8. `InternalsVisibleTo` plus signing: acceptable, but CS8002 unaddressed — Finding 9.
9. ADR 0014 Decision 4: handled correctly; 0023 makes the claim true. Status note under-lists amended decisions — Finding 10.
