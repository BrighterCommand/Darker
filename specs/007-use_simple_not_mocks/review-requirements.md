# Review: requirements — 007-use_simple_not_mocks

**Date**: 2026-06-05
**Threshold**: 60
**Verdict**: NEEDS WORK

> Round 3. All seven prior-round findings verified genuinely fixed (see "Verification of prior-round fixes"). The two blockers below are both in the *acceptance-check mechanics* retained/introduced for the FR10 / no-Exported rewrite — not in the substance.

## Findings

### 1. AC14's `grep -rli moq test src` returns dozens of false positives from build artifacts — guaranteed-failing as written (Score: 82)

AC14 asserts that after migration `grep -rli moq test src Directory.Packages.props` "returns nothing." This fails even after Moq is fully removed from source. The recursive case-insensitive grep walks into git-ignored `bin/`/`obj/` directories, which contain restore/build artifacts that legitimately name Moq — and a clean rebuild *regenerates* some of them (`.deps.json`, `project.assets.json`) with transitive `"Moq/4.20.72"` entries. The requirement explicitly embraces this superset behaviour, so it is a deliberate-but-unverifiable check.

**Evidence**: AC14 (requirements.md:99). `grep -rli moq test src | grep -E '/bin/|/obj/' | wc -l` returns **56** matches; e.g. `test/Paramore.Darker.Core.Tests/obj/project.assets.json` contains `"Moq/4.20.72"`. All are git-ignored, so `git diff` is clean but `grep -rli` still sees them.

**Recommendation**: Scope AC14 to source files and exclude build output — e.g. `grep -rliE 'moq' --include='*.cs' --include='*.csproj' --include='*.props' --exclude-dir=bin --exclude-dir=obj test src` returns nothing, plus a separate `grep -liE 'moq' Directory.Packages.props` returns nothing.

---

### 2. AC4 forbids the bare substring `TestQueryHandler`, which can false-fail a compliant migration (Score: 64)

AC4 says a search of the two QueryProcessor files must find "none of the simple names ... `TestQueryHandler`, `TestQueryHandlerAsync`." As a substring grep, `TestQueryHandler` matches inside `TestQueryHandlerAsync` (harmless overlap) but, more importantly, would match any **new** TestDoubles handler the spec mandates via copy-and-rename — e.g. `RecordingTestQueryHandler`, `TestQueryHandlerDouble` — making AC4 fail on a migration that references no `Exported` type at all. AC4's intent ("files reference no `Exported` type") is correct; the mechanical check doesn't implement it.

**Evidence**: AC4 (requirements.md:89). `grep 'TestQueryHandler'` vs `grep -w 'TestQueryHandler'` differ; the new doubles required by FR6 (requirements.md:48) are likely to embed these substrings.

**Recommendation**: Use whole-word matching (`grep -nwE 'TestQueryA|TestQueryB|TestQueryC|TestQueryHandler|TestQueryHandlerAsync'`) and/or reduce AC4 to the robust check: no `using Paramore.Darker.Core.Tests.Exported;` and no `Exported.` reference. AC7's collision rule already prevents same-name doubles.

---

### 3. FR10 omits the cancellation-token argument from recorded state — async argument-match re-expression under-specified (Score: 41, below threshold)

FR10 sub-bullet 2 records "the query argument" so `It.Is<...>(q => q.Id == id)` becomes a state assertion. The async tests verify two-arg calls including `default(CancellationToken)` (`ExecuteAsync(It.Is<...>(...), default(CancellationToken))`). FR10 doesn't mention the token. In practice it's invariantly `default`, so behaviour is preserved, but strict "behaviours identical" wording creates minor tension.

**Evidence**: FR10 (requirements.md:56) records "the query argument"; QueryProcessorAsyncTests.cs:75–78,97 also constrain the `CancellationToken`.

**Recommendation**: Note that the token is out of scope for state recording because it is invariantly `default` (behaviour-equivalent), or record it. Low severity.

---

### 4. NFR3 has no acceptance criterion (human-judgement only) (Score: 30, below threshold)

NFR→AC map: NFR1→AC9/AC5, NFR2→AC11/AC12, NFR4→AC13, **NFR3→(none)**. Acceptable for a stylistic NFR (not mechanically checkable); the substantive part (mocks gone, Simple*/InMemory* used) is covered by AC1/AC2/AC3/AC5. Flagging for completeness.

**Evidence**: NFR3 (requirements.md:63); no AC references style/preference-order.

**Recommendation**: State NFR3 is review-time/narrative, or add a light AC (largely duplicating AC1/AC2).

---

## Verification of prior-round fixes (all confirmed holding)

- **14 ACs / DoD**: exactly AC1–AC14; DoD says "fourteen (AC1–AC14)." ✓
- **FR10 "15 such calls"**: 9 (QueryProcessorTests) + 6 (AsyncTests) = 15. ✓
- **FR3/FR4 closed lists**: match actual nested classes exactly (incl. all `Result` types, the generic decorator, the attribute). ✓
- **Helper types implement both sync+async contracts**: confirmed. ✓
- **Moq package refs**: `Directory.Packages.props:15` PackageVersion; test csproj PackageReference; only consumer. ✓
- **AC7 pre-fail check**: no existing `TestDoubles/` type shares a simple name with an `Exported/` type. ✓
- **FR7 vs tracking factory**: no contradiction — tracking factory is a concrete `SimpleHandlerFactory`-style double, not a mock; AC2 correctly splits plain vs tracking factory across the four files. ✓
- **FR6 reuse vs invocation-recording**: no conflict — the recording handler doubles are new (FR10); reuse applies to existing doubles "where one already covers the need." ✓
- **NFR1/NFR4 wording**: now explicitly permit interaction→state re-expression; no longer contradicts FR10. ✓
- **Exported untouched / 5 files**: exactly 5 `Exported/*.cs`; README absent as expected. ✓
- **FR→AC coverage**: FR1→AC2/3/4, FR2→AC2/3/4, FR3→AC6, FR4→AC6, FR5→AC6/AC8, FR6→AC4/AC7, FR7→AC1/AC3/AC5, FR8→AC14, FR9→AC10, FR10→AC5. All FRs covered. ✓

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 1 |
| 50-69 (Medium) | 1 |
| 0-49 (Low) | 2 |

**Total findings**: 4
**Findings at or above threshold (60)**: 2

**Verdict logic**: AC14 false positives (82) and AC4 substring trap (64) are ≥ 60 → **NEEDS WORK**. Both are check-mechanics fixes; the spec's substance is sound.
