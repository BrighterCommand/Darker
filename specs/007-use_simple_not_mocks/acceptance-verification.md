# Acceptance Verification — Spec 007 (use_simple_not_mocks)

Date: 2026-06-05 · Issue [#306](https://github.com/BrighterCommand/Darker/issues/306) ·
ADR `docs/adr/0013-migrate-legacy-tests-to-test-doubles.md`

Final verification of the migration off Moq onto ADR 0009 Simple/InMemory doubles
plus the new recording doubles (ADR 0013). All checks run on branch `use_simple`.

## Build & test (AC11, AC12, NFR2)

- `dotnet build Darker.Filter.slnf -c Release` → **0 errors**.
- `dotnet test Darker.Filter.slnf -c Release --no-build` → **green at baseline**:
  - `Paramore.Darker.Core.Tests` = 76 passing (net8.0 and net9.0), 0 failed, 0 skipped.
  - `Paramore.Darker.Extensions.Tests` = 8 passing (net8.0 and net9.0), 0 failed.
  - Matches the Task 0 baseline exactly (no test added/removed/skipped).

## Acceptance criteria

| AC  | Result | Evidence |
|-----|--------|----------|
| AC1  | PASS | `grep -rl 'Mock<'` and `grep -rl 'using Moq'` over `test/Paramore.Darker.Core.Tests/` → both empty. |
| AC2  | PASS | All four target files build and pass with Simple/InMemory/recording doubles; no Moq. |
| AC3  | PASS | Outcome assertions (`result.ShouldBe(id)`, context-bag, exception messages) preserved in every migrated test. |
| AC4  | PASS | Whole-word `grep -nwE 'TestQueryA|TestQueryB|TestQueryC|TestQueryHandler|TestQueryHandlerAsync'` over the two QueryProcessor files → empty; no `using ...Exported;` in them. |
| AC5  | PASS | `grep -nE '\.Verify\(|Times\.|It\.'` over the two QueryProcessor files → empty. |
| AC6  | PASS | No nested double `class` declarations remain in the four target files (only the test fixture classes); every FR3/FR4 closed-list type (incl. `Result` types) now lives under `TestDoubles/`. |
| AC7  | PASS | No `TestDoubles/` simple name collides with an `Exported/` name (`comm -12` of the two basename sets → empty). |
| AC8  | PASS | `git diff` since branch base shows no change to the five `Exported/*.cs` files (only the new `Exported/README.md`). |
| AC9  | PASS | `git diff` shows no change under `src/`. |
| AC10 | PASS | `Exported/README.md` exists and documents the scanning role + the `TestDoubles/` distinction. |
| AC11 | PASS | Baseline recorded in `.baseline-test-count` (Task 0). |
| AC12 | PASS | Post-migration suite green at the AC11 baseline count, zero failures. |
| AC13 | PASS | `[Fact]`/`[Theory]` counts unchanged: QueryProcessorTests 3, QueryProcessorAsyncTests 3, PipelineBuilderExceptionTests 4, FallbackPolicyTests 3. |
| AC14 | PASS | `grep -rliE 'moq'` over `test`/`src` (`*.cs`/`*.csproj`/`*.props`, excluding bin/obj) → empty; `grep -liE 'moq' Directory.Packages.props` → empty. |

## Definition of Done

- [x] All four legacy files migrated off Moq (FR1–FR4, FR7).
- [x] Nested doubles extracted verbatim to `TestDoubles/` (FR3, FR4, FR5).
- [x] Recording handler doubles + recording handler/decorator factories added (FR10).
- [x] Distinct-named QueryProcessor doubles, no `Exported` reuse (FR6).
- [x] `Exported/README.md` documents the two-directory role split (FR9).
- [x] Moq dependency deleted from csproj and `Directory.Packages.props` (FR8).
- [x] Structural-only, behaviour-preserving; suite green at baseline throughout (NFR1, NFR2, NFR4).
- [x] No `src/` change (NFR3, AC9).

**Result: all of AC1–AC14 PASS. Migration complete.**
