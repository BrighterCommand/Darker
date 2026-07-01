# 17. Query Tracing and Database Spans

Date: 2026-07-01

## Status

Accepted

## Context

**Parent Requirement**: [specs/011-telemetry/requirements.md](../../specs/011-telemetry/requirements.md)

**Scope**: This ADR covers Darker's **distributed tracing** surface and its **database-span
support**. It defines the tracer, the query span lifecycle, the span attributes and events, the
`InstrumentationOptions` verbosity control, the `Query` base class that gives a query an id, how
the span is surfaced to handler code, and the DB-span helper and decorator. It also introduces the
`Paramore.Darker.Extensions.Diagnostics` assembly with `AddDarkerInstrumentation` for the
OpenTelemetry `TracerProviderBuilder`. **Metrics are deferred to ADR 0018**, which derives metrics
from the spans defined here.

Darker has no telemetry today. `QueryProcessor.Execute`/`ExecuteAsync` builds a decorator pipeline
in `PipelineBuilder` and invokes the handler, but nothing creates a `System.Diagnostics.Activity`
(span), records the pipeline steps, or exposes the span so a handler can nest its database call.
Requirement RD1–RD4 settled the shape; this ADR turns the tracing/DB portion into a design.

The forces at play:

- **Follow Brighter, adapt to Darker.** Brighter's `BrighterTracer` + `InstrumentationOptions` +
  `BrighterSemanticConventions` model (ADR 0010) is proven and we want ecosystem-consistent
  attribute names. But Darker is far smaller: **one** operation (`query`), **in-process only** (no
  messaging/outbox/dispatcher/pump), and its pipeline is a **chain of `Func<,>` closures**, not
  Brighter's Russian-doll base-class handlers. So only the "Command Processor span/attributes/
  events" slice applies, and the event-writing mechanism must differ (RD2).
- **Opt-in, zero-overhead when unobserved.** This ships in **V5, which permits breaking API
  changes**, so we are not bound to preserve every existing signature. What matters is that
  tracing is *opt-in*: with no tracer configured (or no listener on the source) query execution
  behaves and performs exactly as today (NFR2). We still prefer additive/optional changes where
  they cost nothing.
- **Dependency hygiene.** Core `Paramore.Darker` may depend only on
  `System.Diagnostics.DiagnosticSource`; the OpenTelemetry SDK is confined to the new
  `Paramore.Darker.Extensions.Diagnostics` assembly (NFR4).
- **The DB call is the user's code.** Darker never touches a database; it can only surface the
  parent span and make creating a correctly-shaped child DB span easy (RD3).
- **Queries have no identity today.** `IQuery<TResult>` is an empty marker. Even though V5 would
  permit adding a member to it, doing so forces an `Id` on every query type (most are plain DTOs)
  and a default interface member cannot hold a stable per-instance GUID; an opt-in base class is
  the more ergonomic home for identity (RD1).

## Decision

Introduce an `Observability` namespace in core `Paramore.Darker` holding a `DarkerTracer`
(behind an `IAmADarkerTracer` role), an `InstrumentationOptions` flags enum, a
`DarkerSemanticConventions` constants holder, and DB-span types (`DbSpanInfo`, `DbSystem`). The
`QueryProcessor` becomes the **owner** of the query span lifecycle; `PipelineBuilder` **weaves**
pipeline-step events into the `Func` chain; `IQueryContext` **surfaces** the span and tracer so
handlers and a new DB-span decorator can create child DB spans. A separate
`Paramore.Darker.Extensions.Diagnostics` assembly wires the source into OpenTelemetry.

### Responsibility-Driven Design — roles

| Role (stereotype) | Type | Knows / Does / Decides |
|---|---|---|
| Service provider | `IAmADarkerTracer` → `DarkerTracer` | Owns the `ActivitySource`; **does** create/end the query span and DB span, write step events, record exceptions. |
| Coordinator | `QueryProcessor` | **Decides** to start a span (if a tracer + listener exist), makes it `Activity.Current`, surfaces it on the context, ends it once, records the terminal status/exception. |
| Structurer | `PipelineBuilder` | **Does** weave a step event into each `Func` closure as it composes the chain. |
| Information holder | `Query<TResult>` (base) | **Knows** its `string Id` (defaults to a GUID). |
| Information holder | `InstrumentationOptions` | **Knows/decides** which attribute groups are emitted. |
| Information holder | `DarkerSemanticConventions` | **Knows** the source name and attribute-key constants. |
| Information holder | `IQueryContext` (extended) | **Knows** the ambient `Span` and `Tracer` for the current query. |
| Information holder | `DbSpanInfo`, `DbSystem` | **Knows** the DB span's attributes. |
| Service provider | `QueryDbSpanDecorator` + `[QueryDbSpan]` | **Does** wrap the handler in a child DB span from attribute-supplied `DbSpanInfo`. |
| Interfacer | `DarkerTracerBuilderExtensions` | **Does** register the source + tracer with the OTel `TracerProviderBuilder`. |

### Architecture Overview

```
 caller (ASP.NET controller / Brighter handler)  ── ambient Activity.Current
        │  Execute<TResult>(query, ctx?)      (ctx may already carry a caller Span)
        ▼
 QueryProcessor ─────────────────────────────────────────────────────────┐
   span = tracer?.CreateQuerySpan(query, parent: ctx.Span, options)       │ owns span
        └─ parentId = ctx.Span?.Id;  null ⇒ StartActivity uses ambient    │ lifecycle
           Activity.Current;  tracer sets Activity.Current = span         │
   ctx.Span = span;  ctx.Tracer = tracer                                  │
   entry = pipelineBuilder.Build(query, ctx)   ← weaves step events       │
   try { return entry.Invoke(query); }                                    │
   catch (ex) { tracer.AddExceptionToSpan(span, ex); throw; }             │
   finally { tracer.EndSpan(span); }  ← Ok if unset, restores prior Current┘
        │
        ▼  (the Func chain, outermost decorator first)
   [decorator N] WriteQueryEvent(span, "DecoratorN")   → next
     ...
   [decorator 1] WriteQueryEvent(span, "Decorator1")   → next
   [handler]     WriteQueryEvent(span, "Handler", isSink:true) → Execute
                    │
                    └─ handler code may:  tracer.CreateDbSpan(info, ctx.Span)  ── child CLIENT span
                       or be wrapped by [QueryDbSpan] decorator that does the same
```

Span shape: name `"<QueryType> query"`, kind `Internal`, parent = ambient activity (else root).
DB span: name `"<operation> <dbName> <dbTable>"` (or `"<operation> <dbName>"`), kind `Client`,
parent = the query span.

### Key Components

**1. `IAmADarkerTracer` / `DarkerTracer`** (core, `Paramore.Darker.Observability`)

Wraps one `ActivitySource` named `paramore.darker` (+ assembly version), with a `TimeProvider`
seam for deterministic times. Methods (all no-op-safe when the span is null / no listener):

```csharp
public interface IAmADarkerTracer : IDisposable
{
    ActivitySource ActivitySource { get; }

    Activity? CreateQuerySpan<TResult>(IQuery<TResult> query, Activity? parentActivity = null,
        InstrumentationOptions options = InstrumentationOptions.All);

    Activity? CreateDbSpan(DbSpanInfo info, Activity? parentActivity,
        InstrumentationOptions options = InstrumentationOptions.All);

    void AddExceptionToSpan(Activity? span, Exception exception);  // records exception, sets Error status, tags error.type
    void EndSpan(Activity? span);   // sets Ok if unset, restores previous Activity.Current

    // static event writer — only instance state is the source, so events are static (as in Brighter)
    static void WriteQueryEvent(Activity? span, string stepName, bool isAsync,
        InstrumentationOptions options, bool isSink = false);
}
```

`CreateQuerySpan` extracts the id with a closed-generic pattern match — `query is Query<TResult> q
? q.Id : null` — so **no member is added to `IQuery`** and no reflection is needed (TResult is
always known at the `Execute<TResult>` call site).

**Parenting** follows Brighter's `CreateSpan(..., requestContext?.Span, ...)`: the caller passes the
context's current span as `parentActivity`, and `CreateQuerySpan` sets `parentId =
parentActivity?.Id`. When that is null (no caller-supplied span), `ActivitySource.StartActivity`
treats a null `parentId` as "use `Activity.Current`", so the span still nests under the ambient
ASP.NET/Brighter activity when one exists, or becomes a root otherwise. The method sets
`Activity.Current = activity` (as `BrighterTracer` does) so downstream steps and DB spans nest;
`EndSpan` reverts it.

**2. `Query` base class** (core) — RD1

```csharp
public abstract class Query<TResult> : IQuery<TResult>
{
    protected Query() { }
    protected Query(string id) => Id = id;
    public string Id { get; init; } = Guid.NewGuid().ToString();
}
```

Opt-in: users may derive from `Query<TResult>` to get a defaulted-GUID id (or supply one via ctor /
`init`). Existing types implementing `IQuery<TResult>` directly are untouched and carry no id;
`queryid` is recorded only when present.

**3. `InstrumentationOptions`** (core) — a `[Flags]` enum, the Darker-sized subset of Brighter's:

```csharp
[Flags]
public enum InstrumentationOptions
{
    None = 0,
    QueryInformation = 1,     // .queryid, .querytype, .operation  + step-event tags
    QueryBody = 2,            // .query_body (query serialised as JSON)
    QueryContext = 4,         // .spancontext.* copied from IQueryContext.Bag
    DatabaseInformation = 8,  // db.* attributes on DB spans
    All = QueryInformation | QueryBody | QueryContext | DatabaseInformation
}
```

**4. `DarkerSemanticConventions`** (core) — string constants (`SourceName = "paramore.darker"`,
`QueryId = "paramore.darker.queryid"`, `QueryType`, `Operation`, `QueryBody`, the `spancontext.`
prefix, `HandlerName`, `HandlerType`, `IsSink`, `ErrorType = "error.type"`, plus the `db.*` keys),
so a typo cannot silently break tracing.

**5. `IQueryContext` extension** (core) — surface the ambient observability state:

```csharp
Activity? Span { get; set; }
IAmADarkerTracer? Tracer { get; set; }
```

Both are nullable and default to null (preserve current behaviour / zero-overhead when
unconfigured). `QueryProcessor` sets them; handlers and the DB decorator read them.

**6. `QueryProcessor` changes** (core) — owns the span:

- New ctor parameters `IAmADarkerTracer? tracer = null` and
  `InstrumentationOptions instrumentationOptions = InstrumentationOptions.All`. Optional so existing
  construction still compiles; V5 would permit a required change, but optional keeps the common path
  clean.
- In `Execute`/`ExecuteAsync`, after resolving/initialising the `queryContext`
  (`InitQueryContext`): create the span parented to the **context's current span** —
  `span = tracer?.CreateQuerySpan(query, queryContext.Span, instrumentationOptions)` — mirroring
  Brighter's `CreateSpan(op, command, requestContext?.Span, …)` / `InitRequestContext(span, ctx)`.
  Then assign `queryContext.Span = span` and `queryContext.Tracer = tracer` so downstream steps and
  child DB spans nest under it. Build/invoke the pipeline, record any exception **once**
  (`AddExceptionToSpan`, which sets `ActivityStatusCode.Error`, records the exception per the OTel
  exceptions convention, and tags `error.type` with the exception's type name) inside the existing
  `TargetInvocationException`-unwrapping catch, and `EndSpan` in a `finally`. The `error.type` tag
  is what ADR 0018's metrics processor reads as the success/failure dimension, so trace and metric
  share one value.
- Because the parent is `queryContext.Span` (not `Activity.Current` directly), a caller that already
  holds a span — e.g. a Brighter handler that put its span on the context — parents the query span
  correctly; absent that, the null `parentId` resolves to the ambient `Activity.Current`.

**7. `PipelineBuilder` changes** (core) — weave step events into the `Func` chain (RD2):

The innermost handler closure calls `DarkerTracer.WriteQueryEvent(span, handlerName, isAsync,
options, isSink: true)` before invoking; each decorator-wrapping closure writes
`WriteQueryEvent(span, decoratorName, isAsync, options)` before calling `next`. `PipelineBuilder`
receives the tracer/options/span (via constructor args threaded from `QueryProcessor`, or read from
`queryContext`). Events are pass-through markers; the **exception is recorded once by the
processor** (the span owner) to avoid duplicate exception events as it bubbles — a deliberate
refinement of FR10a keeping the "terminal status" responsibility with the coordinator that owns the
span.

**8. DB-span support** (core) — RD3, two layers:

- `DbSpanInfo` record (mirrors Brighter's `BoxSpanInfo`: `dbSystem`/`dbName`/`dbOperation`/
  `dbTable` + optional `serverAddress`/`dbStatement`/`dbUser`/… and a free `dbAttributes` bag) and
  a `DbSystem` enum (OTel `db.system` values; string escape hatch to avoid primitive obsession).
- `tracer.CreateDbSpan(info, ctx.Span, options)` for handlers that want direct control.
- `QueryDbSpanDecorator<TQuery,TResult>` + `[QueryDbSpan(step, DbSystem, dbName, dbTable,
  operation)]` (sync + async), following the existing `QueryLogging` decorator/attribute pattern
  (`GetAttributeParams`/`InitializeFromAttributeParams`). The decorator reads `Context.Tracer` +
  `Context.Span`, opens a child DB span, invokes `next` (the handler's DB work), ends it, and lets
  the processor record any exception. It times the wrapped handler as a proxy for the DB call;
  handlers needing finer scoping call `CreateDbSpan` directly.

**9. `Paramore.Darker.Extensions.Diagnostics`** (new assembly) — `AddDarkerInstrumentation(this
TracerProviderBuilder)`: constructs a `DarkerTracer`, `TryAddSingleton<IAmADarkerTracer>` it, and
`builder.AddSource(tracer.ActivitySource.Name)`. Mirrors `BrighterTracerBuilderExtensions`. The
`Paramore.Darker.Extensions.DependencyInjection` `AddDarker` path resolves the registered
`IAmADarkerTracer` (if any) and a `DarkerOptions.InstrumentationOptions`, passing them to the
`QueryProcessor` ctor.

### Technology Choices

- **`System.Diagnostics.ActivitySource`/`Activity`** for spans — the .NET-native OTel primitive;
  keeps core free of the OpenTelemetry SDK (NFR4). OTel SDK types (`TracerProviderBuilder`) appear
  only in the Diagnostics assembly.
- **`[Flags] InstrumentationOptions` + `Activity.IsAllDataRequested`/`ActivitySource.HasListeners`
  guards** — attribute-group and cardinality/PII control with near-zero cost when unobserved
  (NFR2); the query body is serialised only when `QueryBody` is set and data is requested.
- **Darker's existing JSON serializer options** (ADR 0012) for `query_body` — no second serializer
  (C4). Uses the runtime-type overload like `QueryLoggingDecorator` so the concrete query (not the
  `IQuery<TResult>` interface) is serialised.
- **`TimeProvider`** seam for deterministic start/end times in tests, as in `BrighterTracer`.
- **Opt-in `Query<TResult>` base class** for identity (RD1) rather than changing `IQuery`.

### Implementation Approach

Structural first (Tidy First): add the `Observability` types and the `IQueryContext`/`QueryContext`
`Span`+`Tracer` properties (defaulting null — behaviour-preserving), then the `Query<TResult>` base.
Then behavioural: span creation/ending in `QueryProcessor`, event weaving in `PipelineBuilder`,
`CreateDbSpan` + the DB decorator, and finally the Diagnostics assembly + DI wiring. Each step is
driven by `/test-first` using an in-memory `ActivityListener` to assert span name/kind/parent,
attribute presence per option, one event per step with `is_sink` on the handler, exception+Error
status, the no-listener no-op path, and correct child-nesting of DB spans (sync and async).

## Consequences

### Positive

- Darker queries appear as spans in any OTel-wired app via one `AddDarkerInstrumentation()` call,
  nested under the triggering ASP.NET/Brighter span, with per-step events and controllable detail.
- Handlers get a first-class, low-effort way to emit a correctly-shaped child DB span (helper or
  attribute), and ADR 0018 can derive metrics from exactly these spans.
- Ecosystem-consistent with Brighter's conventions; core stays SDK-free; fully opt-in with
  zero cost when unobserved.

### Negative

- `QueryProcessor`, `PipelineBuilder`, and `IQueryContext` each gain observability concerns,
  widening their surface (mitigated by pushing all span/attribute logic into `DarkerTracer`).
- A new public `Query<TResult>` base class adds a second, "blessed" way to declare a query
  alongside implementing `IQuery<TResult>` directly (tension with "one obvious way"); justified by
  giving identity an ergonomic, defaulted-GUID home (RD1).
- The DB-span decorator times the wrapped handler, not literally the DB round-trip; documented, with
  `CreateDbSpan` as the precise escape hatch.

### Risks and Mitigations

- **Async `Activity.Current` leakage** across `await` in `ExecuteAsync` — end the span in `finally`
  and restore the previous `Activity.Current` in `EndSpan` (Brighter's proven approach); assert with
  a no-listener test that `Activity.Current` is unchanged.
- **Double exception events** if every step records on unwind — centralise exception recording in
  the processor; steps only write pass-through events.
- **PII via `query_body`/`spancontext.*`** — off unless explicitly enabled and gated by
  `IsAllDataRequested`.
- **Overhead when unobserved** — `HasListeners`/null guards short-circuit before any allocation or
  serialisation; covered by a no-listener test (NFR2).
- **Threading tracer into `PipelineBuilder`** — prefer reading `Span`/`Tracer` from the
  `queryContext` already passed to `Build`/`BuildAsync` over adding constructor params, minimising
  signature churn.

## Alternatives Considered

- **Add `Id` to `IQuery`** (or a default interface member) — although V5 permits the break,
  rejected because it forces an `Id` member on every query type (most are plain DTOs) and a DIM
  cannot hold a stable per-instance GUID; an opt-in `Query<TResult>` base with a defaulted GUID is
  more ergonomic (RD1).
- **Write events from decorators / the handler base class** (Brighter's model) — rejected: Darker
  has no Russian-doll base class and decorators are user-authored; weaving events into the
  `PipelineBuilder` `Func` chain covers every step uniformly, including user decorators (RD2).
- **A dedicated tracing decorator wrapping the whole pipeline** instead of processor-owned span —
  rejected: the span must parent DB spans and be surfaced on the context before the pipeline runs;
  the processor is the natural owner and guarantees the span brackets the entire execution.
- **One combined telemetry ADR** — rejected in favour of splitting tracing/DB (this ADR) from
  metrics (0018), per the chosen 2-ADR split.
- **A distinct `IIdentifiedQuery` role interface to read the id** — rejected as an unnecessary type;
  the closed-generic `is Query<TResult>` check reads the id without it.

## References

- Requirements: [specs/011-telemetry/requirements.md](../../specs/011-telemetry/requirements.md)
- Related ADRs: 0018 (metrics — derives from these spans); 0002 (attribute-driven decorator
  pipeline); 0012 (JSON serializer); 0016 (pipeline attribute memoization)
- Brighter model: `Paramore.Brighter/Observability/BrighterTracer.cs`, `InstrumentationOptions.cs`,
  `BoxSpanInfo.cs`, `IAmABrighterTracer.cs`; `Paramore.Brighter.Extensions.Diagnostics/BrighterTracerBuilderExtensions.cs`
- Brighter ADR 0010 – OpenTelemetry Semantic Conventions
- OTel database spans: https://opentelemetry.io/docs/specs/semconv/db/database-spans/
- OTel exceptions on spans: https://opentelemetry.io/docs/specs/semconv/exceptions/exceptions-spans/
- Jimmy Bogard, "Building end-to-end diagnostics: ActivitySource and OpenTelemetry"
