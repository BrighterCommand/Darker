# 19. Streaming Query Pipeline

Date: 2026-07-09

## Status

Accepted

## Context

Darker today supports only request/response queries: a query returns a single, fully-materialised
`TResult` through `IQueryProcessor.Execute` / `ExecuteAsync`. For large result sets, paged data, or
real-time feeds this forces the whole result into memory before the caller sees the first item.

**Parent Requirement**: [specs/012-streaming_results/requirements.md](../../specs/012-streaming_results/requirements.md)

**Scope**: This ADR decides the **public API shape and pipeline mechanics** for streaming queries
that yield results incrementally as `IAsyncEnumerable<TResult>` — a single, cohesive architectural
decision covering the query/handler/decorator contracts, the processor entry point, how the
existing `PipelineBuilder` is extended, handler resolution, span/lifetime management, and
target-framework support. It resolves the five open questions recorded in the requirements.

### Forces at play

- **V5 targeting (NFR2)** — this ships in the V5 major release, so breaking changes are permitted
  where they yield a simpler, more elegant design. We are *not* constrained to bolt streaming onto
  the existing interfaces without touching them.
- **Consistency (NFR5)** — the streaming API should read as a natural sibling of the existing
  async path (naming, cancellation, `IQueryContext`, DI registration) so there is "one obvious way"
  to do each thing.
- **Laziness (FR6 / NFR1)** — the framework must not buffer the sequence; items are produced on
  demand. This has sharp consequences for span lifetime, handler/decorator lifetime, and exception
  propagation, because an `async` iterator method **defers execution until enumeration**.
- **Existing machinery** — `PipelineBuilder<TResult>` (internal) resolves the handler by type from
  a registry, builds a decorator chain from attributes ordered by `Step`, and invokes via
  reflection. Streaming must extend this machinery, not fork it.
- **Prior art** — MediatR keeps streaming *fully parallel* to request/response: a dedicated
  `IStreamRequest<out TResponse>` marker, a dedicated `CreateStream` method returning
  `IAsyncEnumerable<TResponse>` (no `Task`), and a dedicated `IStreamPipelineBehavior<TRequest,
  TResponse>` whose `next` is a `StreamHandlerDelegate<TResponse>`. (See requirements — verbatim
  signatures.)

### The reflection/laziness interaction (why this ADR is not just "add another overload")

The current pipeline calls `MethodInfo.Invoke(handler, args)` and unwraps
`TargetInvocationException` via `ExceptionDispatchInfo`. For an `async IAsyncEnumerable<T>` iterator
method, `Invoke` returns the enumerable **immediately without running the body** — the body runs
during `MoveNextAsync`. Therefore:

- Handler exceptions surface during `await foreach`, *not* from `Invoke`, so the
  `TargetInvocationException` unwrap that guards `Execute`/`ExecuteAsync` is neither triggered nor
  needed for enumeration faults — they propagate naturally with their original stack trace.
- The processor cannot use its usual `try { return Invoke(); } finally { EndSpan(); } ` +
  `using (pipelineBuilder)` shape: those would end the span and release the handler/decorators
  *before the first item is produced*. Span and pipeline lifetime must instead be bound to the
  **enumeration** lifetime.

## Decision

Adopt a **fully-parallel streaming path** modelled on MediatR's shape but expressed in Darker's
idiom (Execute-style naming, `IQueryContext`, attribute-driven decorators, per-query
factory/lifetime). Concretely:

### 1. A dedicated stream query marker — `IStreamQuery<out TResult>`

```csharp
// TResult is the ITEM type, not the enumerable.
public interface IStreamQuery<out TResult> : IQuery<TResult> { }
```

- **`TResult` is the item type** (a `IStreamQuery<Order>` yields `Order` items), *not*
  `IAsyncEnumerable<Order>`. Reusing `IQuery<IAsyncEnumerable<TResult>>` instead was rejected: it
  routes through the single-result pipeline as `Task<IAsyncEnumerable<TResult>>`, defeating laziness
  and reading as "a task of an enumerable" rather than "a query that yields a stream."
- **Derives from the generic `IQuery<TResult>`** (not the non-generic `IQuery`). This is the choice
  that actually compiles against the existing tracing code: `Query<TResult>` — the id/tracing base
  class — is declared `Query<TResult> : IQuery<TResult>` (`Observability/Query.cs`), and
  `IAmADarkerTracer.CreateQuerySpan<TResult>(IQuery<TResult> query, …)` plus its
  `query is Query<TResult>` id extraction (`Observability/DarkerTracer.cs`) both require an
  `IQuery<TResult>`. A stream query can therefore reuse `Query<TResult>` for a stable `Id` and be
  passed to `CreateQuerySpan` unchanged. Here `TResult` is the item type, so the span is typed on
  the item — acceptable and consistent with treating `TResult` as the item type throughout.
- **Stream-vs-single dispatch is by method + registry, not by the marker.** Because
  `IStreamQuery<TResult>` *is* an `IQuery<TResult>`, the marker alone no longer separates the two
  worlds. Separation comes from (a) the caller choosing `ExecuteStream` vs `ExecuteAsync`, and (b)
  `ExecuteStream` resolving from `IStreamQueryHandlerRegistry` while `ExecuteAsync` resolves from
  `IQueryHandlerRegistryAsync` (§5). **Consequence to document:** a stream query passed to
  `ExecuteAsync` compiles (it is an `IQuery<TResult>`) but fails cleanly at handler lookup with a
  `ConfigurationException` ("no async handler registered"), because stream handlers live only in the
  stream registry — a clear, early error rather than silent misbehaviour.

### 2. A dedicated stream handler — `IStreamQueryHandler<TQuery, TResult>`

```csharp
public interface IStreamQueryHandler<in TQuery, TResult> : IQueryHandler
    where TQuery : IStreamQuery<TResult>
{
    IAsyncEnumerable<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default);
}

public abstract class StreamQueryHandler<TQuery, TResult> : IStreamQueryHandler<TQuery, TResult>
    where TQuery : IStreamQuery<TResult>
{
    public IQueryContext Context { get; set; }
    public abstract IAsyncEnumerable<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default);
}
```

- Reuses the existing non-generic `IQueryHandler` base (for `Context`) so the existing
  **factory and lifetime machinery apply unchanged**.
- **No `Fallback` method.** Unlike single-result handlers, "fall back to a single value" does not
  map onto "a partially-emitted stream." Dropping fallback from the stream contract is the simpler,
  more honest V5 choice; a handler that needs a fallback stream composes it internally.
- Implementers put `[EnumeratorCancellation]` on their iterator method's token parameter (it is an
  implementation concern, not part of the interface) so `await foreach (... .WithCancellation(ct))`
  flows correctly.

### 3. A dedicated stream decorator — `IStreamQueryHandlerDecorator<TQuery, TResult>`

```csharp
public interface IStreamQueryHandlerDecorator<TQuery, TResult> : IQueryHandlerDecorator
    where TQuery : IStreamQuery<TResult>
{
    IAsyncEnumerable<TResult> Execute(
        TQuery query,
        Func<TQuery, CancellationToken, IAsyncEnumerable<TResult>> next,
        CancellationToken cancellationToken);
}

[AttributeUsage(AttributeTargets.Method)]
public abstract class StreamQueryHandlerAttribute : Attribute   // mirrors QueryHandlerAttributeAsync
{
    public int Step { get; }
    protected StreamQueryHandlerAttribute(int step) => Step = step;
    public abstract object[] GetAttributeParams();
    public abstract Type GetDecoratorType();
}
```

- Reuses `IQueryHandlerDecorator` (for `Context` / `InitializeFromAttributeParams`), so the
  existing decorator factory and lifetime apply unchanged.
- `next` is `Func<TQuery, CancellationToken, IAsyncEnumerable<TResult>>` — Darker's explicit
  query+token style (vs MediatR's parameterless closure), consistent with the existing async
  decorator's `next`.
- **No `fallback` continuation** (consistent with §2).
- A **new attribute base** (`StreamQueryHandlerAttribute`) is required so the stream pipeline
  builder selects stream decorators and rejects sync/async decorator attributes (and vice versa),
  mirroring the existing `ValidateNoMismatchedAttributes` guard.

**Decorator semantics (resolves FR7):**

> **On "fallback" — one crisp distinction used consistently below:** the **handler-method** fallback
> model (a `Fallback`/`FallbackPolicy` on the handler) is **removed** for streams; a **Polly
> fallback *strategy*** inside a resilience pipeline **is supported**, but only at stream
> *establishment* (§3a). "Fallback not supported" always means the former; "fallback available"
> always means the latter.

| Decorator kind | V5 behaviour |
|---|---|
| **Logging / Telemetry** | **Supported.** Wraps the stream lifecycle: log/record on start, `yield return` each item, record item count + duration on completion, record exceptions raised during enumeration. Naturally expressible as an `async` iterator over `next`. |
| **Resilience pipeline (Polly v8) — retry / timeout / circuit-breaker / fallback** | **Supported at stream establishment** via a new `UseResiliencePipelineStreamHandler` (stream signature). The pipeline wraps enumerator creation **and the first `MoveNextAsync`**; items are yielded only *after* the pipeline succeeds, so a failure before the first item retries a **fresh** enumerable with **no duplicate emission**. Faults after the first item propagate un-retried. See §3a. |
| **Retry (legacy `RetryableQuery`)** | The request/response-only `RetryableQuery` attribute is **not** applied to streams (its Polly v7 policy model doesn't map). Stream retry is delivered through the resilience pipeline decorator above. |
| **Fallback (`FallbackPolicy` handler-method model)** | **Not supported** as a stream handler method (no `Fallback` on stream handlers, no `fallback` continuation). Fallback-to-an-alternate-stream *is* available through a Polly **fallback strategy** inside the resilience pipeline, applied at establishment (§3a). |
| **Caching** | Out of scope (OOS6). |

### 3a. Resilience for streams — `UseResiliencePipelineStreamHandler`

Darker's existing `UseResiliencePipelineHandlerAsync` executes the handler through a named Polly v8
`ResiliencePipeline` resolved from `IQueryContext.ResiliencePipeline`. Naively wrapping a streaming
handler the same way is a no-op: the pipeline would wrap the *call that returns the enumerable*,
which — because iterator bodies defer — completes before any data is pulled, so it protects nothing.

The fix (per Varnon, *Extending Polly retry policies to cover IAsyncEnumerables*) is to pull the
**first `MoveNextAsync` inside the pipeline boundary** and yield strictly afterwards. A new stream
decorator of the §3 signature, reusing the existing pipeline-resolution logic:

```csharp
public sealed class UseResiliencePipelineStreamHandler<TQuery, TResult>
    : IStreamQueryHandlerDecorator<TQuery, TResult> where TQuery : IStreamQuery<TResult>
{
    public IQueryContext Context { get; set; }
    // InitializeFromAttributeParams resolves _policy exactly as the async decorator.

    public async IAsyncEnumerable<TResult> Execute(
        TQuery query,
        Func<TQuery, CancellationToken, IAsyncEnumerable<TResult>> next,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Untyped pipeline ONLY — see note below on why a type-scoped ResiliencePipeline<TResult>
        // cannot wrap stream establishment.
        var pipeline = Context.ResiliencePipeline.GetPipeline(_policy);

        // Establish the stream + first pull inside the pipeline; both are retriable.
        // Callback disposes the FAILED attempt so retries don't leak enumerators.
        // The pipeline's TState/TResult is the tuple, NOT the item type TResult.
        async ValueTask<(IAsyncEnumerator<TResult>, bool)> Establish(CancellationToken ct)
        {
            var e = next(query, ct).GetAsyncEnumerator(ct);
            try   { return (e, await e.MoveNextAsync()); }
            catch { await e.DisposeAsync(); throw; }
        }

        // Honour an ambient ResilienceContext, branching context-vs-token like the async decorator.
        var resilienceContext = Context.ResilienceContext;
        var (enumerator, moved) = resilienceContext != null
            ? await pipeline.ExecuteAsync(ctx => Establish(ctx.CancellationToken), resilienceContext).ConfigureAwait(false)
            : await pipeline.ExecuteAsync(ct => Establish(ct), cancellationToken).ConfigureAwait(false);

        await using (enumerator.ConfigureAwait(false))
        {
            if (!moved) yield break;
            do { yield return enumerator.Current; }        // yielded only AFTER the pipeline succeeded
            while (await enumerator.MoveNextAsync().ConfigureAwait(false));
        }
    }
}
```

with `UseResiliencePipelineStreamAttribute(int step, string policy) : StreamQueryHandlerAttribute`
returning this decorator type. The ambient-`ResilienceContext` branch mirrors the context-vs-token
call-shapes in `UseResiliencePipelineHandlerAsync`.

> **No `useTypePipeline` for streams (deliberate — differs from the async decorator).** The async
> decorator's `useTypePipeline` resolves a *result-type-scoped* `ResiliencePipeline<TResult>`, which
> can only execute callbacks returning `ValueTask<TResult>`. Stream establishment executes a callback
> returning `ValueTask<(IAsyncEnumerator<TResult>, bool)>` — **not** a `TResult` — so a
> `ResiliencePipeline<TResult>` is type-incompatible with it and the typed branch cannot compile.
> The stream decorator therefore supports only the **untyped** `GetPipeline(_policy)` (whose
> `ExecuteAsync<TState>` is generic over the callback's return type, here the tuple). If a
> per-`(key, item-type)` pipeline is ever wanted for streams, it would have to be keyed on the tuple
> type, not the item type; that is out of scope here.

**Correctness property.** No item leaves the decorator until the pipeline has already succeeded, so
a failure during establishment/first-item retries a **fresh** enumerable and **cannot** re-emit an
already-yielded item. Once the first item is yielded, the pipeline has exited; subsequent
`MoveNextAsync` faults propagate. This is a well-defined **"resilience covers stream
establishment"** semantic.

**Strategy applicability** (the pipeline wraps first-item acquisition only):

- **Retry / circuit-breaker** — apply to establishment + first item. Well-defined.
- **Timeout** — bounds *getting the stream going*, **not** total enumeration time. Documented.
- **Fallback** — a fallback strategy may substitute an alternate `(enumerator, moved)`, i.e. fall
  back to an alternate stream at establishment.
- **Hedging** — **unsupported** for streams (would race multiple enumerables → duplicate items and
  loser-disposal complexity). Documented; not wired up.

**Caveats baked into the design:**

- The in-pipeline callback **disposes the enumerator on throw** so retried attempts don't leak
  (the reference article omits this).
- The handler body up to the first `yield` **re-runs on each retry** — same as any retry; the
  handler must tolerate repeated pre-first-item side effects.
- **Untyped pipeline only** (no `useTypePipeline`) — a `ResiliencePipeline<TResult>` cannot wrap the
  tuple-returning establishment callback (see the note above). The attribute has no
  `useTypePipeline` parameter.
- **Cancellation is not special-cased.** `Establish`'s `catch` rethrows *any* exception — including
  an `OperationCanceledException` from the first `MoveNextAsync` — back into the pipeline, so a
  retry/circuit-breaker strategy will treat a *cancelled* first pull as a fault **unless the
  caller's `ShouldHandle` excludes `OperationCanceledException`** (Polly's defaults exclude it when
  the cancelling token matches the execution token). Darker authors no predicates itself; this is a
  documented caller concern.
- **Fallback disposal is clean.** If the primary `Establish` created an enumerator and then threw,
  that enumerator is disposed by `Establish`'s own `catch` *before* any Polly fallback strategy
  fires. A fallback-substituted `(enumerator, moved)` is therefore the only enumerator the outer
  `await using` owns — no double-dispose, no primary leak.

### 4. Processor entry point — `IQueryProcessor.ExecuteStream<TResult>` (async iterator, no `Task`)

```csharp
IAsyncEnumerable<TResult> ExecuteStream<TResult>(
    IStreamQuery<TResult> query,
    IQueryContext? queryContext = null,
    CancellationToken cancellationToken = default);
```

- Added to the **existing `IQueryProcessor`**, not a separate `IStreamQueryProcessor`: dispatching a
  query through a pipeline to its handler is the processor's single responsibility (coordinator
  role); streaming is the same responsibility with a different result shape. One entry point =
  "one obvious way." (V5 makes this an interface change, which NFR2 permits.)
- **No `Async` suffix and no `Task` wrapper**: the method is itself an `async IAsyncEnumerable`
  iterator that returns the sequence directly; there is nothing to `await` on the call itself (the
  `await` is in the caller's `await foreach`). This mirrors MediatR's `CreateStream` and the BCL
  convention for `IAsyncEnumerable`-returning members. *(Naming — `ExecuteStream` vs
  `ExecuteStreamAsync` — is the one point flagged for confirmation at review.)*

The implementation ties span **and** pipeline lifetime to enumeration:

```csharp
public async IAsyncEnumerable<TResult> ExecuteStream<TResult>(
    IStreamQuery<TResult> query,
    IQueryContext? queryContext = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    // PipelineBuilder gains a new ctor slot for the stream registry (see §5); the handler &
    // decorator factories are the reused async ones.
    var pipelineBuilder = new PipelineBuilder<TResult>(_streamHandlerRegistry, _handlerFactoryAsync, _decoratorFactoryAsync);
    try
    {
        queryContext ??= _queryContextFactory.Create();
        InitQueryContext(queryContext);
        var span = _tracer?.CreateQuerySpan(query, queryContext.Span, queryContext, _instrumentationOptions);
        queryContext.Span = span; queryContext.Tracer = _tracer;

        var entryPoint = pipelineBuilder.BuildStream(query, queryContext, _instrumentationOptions);
        try
        {
            await foreach (var item in entryPoint(query, cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false))
                yield return item;                     // lazy: item surfaces before the handler completes
        }
        finally { _tracer?.EndSpan(span); }
    }
    finally { pipelineBuilder.Dispose(); }              // handler + decorators released when enumeration ends
}
```

- **Laziness (FR6/NFR1):** because `ExecuteStream` and every decorator are `async` iterators, no
  body runs until the caller pulls; framework overhead is O(1) in result-set size.
- **Lifetime (A2):** the `finally` runs when the enumerator is disposed — i.e. when the caller's
  `await foreach` completes *or* `break`s early — so handler/decorators/span are released exactly
  once, at true end-of-stream, including early termination.
- **Exceptions:** faults raised during enumeration propagate through `MoveNextAsync` with their
  original stack trace; no `TargetInvocationException` unwrap is required for the stream body
  (argument-binding errors from `MethodInfo.Invoke`, which occur before enumeration, are still
  unwrapped as today).
- **Deferred configuration errors (behavioural difference — documented, deliberate):** because the
  whole method is one `async` iterator, `BuildStream` — and therefore handler resolution and
  attribute-mismatch validation — runs on the **first `MoveNextAsync`**, not on the `ExecuteStream`
  call. So a missing/mismatched handler surfaces its `ConfigurationException` from the caller's first
  `await foreach` iteration, whereas `Execute`/`ExecuteAsync` throw eagerly at call time
  (`QueryProcessor.cs` builds before returning). We accept this: it is the price of the
  leak-on-abandon safety above (resolving the handler eagerly in a non-iterator wrapper would either
  leak the handler/`QueryLifetimeScope` when the caller never enumerates, or require splitting
  `PipelineBuilder` into a non-allocating "resolve type + validate" phase and a separate
  "create instances" phase). The error still surfaces immediately on first use and is unambiguous, so
  the simpler single-iterator shape wins. *(Alternative — eager type-only resolution — noted under
  Alternatives Considered.)*
- **Multiple / concurrent enumeration:** the returned `IAsyncEnumerable<TResult>` is **cold and not
  cached**. Each `await foreach` (each `GetAsyncEnumerator` call) starts a fresh iterator state
  machine, hence a fresh `PipelineBuilder`, handler, and decorators — so re-enumerating
  **re-executes and re-traces** the query. This differs from `ExecuteAsync`, whose materialised
  `TResult` can be re-read freely. Callers who need to iterate twice should buffer (e.g.
  `ToListAsync`) themselves. This is intended; it is not a single-execution/replayable stream.
  - **Context ownership caveat (concurrency safety).** Safe concurrent/repeat enumeration requires
    the processor to **own the context**: when `queryContext == null` each enumeration gets a fresh
    `_queryContextFactory.Create()` and its own span, so the pipelines are truly independent. When
    the caller **passes** a non-null `queryContext`, that single instance is shared and the iterator
    mutates `queryContext.Span` / `.Tracer` on it; two overlapping enumerations would then race on
    those single-valued properties (one `EndSpan` could stop a span the other is still writing, and
    decorators reading `Context.Span` per FR9 could observe a torn value). **Decision:** a
    caller-supplied `queryContext` is therefore scoped to a **single enumeration**; concurrent or
    repeated enumeration is supported only on the processor-created-context path. This is documented
    on `ExecuteStream`; a future option is to snapshot/clone a supplied context per enumeration.

### 5. Handler resolution — new `IStreamQueryHandlerRegistry`, **reused** factories

```csharp
public interface IStreamQueryHandlerRegistry
{
    Type Get(Type queryType);
    void Register<TQuery, TResult, THandler>()
        where TQuery : IStreamQuery<TResult>
        where THandler : IStreamQueryHandler<TQuery, TResult>;
    void Register(Type queryType, Type resultType, Type handlerType);
}
```

- A **new registry** is warranted because its typed `Register` constrains to `IStreamQueryHandler`
  (a distinct role — "knowing which stream handler serves a stream query").
- The **handler and decorator factories are reused** (`IQueryHandlerFactoryAsync`,
  `IQueryHandlerDecoratorFactoryAsync`): their `Create(Type, IAmALifetime) : IQueryHandler` /
  generic `Create<T>` already resolve any `IQueryHandler` / `IQueryHandlerDecorator` by type, and
  `IStreamQueryHandler : IQueryHandler`, `IStreamQueryHandlerDecorator : IQueryHandlerDecorator`.
  We add new types only where the role genuinely differs, reusing where the responsibility is
  identical.
- **Registration mirrors async handlers *for the user*, but is net-new plumbing *internally*** — it
  is NOT a reuse of the existing scan. The current assembly scan hard-matches a single closed
  interface (`i.GetGenericTypeDefinition() == typeof(IQueryHandlerAsync<,>)` in
  `QueryHandlerRegistryAsync.RegisterFromAssemblies`), so it will **never** pick up an
  `IStreamQueryHandler<,>`. Delivering FR4 therefore requires the following explicit wiring, all of
  which this ADR treats as in scope:
  1. A **new stream `RegisterFromAssemblies`** that matches `typeof(IStreamQueryHandler<,>)`, plus
     the concrete `StreamQueryHandlerRegistry` implementing `IStreamQueryHandlerRegistry`.
  2. `IHandlerConfiguration` / `HandlerConfiguration` gain a **`StreamHandlerRegistry`** member
     (optional; null when streaming is unused).
  3. **`PipelineBuilder<TResult>` gains a new constructor parameter** for the stream registry (its
     current ctor has slots only for the sync + async registries/factories); `BuildStream` resolves
     the handler type from it.
  4. **`QueryProcessor`** reads `StreamHandlerRegistry` off the configuration and threads it (with
     the reused async factories) into the `PipelineBuilder` it constructs in `ExecuteStream` (§4).
  5. The DI `AddDarker(...).AddHandlers...` builder invokes the new stream scan alongside the async
     one, so from the *user's* perspective registration looks identical — but none of the above is
     shared code with the async path.

### 6. Pipeline construction — `PipelineBuilder<TResult>.BuildStream`

Add a third build method alongside `Build` / `BuildAsync`:

```csharp
public Func<IStreamQuery<TResult>, CancellationToken, IAsyncEnumerable<TResult>> BuildStream(
    IStreamQuery<TResult> query, IQueryContext queryContext, InstrumentationOptions options);
```

The **shared, factored** steps with `BuildAsync` are: resolve the handler from the (stream)
registry, validate attributes, and order decorators by `Step` descending. Two things **differ** and
must not be glossed:

1. **Generic close.** `BuildAsync` closes decorators as
   `GetDecoratorType().MakeGenericType(typeof(IQuery<TResult>), typeof(TResult))`
   (`PipelineBuilder.cs:243,:279`). Stream decorators are constrained
   `where TQuery : IStreamQuery<TResult>`, so `BuildStream` must close them (and the handler) over
   **`typeof(IStreamQuery<TResult>)`**, not `IQuery<TResult>` — closing with `IQuery<TResult>` would
   violate the constraint.
2. **Sink + delegate type.** The delegates return `IAsyncEnumerable<TResult>` (not `Task<TResult>`);
   the sink invokes the handler's `ExecuteAsync` returning the enumerable, with **no**
   `TargetInvocationException` unwrap around enumeration (§4 — the iterator defers, so `Invoke`
   returns the enumerable without running the body).

Attribute-mismatch validation reuses the existing `ValidateNoMismatchedAttributes(MemberInfo, Type,
string)` guard (`PipelineBuilder.cs:184`), which is driven by the attribute **base type** (not the
method name), rejecting `QueryHandlerAttribute` / `QueryHandlerAttributeAsync` on a stream handler's
`ExecuteAsync` (and `StreamQueryHandlerAttribute` on sync/async handlers), with a clear
`ConfigurationException`.

**Method resolution must be by signature, not by bare name.** The existing async resolver is
`handlerType.GetMethod("ExecuteAsync")` with no argument types (`PipelineBuilder.cs:179`). A stream
handler's method is *also* named `ExecuteAsync` (§2), so `BuildStream` must resolve the stream method
by its **signature** — return type `IAsyncEnumerable<TResult>`, parameters `(TQuery,
CancellationToken)` — rather than a bare-name lookup, to avoid binding to an async `Task<TResult>`
`ExecuteAsync` and to avoid `AmbiguousMatchException` on any type that exposed both.

### 7. Target frameworks — all current targets

Support streaming on **`netstandard2.0;net8.0;net9.0`** (Darker's existing targets). `IAsyncEnumerable`
is native on net8.0+; on `netstandard2.0` add a conditional
`Microsoft.Bcl.AsyncInterfaces` package reference (small, Microsoft-owned, managed via CPM in
`Directory.Packages.props`). This avoids `#if`-guarding the streaming API and keeps one consistent
surface across targets — simpler for users than a net8.0-only streaming path.

**NFR4 (AOT/trimming).** `BuildStream` uses the same `MakeGenericType` + `MethodInfo.Invoke`
reflection the existing `BuildAsync` already uses (`PipelineBuilder.cs:243,:144`); the project is
`IsAotCompatible` on net8.0+ (`Paramore.Darker.csproj`) with the existing `IL2026/IL3050`
suppressions. Streaming introduces no reflection *beyond* the current pipeline, so the existing AOT
posture carries over unchanged — satisfying NFR4's wording ("must not introduce trim/AOT-hostile
reflection beyond what the existing pipeline already uses").

### Architecture Overview

```
Caller
  │  await foreach (var item in processor.ExecuteStream(query, ct))
  ▼
IQueryProcessor.ExecuteStream<TResult>            (async iterator: owns span + pipeline lifetime)
  │        creates IQueryContext + span, builds pipeline, yields items, releases on enumerator dispose
  ▼
PipelineBuilder<TResult>.BuildStream              (resolves handler from IStreamQueryHandlerRegistry)
  │        chains IStreamQueryHandlerDecorator (attrs ordered by Step) innermost→outermost
  ▼   Func<IStreamQuery<TResult>, CancellationToken, IAsyncEnumerable<TResult>>
[ logging/telemetry decorator ] → … → [ handler.ExecuteAsync ]   (all async iterators — lazy)
```

### Key Components

| Component | Role (RDD) | New / Reused |
|---|---|---|
| `IStreamQuery<out TResult>` | information holder — "a query that yields a stream" | **New** |
| `IStreamQueryHandler<TQuery,TResult>` / `StreamQueryHandler<,>` | service provider — produces the stream | **New** |
| `IStreamQueryHandlerDecorator<TQuery,TResult>` + `StreamQueryHandlerAttribute` | interfacer — wraps the stream | **New** |
| `UseResiliencePipelineStreamHandler<TQuery,TResult>` + `UseResiliencePipelineStreamAttribute` | interfacer — applies a Polly v8 pipeline at stream establishment | **New** (reuses pipeline-resolution logic from the async decorator) |
| `IStreamQueryHandlerRegistry` | information holder — query type → stream handler type | **New** |
| `IQueryProcessor.ExecuteStream` | coordinator — dispatch + lifetime | Extends existing role |
| `PipelineBuilder.BuildStream` | structurer — assemble the chain | Extends existing type |
| `IQueryHandlerFactoryAsync`, `IQueryHandlerDecoratorFactoryAsync`, `IAmALifetime`, `IQueryContext` | service providers | **Reused unchanged** |

### Technology Choices

- `System.Collections.Generic.IAsyncEnumerable<T>` + C# 8 `async` iterators (`await foreach` /
  `yield return`) — the idiomatic, lazy, cancellable streaming primitive.
- `Microsoft.Bcl.AsyncInterfaces` — only on `netstandard2.0`, to provide `IAsyncEnumerable<T>`.
- `[EnumeratorCancellation]` — to unify the method token with `WithCancellation(ct)`.

### Implementation Approach

Delivered TDD-first (`/test-first` per task). Rough sequence, structural-before-behavioural
(Tidy First): (1) contracts `IStreamQuery` / `IStreamQueryHandler` / base class; (2)
`IStreamQueryHandlerRegistry` + concrete registry; (3) `PipelineBuilder.BuildStream` sink (no
decorators) + `IQueryProcessor.ExecuteStream`; (4) decorator contract + attribute + chain build +
mismatch validation; (5) logging/telemetry stream decorators; (6) `UseResiliencePipelineStreamHandler`
+ attribute (reusing pipeline resolution); (7) DI scanning + `HandlerConfiguration`;
(8) `netstandard2.0` package + multi-target build. Each behaviour (happy path, laziness,
cancellation mid-stream, exception mid-stream, early-`break` lifetime release, decorator ordering,
resilience: retry-before-first-item with **no** duplicate emission, failed-attempt disposal) gets a
failing test approved before implementation.

## Consequences

### Positive

- Large/unbounded/real-time result sets stream with O(1) framework overhead; first item is
  observable before the handler finishes.
- The streaming path reads as a natural sibling of the async path; factories, lifetime, and context
  are reused, so there is little new surface to learn.
- Span and handler/decorator lifetime are correctly bound to enumeration (including early
  termination) — a subtle correctness win over a naive overload.
- Enumeration faults propagate with original stack traces without reflection unwrap ceremony.
- **Polly v8 resilience (retry / timeout / circuit-breaker / fallback) is available for streams**
  via `UseResiliencePipelineStreamHandler`, reusing the existing pipeline-resolution logic, with a
  well-defined "covers establishment + first item, no duplicate emission" semantic — no capability
  cliff versus the request/response path for the common case.

### Negative

- Net-new public types (query/handler/decorator/attribute/registry/resilience decorator) enlarge
  the API surface.
- Stream resilience covers **establishment + first item only**: mid-stream faults after the first
  item are not retried, and `Timeout` bounds start-up rather than total enumeration. `Hedging` is
  unsupported for streams. These are documented semantics, not bugs, but differ from the
  request/response pipeline.
- The legacy `RetryableQuery` / `FallbackPolicy` (handler-method) attributes do **not** apply to
  streams; stream resilience goes exclusively through the resilience-pipeline decorator.
- `PipelineBuilder` grows a third build path, adding some duplication with `BuildAsync`.
- `netstandard2.0` gains a `Microsoft.Bcl.AsyncInterfaces` dependency.
- Adding `ExecuteStream` to `IQueryProcessor` is a breaking interface change for custom
  implementers (acceptable under V5 / NFR2).

### Risks and Mitigations

- **Risk: accidental buffering** in a decorator (e.g. `ToListAsync`) silently defeats streaming.
  *Mitigation:* a laziness test asserting the first item is observed before the handler produces
  the last; document the "don't buffer" rule on the decorator contract.
- **Risk: span/handler leak** if enumeration is abandoned without disposal. *Mitigation:* the
  `finally` in the processor iterator runs on enumerator `DisposeAsync`, which `await foreach`
  always calls (including on `break`/exception); covered by an early-termination lifetime test.
- **Risk: users apply the legacy `RetryableQuery` / `FallbackPolicy` attributes to a stream and
  expect them to work.** *Mitigation:* the attribute mismatch validator throws a clear
  `ConfigurationException` (those are `QueryHandlerAttributeAsync`, not `StreamQueryHandlerAttribute`);
  the doc points users to `UseResiliencePipelineStream` instead.
- **Risk: enumerator leak on resilience retry.** Each failed attempt inside the pipeline created an
  enumerator and called `MoveNextAsync`. *Mitigation:* the in-pipeline callback disposes the
  enumerator on throw before rethrowing (the reference article omits this); covered by a test that
  asserts N failed attempts ⇒ N disposals.
- **Risk: users assume stream resilience covers the whole stream** (mid-stream retry, total-time
  timeout). *Mitigation:* documented as "establishment + first item only"; a retry test asserts a
  fault *after* the first item is **not** retried and items are **not** re-emitted.
- **Risk: `BuildAsync`/`BuildStream` divergence** over time. *Mitigation:* keep the shared
  resolve/validate/order steps factored; the two differ only in return type and sink.

## Alternatives Considered

- **Reuse `IQuery<IAsyncEnumerable<TResult>>` with the existing async pipeline.** Rejected: routes
  as `Task<IAsyncEnumerable<TResult>>`, invites buffering, muddies laziness/lifetime, and reads
  poorly. A dedicated marker is clearer and lets the pipeline dispatch on type.
- **Separate `IStreamQueryProcessor`.** Rejected: fragments the processor's single dispatch
  responsibility across two roles for no user benefit; MediatR keeps `CreateStream` on `ISender`.
- **Reuse `IQueryHandlerDecoratorAsync` for stream decorators.** Rejected: its `next`/`fallback`
  are `Func<…, Task<TResult>>`; a stream decorator must operate over `IAsyncEnumerable<TResult>`.
  A distinct contract + attribute is required and enables the mismatch guard.
- **`ExecuteStreamAsync` returning `Task<IAsyncEnumerable<TResult>>`.** Rejected: the extra `Task`
  is pure ceremony (the method returns the enumerable synchronously) and misrepresents the model.
- **No resilience for streams in V5** (the earlier draft position). Superseded: Varnon's
  first-`MoveNextAsync`-inside-the-pipeline pattern gives a safe, no-duplicate-emission semantic at
  modest cost, reusing the existing pipeline-resolution logic, so §3a adopts it rather than
  deferring.
- **Wrap *each* `MoveNextAsync` in the pipeline (per-item resilience).** Rejected: re-calling
  `MoveNextAsync` on an enumerator that already threw is undefined/terminal — you cannot resume; the
  only way to "retry item N" is to rebuild the whole stream and skip N, re-running side effects and
  requiring positional idempotency. Not viable generically. Wrapping only the first item is the
  safe choice.
- **Reuse the async `UseResiliencePipelineHandlerAsync` directly.** Rejected: wrapping the call that
  returns the enumerable protects nothing (iterator bodies defer); a stream-specific decorator that
  pulls the first item inside the pipeline is required.
- **Eager handler resolution / config validation before the iterator defers** (a non-iterator
  `ExecuteStream` that resolves + validates, then returns an inner iterator). Rejected for the base
  design: resolving the handler eagerly either leaks the handler + `QueryLifetimeScope` when the
  caller never enumerates (breaking the leak-on-abandon property), or forces splitting
  `PipelineBuilder` into a non-allocating "resolve type + validate attributes" phase and a separate
  "create instances + run" phase. The single-iterator shape is simpler and the config error still
  surfaces unambiguously on first enumeration (§4). A future refinement could add type-only eager
  validation if the deferred error proves surprising in practice.
- **net8.0+ only for streaming.** Rejected in favour of all-targets via
  `Microsoft.Bcl.AsyncInterfaces`: avoids `#if` fragmentation and a split user story.

## References

- Requirements: [specs/012-streaming_results/requirements.md](../../specs/012-streaming_results/requirements.md)
  (includes verbatim MediatR streaming signatures as prior art)
- Linked issue: [#299](https://github.com/BrighterCommand/Darker/issues/299)
- Related ADRs: `0017-query-tracing-and-database-spans.md` (span lifecycle this ADR must respect),
  `0016-pipeline-attribute-memoization.md` (attribute resolution the stream builder reuses)
- Current code: `src/Paramore.Darker/PipelineBuilder.cs`, `src/Paramore.Darker/QueryProcessor.cs`,
  `src/Paramore.Darker/IQueryHandlerDecoratorAsync.cs`,
  `src/Paramore.Darker/IQueryHandlerRegistryAsync.cs`,
  `src/Paramore.Darker/Policies/Handlers/UseResiliencePipelineHandlerAsync.cs` (the async resilience
  decorator whose pipeline-resolution logic §3a reuses)
- External: MediatR streaming — <https://deepwiki.com/jbogard/MediatR/3.2-streaming-support>
- External: A. Varnon, *Extending Polly retry policies to cover IAsyncEnumerables* —
  <https://avarnon.medium.com/extending-polly-retry-policies-to-cover-iasyncenumerables-7e609ad0b9ad>
  (the first-`MoveNextAsync`-inside-the-policy pattern adopted, with added failed-attempt disposal)
