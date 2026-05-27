# Review: tasks — 005-merge_builtin_decorators (round 2)

**Date**: 2026-05-27
**Threshold**: 60
**Verdict**: PASS

All round-1 findings verified fixed:

- **F1 (was 70)**: Step 5 now says "5 root-level files (4 `When_*` files plus `PipelineBuilderExceptionTests.cs`, per FR8 line 158)". ✓
- **F2 (was 65)**: Step 7 Verify now reads "primary verification of the xunit-entry-assembly-only-discovery assumption; `<PrivateAssets>all</PrivateAssets>` is a belt-and-braces measure for transitive test-asset flow per ADR §10, not the load-bearing control". ✓
- **F3 (was 50)**: Step 5 Decorators row now says "3 files (two `When_*` feature tests plus `FallbackPolicyTests.cs`)". ✓
- **F4 (was 35)**: Comment-adding action moved from Step 7 to Step 8 ("during Step 8 implementation"). ✓
- **F5 (was 45)**: Step 1 → Step 5 boundary explicit — Step 1 touches production-code + side-package files only; Step 5 sweeps all test-file usings. Step 1 verify command scoped to project-level builds, full-filter build deferred to Step 5. ✓

## Findings

### 1. Step 5 references SampleMauiTestApp by its post-move path before Step 6 moves it (Score: 30)

Step 5 says: "`samples/SampleMauiTestApp` (current location — moved in Step 6): 4 files". At Step 5 execution time the file system still has the MAUI sample at `SampleMauiTestApp/` (repo root) — Step 6 is what relocates it to `samples/SampleMauiTestApp/`. The parenthetical "(current location — moved in Step 6)" attempts to disambiguate but applies the qualifier "current location" to a path that is in fact the *post*-move location. An implementer skimming the bullet may look in `samples/SampleMauiTestApp/` and find nothing there.

**Evidence**: Tasks.md Step 5: "`samples/SampleMauiTestApp` (current location — moved in Step 6): 4 files". Step 6: "Move the entire folder: `SampleMauiTestApp/` → `samples/SampleMauiTestApp/`."

**Recommendation**: Reword to "`SampleMauiTestApp/` (root-level — moved to `samples/` in Step 6): 4 files".

---

### 2. Step 5 verify command will not catch MAUI sample using-update mistakes (Score: 30)

Step 5 updates `using`s in `SampleMauiTestApp/` (4 files) but its verify command is `dotnet build Darker.Filter.slnf -c Release` — the filter deliberately excludes the MAUI sample (CI has no MAUI workload). A typo in one of the MAUI `using` updates would slip past Step 5's verification and only surface at Step 6's IDE sanity check or Step 9's full-`Darker.slnx` build.

**Evidence**: Step 5 verify uses `Darker.Filter.slnf` only. First full-solution build is at Step 9.

**Recommendation**: Add a workload-conditional clause — "On a workload-capable machine, also run `dotnet build samples/SampleMauiTestApp/SampleMauiTestApp.csproj`" — or explicitly note in Step 5's Verify that MAUI compile-check is deferred to Step 6 / Step 9.

---

### 3. Step 7 AOT csproj path is rewritten twice without a defensive grep at Step 8 (Score: 25)

The AOT csproj's `<ProjectReference>` is rewritten twice (Step 7 to pre-rename name `Paramore.Darker.Tests`, Step 8 to post-rename `Paramore.Darker.Core.Tests`). The ADR Consequences section flags this as a Negative risk ("easy to forget the second update"). Tasks.md captures the two-step rewrite but doesn't include a defensive grep at Step 8 to verify the rewrite landed.

**Evidence**: Tasks.md Step 7 (rewrite to `..\Paramore.Darker.Tests\...`) and Step 8 (retarget to `..\Paramore.Darker.Core.Tests\...`). ADR Negative Consequences: "easy to forget the second update".

**Recommendation**: Add to Step 8 verify: `grep -n 'Paramore.Darker.Tests' test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj` should show only `Paramore.Darker.Core.Tests` references, not stale `Paramore.Darker.Tests` ones.

---

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 0 |
| 0-49 (Low) | 3 |

**Total findings**: 3
**Findings at or above threshold (60)**: 0
