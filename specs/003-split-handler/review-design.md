# Review: design — 003-split-handler

**Date**: 2026-05-18
**Round**: 3
**Threshold**: 60
**Verdict**: PASS

## Round 2 Fix Verification

### Finding 1 (was Score 75): VERIFIED FIXED
ADR now uses existing `ConfigurationException` throughout. No new type introduced. `MissingHandlerException` and `MissingHandlerDecoratorException` explicitly retired.

### Finding 2 (was Score 65): VERIFIED FIXED
DI package section now lists all 6 affected files with parenthetical descriptions.

### Finding 3 (was Score 62): VERIFIED FIXED
`QueryHandlerRegistry.cs` listed with explicit note about async scanning counterpart for FR17.

### Finding 4 (was Score 55): VERIFIED FIXED
Policies and QueryLogging entries now explicitly state dual-path decorator registration in `QueryProcessorBuilderExtensions.cs`.

## New Findings

### 1. `ServiceCollectionDecoratorRegistry.cs` omitted from DI affected files list (Score: 58)

This file implements `IQueryHandlerDecoratorRegistry` and contains `RegisterDefaultDecorators()` which registers `FallbackPolicyDecorator<,>`. Will need an async counterpart. Implementers will discover it through `ServiceCollectionDarkerHandlerBuilder` dependency, but it is an oversight in the file list.

**Evidence**: `src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionDecoratorRegistry.cs` not mentioned in DI file list.

**Recommendation**: Add to DI file list with async counterpart note.

---

### 2. `IQueryProcessorExtensionBuilder` not mentioned in ADR (Score: 55)

This interface defines `RegisterDecorator(Type)` — the API extension packages call to register decorators. Will need dual-path support for async decorator registration.

**Evidence**: `src/Paramore.Darker/Builder/IQueryProcessorExtensionBuilder.cs` used by Policies and QueryLogging extension methods. Not mentioned in Builder file list.

**Recommendation**: Add to Builder file list with dual-path registration note.

---

### 3. `INeedAQueryContext` builder interface omitted (Score: 30)

Minor nit — likely does not need changes since query context is path-independent.

**Evidence**: File exists but not listed. "Builder interfaces" may cover it generically.

**Recommendation**: No action needed.

---

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 2 |
| 0-49 (Low) | 1 |

**Total new findings**: 3
**New findings at or above threshold (60)**: 0
