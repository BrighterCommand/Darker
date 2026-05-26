# Review: requirements — 004-pass_query_context (Round 4)

**Date**: 2026-05-20
**Threshold**: 60
**Verdict**: PASS

## Findings

### 1. FR12/FR15 do not cover `AddPolicies<TBuilder>` generic method (Score: 55)

FR12 mentions updating `AddDefaultPolicies` to register in DI. FR15 mentions updating `Policies()` / `DefaultPolicies()` fluent builder methods. However, the sample application actually calls `.AddPolicies(DarkerSettings.ConfigurePolicies())`, which resolves to the generic `AddPolicies<TBuilder>` method. This method also calls `builder.AddContextBagItem` and is not explicitly mentioned.

**Evidence**: `src/Paramore.Darker.Policies/QueryProcessorBuilderExtensions.cs` defines `AddPolicies<TBuilder>` which calls `AddContextBagItem`. `samples/SampleMinimalApi/Program.cs` calls `.AddPolicies(DarkerSettings.ConfigurePolicies())`.

**Recommendation**: Add `AddPolicies<TBuilder>` to FR12 or FR15 explicitly.

---

### 2. Benchmark project uses old API pattern (Score: 45)

The Benchmark project uses the old fluent builder pattern. The changes in this spec should not break it since it doesn't use policies or context bag. Informational only.

**Recommendation**: No action needed for this spec.

---

### 3. AC20 "clear compiler errors" is not testable (Score: 40)

AC20 is true by definition of removing methods from interfaces. It's more of a migration note than a testable criterion.

**Recommendation**: Reclassify as a constraint or rephrase as a migration guide note.

---

### 4. NFR1 lacks a measurable threshold (Score: 35)

NFR1 states "No performance regression" without defining what constitutes a regression.

**Recommendation**: State "Benchmark results should be within noise margins of the pre-change baseline."

---

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 1 |
| 0-49 (Low) | 3 |

**Total findings**: 4
**Findings at or above threshold (60)**: 0
