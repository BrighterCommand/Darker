# Review: tasks — 006-json_serializer

**Date**: 2026-06-01
**Threshold**: 60
**Verdict**: NEEDS WORK

## Findings

### 1. AOT csproj does NOT have `<PublishAot>true</PublishAot>` — Step 9's entire verification is hollow (Score: 95)

Step 9 of the tasks asserts that `dotnet publish test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj -c Release -f net8.0` and `-f net9.0` will "succeed" with no `IL2xxx` / `IL3xxx` warnings outside the FR13 allow-list. This is the load-bearing verification for AC4 and FR13. The problem: `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj` does **not** set `<PublishAot>true</PublishAot>` (verified — the csproj only has `<TargetFrameworks>`, `<IsPackable>false</IsPackable>`, `<Nullable>enable</Nullable>`; `grep -rn "PublishAot\|IsAotCompatible" test/Paramore.Darker.Tests.AOT/ src/` returns nothing). Without it, `dotnet publish` produces a normal IL publish, the AOT analyzer never runs, and no `IL2026` / `IL3050` warnings will fire **at all**. Step 9's verification will pass vacuously — and the supposed AC4 guard against new BCL warning categories will silently produce false negatives.

The task says implementation "should ensure `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj` continues to publish with `PublishAot=true`" — but that property has never been set. There is no "continues" path; the property must be **added** as part of this task. The task wording presumes a state that does not exist.

**Evidence**: `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj` lines 1–29 contain no `<PublishAot>` property. `grep -rn "PublishAot\|IsAotCompatible" test/ src/` returns no hits.

**Recommendation**: Add an explicit structural sub-task to Step 9 (or fold into Step 2) that sets `<PublishAot>true</PublishAot>` on `Paramore.Darker.Tests.AOT.csproj` for both TFMs, plus the usual companion properties (`<PublishTrimmed>true</PublishTrimmed>` or `<IsTrimmable>true</IsTrimmable>` on `src/Paramore.Darker/Paramore.Darker.csproj` if AC4 is to mean anything). Without this, AC4 is not actually verified and Step 13's "Run the published AOT binary — both FR11 subtests pass" is not actually AOT-binary execution.

---

### 2. Pre-flight commit is described as already-staged but is actually still untracked, and tasks.md is itself untracked (Score: 88)

The pre-flight task says: "*Branch `feature/json-serializer` already carries the ADR file and `specs/006-json_serializer/` content as untracked / pending changes. Stage and commit `docs/adr/0012-json-serializer-swap.md`, the `specs/006-json_serializer/` directory, and `specs/.current-spec`.*" with verify "*shows the ADR commit as the first commit on the branch*". But `git log master..feature/json-serializer` returns **empty** — there are zero commits ahead of master on this branch. `git status` shows `docs/adr/0012-json-serializer-swap.md` and `specs/006-json_serializer/` both still untracked, and `specs/.current-spec` modified.

Critically: the ADR was **amended after the design review** (the prompt confirms this; `review-design.md` exists and the ADR text shows tightened wording on Decision steps 1/3/5/8 etc.). So the pre-flight commit message "*docs: add ADR 0012 — swap Newtonsoft.Json for System.Text.Json in query logging (#294)*" will land an ADR that differs from the pre-amendment version the tasks doc was originally written against. The tasks doc still talks about Decision steps as if pre-amendment (e.g. "single call-site discipline" framing carries through without acknowledging the cast-and-throw `NotSupportedException` path the amended ADR explicitly adds in step 5 — see finding #3). The pre-flight task does not call out that `review-design.md` should also be committed in this initial commit, leaving a dangling untracked review file on the branch.

**Evidence**: `git log master..feature/json-serializer` returns no commits; `git status` shows ADR and spec dir untracked, `specs/.current-spec` modified. `specs/006-json_serializer/review-design.md` exists but is not mentioned in pre-flight.

**Recommendation**: Update pre-flight to (a) explicitly stage `review-design.md` and `review-requirements.md` alongside the ADR/spec/`.current-spec`; (b) acknowledge that the ADR being committed is the post-amendment version; (c) the tasks doc itself (this file) should be committed at pre-flight time, not just listed under "specs dir content". The current language ambiguously hand-waves "the `specs/006-json_serializer/` directory" without enumerating what's actually in it.

---

### 3. Tasks doc retains pre-amendment framing on "single call-site discipline" — the `NotSupportedException` cast path is now buried (Score: 85)

The amended ADR Decision step 5 (line 86 of `docs/adr/0012-json-serializer-swap.md`) explicitly states: "*cast `IBuildTheQueryProcessor` to the concrete `QueryProcessorBuilder` and throw `NotSupportedException` if the consumer supplied a custom `IBuildTheQueryProcessor` implementation*" — this is a documented limitation that consumers using custom `IBuildTheQueryProcessor` implementations cannot use the JSON-logging builder surface at all. The tasks doc Step 6 says: "*Change the `JsonQueryLogging(IBuildTheQueryProcessor, Action<JsonSerializerOptions>?)` method to forward to the canonical generic (cast to `QueryProcessorBuilder`).*" — it mentions the cast but says **nothing** about the `NotSupportedException` throw for non-`QueryProcessorBuilder` types, and there is no test task that verifies the `NotSupportedException` behaviour. This is a real behavioural change being inherited from the existing precedent (`Policies/QueryProcessorBuilderExtensions.cs:11-14`) but no test pins it.

The amended ADR was explicit that this is a "real, documented limitation" — yet the tasks doc does not have a test that asserts the `NotSupportedException` is thrown, nor a doc/migration task that calls it out. The release-notes draft (Step 12) doesn't mention the `IBuildTheQueryProcessor` limitation either.

**Evidence**: ADR line 86 explicitly says "throw `NotSupportedException` if the consumer supplied a custom `IBuildTheQueryProcessor` implementation". Tasks Step 6 only says "cast to `QueryProcessorBuilder`". No test in any task asserts `NotSupportedException` is thrown for a custom `IBuildTheQueryProcessor`. Step 12 migration notes do not mention this limitation.

**Recommendation**: Add a TEST + IMPLEMENT sub-task to Step 6 that asserts a custom `IBuildTheQueryProcessor` throws `NotSupportedException` when `.JsonQueryLogging(...)` is called. Add the limitation to Step 12's release notes draft. This is what the amended ADR (Decision step 5) explicitly requires.

---

### 4. Tasks doc misses ADR's "caller-propagation risk" — AC4 allow-list may need to expand to cover `Execute<TQuery>` and `ExecuteAsync<TQuery>` (Score: 82)

The amended ADR Decision step 3 (line 82) explicitly adds the **caller-propagation risk**: "*because `Serialize<T>` takes an unconstrained generic `T`, the analyser may also surface `IL2026` / `IL3050` at the *caller* of `Serialize<T>` (i.e. inside `Execute<TQuery>` / `ExecuteAsync<TQuery>`)*". The ADR explicitly says "If the AOT publish surfaces caller-site warnings, the allow-list expands to include the calling methods, with the same justification. The expansion is implementation-time discovery, not a defect in this ADR."

The tasks doc Step 4 / Step 5 instruct adding `UnconditionalSuppressMessage` **only** on `Serialize<T>`. Step 9's verification is **categorical** — any `IL2xxx` / `IL3xxx` warning under `src/Paramore.Darker/Logging/` outside the allow-list is a FAIL. If the analyser warns at `Execute<TQuery>`'s call to `Serialize` (which the amended ADR says is plausible), then Step 9 will FAIL with no remediation path documented other than "amend the spec". The tasks doc has no contingency for this — no "if the analyser warns at Execute, also add suppression with the same justification" branch.

This is exactly the design-review carry-over the prompt's probe explicitly flags (review-design.md finding #3 scored 72): the design review surfaced the risk; the ADR was amended to acknowledge it; the tasks doc was not updated to operationalise the contingency.

**Evidence**: ADR line 82 (caller-propagation paragraph); tasks Step 4 ("Add the FR13 `UnconditionalSuppressMessage` pair (`IL2026`, `IL3050`) on the `Serialize<T>` method"); Step 5 (mirrors); Step 9 ("Any other warning is a FAIL"). No task contemplates `Execute`/`ExecuteAsync` carrying suppressions.

**Recommendation**: Update Step 4 / Step 5 implementation notes to: "*Add `UnconditionalSuppressMessage` on `Serialize<T>`. If AOT publish in Step 9 surfaces caller-site `IL2026`/`IL3050` warnings at `Execute<TQuery>`/`ExecuteAsync<TQuery>`, expand the suppression to those methods with the same justification (per ADR Decision step 3 caller-propagation note)*". Step 9 should explicitly enumerate the calling methods as also-acceptable allow-list entries if discovered during implementation, rather than treating them as a spec violation.

---

### 5. Step 12 (release notes drafting) is mis-tagged as `/tidy-first` — that's a category error (Score: 78)

`/tidy-first` is documented (CLAUDE.md, `.claude/commands/refactor/tidy-first.md`) as a workflow for **separating structural code refactors from behavioural changes**. Step 12 says "**STRUCTURAL: Draft V5 migration entry covering serialiser swap (DoD)**" with `/tidy-first draft V5 release notes migration entry for JSON serialiser swap`. Drafting release notes is not a code refactor; it is documentation authoring. Invoking `/tidy-first` for it is a misapplication of the skill — the skill's "structural-only, no behaviour change" rationale doesn't apply to docs that have no compiled output. The CLAUDE.md skill description for `/tidy-first` calls out "Tidy First means separating structural changes (refactoring) from behavioural changes (new features/bug fixes)" — release notes are neither.

The release-notes draft also ends with "Park the draft in the spec dir or in a release-notes scratch file; the canonical landing happens at release tagging." That's a polite way of saying the DoD's release-notes requirement is **not satisfied** by this task — it's punted to release-tagging time, which is outside this PR's scope. DoD requires the release notes exist for V5; Step 12 produces a parked scratch file that may or may not survive to release tagging.

**Evidence**: Tasks Step 12 ("STRUCTURAL: Draft V5 migration entry…"); CLAUDE.md `Use /tidy-first when code needs refactoring before/during feature work`. The DoD in requirements.md lines 272–294 lists ~9 bullets that the release notes must cover.

**Recommendation**: (a) Drop the `/tidy-first` tag from Step 12 — it's a plain documentation task. (b) Pin a concrete landing location (e.g. `specs/006-json_serializer/release-notes-draft.md`) so the artefact is auditable, rather than "park in spec dir or scratch file". (c) Add a sub-task or note to Step 13 (final validation) that verifies the draft exists and covers all DoD bullets, so the DoD release-notes requirement is actually testable by Step 13 rather than evaporating.

---

### 6. FR9's "with-fallback" tests assert the suffix but do not verify the runtime-concatenation pattern is preserved (Score: 75)

FR9 (requirements lines 74–82) is explicit: "*The runtime-concatenation pattern itself is preserved as-is — refactoring it to a structured placeholder is out of scope*". The pattern at `QueryLoggingDecorator.cs:42` is `Logger.LogInformation("Execution of query {QueryName} completed in {Elapsed}ms" + withFallback, ...)` — note the `+ withFallback` runtime string concatenation onto the *template*, not as a structured argument.

Step 4 (sync) and Step 5 (async) each have a "with-fallback" TEST + IMPLEMENT sub-task that says "*Captured `LogInformation` entry has the exact template `"Execution of query {QueryName} completed in {Elapsed}ms (with fallback)"`*". This asserts the **observed output**, which is what a structured-logging sink would see — but a refactor that replaces `+ withFallback` with `"Execution of query {QueryName} completed in {Elapsed}ms{Fallback}"` and passes `withFallback` as a structured argument would **also pass this assertion** if the test inspects the rendered message rather than the message-template-as-string. Whether the test pins the *concatenation pattern* depends on whether `CapturingLoggerProvider` retains the original `state.ToString()` (which is the concatenated template + args formatted) versus the message template before concatenation. The tasks doc does not pin this distinction.

If the test only checks `captured.Message.ShouldBe("Execution of query CoreLoggingTestQuery completed in 5ms (with fallback)")`, then a structural refactor of `"" + withFallback` to a placeholder would produce the same rendered message and pass the test — exactly the failure mode FR9 says is out of scope. The tasks doc should explicitly require asserting against the **message template** (`{OriginalFormat}` from the state KVP collection), not the rendered message.

**Evidence**: Tasks Step 4 with-fallback test; Step 5 async with-fallback; requirements FR9 lines 74–82. `CapturedLogEntry` is described in Step 3 as retaining "the original message template, and the structured argument values" — so the infrastructure can do it, but the assertion in Steps 4/5 doesn't say to check it.

**Recommendation**: Explicitly require Steps 4/5 with-fallback tests to assert against `CapturedLogEntry.MessageTemplate` (the pre-concatenation template, sourced from `{OriginalFormat}`) and verify it equals the literal `"Execution of query {QueryName} completed in {Elapsed}ms (with fallback)"` — i.e. the suffix appears in the template string itself, not as a structured placeholder. Without this, a future refactor that "improves" the concatenation pattern silently passes these tests.

---

### 7. Direct-assignment path (ADR Decision step 7) has no test coverage (Score: 73)

ADR Decision step 7 (line 90 of `0012-json-serializer-swap.md`): "*Direct assignment to `QueryLoggingJsonOptions.Options` is supported but documented as the 'you own all the defaults' path. Direct assignment drops `ReferenceHandler.IgnoreCycles` (and any future FR3 defaults) unless the consumer re-applies them. The release notes call this out.*" Requirements C6 (line 181) confirms: "*Permitted but discouraged: direct assignment to `QueryLoggingJsonOptions.Options`*". The setter is null-guarded (FR2) — that test exists in Step 2 — but the direct-assignment **behaviour** (assigning a fresh `new JsonSerializerOptions { ... }` instance and verifying that the new instance is what `Options` returns, and that `IgnoreCycles` is no longer set) has no test in any of Steps 2 through 13.

This is a documented-but-untested code path. If a future refactor adds a defensive setter that auto-applies `ReferenceHandler.IgnoreCycles` when the assigned instance doesn't have it (which review-design.md finding #5 actually recommended), the change would be **invisible** to the test suite — the existing setter null-guard would still pass, and there's no test that asserts "after assigning `new JsonSerializerOptions()`, the returned `Options.ReferenceHandler` is the default (not `IgnoreCycles`)." That's a real coverage gap given the ADR explicitly classifies this as supported.

**Evidence**: ADR Decision step 7 (line 90); requirements C6 line 181; no test in tasks doc covers direct assignment of a fresh instance. The FR2 null-guard test (Step 2) only verifies the `null` branch.

**Recommendation**: Add a TEST + IMPLEMENT sub-task under Step 2 (or Step 6): "*When `QueryLoggingJsonOptions.Options` is assigned a new instance without `IgnoreCycles`, subsequent reads return the new instance and `ReferenceHandler` is the JSON default (not `IgnoreCycles`).*" — this pins the supported-but-lossy contract per ADR Decision step 7.

---

### 8. Step 1's six xunit.v3 sub-tasks bundle a behavioural change under `/tidy-first` and contain placeholder versions (Score: 72)

Step 1 is 6 sub-tasks: replace `xunit` pin, repoint 4 csprojs, drop `Xunit.Abstractions` usings in 6 files, update `IAsyncLifetime` returns, replace `TestOutputHelper` reflection in `TestClassBase.cs`, then run the full suite green. Several issues:

1. **Granularity**: each sub-task ships separately under `/tidy-first`, but several are conjoined — e.g. you cannot land "drop `Xunit.Abstractions` usings" without first landing the csproj repoint, or the build is red. The dependency chain inside Step 1 means each cut may not leave the build green individually. The tasks doc Dependencies section only describes inter-step dependencies, not intra-Step-1 ones.

2. **`/tidy-first` mis-tagging for reflection rework**: the `TestOutputHelper` reflection rework (sub-task 5, "Replace `TestOutputHelper` private-`test`-field reflection") is **behavioural** — option (a) replaces reflection with `TestContext.Current.Test`; option (b) accepts a `null` fallback that changes log-naming behaviour (the tasks themselves say so: "fall back consistently to type-name labels"). That is *observable behaviour change* in test output. Using `/tidy-first` for an observable behaviour change in test-log naming is a misapplication of the skill — should be `/test-first` with a test that pins the new behaviour. The tasks doc handles this with "Document the trade-off in a one-line comment" — that's not a test.

3. **xunit.v3 latest stable version is a placeholder**: sub-task 1 says `Version="<latest stable at design time>"` — a literal placeholder. This is a tasks doc, not a requirements doc — every concrete value should be pinned before approval. Without pinning, two contributors may install different `xunit.v3` versions on different sub-tasks and silently produce different behaviour.

4. **xunit.analyzers version**: sub-task 1 says "*bump version only if required*" but doesn't specify the criterion. "Required" by what — compile error? Analyser warning? Runtime test failure? Undefined.

**Evidence**: Tasks Step 1 sub-tasks; sub-task 5 option (b) is a behavioural change; sub-task 1 placeholder `<latest stable at design time>`.

**Recommendation**: (a) Split Step 1 sub-task 5 (TestOutputHelper rework) into a separate TEST + IMPLEMENT task with `/test-first` — option (a) needs a test that the `XunitTest` property returns a non-null `IXunitTest` under v3; option (b) needs an explicit acknowledgment that the test count or log-content changes. (b) Pin the actual `xunit.v3` version at design time (today is 2026-06-01 — pick the stable version on that date). (c) Pin the `xunit.analyzers` version explicitly. (d) Either consolidate Step 1's six sub-tasks into a single PR-sized commit, or add intra-step dependency notes confirming each sub-task leaves the build green.

---

### 9. `LoggerCaptureFixture` install-before-touch ordering is hidden in Step 3 — the implementer may not realise the cache pins the LoggerFactory at first decorator load (Score: 70)

FR10 (requirements line 88) explicitly states the cached `static readonly Logger` per closed generic "is **not invalidated** when `ApplicationLogging.LoggerFactory` is reassigned" and that tests "**must** install a capturing `ILoggerFactory` *before* any `QueryLoggingDecorator<,>` closed generic is touched". This is a load-bearing ordering constraint — if a test class or fixture initialisation accidentally touches `QueryLoggingDecorator<CoreLoggingTestQuery, …>` before `LoggerCaptureFixture` installs the capturing factory, the static field caches the *default* `ApplicationLogging.LoggerFactory` (which is `new LoggerFactory()` per `ApplicationLogging.cs:7` — a no-provider factory), and the capture buffer stays empty silently. Tests pass falsely.

Step 3 describes the fixture's constructor saving and replacing `ApplicationLogging.LoggerFactory` — but does NOT explicitly call out: (a) `IAssemblyFixture<T>` runs the constructor at assembly-fixture-creation time, which xunit.v3 guarantees runs **before** any test in the assembly executes — good; BUT (b) if any *other* code path in the assembly initialises the closed generic before the fixture's constructor runs (e.g. a `ModuleInitializer`, a static field initialiser on a different test class that mentions the type, even an `[Assembly]` attribute), the cache pins prematurely. The tasks doc does not flag this hazard, does not require an assertion that the captured buffer received non-zero entries (a positive check that the install-before-touch actually happened), and does not require any test to verify the fixture's install-order claim.

**Evidence**: `src/Paramore.Darker/Logging/ApplicationLogging.cs:7` shows `LoggerFactory` defaults to `new LoggerFactory()` (no providers — silent no-op logger); tasks Step 3 describes the fixture but does not pin install-before-touch verification; FR10 line 88 explicitly raises the hazard.

**Recommendation**: Add a sub-task to Step 3 (or as a standing assertion in Step 4): "Verify the fixture's `CapturedLogs` is non-empty after each decorator-exercising test — an empty buffer indicates the install-before-touch ordering broke and the test is silently passing against a no-op logger." Make this a `Should.NotBeEmpty()` precondition before any `{Query}` content assertion runs.

---

### 10. Step 10's ordering test asserts an exact STJ-indented JSON string without verifying STJ's actual default indentation (Score: 68)

Step 10 asserts `"{\n  \"Marker\": \"x\"\n}"` as the expected indented form for `OrderingTestQuery`. The exact form depends on STJ's `WriteIndented = true` defaults: 2-space indent, `\n` line endings, key-value separated by `": "`. STJ's `JsonSerializerOptions.IndentSize` defaults to 2 (only configurable in .NET 9+ — earlier versions hard-coded 2), `IndentCharacter` defaults to space, `NewLine` defaults to `Environment.NewLine` on .NET 9+ (`\r\n` on Windows!) and `\n` on .NET 8 — meaning on a Windows CI runner running net9.0, the actual output may be `"{\r\n  \"Marker\": \"x\"\r\n}"`. The tasks doc says "normalised line endings — STJ emits `\n` regardless of platform" — that claim is **incorrect** for net9.0 where `JsonWriterOptions.NewLine` defaults to `Environment.NewLine`. The test as specified will be flaky on Windows CI on net9.0.

This is a real gotcha. The fix is either (a) explicitly set `QueryLoggingJsonOptions.Options.NewLine = "\n"` in the test's arrange (which itself locks an option), or (b) normalise both sides of the assertion to `\n` before comparing. The tasks doc claims STJ emits `\n` unconditionally, which is empirically false for .NET 9 STJ writer defaults.

**Evidence**: Tasks Step 10 "normalised line endings — STJ emits `\n` regardless of platform"; STJ on .NET 9+ has `JsonWriterOptions.NewLine` defaulting to `Environment.NewLine`. Requirements AC3 step 3 (line 243) makes the same claim — but the requirements doc isn't being reviewed here; the tasks doc is the operational sheet.

**Recommendation**: Update Step 10 test assertion to either normalise newlines on both sides (`captured.Replace("\r\n", "\n").ShouldBe(...)`) or explicitly set `QueryLoggingJsonOptions.Options.NewLine = "\n"` in the test arrange (with try/finally restore). Without one of these, the test is flaky on Windows.

---

### 11. Step 13 "final validation" is monolithic and obscures partial failures (Score: 65)

Step 13 bundles 8 distinct validation commands into a single checkbox: build, test, two AOT publishes, run the AOT binary, two `dotnet list package` checks, csproj content audit, the four FR12 csproj audit, sample app smoke test. The task says "Any failure here loops back to the relevant step above; do not partially mark this task complete." That's the right intent, but the single-checkbox structure means a tracker can't visibly distinguish "all 8 passed" from "5 passed, 3 unknown". For a closing task that gates the spec being done, individual checkboxes per command would be a meaningful granularity improvement — each is independent and can be verified separately.

This is the design-review-style concern Probe 11 of the adversarial probes raised. The monolithic shape is OK for a closing task in principle, but given AC4 and AC1 are CI-style categorical assertions, individual sub-checkboxes would meaningfully reduce the chance of one quiet failure being subsumed by the rest passing.

**Evidence**: Tasks Step 13 is one checkbox with 8 commands.

**Recommendation**: Convert Step 13 into 8 sub-checkboxes, one per command/assertion. The task already enumerates them — making each a checkbox is a 5-minute structural improvement.

---

### 12. FR12 items 3, 4, 5 are not covered by any task (Score: 60)

FR12 (requirements lines 114–130) enumerates 6 API-break items. The tasks doc handles items 1, 2, 6 (with the option (b) trade-off documented). Items 3 (test method visibility convention `internal` over `public`), 4 (`[Theory]` + `[InlineData]` semantics unchanged), and 5 (`Xunit.Sdk` types) are **not addressed** in any task — there is no audit task that asserts "items 3/4/5 are no-ops in this repo and require no work". Item 5 is verified absent by grep (confirmed: `grep -rn "Xunit.Sdk" test/` returns nothing) — fine. Item 4 is genuinely no-op. But item 3 is a **convention change** the requirements explicitly call out as "the convention shifted" — and the AOT test classes are all `public` (verified: `AOTQueryProcessorTests.cs:21` is `public class AOTQueryProcessorTests`). The requirements say "`public` continues to work" so it's not a hard break, but the tasks doc should either (a) explicitly mark this as a "no action required, public continues to work" item, or (b) include a follow-up tidy task to migrate to `internal`. As written, FR12 item 3 is silently unaddressed.

**Evidence**: Requirements FR12 items 3/4/5 (lines 126–129); tasks doc has no task referencing items 3, 4, or 5. AOT test class `public class AOTQueryProcessorTests` (verified `test/Paramore.Darker.Tests.AOT/QueryProcessor/AOTQueryProcessorTests.cs:21`).

**Recommendation**: Add a one-line bullet to Step 1 sub-task 6 (the green-baseline check) noting that items 3, 4, 5 of FR12 are no-action-required for this repo, with the verification evidence (item 5: `grep -rn "Xunit.Sdk" test/` is empty; item 3: `public` continues to work, no migration in scope; item 4: no `[Theory]` change). Without this, an implementer-or-reviewer cannot tell whether the items were considered or overlooked.

---

### 13. "Logging" subdirectory file paths in tests don't exist yet — no task creates the directory (Score: 58)

Multiple tasks (Steps 2, 3, 4, 5, 6, 7, 9, 10) place new test files under a `Logging/` subdirectory of the test projects, e.g. `test/Paramore.Darker.Core.Tests/Logging/LoggerCaptureFixture.cs`, `test/Paramore.Darker.Extensions.Tests/Logging/LoggerCaptureFixture.cs`, etc. Verified: neither `test/Paramore.Darker.Core.Tests/Logging/` nor `test/Paramore.Darker.Extensions.Tests/Logging/` exists today. The existing tests sit at the root (`When_logging_decorator_executes_should_use_injected_serializer_settings.cs` is at the Core.Tests root, `When_AddJsonQueryLogging_called_should_register_serializer_settings.cs` at the Extensions.Tests root). The tasks doc implicitly assumes a subdirectory restructure for new tests — but never says "*create a `Logging/` subdirectory*" or justifies why new tests sit in a subdirectory when existing tests are flat. This is a minor inconsistency that may produce churn at implementation time when a contributor asks "should I move the existing flat tests into `Logging/` too?" — the doc doesn't answer.

**Evidence**: `ls test/Paramore.Darker.Core.Tests/Logging` and `ls test/Paramore.Darker.Extensions.Tests/Logging` both return "No such file or directory". Existing test files cited in tasks (e.g. `When_logging_decorator_executes_should_use_injected_serializer_settings.cs`) sit at the assembly root.

**Recommendation**: Either (a) drop the `Logging/` subdirectory prefix and place new tests at the root alongside existing tests, or (b) add a structural sub-task to Step 3 that creates both `Logging/` subdirectories explicitly, and decide whether the existing flat tests should also move. As written, layout is undefined.

---

### 14. Step 10's "save-and-restore" of an irreversible lock is hand-waved (Score: 55)

Step 10 says "*Save-and-restore as much state as is meaningful (notably, the lock itself is irreversible — see AC3's "process-scope" note; the test design accepts this).*" This is the only place in the tasks doc that acknowledges the ordering test **permanently locks `QueryLoggingJsonOptions.Options` for the rest of the test process** — every subsequent test in the same `dotnet test` invocation that mutates `Options` will throw `InvalidOperationException`. The mitigation given is `DisableParallelization = true` on the dedicated collection. But what guarantees the ordering test runs **last** in the collection? xUnit collections do not guarantee test ordering across the assembly — they only guarantee non-parallelism within the collection. If the ordering test runs before any other test that mutates options (e.g. Step 4's sync decorator test that arranges `WriteIndented = false`), that subsequent test will throw `InvalidOperationException` from the lock that the ordering test installed.

The cross-assembly process-isolation assumption (`dotnet test` forks per-assembly) protects across assemblies, but **within `Paramore.Darker.Core.Tests`** all tasks land — including Step 2's null-guard test (which mutates and restores), Step 4's sync decorator test (which mutates `WriteIndented` and restores), Step 7's rewritten core test (which mutates and restores), and Step 10's ordering test (which permanently locks). Without test-ordering control, these tests can run in any order in the same process. Step 10 running first kills the rest.

**Evidence**: Tasks Step 10 "save-and-restore as much state as is meaningful"; xUnit does not provide cross-collection test ordering guarantees; Step 2, Step 4, Step 7 tests in the same assembly all mutate `QueryLoggingJsonOptions.Options`.

**Recommendation**: Either (a) pin Step 10's ordering test to run in a *separate test assembly* (e.g. `test/Paramore.Darker.Core.Tests.Ordering/`) so process-isolation gives a clean global, or (b) explicitly document that the collection's `DisableParallelization` does not guarantee ordering and require a fresh process per `dotnet test` invocation (i.e. consumers running `dotnet test --filter` to scope to one test risk locking the global). The tasks doc accepts the irreversible lock without ensuring a clean isolation mechanism.

---

## Coverage Matrix

| FR / AC / Decision-step | Tasks that cover it | Status |
|---|---|---|
| FR1 | Step 4 (sync TEST+IMPL), Step 5 (async TEST+IMPL) | covered |
| FR2 | Step 2 (null-guard TEST+IMPL) | covered |
| FR3 | Step 2 (default IgnoreCycles TEST+IMPL); Step 9 (cycle-bearing AOT TEST) | covered |
| FR4 | Step 6 (callback signature TEST+IMPL) | partial — Step 6 covers DI + canonical surfaces but does not pin `JsonQueryLogging(IBuildTheQueryProcessor)` `NotSupportedException` per amended ADR step 5 (see finding #3) |
| FR5 | Step 8 (drop Newtonsoft) | covered |
| FR6 | Step 2 (add STJ package) | covered |
| FR7 | Implicit in Steps 4-6 (no rename) | covered |
| FR8 | Steps 4, 5, 6 (drop ctor param, drop ConfigurationException, drop DI singleton) | covered |
| FR9 | Step 4 with-fallback, Step 5 with-fallback | partial — see finding #6 (suffix asserted but concatenation pattern not pinned) |
| FR10 | Step 3 (fixture), Step 6 (extensions rewrite), Step 7 (core rewrite + delete obsolete) | covered |
| FR11 | Step 9 case 1 + case 2 | partial — see finding #1 (AOT publish not actually enabled) |
| FR12 | Step 1 (6 sub-tasks) | partial — items 3, 4, 5 not addressed (finding #12); reflection rework mis-tagged tidy-first (finding #8) |
| FR13 | Step 4 + Step 5 (Serialize suppressions); Step 9 (AOT verify) | partial — caller-propagation contingency missing (finding #4) |
| FR14 | Step 2 (no class-init lock TEST+IMPL); Step 10 (lock-after-use TEST) | covered |
| NFR1 | Step 8 (dotnet list package), Step 13 | covered |
| NFR2 | Step 9 (AOT publish) | partial — AOT publish not actually enabled (finding #1) |
| AC1 | Step 8 (dotnet list package), Step 13 | covered |
| AC2 | Step 13 (full suite green) | covered |
| AC3 | Step 6 (config), Step 7 (decorator rewrite), Step 10 (ordering) | partial — Step 10 ordering hazards (finding #14); STJ indentation/newline assertion fragile (finding #10) |
| AC4 | Step 9 (AOT publish + warnings) | partial — `<PublishAot>true</PublishAot>` not set, so verification is vacuous (finding #1); allow-list expansion contingency missing (finding #4) |
| AC5 | Step 11 (sample) | covered |
| Decision 1 (`QueryLoggingJsonOptions`) | Step 2 | covered |
| Decision 2 (rewrite decorators) | Steps 4, 5 | covered |
| Decision 3 (`UnconditionalSuppressMessage` + caller-propagation) | Step 4 + Step 9 | partial — caller-propagation operationalisation missing (finding #4) |
| Decision 4 (callback type) | Step 6 | covered |
| Decision 5 (single call-site + `NotSupportedException` for custom builder) | Step 6 | partial — cast covered, `NotSupportedException` test missing (finding #3) |
| Decision 6 (drop DI singleton) | Step 6 | covered |
| Decision 7 (direct assignment supported) | (no test) | **missing** — finding #7 |
| Decision 8 (startup-only contract — three failure modes) | Step 10 (lock-after-use); Step 12 release notes (parallel-host) | partial — Step 12 covers (c), Step 10 covers (a); (b) torn-write is not testable but should be acknowledged |
| Decision 9 (add `System.Text.Json` ref) | Step 2 | covered |
| Decision 10 (drop `Newtonsoft.Json` ref) | Step 8 | covered |
| DoD: release notes drafting | Step 12 | partial — mis-tagged `/tidy-first` and "parked" rather than landed (finding #5) |
| Pre-flight commit | Pre-flight section | partial — review files not committed; tasks.md not enumerated explicitly (finding #2) |

---

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 1 |
| 70-89 (High) | 8 |
| 50-69 (Medium) | 5 |
| 0-49 (Low) | 0 |

**Total findings**: 14
**Findings at or above threshold (60)**: 13
