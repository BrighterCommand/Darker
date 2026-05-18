# Review: requirements — 003-split-handler (Post-edit re-review)

**Date**: 2026-05-18
**Threshold**: 60
**Phase status**: Previously approved (re-review after terminology changes)
**Verdict**: PASS

## Findings

### 1. Stale `DarkerConfigurationException` references in review-design.md (Score: 35)

The design review document (`review-design.md`) still references `DarkerConfigurationException` in its Round 1 Fix Verification and Finding 1. However, `review-design.md` is a historical review artifact, not the normative requirements or ADR. The requirements document itself and the ADR have been fully updated to use `ConfigurationException` consistently.

**Evidence**: `DarkerConfigurationException` appears only in `review-design.md`. The requirements and ADR have zero occurrences.

**Recommendation**: No action required. The review document is a historical record.

---

### 2. Constraints section references `MissingHandlerException` without mentioning `MissingHandlerDecoratorException` (Score: 40)

The Constraints section states: "The exception type for handler-not-found changes from `MissingHandlerException` to `ConfigurationException` in V5 (FR15/FR16)." However, the ADR also retires `MissingHandlerDecoratorException`. The requirements do not explicitly mention this second retirement.

**Evidence**: Constraints bullet only mentions `MissingHandlerException`. The ADR retires both `MissingHandlerException` and `MissingHandlerDecoratorException`.

**Recommendation**: Add `MissingHandlerDecoratorException` to the constraints bullet for completeness.

---

### 3. FR5 and FR5a share the same numbering prefix (Score: 30)

FR5 covers `QueryHandlerAsync` and FR5a covers decorator marker interface sharing. Using "FR5a" for an unrelated decorator concern under the FR5 handler numbering is slightly confusing.

**Evidence**: FR5 is about handler base classes. FR5a is about decorator interfaces. Distinct concerns grouped under the same number.

**Recommendation**: Minor organizational nit. No action required.

---

### 4. No explicit AC for decorator factory/registry separation (Score: 45)

FR8d specifies separate decorator registries. FR11 specifies separate decorator factories. While AC7 covers the pipeline build using the correct path, there is no specific AC that directly tests the decorator registry and factory separation independently.

**Evidence**: AC6 covers handler registries. AC7 covers pipeline building. Neither explicitly verifies decorator registries and factories are independently wired per path.

**Recommendation**: Implicitly tested through AC7 and AC5. Consider adding explicit AC if decorator factory wiring is a distinct failure mode, but not strictly necessary.

---

### 5. Terminology consistency check: `ConfigurationException` usage is consistent (Score: 10)

After the revert from `DarkerConfigurationException` to `ConfigurationException`, all references in the requirements document are consistent. All 10 occurrences refer to the same existing type.

**Evidence**: No `DarkerConfigurationException` remnants. ADR confirms this is the existing type.

**Recommendation**: None. The terminology revert was applied cleanly.

---

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 0 |
| 0-49 (Low) | 5 |

**Total findings**: 5
**Findings at or above threshold (60)**: 0
