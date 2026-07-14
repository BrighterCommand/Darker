# 20. Agreement Dispatch: Content- and Context-Based Handler Routing

Date: 2026-07-14

## Status

Accepted

## Context

Darker resolves a handler purely by query **type**: `IQueryHandlerRegistry` maps a query
`Type` to exactly one handler `Type`, fixed at registration time
(`QueryHandlerRegistry._registry : Dictionary<Type, Type>`). `PipelineBuilder` calls
`registry.Get(queryType)` and everything downstream (decorator discovery, pipeline build,
execution) hangs off the single handler type that lookup returns.

**Parent Requirement**: [specs/013-agreement_dispatcher/requirements.md](../../specs/013-agreement_dispatcher/requirements.md)

**Scope**: This ADR covers **one** architectural decision — *how a query is routed to a
handler type when the choice must depend on the query instance and the `IQueryContext`, not
just the query type*. It deliberately does **not** change decorator resolution/ordering,
pipeline construction beyond handler-type selection, the handler/decorator factory lifecycle,
or the streaming enumerator/cancellation semantics of ADR 0019.

The problem to solve (from the requirements): a query type must be registerable with a
**routing function** `(TQuery, IQueryContext) => Type` that is evaluated *per execution* and
selects one handler type from a set of candidates registered up front. This mirrors Brighter's
Agreement Dispatcher (`docs/adr/0031-support-agreement-dispatcher.md` in Brighter), but where
Brighter's `Publish` returns `List<Type>` (multiple observers), a Darker query yields exactly
one `TResult`, so the function returns exactly one `Type` (or `null` → routing error).

Forces at play:

- **Where does routing live?** The registry is the sole authority for "which handler type
  serves this query." Extending *that* responsibility keeps the change cohesive; putting routing
  in `PipelineBuilder` or `QueryProcessor` would smear the decision across two collaborators and
  duplicate it three times (sync/async/stream).
- **The lookup signature is too narrow.** `Get(Type)` cannot express a content-based decision —
  it never sees the query instance or the context. Both are already in scope at the one call
  site (`PipelineBuilder.Build*`), so widening the lookup is cheap and local.
- **Uniformity across three registries.** `QueryHandlerRegistry`, `QueryHandlerRegistryAsync`,
  and `StreamQueryHandlerRegistry` share the same `Get`/`Register`/`RegisterFromAssemblies`
  shape and the same eager, pre-execution resolution point. Routing must apply to all three
  identically. Because `IStreamQuery<T> : IQuery<T> : IQuery`, `IQuery` is a common base the
  lookup can accept for every registry.
- **Backwards behaviour must be preserved for non-routed queries**, but this is targeting **V5**,
  so breaking *API/interface* changes (widening `Get`, adding a `Register` overload) are
  acceptable where they buy a cleaner model.
- **Clarity of failure.** "No handler registered for this query type" (today's
  `ConfigurationException`) must remain distinct from the two new failure modes a routing
  function introduces: it resolved to `null`, or it resolved to a type that was not registered
  as a candidate.
- **netstandard2.0 is a target**, so default interface methods are unavailable — an evolution
  strategy relying on them would not compile. Direct interface evolution (allowed in V5) sidesteps
  this entirely.

## Decision

Model **every** registration as a *handler route* — an object that knows how to resolve a
handler `Type` for a given query execution — and give the registry a single, instance-aware
lookup that delegates to it. Type-based registration and agreement dispatch become two
implementations of the same routing role, stored side by side in one dictionary.

### Architecture Overview

```
                 Register(Type,Type,Type)          Register(router, candidates[])
                 RegisterFromAssemblies                     │
                          │                                 │
                          ▼                                 ▼
        Dictionary<Type, IResolveHandlers>
              queryType ─┬─────────────► FixedHandlerRoute { handlerType }
                         └─────────────► RoutedHandlers   { router, candidateSet }

PipelineBuilder.Build/BuildAsync/BuildStream (already holds query + context)
        │
        ▼
  registry.Get(queryType, query, context)
        │
        ├─ no entry for queryType ──────► return null  →  caller throws ConfigurationException
        │                                                 ("no handler registered …", unchanged)
        └─ entry found ─────────────────► route.ResolveHandlerType(query, context)
                                                 │
              FixedHandlerRoute ────────────────►  returns fixed handlerType (ignores query/ctx)
              RoutedHandlers   ────────────────►  t = router(query, ctx)
                                                   t == null            → throw RoutingException(NoHandlerResolved, queryType)      [schematic — see canonical ctor below]
                                                   t ∉ candidateSet     → throw RoutingException(UnregisteredCandidate, queryType, t) [schematic — see canonical ctor below]
                                                   otherwise            → return t
```

The chosen handler `Type` re-enters the **existing** machinery unchanged: `PipelineBuilder`
creates the handler via the factory, discovers decorators on its `Execute`/`ExecuteAsync`
method, and builds the pipeline exactly as today.

### Key Components

- **`IResolveHandlers` (new role — "deciding")** — the routing role. One method:
  `Type ResolveHandlerType(IQuery query, IQueryContext context)`. This is the seam that makes
  type-based and agreement dispatch interchangeable.
- **`FixedHandlerRoute` (new)** — implements `IResolveHandlers` by returning a single handler
  `Type`, ignoring the query and context. Produced by `Register(Type,Type,Type)` and
  `RegisterFromAssemblies`. Encapsulates *today's* behaviour.
- **`RoutedHandlers` (new)** — implements `IResolveHandlers` by invoking the user's routing
  `Func`, then validating the result against its candidate set. Produced by the new routing
  `Register` overload. Encapsulates *agreement dispatch*.
- **`IQueryHandlerRegistry` / `IQueryHandlerRegistryAsync` / `IStreamQueryHandlerRegistry`
  (evolved)** — `Get(Type)` widens to `Get(Type queryType, IQuery query, IQueryContext context)`;
  each gains a routing `Register` overload. Storage changes from
  `Dictionary<Type, Type>` to `Dictionary<Type, IResolveHandlers>`.
- **`RoutingException` (new)** — a *distinct* exception (not a `ConfigurationException`) for the
  two routing failure modes. It exposes a `RoutingFailure Reason { get; }` property backed by the
  new `enum RoutingFailure { NoHandlerResolved, UnregisteredCandidate }`. Canonical constructor:
  `RoutingException(RoutingFailure reason, Type queryType, Type resolvedHandlerType = null)` — the
  optional `resolvedHandlerType` is populated only for `UnregisteredCandidate`. The constructor
  composes the message from the reason so the two sub-cases read differently.
- **`PipelineBuilder<TResult>` (touched, minimally)** — `ResolveHandler`,
  `ResolveHandlerAsync`, `ResolveStreamHandler` take `(queryType, query, context)` and call the
  widened `Get`. Build methods already hold `query` and `queryContext`, so nothing new is
  threaded in from `QueryProcessor`.
- **`ServiceCollectionHandlerRegistry` / …Async / …Stream (touched)** — override the new routing
  `Register` overload to `TryAdd` each candidate handler type into the `IServiceCollection`
  (exactly as the existing override does for the single type-based handler), then delegate to
  base.

### Technology Choices

- **A first-class role (`IResolveHandlers`) over a `Func`-in-a-dictionary or a second parallel
  dictionary.** Two implementations of one role keep the registry's `Get` free of `if
  (isRouted)` branching, make each policy independently testable, and name the concept. A bare
  `Func<IQuery,IQueryContext,Type>` value type would work mechanically but hides the candidate
  set and validation behind a closure and offers nowhere to hang the "fixed vs routed" identity
  used by the duplicate-registration guard.
- **Widen `Get` rather than add an overload.** Resolution genuinely needs the query and context
  now; a lingering `Get(Type)` would be a half-working method (it cannot resolve a routed entry).
  V5 permits the breaking signature change, and there is exactly one production call site.
- **`RoutingException` derives from `Exception`, not `ConfigurationException`.** The requirement
  demands routing failures be distinguishable from "no handler registered." Sharing a base would
  let a `catch (ConfigurationException)` swallow both; a sibling type keeps them separable. The
  `enum RoutingFailure` (`NoHandlerResolved`, `UnregisteredCandidate`), surfaced via the
  `RoutingException.Reason` property, distinguishes the two routing sub-cases programmatically
  without needing two exception classes.
- **Type-erased router storage.** The public overload is generic
  (`Func<TQuery,IQueryContext,Type>`) for a typed, ergonomic call site; `RoutedHandlers` stores
  it as `Func<IQuery,IQueryContext,Type>` via a thin cast wrapper `(q,ctx) => router((TQuery)q,ctx)`.
  The `(TQuery)q` cast is always safe: the route is keyed on `typeof(TQuery)` and `Get` is only
  ever reached with `query.GetType() == queryType` (see `PipelineBuilder.Build*`, which key the
  lookup on `query.GetType()`), so the runtime query type always matches the closure's `TQuery`.

### Implementation Approach

Registry (`QueryHandlerRegistry`, mirrored in the async and stream registries):

- Storage becomes `Dictionary<Type, IResolveHandlers>`.
- `Get(Type queryType, IQuery query, IQueryContext context)`:
  `_routes.TryGetValue(queryType, out var route) ? route.ResolveHandlerType(query, context) : null`.
  This gives `Get` **three** outcomes, where today it has two:
  - *absent query type* → returns `null` (unchanged — the caller `PipelineBuilder` still throws the
    existing `ConfigurationException("No … handler registered …")`);
  - *present, resolvable* → returns the handler `Type` (unchanged);
  - *present routed entry that resolves to `null` or a non-candidate* → **throws `RoutingException`**
    (new — `Get` was previously a never-throws pure lookup).
  The interface XML docs on all three registries (e.g. `IStreamQueryHandlerRegistry.Get`, which
  currently reads "*…or null if not registered*") must be rewritten to document these three
  outcomes.
- Existing `Register(Type,Type,Type)` keeps its duplicate-key and result-type validation, then
  stores a `FixedHandlerRoute`. `RegisterFromAssemblies` is unchanged (it routes through
  `Register`).
- New overload, carrying the **same generic constraints** as the existing
  `Register<TQuery,TResult,THandler>()` on each registry (this is why the constraint differs by
  registry):
  - sync: `Register<TQuery,TResult>(Func<TQuery,IQueryContext,Type> router, params Type[] candidateHandlerTypes) where TQuery : IQuery<TResult>`
  - async: same signature, `where TQuery : IQuery<TResult>`
  - stream: same signature, `where TQuery : IStreamQuery<TResult>`

  It rejects a duplicate query-type key (this is what makes "agreement dispatch cannot be
  combined with `RegisterFromAssemblies`/type-based registration for the same query type" fall
  out automatically — both live in one dictionary), validates that every candidate implements the
  registry's handler interface (`IQueryHandler<TQuery,TResult>` / `IQueryHandlerAsync<TQuery,TResult>`
  / `IStreamQueryHandler<TQuery,TResult>`) for `TResult`, then stores a `RoutedHandlers`. Candidate
  validation is a runtime `IsAssignableFrom` check (the `params Type[]` cannot be constrained at
  compile time), throwing `ConfigurationException` for a candidate that does not implement the
  registry's handler interface — a *registration-time* configuration error, distinct from the
  *execution-time* `RoutingException`.

`RoutedHandlers.ResolveHandlerType(query, context)`:

```
var handlerType = _router(query, context);
if (handlerType is null)
    throw new RoutingException(RoutingFailure.NoHandlerResolved, _queryType);
if (!_candidates.Contains(handlerType))
    throw new RoutingException(RoutingFailure.UnregisteredCandidate, _queryType, handlerType);
return handlerType;
```

`PipelineBuilder`: change the three `ResolveHandler*` helpers to accept `(query, context)` and
call the widened `Get`. No change to how the returned type is used.

DI extensions: override the routing `Register` overload to register each candidate handler type
in the container before delegating to base — symmetric with the current single-handler override.

**Implementation sequencing (Tidy First).** Split into two commits, structural before behavioural:

1. *Structural (no behaviour change):* introduce `IResolveHandlers` + `FixedHandlerRoute`, change
   registry storage to `Dictionary<Type, IResolveHandlers>`, and route today's
   `Register(Type,Type,Type)` through `FixedHandlerRoute`. Widening `Get` and threading
   `(query, context)` through `PipelineBuilder` is part of this step — the values are passed but
   `FixedHandlerRoute` ignores them, so existing tests pass unmodified.
2. *Behavioural:* add `RoutedHandlers`, the routing `Register` overload, `RoutingException`, and
   the DI override. This is the only commit that introduces new observable behaviour and new tests.

## Consequences

### Positive

- **One cohesive home for routing.** The "which handler type" decision stays entirely inside the
  registry/route role; `PipelineBuilder` and `QueryProcessor` are oblivious to whether a query is
  agreement-dispatched.
- **The awkward constraint is free.** "Can't combine auto-scan with agreement dispatch for the
  same query type" is enforced by the pre-existing single-dictionary duplicate guard — no special
  case.
- **Uniform across sync/async/stream** because all three share the `IResolveHandlers` seam and the
  common `IQuery` base; streaming is unaffected because resolution completes before any
  `IAsyncEnumerable`/cancellation concerns (ADR 0019) arise.
- **Non-routed queries are behaviourally identical** — they resolve through `FixedHandlerRoute`,
  and the absent-type path still yields the same `ConfigurationException`.
- **Failure modes are clearly separated**: unregistered query type (`ConfigurationException`) vs.
  routed-to-null vs. routed-to-unregistered-candidate (`RoutingException` with a `Reason`).

### Negative

- **Breaking API surface (accepted for V5):** the three registry interfaces change `Get`'s
  signature and gain a `Register` overload; internal storage type changes. Any external custom
  `IQueryHandlerRegistry` implementation must be updated.
- **`Get`'s exception contract widens, not just its signature.** Today `Get` is a pure
  never-throws lookup (`Type`-or-`null`), as its XML docs state. After this change a present routed
  entry can make `Get` **throw** `RoutingException`. Callers and external implementers that assumed
  `Get` never throws must account for this, and the interface XML docs must be updated (see
  Implementation Approach). This is a behavioural contract change on the lookup itself, above and
  beyond the signature break.
- **A new role + two classes + an exception** — more types than a two-dictionary hack, in
  exchange for cohesion and testability.
- **Candidate handler types must be registered twice conceptually** — once as the routing
  candidate allow-list, and (in DI) once in the container so the factory can create them. The DI
  override hides this, but hand-rolled factory setups must ensure candidates are creatable.

### Risks and Mitigations

- **Risk:** routing `Func` on the hot path adds overhead. **Mitigation:** resolution is a single
  delegate invocation plus a `HashSet.Contains`; no reflection or allocation beyond what type-based
  resolution already does. The NFR only requires "no overhead beyond invoking a `Func`."
- **Risk:** a routing `Func` that throws leaks an arbitrary exception. **Mitigation:** document
  that user routing functions should be total; the registry only wraps the *decision* outcomes
  (null / non-candidate) in `RoutingException`, and does not swallow exceptions thrown *inside*
  the func (they surface to the caller as-is, preserving stack trace).
- **Risk:** candidate set and DI registration drift (a candidate returned by the func but never
  added to the container). **Mitigation:** the DI overload registers exactly the declared
  candidate array, so the allow-list and the container are populated from the same source.
- **Risk:** the `Dictionary<Type, IResolveHandlers>` is mutated at registration and read on the
  execution hot path (via widened `Get`). **Mitigation:** this profile is unchanged from today's
  `Dictionary<Type, Type>` — registration is assumed to complete during startup, *before* any query
  executes, and the registry is not mutated concurrently with resolution. No runtime re-registration
  is supported; no new synchronisation is introduced or required.
- **Risk:** trimming/AOT regression (`Paramore.Darker` sets `IsAotCompatible` for the net8.0/net9.0
  targets — `Condition="'$(TargetFramework)' != 'netstandard2.0'"`).
  **Mitigation:** routing adds no reflection beyond the *existing* resolution path — the routing
  `Func` is user-supplied delegate invocation, and the chosen handler `Type` flows into the same
  factory `Create(Type)` / `GetMethod("Execute")` machinery already used for type-based dispatch.
  The trimming/AOT posture is therefore unchanged.

## Alternatives Considered

- **Second parallel `Dictionary<Type, (Func, ISet<Type>)>` for routes, `Get(Type)` unchanged,
  new `Get(Type,IQuery,IQueryContext)` overload.** Non-breaking, but leaves two lookups, an
  `if (routed) … else …` in the builder, a half-working `Get(Type)` for routed entries, and no
  named concept for "a route." Rejected: V5 lets us pick the cleaner unified model.
- **Routing in `PipelineBuilder`/`QueryProcessor`.** The processor would consult a routing map,
  then call `registry.Get`. Rejected: it duplicates the decision across sync/async/stream and
  splits the "which handler" responsibility away from its natural owner, the registry.
- **A capability side-interface (`IResolveHandlers` implemented *alongside* the registry, detected
  via `is`).** This was the only non-breaking evolution available under netstandard2.0 (no default
  interface methods). Rejected because V5 permits direct interface evolution, which avoids runtime
  type-probing and a bifurcated resolution path.
- **Routing `Func` returning `List<Type>` (Brighter parity).** Rejected per requirements: a Darker
  query yields exactly one `TResult`; a single-`Type` return encodes that invariant in the type
  system instead of validating a list length at runtime.
- **Make `RoutingException : ConfigurationException`.** Rejected: it would let existing
  `catch (ConfigurationException)` blocks silently absorb routing failures, defeating the
  "clearly distinguishable" requirement.

## References

- Requirements: [specs/013-agreement_dispatcher/requirements.md](../../specs/013-agreement_dispatcher/requirements.md)
- Related ADRs:
  - `docs/adr/0019-streaming-query-pipeline.md` — streaming resolution point this routing sits ahead of
  - `docs/adr/0011-*` — `ExportedTypes` scanning that agreement dispatch must *not* be combined with
- External: Brighter `docs/adr/0031-support-agreement-dispatcher.md`;
  [Brighter Agreement Dispatcher docs](https://brightercommand.gitbook.io/paramore-brighter-documentation/brighter-request-handlers-and-middleware-pipelines/agreementdispatcher)
