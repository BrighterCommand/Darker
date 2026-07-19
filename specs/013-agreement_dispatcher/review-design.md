# Review: design — agreement_dispatcher (re-review)

**Date**: 2026-07-14
**Threshold**: 60
**Verdict**: PASS
**ADR reviewed**: `docs/adr/0020-agreement-dispatch-handler-routing.md`

> Re-review after the six prior findings were addressed. All six were genuinely resolved (not
> merely reworded). Two new low-severity issues surfaced from the edits — both below threshold and
> both fixed post-review (see "Post-review fixes").

## Findings

### 1. ADR stated a false codebase fact: `Paramore.Darker` does NOT set `IsTrimmable` (Score: 48)

The F5 edit added a trimming/AOT Risk whose parenthetical asserted a build-property fact that does
not hold. `IsTrimmable` appears in no `.csproj` in the repo. `Paramore.Darker.csproj` sets only
`IsAotCompatible`, and even that is **conditional** — `Condition="'$(TargetFramework)' != 'netstandard2.0'"`
— so it applies to net8.0/net9.0 only. The mitigation's *conclusion* (routing adds no reflection
beyond the existing path, so posture is unchanged) is sound, but the grounding claim was a
verifiable falsehood.

**Evidence**: `src/Paramore.Darker/Paramore.Darker.csproj:7` sets only
`<IsAotCompatible Condition="'$(TargetFramework)' != 'netstandard2.0'">true</IsAotCompatible>`;
`grep -rn IsTrimmable --include=*.csproj` returns nothing.

**Recommendation**: Drop `/IsTrimmable`; say "sets `IsAotCompatible` (net8.0/net9.0 targets)".
**→ Fixed** (see Post-review fixes).

---

### 2. Flow diagram showed a `RoutingException` construction not matching the canonical constructor (Score: 40)

Prose and code block were consistent with the canonical
`RoutingException(RoutingFailure, Type queryType, Type resolvedHandlerType = null)` (mandatory
`queryType`), but the flow diagram still showed the one-argument schematic form, omitting the
mandatory `queryType` — the exact diagram-vs-code inconsistency F2 targeted.

**Evidence**: ADR flow diagram `→ throw RoutingException(RoutingFailure.NoHandlerResolved)` vs.
canonical ctor and code `new RoutingException(RoutingFailure.NoHandlerResolved, _queryType)`.

**Recommendation**: Add `queryType` to the diagram labels or annotate them as schematic.
**→ Fixed** (see Post-review fixes).

---

## Prior findings status

- **F1 (Get can throw / three outcomes / XML docs) — RESOLVED.** Now an explicit Negative
  consequence ("`Get`'s exception contract widens, not just its signature… can make `Get` **throw**…
  interface XML docs must be updated") plus the three-outcome enumeration in Implementation Approach.
  Verified against `IStreamQueryHandlerRegistry.cs:10-13` and `QueryHandlerRegistry.cs:18-21`.
- **F2 (one enum name + one canonical ctor) — RESOLVED** (residual diagram nit → Finding 2, now
  fixed). Enum uniformly `RoutingFailure` with a `Reason` property; prose and code match the
  canonical constructor.
- **F3 (per-registry generic constraints + candidate validation) — RESOLVED.** sync/async
  `where TQuery : IQuery<TResult>`, stream `where TQuery : IStreamQuery<TResult>`, matching the real
  overloads (`QueryHandlerRegistry.cs:24`, `QueryHandlerRegistryAsync.cs:24`,
  `StreamQueryHandlerRegistry.cs:22`); `params Type[]` candidates validated by runtime
  `IsAssignableFrom` throwing `ConfigurationException` at registration. Handler interface names
  `IQueryHandler<,>` / `IQueryHandlerAsync<,>` / `IStreamQueryHandler<,>` verified correct.
- **F4 (registration-vs-execution concurrency) — RESOLVED.** States registration completes before
  execution, no concurrent mutation, no runtime re-registration, no new synchronisation — matching
  the unsynchronised `Dictionary` at `QueryHandlerRegistry.cs:11`.
- **F5 (trimming/AOT posture) — RESOLVED in intent**, but introduced a factual error (Finding 1,
  now fixed).
- **F6 (`(TQuery)q` cast safety) — RESOLVED.** Justified via the `typeof(TQuery)` key and
  `query.GetType()` lookup; verified at `PipelineBuilder.cs:64/125/279`.

Additional grounding re-verified true: `Dictionary<Type,Type>` storage and
`Get`/`Register`/`RegisterFromAssemblies` shape across all three registries; `RegisterFromAssemblies`
funnels through `Register(Type,Type,Type)`; `IStreamQuery<T> : IQuery<T> : IQuery`;
`ConfigurationException` is `sealed`; the three `ServiceCollection*` registries are `sealed` and
override `Register(Type,Type,Type)` then delegate to base; `netstandard2.0` is a target.

---

## Post-review fixes (applied after this pass)

- Finding 1: replaced the false `IsAotCompatible`/`IsTrimmable` claim with the accurate
  "sets `IsAotCompatible` for the net8.0/net9.0 targets (`Condition != netstandard2.0`)".
- Finding 2: the flow-diagram `RoutingException(...)` labels now include `queryType` and are marked
  "schematic — see canonical ctor below".

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
