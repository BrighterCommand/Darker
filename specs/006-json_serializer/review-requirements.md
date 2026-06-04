# Review: requirements — 006-json_serializer (v7)

**Date**: 2026-05-29
**Threshold**: 60
**Verdict**: PASS

## Findings

### 1. AC3 step 1 gloss labels the C6 path inaccurately ("direct-mutation" vs C6's "direct assignment") (Score: 35)

The v7 gloss added to AC3 step 1 (line 249) reads: *"This pre-bootstrap mutation is the C6 direct-mutation path (permitted but discouraged for consumer code)..."*. But C6 (line 189) defines its "permitted but discouraged" path as `direct assignment to QueryLoggingJsonOptions.Options. This replaces the default instance entirely`. AC3 step 1's operation is `QueryLoggingJsonOptions.Options.MaxDepth = 32;` — that is a property mutation on the existing instance, not a replacement of the instance. So the gloss conflates two distinct operations:

- **Direct assignment** (C6's "permitted but discouraged"): `QueryLoggingJsonOptions.Options = new JsonSerializerOptions { ... }` — drops `ReferenceHandler.IgnoreCycles`.
- **Direct property mutation** (what AC3 step 1 actually does): `QueryLoggingJsonOptions.Options.MaxDepth = 32` — preserves defaults.

C6 actually does not explicitly classify direct property mutation (it only enumerates the callback path and the assignment path). The AC3 gloss reaches for "C6 direct-mutation path" as if that label exists, but it does not. This is wording drift, not a behavioural contradiction — the operation is still permitted (FR14 only constrains lock-causing calls, and a property setter doesn't lock the options) — but the cross-reference is loose.

**Evidence**: `specs/006-json_serializer/requirements.md:189` (C6: `Permitted but discouraged: direct assignment to QueryLoggingJsonOptions.Options. This replaces the default instance entirely`); `specs/006-json_serializer/requirements.md:249` (AC3 step 1: `This pre-bootstrap mutation is the C6 direct-mutation path`).

**Recommendation**: Replace "the C6 direct-mutation path" with something like "an in-place property mutation on the existing instance (distinct from C6's 'direct assignment' which replaces the instance); FR14's lock-after-use invariant only constrains `Serialize` calls, so the property setter is safely callable pre-bootstrap." Or, simpler: add a sentence to C6 explicitly naming "direct in-place property mutation" as a third path and have AC3 step 1 point at that bullet by name.

---

### 2. FR12 item 6 cite range `107-109` is off by one (Score: 25)

The v7 doc cites the GetField reflection at `test/Paramore.Test.Helpers/Base/TestClassBase.cs:107-109` (FR12 item 6, line 137) and at the Additional Context bullet (line 324). The actual method spans lines 107-110:

- Line 107: `private static FieldInfo? GetTestField(ITestOutputHelper testOutputHelper)`
- Line 108: `{`
- Line 109: `return testOutputHelper.GetType().GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);`
- Line 110: `}`

The call itself is line 109, the attribute is line 106, the closing brace is line 110. The cite `107-109` cuts off the closing brace. Implementers reading `107-109` would still find the right code, but the v6 review (which cited `107-110`) was strictly more accurate. Minor nit.

**Evidence**: `test/Paramore.Test.Helpers/Base/TestClassBase.cs:107-110` (actual method extent) vs `specs/006-json_serializer/requirements.md:137` and `:324` (doc cite `107-109`).

**Recommendation**: Update both cites to `107-110` to span the full method block, matching the v6 review's cite.

---

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 0 |
| 0-49 (Low) | 2 |

**Total findings**: 2
**Findings at or above threshold (60)**: 0

**Verification notes** (factual claims spot-checked against the codebase):
- **AC3 step 3 expected JSON string**: verified empirically — `JsonSerializer.Serialize(new OrderingTestQuery(), new JsonSerializerOptions { WriteIndented = true })` produces exactly `"{\n  \"Marker\": \"x\"\n}"` (2-space indent, `\n` line endings). The doc's expected string is precisely correct, and the "STJ emits `\n` regardless of platform" qualifier is accurate.
- **FR12 item 6 reflection location**: verified — `test/Paramore.Test.Helpers/Base/TestClassBase.cs` lines 49 (`(ITest?)GetTestField(...)?.GetValue(...)`), 52 (`TestQualifiedName` falls back to `typeof(T).GetLoggerCategoryName()`), and the GetField method spans lines 107-110 (call on line 109). The behavioural-regression-under-v3 analysis is correct.
- **`TestContext.Current.Test` xunit.v3 API**: confirmed via xunit.v3 docs — `TestContext.Current` exists, exposes pipeline state, and `IXunitTest` is the v3 test-info type. The doc's proposed mitigation is the right API surface.
- **FR12 csproj enumeration**: confirmed — `grep -l '"xunit"' test/**/*.csproj` returns exactly the 4 csprojs FR12 enumerates; `Paramore.Darker.Benchmarks.csproj` is correctly excluded.
- **AC5 sample-app types**: `GetPersonNameQuery` and `GetPersonQueryHandler` in `samples/SampleMinimalApi/QueryHandlers/GetPersonQueryHandler.cs` match the doc's cite.

All four v6 findings are functionally closed by the v7 edits. The two remaining nits (terminology drift in AC3 step 1 gloss; one-line cite range) are well below the score-60 threshold. Verdict: **PASS**.
