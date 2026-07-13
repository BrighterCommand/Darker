# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: [#299](https://github.com/BrighterCommand/Darker/issues/299) — Add streaming query support with `IAsyncEnumerable`

## Problem Statement

Today Darker only supports request/response queries: a query returns a single, fully-materialised
`TResult` via `IQueryProcessor.Execute` / `ExecuteAsync`. For large result sets, paged data, or
real-time feeds this forces the whole result into memory before the caller sees the first item.

> **As an** application developer using Darker,
> **I would like** to execute a query that yields its results incrementally as an
> `IAsyncEnumerable<TResult>`,
> **so that** I can process, forward, or render items as they arrive — enabling large/unbounded
> result sets, server-sent events, and gRPC server-streaming — without buffering the entire result
> set in memory.

## Proposed Solution

Add a first-class streaming path to Darker that mirrors the existing async request/response path:

- A way to declare a **streaming query** whose result is a stream of `TResult` items.
- A **streaming handler** contract that yields items via
  `IAsyncEnumerable<TResult> ExecuteAsync(TQuery query, CancellationToken ct)`.
- A **processor entry point** to execute a streaming query and consume it with `await foreach`.
- Streaming queries flow through the **same attribute-driven decorator pipeline** as ordinary
  queries, with clearly defined semantics for each decorator kind (see below).

From the caller's perspective:

```csharp
await foreach (var item in queryProcessor.ExecuteStreamAsync(new MyStreamQuery(), ct))
{
    // handle each item as it is produced
}
```

The precise API shape (`IStreamQuery<TResult>` vs `IQuery<IAsyncEnumerable<TResult>>`, a new
`ExecuteStreamAsync` on `IQueryProcessor` vs a separate `IStreamQueryProcessor`, and whether this
lives in core or a separate package) is an **open design decision deferred to the ADR** — this
document records the required behaviour, not the mechanism.

## Requirements

### Functional Requirements

- **FR1 — Streaming query contract**: Provide a way to declare a query whose result is a stream of
  `TResult` items, distinct from an ordinary `IQuery<TResult>`.
- **FR2 — Streaming handler contract**: Provide a handler contract exposing
  `IAsyncEnumerable<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken)`.
- **FR3 — Processor entry point**: Provide a processor method that returns
  `IAsyncEnumerable<TResult>` for a streaming query so callers can `await foreach` over it.
- **FR4 — Handler registration & resolution**: Streaming queries resolve to their handler via the
  existing registry/factory mechanism, consistent with how sync/async handlers are registered
  (including DI registration via `AddDarker(...).AddHandlers...`).
- **FR5 — Cancellation**: The `CancellationToken` passed to the processor flows to the handler and
  observing cancellation stops enumeration promptly; `[EnumeratorCancellation]` semantics are
  honoured for `await foreach (... .WithCancellation(ct))`.
- **FR6 — Lazy / incremental execution**: The handler body must not run to completion before the
  first item is observed; items are produced on demand as the caller enumerates (no eager
  buffering of the full result set by the framework).
- **FR7 — Decorator pipeline semantics** for streaming queries must be explicitly defined per
  decorator category:
  - **Logging / Telemetry**: wrap the whole stream lifecycle (start, completion, item count and/or
    per-item as designed), and record exceptions raised during enumeration.
  - **Retry**: define semantics explicitly — retrying mid-stream can re-emit already-yielded items,
    so behaviour must be specified (e.g. retry only before first item, or documented as
    unsupported / no-op for streams).
  - **Fallback**: define how fallback applies when enumeration faults (e.g. fall back to an
    alternate stream, or documented as unsupported).
  - **Caching** (if/when present): documented behaviour for streams (likely not applicable).
- **FR8 — Coexistence**: Streaming and request/response querying coexist in one model. Streaming is
  preferably additive, but per NFR2 the request/response surface (`Execute` / `ExecuteAsync`, query
  and handler contracts) **may be reshaped** if doing so produces a cleaner unified design for V5.
  The end state must still support ordinary single-result queries.
- **FR9 — Query context**: A streaming query has access to `IQueryContext` (bag, policies, tracing
  span) consistent with the async path, for the lifetime of the stream.

### Non-functional Requirements

- **NFR1 — Memory**: Framework overhead per stream is O(1) with respect to result-set size; the
  framework does not materialise the full sequence.
- **NFR2 — Simplicity over compatibility (V5)**: This feature targets the **V5** major release, so
  **breaking changes to existing public APIs are permitted where they yield a simpler, more elegant
  design**. Prefer the cleanest streaming model even if it means changing `IQueryProcessor`,
  `IQuery<TResult>`, handler base classes, or decorator interfaces. Breaking changes are not a goal
  in themselves — introduce them only when they materially improve clarity/elegance, and record
  each one (with migration notes) in the ADR and release notes.
- **NFR3 — Target frameworks**: Must build across Darker's current targets. `IAsyncEnumerable`
  is native on `net8.0`/`net9.0`; on `netstandard2.0` it requires `Microsoft.Bcl.AsyncInterfaces`.
  Whether `netstandard2.0` is supported for streaming, or streaming requires `net8.0+`, is a design
  decision for the ADR (issue #299 suggests .NET 8+).
- **NFR4 — AOT / trimming**: Preserve existing AOT-compatibility posture (`IsAotCompatible` on
  net8.0+); the streaming path must not introduce trim/AOT-hostile reflection beyond what the
  existing pipeline already uses.
- **NFR5 — Consistency**: The streaming API should feel like a natural sibling of the existing
  async API (naming, cancellation, context, DI registration) to minimise the learning curve.

### Constraints and Assumptions

- **C1**: Darker is in-process, query-side only (CQRS read side). Streaming here means in-process
  `IAsyncEnumerable`, not a network/transport streaming protocol — transport concerns (SSE, gRPC)
  are the caller's responsibility.
- **C2**: The existing pipeline builds decorators from attributes on the handler's execute method
  and invokes via reflection (`PipelineBuilder`). Streaming must integrate with, or deliberately
  parallel, this mechanism.
- **C3**: `netstandard2.0` support for `IAsyncEnumerable` requires an added dependency
  (`Microsoft.Bcl.AsyncInterfaces`); adding it is subject to the dependency-management guidelines.
- **A1**: Assumes consumers target a runtime where `IAsyncEnumerable` is available (net8.0+, or
  netstandard2.0 with the BCL async-interfaces package).
- **A2**: Assumes the same handler/decorator lifetime model (created per-query, released after the
  pipeline completes) — for streams, "completion" means the stream is fully enumerated or disposed.

### Out of Scope

- **OOS1**: Synchronous streaming (`IEnumerable<TResult>` / `yield return` on a sync handler). Only
  `IAsyncEnumerable<TResult>` is in scope.
- **OOS2**: Transport-level streaming implementations (server-sent events endpoints, gRPC service
  wiring). Samples may demonstrate consumption but the transport is not part of Darker.
- **OOS3**: Back-pressure/flow-control primitives beyond what `IAsyncEnumerable` + `await foreach`
  naturally provide.
- **OOS4**: Bidirectional or client-streaming patterns; only server-to-caller result streaming.
- **OOS5**: Gratuitous changes to request/response *behaviour/semantics*. Note: per NFR2/FR8, V5
  *structural* reshaping of the request/response API (signatures, contracts) is permitted where it
  yields a cleaner unified model — what's out of scope is changing what existing queries *do*
  without a design reason.
- **OOS6**: Caching of streamed results.

## Acceptance Criteria

**Definition of done:**

- A streaming query + handler can be defined, registered, and executed, and the caller can
  `await foreach` the results.
- Items are produced lazily (verifiable: a handler that logs/records per-item production shows the
  first item observed before the handler has produced the last).
- Cancellation via the token stops enumeration promptly and propagates
  `OperationCanceledException` as expected.
- The decorator pipeline runs for a streaming query with the semantics defined in FR7, and each
  decorator category's behaviour is covered by a test.
- Exceptions thrown during enumeration surface to the caller (unwrapped, preserving stack trace,
  consistent with the existing `TargetInvocationException` handling).
- The solution builds and its test suite is green on all supported target frameworks (NFR3). Where
  V5 breaking changes reshape the request/response surface (NFR2), existing tests are updated to
  the new API rather than treated as a compatibility contract; any behavioural change is deliberate
  and documented.

**Testing approach:**

- TDD per the mandatory `/test-first` workflow: each behaviour is specified by a failing test that
  is approved before implementation.
- Use real/Simple/InMemory test doubles (registries, `SimpleHandlerFactory`,
  `InMemoryDecoratorRegistry`, `InMemoryQueryContextFactory`) per project conventions; Moq only as
  a last resort.
- Cover: happy-path enumeration, lazy production, cancellation mid-stream, exception mid-stream,
  and each decorator category from FR7.

**Success metrics:**

- Feature parity intent with MediatR's `IStreamRequest<T>` / `IStreamRequestHandler<T>` model,
  adapted to Darker's decorator pipeline.
- Zero breaking changes to existing public API.

## Additional Context

### Prior art — MediatR streaming

Reviewed <https://deepwiki.com/jbogard/MediatR/3.2-streaming-support> and pulled the **verbatim
signatures from the MediatR source** (jbogard/MediatR, `master`). MediatR keeps the streaming path
*fully parallel* to request/response — a separate marker, a separate execute method, and a separate
behavior pipeline:

```csharp
// src/MediatR.Contracts/IStreamRequest.cs
public interface IStreamRequest<out TResponse> { }

// src/MediatR/IStreamRequestHandler.cs
public interface IStreamRequestHandler<in TRequest, out TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

// src/MediatR/ISender.cs — sibling of Send<TResponse>(...), NOT an overload of it
IAsyncEnumerable<TResponse> CreateStream<TResponse>(
    IStreamRequest<TResponse> request, CancellationToken cancellationToken = default);
IAsyncEnumerable<object?> CreateStream(
    object request, CancellationToken cancellationToken = default);

// src/MediatR/IStreamPipelineBehavior.cs — distinct from IPipelineBehavior
public delegate IAsyncEnumerable<TResponse> StreamHandlerDelegate<out TResponse>();

public interface IStreamPipelineBehavior<in TRequest, TResponse>
    where TRequest : notnull
{
    IAsyncEnumerable<TResponse> Handle(
        TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
```

How this lands on our deferred design questions:

- **Q1 (marker)**: MediatR uses a dedicated `IStreamRequest<out TResponse>` marker, *not* an
  overload of `IRequest<T>`. Prior art for a dedicated `IStreamQuery<TResult>`.
- **Q2 (execute method)**: `CreateStream` is a **sibling** of `Send` on `ISender`, returning
  `IAsyncEnumerable<TResponse>` directly (no `Task` wrapper). Prior art for `ExecuteStreamAsync`
  as its own method rather than reusing `ExecuteAsync`.
- **Q5 + FR7 (decorators)**: `IStreamPipelineBehavior<TRequest, TResponse>` is a **separate
  contract** from `IPipelineBehavior`; `next` is a parameterless `StreamHandlerDelegate<TResponse>`
  returning `IAsyncEnumerable<TResponse>` (contrast with Darker's `IQueryHandlerDecoratorAsync`
  whose `next` is `Func<TQuery, CancellationToken, Task<TResult>>`). Strong prior art that Darker
  needs a stream-specific decorator contract, not a reuse of the async one.
- **FR5 (cancellation)**: the handler takes a `CancellationToken` directly; MediatR's docs stress
  `[EnumeratorCancellation]` for correct `IAsyncEnumerable<T>` cancellation.
- **FR6/NFR1 (lazy)**: MediatR warns handlers not to buffer all items — incremental, not eager.

> Naming note: Darker uses `ExecuteAsync`/`Execute` where MediatR uses `Send`, so the Darker
> equivalents would read as `ExecuteStreamAsync` (processor) and a `Handle`-style stream method on
> the handler (or `ExecuteAsync` returning `IAsyncEnumerable<TResult>`) — final naming is an ADR
> decision.

Implication for Darker: MediatR's model is *fully parallel* (separate marker, separate execute
method, separate behavior pipeline) rather than overloading the request/response path. The ADR
should weigh adopting that same fully-parallel shape versus a lighter integration into the existing
`PipelineBuilder`.
- Codebase grounding (current async model to mirror):
  - `IQueryProcessor` — `src/Paramore.Darker/IQueryProcessor.cs` (`ExecuteAsync<TResult>`)
  - Async handler — `src/Paramore.Darker/IQueryHandlerAsync.cs`,
    `src/Paramore.Darker/QueryHandlerAsync.cs`
  - Async decorator — `src/Paramore.Darker/IQueryHandlerDecoratorAsync.cs`
  - Pipeline construction — `src/Paramore.Darker/PipelineBuilder.cs` (`BuildAsync` Func chain)
  - Target frameworks — `src/Paramore.Darker/Paramore.Darker.csproj`
    (`netstandard2.0;net8.0;net9.0`)
- Key **open design questions for the ADR** (deferred, not decided here):
  1. `IStreamQuery<TResult>` marker vs reusing `IQuery<IAsyncEnumerable<TResult>>`.
  2. `ExecuteStreamAsync` on `IQueryProcessor` vs a separate `IStreamQueryProcessor`.
  3. Core library vs a separate `Paramore.Darker.Streaming` package.
  4. `netstandard2.0` support (via `Microsoft.Bcl.AsyncInterfaces`) vs net8.0+ only.
  5. Retry/fallback semantics for a faulting stream (supported, restricted, or no-op).
