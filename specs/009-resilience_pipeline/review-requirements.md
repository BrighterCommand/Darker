# Review: requirements — 009-resilience_pipeline

**Date**: 2026-06-12
**Threshold**: 60
**Verdict**: PASS

> Phase status: **not yet approved** (no `.requirements-approved` marker). This is the second re-review. The prior round's three blocking findings (plus four sub-threshold ones) were fixed; this pass verifies the fixes landed and checks for regressions. No findings at or above threshold remain.

## Findings

### 1. "null-guards then sets" wording for QueryProcessor.cs:104-105 is imprecise (Score: 25)
The Darker-counterparts list (line 297) describes `QueryProcessor.cs:104-105` as "null-guards then sets `Context.Policies` from the processor." The actual code is a conditional *fill-if-absent* (`if (queryContext.Policies == null) queryContext.Policies = _policyRegistry;`), not a defensive "null-guard" in the throw-on-null sense used elsewhere in the same document (FR4/FR5/AC10/AC11 use "null-guard" / "guards its argument" to mean *throwing* `ArgumentNullException`). Using the same verb for two different behaviours (throw vs. fill-default) is a mild terminology inconsistency; the line reference itself is correct and the intent ("set analogously") is clear.

**Evidence**: requirements.md:297 vs QueryProcessor.cs:104-105 (`if (queryContext.Policies == null) queryContext.Policies = _policyRegistry;`).

**Recommendation**: Reword to "sets `Context.Policies` from the processor only when the context has none (fill-if-absent)" to avoid colliding with the throw-on-null meaning of "guard" used in FR4/FR5.

---

### 2. FR4 "No registration-time content validation" silently diverges from the legacy parity it invokes (Score: 30)
FR4 leans on the legacy `Policies(...)` path for parity ("the legacy `Policies(...)` path likewise guards its argument") and then states "No registration-time *content* validation ... is required for the caller-supplied registry path." This is a deliberate and correct design choice, but it is worth flagging that the legacy path is NOT a parity model for *content* validation: `AddPolicies` (QueryProcessorBuilderExtensions.cs:30-34) throws `ConfigurationException` if `RetryPolicyName`/`CircuitBreakerPolicyName` keys are missing. The document is careful to scope the parity claim to the *argument* (null) guard only, so the statement is technically accurate — but a reader could infer broader parity.

**Evidence**: requirements.md:114-117 vs QueryProcessorBuilderExtensions.cs:27-34 (legacy DOES do content validation).

**Recommendation**: Add a half-sentence: "Unlike the legacy `AddPolicies` path, which requires specific keys, the new path performs no content validation." This pre-empts a misread.

---

## Verification of prior-round fixes

| Prior finding | Status |
|---------------|--------|
| AC numbering out of sequence (AC10 before AC8/AC9) | FIXED — AC1–AC11 contiguous and in order |
| FR4/FR5 lacked dedicated ACs | FIXED — AC10 (builder) + AC11 (DI), both assert `ArgumentNullException` |
| FR7 failure-timing ambiguous (build vs execute) | FIXED — now explicitly "pipeline build time, in `InitializeFromAttributeParams`"; verified correct against source |
| AC7 only tested already-cancelled token | FIXED — clause (a) already-cancelled AND (b) cancelled-in-flight |
| NFR3 mis-cited ADR-0006 | FIXED — citation removed ("needs no ADR") |
| FR6 unspecified default timings | FIXED — deferred to ADR; AC6 weakened to resolvable + executable |
| FR8 unconditional independence in user story | FIXED — Proposed Solution item 3 and FR8 both carry the per-key-builder precondition |

## FR -> AC Coverage Table

| FR | Description | Mapped AC(s) | Adequate |
|----|-------------|--------------|----------|
| FR1 | Pipeline provider on context | AC1, AC6 | yes |
| FR2 | Resilience-pipeline decorators | AC1, AC2, AC3 | yes |
| FR3 | New attributes | AC1, AC4 | yes |
| FR4 | Builder registration (+null guard) | AC10 | yes |
| FR5 | DI registration (+null guard) | AC11 | yes |
| FR6 | Default pipelines | AC6 | yes |
| FR7 | Configuration validation (build-time) | AC5 | yes |
| FR8 | Type-scoped pipelines | AC4 | yes |
| FR9 | Context propagation | AC7, AC8 | yes |

Every FR maps to at least one AC. NFR1 is additionally covered by AC9.

## Code Reference Verification

| Citation | Result | Detail |
|----------|--------|--------|
| Polly 8.6.6 in Directory.Packages.props | CONFIRMED | `<PackageVersion Include="Polly" Version="8.6.6" />` |
| RetryableQueryDecorator.cs:23-24 (missing-key ConfigurationException) | CONFIRMED | Throws inside `InitializeFromAttributeParams` |
| RetryableQueryDecorator.cs:34-35 (missing-provider ConfigurationException) | CONFIRMED | `GetPolicyRegistry()` throws; called from `InitializeFromAttributeParams` — both checks fire at build time |
| FR7 "both detected at pipeline build time" | CONFIRMED | Provider check + key check both run synchronously in `InitializeFromAttributeParams` |
| QueryProcessor.cs:104-105 | CONFIRMED (line ref) | Sets `Policies` if null; "null-guards" wording loose (Finding 1) but line numbers/behaviour correct |
| IQueryContext.cs (`Policies` member) | CONFIRMED | `IPolicyRegistry<string> Policies { get; set; }`; new provider additive |
| Legacy `Policies(...)` throws `ArgumentNullException` on null registry | CONFIRMED | Delegates to `AddPolicies`, which throws `ArgumentNullException(nameof(policyRegistry))`; DI `AddPolicies` routes through the same guard — FR4/FR5 parity holds |
| NFR3 no longer cites ADR-0006 | CONFIRMED | ADR-0006 is "Remove Third-Party DI Container Integration Packages"; correctly no longer referenced |
| ADR-0005 / ADR-0002 / ADR-0004 / ADR-0014 | CONFIRMED | Dual sync/async; attribute-driven pipeline; factory/registry abstractions; lifetime-aware factories |

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 0 |
| 0-49 (Low) | 2 |

**Total findings**: 2
**Findings at or above threshold (60)**: 0

**Verdict: PASS** — all seven prior-round findings are verifiably fixed, every FR (FR1–FR9) maps to at least one testable AC, the AC list is contiguous AC1–AC11 and in order, and every load-bearing code citation checks out against source. The two remaining findings are sub-threshold wording nits and do not block approval.
