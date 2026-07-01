# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: _none yet (feature request captured directly)_
**Target Release**: V5 (new public API surface; V5 permits breaking changes, though telemetry is
designed to be opt-in and behaviour-preserving when unconfigured)

## Problem Statement

As an application developer using Darker to execute queries (the CQRS read side),
I would like Darker's query pipeline to emit **OpenTelemetry traces and metrics**, so that
I can observe query execution — latency, failures, and which pipeline steps ran — in my
existing observability tooling, and correlate a query with the ASP.NET request (or Brighter
command) that triggered it, without hand-instrumenting every handler.

Today Darker has **no telemetry surface**. The `QueryProcessor` builds a decorator pipeline
and invokes a handler, but nothing creates a `System.Diagnostics.Activity` (span), records
events for the decorators it passes through, or exposes metrics. A developer who wants to see
Darker queries in a distributed trace must wrap every `Execute` call themselves, and even then
cannot see the internal pipeline steps or nest the actual database call underneath the query.

Brighter has already solved the equivalent problem on the command side. Its approach is
recorded in [Brighter ADR 0010 – OpenTelemetry Semantic Conventions](../../../Brighter/docs/adr/0010-brighter-semantic-conventions.md)
and implemented in `BrighterTracer` (a wrapper over `ActivitySource`) plus a separate
`Paramore.Brighter.Extensions.Diagnostics` assembly that wires the source/meter into the
OpenTelemetry SDK. Darker should adopt the **same model and the same semantic-convention
style**, adapted to Darker's much smaller surface: Darker has exactly **one** processor
operation (executing a query) and **no** messaging, outbox, dispatcher, or message-pump
concepts, so only the "Command Processor span / attributes / events" portion of the Brighter
ADR is relevant here.

### Key differences from Brighter (shape the scope)

1. **Single operation.** Brighter has `send` / `publish` / `deposit` / `clear`. Darker has a
   single logical operation, proposed name **`query`**, mirroring Brighter's convention of
   naming the span `<request type> <operation>`.
2. **Queries gain an identity.** `IQuery<TResult>` currently exposes no `Id` (Brighter's
   `IRequest` has `.Id`). This feature **adds** a `string Id` to the query contract that
   **defaults to `Guid.NewGuid().ToString()`** when the caller does not supply one via the
   constructor, giving Darker a natural `queryid` attribute (**resolved — RD1**).
2a. **No Russian-doll base class to hook.** Brighter records its handler events from the base
   `RequestHandler`/`RequestHandlerAsync.Handle` method (each handler wraps its successor). Darker
   does **not** use that model — `PipelineBuilder` composes the pipeline as a chain of
   `Func<IQuery<TResult>, TResult>` (and the async `Func<…, Task<TResult>>`) closures. There is no
   per-decorator base class to instrument, so the entry/exit and exception events must be woven
   into that `Func` chain as it is built (**resolved — RD2**).
3. **In-process only.** No broker, no message headers, so no messaging/producer/consumer spans
   and no cross-process `traceparent` propagation from Darker itself. Darker is normally a
   *child* span of an ASP.NET controller or a Brighter handler.
4. **The database call is the user's code.** Darker never touches a database itself; the query
   handler does. Central support is therefore limited to *surfacing the query span* to handler
   code and *optionally* offering a helper to create a correctly-named child DB span.

## Proposed Solution

Give Darker a first-class, standards-aligned telemetry surface modelled on Brighter:

1. **A Darker tracer** — a dedicated class (working name `DarkerTracer`, in a
   `Paramore.Darker.Observability` namespace) that wraps a single `ActivitySource` named
   **`paramore.darker`** (with the assembly version), and provides methods to create/end the
   query span and to record decorator/handler events and exceptions. This mirrors
   `BrighterTracer`.

2. **A query span per executed query.** `QueryProcessor.Execute` / `ExecuteAsync` start a span
   named `<QueryType> query` (span kind `Internal`), make it the current `Activity` so the
   handler's own work (including any DB span) nests beneath it, and end it — recording success
   or the exception — when the pipeline completes.

3. **Query Processor Attributes** on the span (the Darker analogue of "Command Processor
   Attributes"), whose inclusion is governed by an **`InstrumentationOptions`** flags enum so a
   developer can trade off between *tracing that a query ran* and *revealing the query's
   parameters*.

4. **Query Processor Events** — as the pipeline passes through each decorator, and finally the
   handler, an event is recorded on the span named after that step, with the handler marked as
   the sink. Exceptions are recorded as events per the OTel exceptions convention.

5. **Access to the span from handler code**, via the `IQueryContext` that already flows through
   the pipeline, so a handler (or a user-written decorator) can create a child span for the
   actual database call.

6. **Optional DB-span helper** — a `DarkerTracer.CreateDbSpan(...)` method analogous to
   Brighter's, so users who *want* central help creating a DB-semantic-convention child span
   can use it rather than hand-rolling one (**OQ3**).

7. **Metrics derived from the query pipeline** — counters/histograms for query throughput,
   duration, and errors, exposed through a Darker `Meter`, so the same execution that produces a
   span also produces metrics.

8. **A separate wiring assembly** — a new `Paramore.Darker.Extensions.Diagnostics` project
   providing `AddDarkerInstrumentation()` extension methods on the OpenTelemetry
   `TracerProviderBuilder` and `MeterProviderBuilder`, mirroring
   `BrighterTracerBuilderExtensions` / `BrighterMetricsBuilderExtensions`. The OpenTelemetry SDK
   dependency lives **only** in this assembly; core Darker depends only on
   `System.Diagnostics.DiagnosticSource`.

From the developer's perspective: *"I call `AddDarkerInstrumentation()` on my OpenTelemetry
tracer and meter builders, and now every Darker query shows up as a span — named for the query
type, nested under my ASP.NET request, with an event per pipeline step — plus query
count/latency/error metrics. I can dial the attribute detail up or down, and my handler can grab
the span from the query context to nest its database call underneath."*

## Key Terms

- **Query span** — the `Activity` covering one `Execute`/`ExecuteAsync` invocation, from
  pipeline build through handler completion.
- **Operation** — the single Darker processor operation, proposed value `query`, used in the
  span name and the `operation` attribute.
- **Decorator event / handler event** — an `ActivityEvent` on the query span recorded as the
  pipeline enters each decorator, and the handler (the sink).
- **`InstrumentationOptions`** — a `[Flags]` enum controlling which attribute groups are added
  to spans (verbosity / cardinality / PII control).
- **Sink** — the terminal step in the pipeline: the query handler itself.

## Requirements

### Functional Requirements

**Tracer & span lifecycle**

- **FR1** — Provide a `DarkerTracer` (working name) that wraps a single `ActivitySource` named
  `paramore.darker`, tagged with the assembly version, exposed via an interface (e.g.
  `IAmADarkerTracer`) so it can be injected and substituted in tests. It must expose a
  `TimeProvider` seam for deterministic start/end times, following `BrighterTracer`.
- **FR2** — On `Execute` and `ExecuteAsync`, the `QueryProcessor` starts a span named
  `<QueryType> query` with span kind `Internal`, parented to the ambient `Activity.Current`
  (typically the ASP.NET controller or a Brighter handler span) when one exists, otherwise a
  root span.
- **FR3** — The query span is set as `Activity.Current` for the duration of pipeline execution
  (so handler/DB work nests under it) and is ended exactly once when the pipeline completes,
  including on the exception paths, restoring the previously-current activity.
- **FR4** — When the pipeline throws, the span records the exception (OTel exceptions
  convention) and sets `ActivityStatusCode.Error`; on success it sets `Ok`. `TargetInvocationException`
  unwrapping already performed by the processor must not obscure the recorded exception.

**Query Processor Attributes (governed by `InstrumentationOptions`)**

- **FR5** — Provide an `InstrumentationOptions` `[Flags]` enum with at minimum:
  `None`, `QueryInformation`, `QueryBody`, `QueryContext`, and `All` (a
  `DatabaseInformation` flag is added if FR11 is adopted). Naming/values are finalised in the ADR
  but must follow Brighter's pattern.
- **FR6** — With `QueryInformation`, record: `paramore.darker.queryid` (from `IQuery.Id`, see
  FR6a), `paramore.darker.querytype` (full query type name), and `paramore.darker.operation`
  (`query`).
- **FR6a** — Provide a **base `Query` class** (working name; a `Query`/`Query<TResult>` that
  implements `IQuery<TResult>`) carrying a `string Id` that **defaults to
  `Guid.NewGuid().ToString()`** when the caller does not pass one via the constructor. User query
  types derive from this base to get an id for free (or pass an explicit id). This is additive and
  **non-breaking**: existing query types that implement `IQuery<TResult>` directly and do **not**
  derive from the base continue to compile and execute unchanged — they simply have no id, and the
  processor records `paramore.darker.queryid` only when the query exposes one (i.e. derives from
  the base). Whether `IQuery` itself also declares `Id` (so the value can be read without a type
  check) versus reading it only from the base type is an ADR detail, provided direct
  `IQuery<TResult>` implementers are not forced to add a member (NFR1).
- **FR7** — With `QueryBody`, record `paramore.darker.query_body` — the query serialised as JSON
  using Darker's configured serializer options (per ADR 0012). This is the switch that "reveals
  the parameters of the query" and is **off** unless explicitly enabled.
- **FR8** — With `QueryContext`, copy any `IQueryContext.Bag` entries whose key begins with
  `paramore.darker.spancontext.` onto the span as attributes, mirroring Brighter's
  `spancontext.*` mechanism, so callers can attach ambient attributes (e.g. a user id) to the
  span.
- **FR9** — Attributes are added only when `Activity.IsAllDataRequested` is true, and each group
  is gated by its `InstrumentationOptions` flag, so cardinality/PII/cost are controllable exactly
  as in Brighter.

**Query Processor Events**

- **FR10** — As the pipeline executes, record an `ActivityEvent` per pipeline step named after
  the decorator, and one for the handler, carrying at least `paramore.darker.handlername` (full
  type name), `paramore.darker.handlertype` (`sync`/`async`), and `paramore.darker.is_sink`
  (`true` for the handler). This is the Darker analogue of "Command Processor Events" and applies
  to both the sync and async pipelines.
- **FR10a** — Because Darker has no Russian-doll base class (RD2), these events are woven into the
  `Func` chain that `PipelineBuilder.Build`/`BuildAsync` composes: each step's closure records its
  entry event before invoking the wrapped handler/decorator, and records the exception on the span
  (OTel exceptions convention, `Status = Error`) if the invocation throws — inside the existing
  `TargetInvocationException` unwrapping so the recorded exception is the real one. The handler
  (sink) closure and every decorator closure are instrumented, in both the sync and async
  builders.

**Database-call support (surface + optional helper)**

- **FR11** — Surface the active query span to handler/decorator code through `IQueryContext`
  (e.g. an `Activity? Span` property, or via the `Bag`), so a handler can create a child span for
  its database call and have it correctly nested. This is the primary, always-available DB
  support.
- **FR12** — Provide a `DarkerTracer.CreateDbSpan(DbSpanInfo, Activity? parent,
  InstrumentationOptions)` helper (and a `DbSpanInfo` value type) that builds a child span
  following the [OTel database span conventions](https://opentelemetry.io/docs/specs/semconv/database/database-spans/),
  modelled on `BrighterTracer.CreateDbSpan`. This is the primary way a handler "does the right
  thing" for its data-store call, and — because the DB span is derived from the same tracer — it
  is the source from which DB metrics are derived (FR13/RD4). (**resolved — RD3**)
- **FR12a** — Additionally provide a **DB-span decorator** whose attribute parameters supply the
  `DbSpanInfo` values (e.g. db system, db name, table/collection, operation). A handler author can
  then annotate the pipeline with the attribute instead of writing span code by hand, and the
  decorator creates/ends the child DB span (nested under the query span from FR11) around the
  handler invocation. The attribute-parameter surface and how much can be known statically vs.
  supplied at runtime are ADR details.

**Metrics**

- **FR13** — Emit query-execution metrics through a Darker `Meter` (name defined in the ADR):
  at minimum query **count**, query **duration** (histogram), and **error** count/outcome,
  dimensioned by query type and success/failure, plus DB metrics derived from the FR12 DB spans.
  **Metrics are derived from the emitted spans** via an OpenTelemetry span processor, mirroring
  Brighter's `BrighterMetricsFromTracesProcessor` (a `DarkerMetricsFromTracesProcessor` registered
  by the metrics builder extension). Metrics are therefore only produced when a query span exists,
  keeping a single instrumentation path. (**resolved — RD4**)

**Configuration & wiring**

- **FR14** — Provide a new `Paramore.Darker.Extensions.Diagnostics` assembly with
  `AddDarkerInstrumentation()` extensions on `TracerProviderBuilder` and `MeterProviderBuilder`
  that register the `paramore.darker` source/meter and the tracer/meter services, mirroring the
  Brighter builder extensions.
- **FR15** — Allow the `QueryProcessor` (and the query-processor builder + the
  `Paramore.Darker.Extensions.DependencyInjection` integration) to be supplied with the tracer
  and a default `InstrumentationOptions`. When no tracer is supplied, Darker behaves exactly as
  today (no spans, no metrics) — instrumentation is strictly additive and opt-in.

### Non-functional Requirements

- **NFR1 — Opt-in, behaviour-preserving when unconfigured.** V5 permits breaking API changes, so
  this is **not** a strict no-break constraint. What is required is that telemetry is *opt-in*: when
  no tracer is configured, existing `QueryProcessor` construction, the fluent builder, and DI
  extensions behave identically to today. Prefer additive/optional changes (e.g. optional ctor
  parameters) where they cost nothing.
- **NFR2 — Near-zero overhead when unobserved.** When the `paramore.darker` `ActivitySource`
  has no listeners, query execution must incur no meaningful allocation or cost beyond a
  `HasListeners`/null check (no span, no JSON serialisation of the query body, no event tag
  collections). Guard with `ActivitySource.HasListeners()` / `Activity.IsAllDataRequested`.
- **NFR3 — Async correctness.** Span currency must flow correctly across `await` boundaries in
  `ExecuteAsync` (via `ExecutionContext`/`Activity.Current`) and must not leak the query span to
  unrelated ambient work; the previously-current activity is restored on completion.
- **NFR4 — Dependency hygiene.** Core `Paramore.Darker` takes a dependency only on
  `System.Diagnostics.DiagnosticSource` (no OpenTelemetry SDK). The OpenTelemetry SDK dependency
  is confined to `Paramore.Darker.Extensions.Diagnostics`. All versions via Central Package
  Management (`Directory.Packages.props`).
- **NFR5 — Standards alignment.** Attribute names, span kind, event conventions, and DB/exception
  semantics follow the OTel semantic conventions and the Brighter conventions where a direct
  analogue exists, so Darker integrates cleanly with the wider ecosystem.
- **NFR6 — Target frameworks.** Builds and tests pass on .NET 8.0 and 9.0 per the existing CI
  matrix, using `Darker.Filter.slnf`.

### Constraints and Assumptions

- **C1** — Darker is an in-process, query-side library; it has no messaging, outbox, dispatcher,
  or message-pump surface, so the messaging/producer/consumer/pump portions of Brighter ADR 0010
  do not apply.
- **C2** — `IQuery<TResult>` has no `Id` today; this feature introduces one via a base `Query`
  class (FR6a/RD1) as a `string` defaulting to a GUID. Queries that do not derive from the base
  keep working and carry no id (`queryid` recorded only when present) (NFR1).
- **C3** — The actual database (or other data-store) call lives in user handler code; Darker can
  surface and optionally help create the DB span but cannot instrument the call itself.
- **C4** — Query-body serialisation reuses Darker's existing JSON serializer configuration
  (ADR 0012); telemetry introduces no second serializer.
- **C5** — Source name is fixed as `paramore.darker` to avoid typos causing missed traces
  (a registration helper should expose it), matching Brighter's approach.

### Out of Scope

- Messaging, producer/consumer, outbox/inbox, dispatcher, and message-pump spans (Darker has
  none).
- Cross-process trace-context propagation *originated by Darker* via message headers (no broker).
  Darker still participates as a child of an existing ambient trace.
- Instrumenting the user's actual database driver/ORM call beyond surfacing the parent span and
  the optional `CreateDbSpan` helper.
- Retiring or replacing the existing `QueryLogging` decorator (logging and tracing coexist;
  log/trace correlation is a possible follow-up, not part of this work).
- Baggage-as-attributes automatic promotion (may be considered later, as in Brighter).

## Resolved Decisions

- **RD1 — Query identity (was OQ1).** Introduce a **base `Query` class** exposing a `string Id`
  that defaults to `Guid.NewGuid().ToString()` when not supplied via the constructor; record it as
  `paramore.darker.queryid`. Using a base class (rather than changing `IQuery`) keeps the change
  non-breaking — direct `IQuery<TResult>` implementers are untouched and simply carry no id. See
  FR6/FR6a.
- **RD2 — Where events are raised (was OQ2).** Darker has no Russian-doll base class; events are
  woven into the `Func<…>` pipeline chain built by `PipelineBuilder.Build`/`BuildAsync`, for both
  the sync and async pipelines, recording entry events and exceptions per step. See FR10/FR10a.
- **RD3 — DB support (was OQ3).** Ship both: `IQueryContext` surfaces the query span (FR11), a
  `DarkerTracer.CreateDbSpan` helper + `DbSpanInfo` type make the child DB span easy and become
  the source for DB metrics (FR12), and a DB-span **decorator** takes the span parameters as
  attribute values so handler authors can opt in declaratively (FR12a).
- **RD4 — Metrics strategy (was OQ4).** Derive metrics from spans via a
  `DarkerMetricsFromTracesProcessor`, mirroring Brighter, rather than emitting metrics on a
  separate code path. See FR13.

## Remaining ADR-level details (not blocking approval)

- Exact `InstrumentationOptions` flag names/values and the `DarkerSemanticConventions` attribute
  string constants.
- The concrete `Query` base type/API for RD1 (naming, `Query` vs `Query<TResult>`, whether
  `IQuery` also declares `Id`) and how the DI + fluent builder pass the tracer and default
  `InstrumentationOptions` into the processor.
- The DB-span decorator's attribute-parameter surface (FR12a) and the meter/metric names (FR13).

## Acceptance Criteria

How we'll know this is working correctly:

- **AC1** — With an `ActivityListener`/OTel `TracerProvider` subscribed to source
  `paramore.darker`, executing a query (sync and async) produces exactly one span named
  `<QueryType> query`, kind `Internal`, parented to the ambient activity when present.
- **AC2** — The span carries the `QueryInformation` attributes, including
  `paramore.darker.queryid`; with `QueryBody` enabled it also carries `paramore.darker.query_body`,
  and with it disabled that attribute is absent. Toggling `InstrumentationOptions` demonstrably
  changes which attribute groups appear.
- **AC2a** — A query constructed without an explicit id exposes a non-empty GUID `Id` and that
  value appears as `paramore.darker.queryid`; a query given an explicit id surfaces that id
  instead. Existing query types that predate the `Id` member still compile and execute (NFR1).
- **AC3** — `spancontext.*` entries placed in `IQueryContext.Bag` appear as span attributes when
  `QueryContext` is enabled.
- **AC4** — Each pipeline decorator and the handler produce an event on the span; the handler
  event has `is_sink = true` and the correct `handlertype` for sync vs async pipelines.
- **AC5** — A handler that throws yields a span with `Status = Error` and a recorded exception;
  the original exception still propagates to the caller unchanged (stack trace preserved).
- **AC6** — With **no** listener on `paramore.darker`, executing a query creates no span, performs
  no query-body serialisation, and shows no measurable regression versus the un-instrumented
  processor (verified by a no-listener test asserting `Activity.Current` is unchanged / null).
- **AC7** — A handler can obtain the query span from `IQueryContext` and create a child span that
  nests correctly under the query span — both via `DarkerTracer.CreateDbSpan` (FR12) and via the
  DB-span decorator (FR12a), the latter driven by attribute parameters and requiring no span code
  in the handler.
- **AC8** — `AddDarkerInstrumentation()` on `TracerProviderBuilder` registers the source and the
  tracer service; on `MeterProviderBuilder` it registers the Darker meter **and the
  `DarkerMetricsFromTracesProcessor`**; a query then produces query count/duration/error metrics
  derived from its span, and a DB span produces DB metrics (AC for FR13/RD4).
- **AC9** — With telemetry unconfigured, existing behaviour is unchanged and the existing suite
  passes; any breaking API change (permitted in V5) is deliberate, minimal, and called out in the
  ADR rather than incidental (NFR1). New behaviour is covered by tests authored via `/test-first`.

**Testing approach**: prefer real implementations over mocks (per CLAUDE.md) — use an in-memory
`ActivityListener` to capture spans/events and an in-memory metrics reader to capture
measurements, with `QueryHandlerRegistry` + `SimpleHandlerFactory` + `InMemoryQueryContextFactory`
as the processor test rig. A fixed `TimeProvider` gives deterministic durations.

**Definition of done**: all FRs implemented behind opt-in configuration, all ACs met on .NET 8
and 9, ADR 0017 accepted after adversarial review, tasks completed via the TDD workflow, and no
regression in the existing suite.

## Additional Context

- Reference model — Brighter ADR 0010: `../../../Brighter/docs/adr/0010-brighter-semantic-conventions.md`
- Reference implementation — `Paramore.Brighter/Observability/BrighterTracer.cs`,
  `InstrumentationOptions.cs`, `BrighterSemanticConventions.cs`, and the
  `Paramore.Brighter.Extensions.Diagnostics` builder extensions.
- OTel database span conventions: https://opentelemetry.io/docs/specs/semconv/db/database-spans/
- OTel exceptions on spans: https://opentelemetry.io/docs/specs/semconv/exceptions/exceptions-spans/
- Darker touch points: `QueryProcessor.cs` (span lifecycle), `PipelineBuilder.cs` (decorator/handler
  events), `IQueryContext.cs`/`QueryContext.cs` (surfacing the span, `spancontext.*` bag), and the
  DI integration in `Paramore.Darker.Extensions.DependencyInjection`.
