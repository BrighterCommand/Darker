# Review: requirements — 014-Caching-Decorator

**Date**: 2026-07-20
**Threshold**: 60
**Verdict**: NEEDS WORK (round 3) — all findings addressed in the subsequent revision

_Round 3. The five round-2 fixes were all verified coherent with no new contradictions, and every codebase fact the document relies on was re-confirmed (`IQueryContext.Bag` = `IDictionary<string,object>`; `InstrumentationOptions` `[Flags]` gates span attribute groups; async `ExecuteAsync` → `Task<TResult>`, sync `Execute` → `TResult`; core targets `netstandard2.0;net8.0;net9.0`; validation uses two attributes + `ConfigurationException` fail-fast). One above-threshold gap surfaced on a fresh full pass._

## Findings

### 1. Attribute constructor omits the mandatory `step`, contradicting the "ordering is significant" emphasis (Score: 64)

Every Darker decorator attribute derives from `QueryHandlerAttribute`, whose only constructor is `protected QueryHandlerAttribute(int step)` — step is mandatory and there is no parameterless base. The existing convention is step-first positional (`[QueryLogging(1)]`, `[FallbackPolicy(2)]`, `[ValidateQuery(step)]`). Yet the caching doc never stated that the caching attribute takes a `step` argument, and **every** example omitted it, e.g. FR2's `[CacheableQuery(expirationSeconds: 300)]`. FR2 described expiry as "a **required** constructor argument" in the singular, implying it was the sole constructor parameter — a direct tension with the feature's headline concern that pipeline position/step ordering "is significant."

**Evidence**: FR2 (pre-fix): "expiry is expressed as an integer count of seconds (e.g. `[CacheableQuery(expirationSeconds: 300)]`)". Verified: `src/Paramore.Darker/QueryHandlerAttribute.cs` — `protected QueryHandlerAttribute(int step)` (no parameterless ctor); `src/Paramore.Darker.Validation/ValidateQueryAttribute.cs` — `public ValidateQueryAttribute(int step) : base(step)`.

**Recommendation**: State that the caching attribute constructor also takes the mandatory `step`, signature `(int step, int expirationSeconds)`, and update examples to `[CacheableQuery(step: 1, expirationSeconds: 300)]`.

**Resolution**: FR1 now states both variants derive from `QueryHandlerAttribute(int step)` and take `step` first; FR2 gives the full signature `(int step, int expirationSeconds)` and the corrected example; a new AC asserts `step` ordering behaviour. ✅

---

### 2. `IAmCacheable.CacheKey` runtime null/empty is unspecified (Score: 46)

FR4 pins the interface as getter-only non-nullable `string`, but nullable annotations are compile-time only; nothing prevents a runtime `null`/`""`. The doc did not say whether the decorator throws, falls back to the default strategy, or passes an empty key to `GetOrCreateAsync`.

**Evidence**: FR4: `public interface IAmCacheable { string CacheKey { get; } }` (getter-only, non-nullable). No FR/AC addressed a null/empty runtime value.

**Recommendation**: Add a rule (and AC) — fail fast or fall back to default.

**Resolution**: FR4 now fails fast with a configuration exception on null/empty/whitespace runtime `CacheKey`; AC added. ✅

---

### 3. Zero/negative `expirationSeconds` boundary is unspecified (Score: 43)

`[CacheableQuery(expirationSeconds: 0)]` or a negative value compiles. FR2 removed the "silent default" but did not define behaviour for a non-positive expiry mapped onto `HybridCacheEntryOptions.Expiration`.

**Evidence**: FR2: "no silent default … every cached query has an explicit lifetime." No lower-bound validation stated.

**Recommendation**: Reject non-positive with a configuration exception (fail-fast) + AC.

**Resolution**: FR2 now requires positive `expirationSeconds`, failing fast at pipeline build; AC added. ✅

---

### 4. `[CacheableQuery]` shorthand in the ACs names only the sync attribute (Score: 33)

FR1 defines two attributes, but the ACs/Problem Statement used bare `[CacheableQuery]` (literally the **sync** variant) for async-by-nature behaviours; no AC explicitly exercised the async attribute on the core hit/miss path.

**Evidence**: ACs "A query handler annotated with `[CacheableQuery]` …". FR1 defines sync + async as distinct.

**Recommendation**: Clarify `[CacheableQuery]` as generic shorthand, and exercise the async attribute in the core ACs.

**Resolution**: AC preamble now defines `[CacheableQuery]` as shorthand for "the applicable variant" and requires the core hit/miss criteria on both (async at minimum). ✅

---

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 1 |
| 0-49 (Low) | 3 |

**Total findings**: 4
**Findings at or above threshold (60)**: 1

_All four findings addressed in the revision that followed this review._
