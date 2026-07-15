# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: #349

## Problem Statement

As a Darker user, I would like to route a query to different handlers based on the query's
content and the query context — not just the query's static type — so that I can evolve how a
query is handled over time (or by other runtime conditions) without changing the query type or
forcing all callers onto a single handler implementation.

Darker currently only supports type-based routing: `IQueryHandlerRegistry` maps a query `Type` to
exactly one handler `Type`, fixed at registration time. Brighter already solves this problem for
commands/events via its "Agreement Dispatcher" (see
[Brighter docs](https://brightercommand.gitbook.io/paramore-brighter-documentation/brighter-request-handlers-and-middleware-pipelines/agreementdispatcher)
and `docs/adr/0031-support-agreement-dispatcher.md` in the Brighter repo). Darker should offer the
equivalent capability for queries.

A representative scenario: queries for a metric change shape after a given date — queries dated
before the cutover should be handled by the legacy handler, queries dated after by the new
handler, without the caller having to know which one applies.

## Proposed Solution

Allow a query type to be registered with a **routing function** instead of (or alongside) a fixed
handler type. The routing function is evaluated per-query-execution and receives both the query
instance and the `IQueryContext`, and resolves to the single handler type that should process
that particular query. Everything downstream of handler resolution — pipeline building, decorator
resolution, execution — behaves exactly as it does today; only *which handler type* is selected
changes.

Query types not registered with a routing function continue to use today's fixed type-to-handler
mapping, including `RegisterFromAssemblies` auto-scanning. Both styles of registration coexist in
the same `IQueryHandlerRegistry` / `IQueryHandlerRegistryAsync`.

## Requirements

### Functional Requirements

- A query type can be registered with a routing function that is given the query instance and the
  `IQueryContext`, and returns the single handler `Type` that should handle that specific query
  execution.
- The routing function is evaluated on every execution of that query type (not cached at
  registration time), so its result can depend on the query's data (e.g. a date field) and/or
  anything available on the context (e.g. `IQueryContext.Bag`).
- All handler types a routing function can possibly resolve to must be registered explicitly up
  front (see Constraints — no implicit discovery of routed handlers).
- If the routing function resolves to a handler type that was not registered as a candidate for
  that query type, this is a configuration/routing error, surfaced clearly to the caller.
- If the routing function resolves to no handler (e.g. returns `null`), this is a routing error,
  surfaced clearly to the caller — distinguishable from "no handler registered for this query
  type at all" (today's existing error).
- Supported for `QueryHandlerRegistry` (sync), `QueryHandlerRegistryAsync` (async), and
  `StreamQueryHandlerRegistry` (streaming). All three registries share the same
  `Get`/`Register`/`RegisterFromAssemblies` shape and the same eager, pre-execution handler-type
  resolution point, so agreement dispatch applies uniformly across them — resolving the handler
  type is unaffected by the streaming pipeline's later `IAsyncEnumerable` enumerator lifecycle,
  cancellation, or post-first-item exception handling (see ADR 0019), since those all occur after
  the handler type has already been chosen.
- Existing type-based registration (`Register<TQuery, TResult, THandler>()`,
  `RegisterFromAssemblies`) continues to work unchanged for queries that don't use agreement
  dispatch, in the same registry instance as agreement-dispatched queries.
- The routing function, once selected a handler type, hands off to the existing pipeline-building
  and decorator-resolution machinery unchanged — decorators attached to the resolved handler's
  `Execute`/`ExecuteAsync` method are still discovered and ordered exactly as today.

### Non-functional Requirements

- **Performance**: Routing function evaluation happens on the hot path (every query execution) —
  it must not introduce noticeable overhead beyond invoking a `Func`. No caching of routing
  decisions is required (results may legitimately differ per call).
- **Backwards compatibility**: No behavioural change for query types that don't opt into
  agreement dispatch. Existing registration APIs and call sites continue to compile and behave as
  they do today.
- **Clarity of failure**: Routing errors (unregistered candidate resolved, no match resolved) must
  produce exceptions that clearly distinguish "routing failed" from "no handler registered for
  this query type."

### Constraints and Assumptions

- Mirrors Brighter's constraint that `RegisterFromAssemblies` (auto-scan) cannot be combined with
  agreement dispatch for the same query type: the set of candidate handler types for an
  agreement-dispatched query must be registered explicitly, not discovered by assembly scanning.
  Other, non-routed query types in the same application can still use `RegisterFromAssemblies`.
- Unlike Brighter's `Publish`, which allows a routing function to return multiple handler types
  (multiple observers), Darker queries always produce exactly one `TResult`. The routing function
  therefore resolves to exactly one handler type per execution — not a list. This is a deliberate
  deviation from Brighter's `List<Type>`-returning signature, justified by Darker's single-result
  `Execute`/`ExecuteAsync` semantics.
- The routing function's input is the query instance itself (typed `TQuery`, so any of its own
  fields — e.g. a date — are naturally available for the routing decision) together with
  `IQueryContext`. Routing can depend on the properties of either. 
- Assumes existing `IQueryContext` (with its `Bag`, tracing, and resilience members) is sufficient
  for providing the contextual input for routing decisions — no new context members are required by this feature.

### Out of Scope

- Returning/executing multiple handlers for a single query (fan-out); Darker's query model remains
  strictly one handler, one result.
- Caching or memoizing routing decisions.
- Any change to decorator resolution/ordering, pipeline building beyond handler-type selection, or
  the `IQueryHandlerFactory` / `IQueryHandlerDecoratorFactory` lifecycle.
- DI container-specific registration helpers beyond what's needed to register the routing function
  and its candidate handler types (e.g. no MAUI-specific work).

## Acceptance Criteria

- A query type can be registered with a routing function and a set of candidate handler types; on
  execution, the handler chosen by the routing function (given the query and context) is the one
  that runs.
- Two queries of the same type, differing only in a field the routing function inspects (e.g. a
  date), are routed to different handlers within the same registry.
- Registering a routing function for a query type whose candidate handlers were also picked up by
  `RegisterFromAssemblies` is rejected/prevented (or otherwise clearly disallowed per the
  constraint above).
- A routing function that resolves to an unregistered/unexpected handler type throws a clear,
  distinct exception at execution time.
- A routing function that resolves to no handler throws a clear, distinct exception at execution
  time, different from "unregistered query type."
- Existing tests for type-based registration and dispatch continue to pass with their **behaviour and
  assertions unchanged**. Mechanical source-level updates required purely by a deliberate breaking API
  change in the approved design (e.g. widening the registry `Get` signature, which the design chose
  over adding an overload) are permitted at their call sites; no test's expected behaviour or
  assertions may change.
- New tests cover: routing to different handlers based on query content, routing based on
  `IQueryContext`, the "no match" error path, and the "unregistered candidate" error path — for
  sync, async, and streaming registries.

## Additional Context

Reference implementation: Brighter's `docs/adr/0031-support-agreement-dispatcher.md` and
`SubscriberRegistry.Register<TRequest>(routingFunc, handlerTypes)` overload. Brighter's routing
function signature is `(IRequest, IRequestContext) => List<Type>`; Darker's equivalent should be
`(TQuery, IQueryContext) => Type` (or `Type?`), reflecting the single-handler constraint above.
