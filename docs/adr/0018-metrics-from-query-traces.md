# 18. Metrics from Query Traces

Date: 2026-07-01

## Status

Accepted

## Context

**Parent Requirement**: [specs/011-telemetry/requirements.md](../../specs/011-telemetry/requirements.md)

**Scope**: This ADR covers Darker's **metrics** surface (requirement FR13 / RD4). It defines how
query-execution and database metrics are produced, the meter(s) and instruments, their names/units/
dimensions, and the `AddDarkerInstrumentation` extension on the OpenTelemetry `MeterProviderBuilder`.
It **depends on** ADR 0017 (the `DarkerTracer`, the query span and DB span, `InstrumentationOptions`,
`DarkerSemanticConventions`) and does **not** re-specify any of it. No changes to the *tracing*
behaviour of 0017 are introduced here beyond adding a small number of shared constants and one
`error.type` tag on failed spans (see Integration Points).

RD4 chose to **derive metrics from the spans** Darker already emits, rather than instrumenting a
second, parallel metrics code path in `QueryProcessor`/`PipelineBuilder`. Every executed query
already produces a query span (0017), and DB work produces a DB span; those activities carry exactly
the data a metric needs (duration, query type, operation, success/failure, `db.*`). Reading them
once, at span end, gives us metrics for free and guarantees traces and metrics never disagree.

Forces at play:

- **Single instrumentation path (RD4).** Avoid duplicating "start timer / read query type / decide
  success" logic in both a tracer and a meter; the span is the single source of truth.
- **Follow Brighter.** Brighter's `BrighterMetricsFromTracesProcessor` + `DbMeter`/`MessagingMeter`
  + `BrighterMetricsBuilderExtensions` are proven; we mirror the shape and reuse OTel database
  semantic-convention metric names so Darker fits the ecosystem.
- **Opt-in, zero-overhead when unobserved (NFR2).** Metrics only exist when the user wires them; the
  processor must short-circuit cheaply when no meter has listeners.
- **Dependency hygiene (NFR4).** The OpenTelemetry SDK types (`BaseProcessor<Activity>`,
  `MeterProviderBuilder`, `IMeterFactory`) live only in `Paramore.Darker.Extensions.Diagnostics`.
  Core `Paramore.Darker` keeps only `System.Diagnostics` (the meters use
  `System.Diagnostics.Metrics`, which is part of `DiagnosticSource`, so the meter classes *could*
  sit in core — but because they depend on `MeterProvider`/`IMeterFactory` from the OTel SDK and are
  only useful with it, they live in the Diagnostics assembly, unlike the tracer which is core).
- **Experimental spec.** OTel *metrics-from-traces* and some DB metric conventions are still
  unstable; we isolate that instability in one processor + meter, as Brighter does.

## Decision

Add a `DarkerMetricsFromTracesProcessor` (an OpenTelemetry `BaseProcessor<Activity>`) and two
meters — a **query meter** and a **DB meter** — in `Paramore.Darker.Extensions.Diagnostics`. On each
span end, the processor filters to the `paramore.darker` source and dispatches to the appropriate
meter, which records a **duration histogram**. Count and success/failure are *derived* from the
histogram (its data-point count) and an `error.type` dimension — so no separate counters are needed.
`AddDarkerInstrumentation(this MeterProviderBuilder)` registers the meters; the tracer-side
`AddDarkerInstrumentation(this TracerProviderBuilder)` from 0017 conditionally adds the processor
when both meters are present (metrics-from-traces needs a processor on the *tracer* pipeline).

### Responsibility-Driven Design — roles

| Role (stereotype) | Type | Knows / Does / Decides |
|---|---|---|
| Coordinator / controller | `DarkerMetricsFromTracesProcessor` | On span end, **decides** whether the span is ours and whether it is a query or DB span, and **delegates** to the right meter. Holds no metric state. |
| Service provider | `IAmADarkerQueryMeter` → `QueryMeter` | Owns the query-duration instrument; **does** record a measurement from a query activity. Knows its allowed tags + `Enabled`. |
| Service provider | `IAmADarkerDbMeter` → `DbMeter` | Owns the `db.client.operation.duration` instrument; **does** record from a DB activity. |
| Information holder | `DarkerSemanticConventions` (extended) | **Knows** the meter name, metric names, and the allowed-tag sets. |
| Interfacer | `DarkerMetricsBuilderExtensions` | **Does** register meters + `AddMeter` on the `MeterProviderBuilder`. |

Splitting the *decision* (processor: which meter?) from the *doing* (meters: record what?) keeps each
class cohesive: the processor knows dispatch, each meter knows one instrument. New metric families
(e.g. a future cache meter) are added as new meters + a new switch arm, not by growing one class.

### Architecture Overview

```
 OTel TracerProvider pipeline                 OTel MeterProvider pipeline
 ────────────────────────────                 ───────────────────────────
 query/DB spans (source: paramore.darker)     meters (name: paramore.darker)
        │  Activity.OnEnd                          ▲ instruments recorded
        ▼                                          │
 DarkerMetricsFromTracesProcessor : BaseProcessor<Activity>
   if (!query.Enabled && !db.Enabled) return;          // cheap short-circuit
   if (activity.Source.Name != "paramore.darker") return;
   switch (activity.Kind) {                             // our source emits only these two
     case Internal:  queryMeter.RecordQueryOperation(activity);   // the query span
     case Client:    dbMeter.RecordClientOperation(activity);     // a DB span
   }
        │                                   │
        ▼                                   ▼
 QueryMeter                          DbMeter
   Histogram "paramore.darker.query.duration" (s)   Histogram "db.client.operation.duration" (s)
   tags: query.type, operation, error.type          tags: db.system, db.namespace, db.operation.name,
   Record(activity.Duration.TotalSeconds, tags)            db.collection.name, error.type, server.* …
```

Wiring (user code):

```csharp
Sdk.CreateTracerProviderBuilder().AddDarkerInstrumentation()...;   // source + (conditional) processor
Sdk.CreateMeterProviderBuilder().AddDarkerInstrumentation()...;    // meters + AddMeter
services.AddDarker(...);                                            // supplies the tracer (0017)
```

### Key Components

**1. `DarkerMetricsFromTracesProcessor : BaseProcessor<Activity>`** — constructed with
`IAmADarkerTracer` (for the source name), `IAmADarkerQueryMeter`, `IAmADarkerDbMeter`. `OnEnd`:
guards on `queryMeter.Enabled || dbMeter.Enabled`, then source name, then dispatches on
`activity.Kind` — `Internal` ⇒ query span ⇒ `queryMeter.RecordQueryOperation`; `Client` ⇒ DB span
⇒ `dbMeter.RecordClientOperation`. Dispatching on `ActivityKind` is unambiguous *within our own
source*, which emits exactly those two span kinds (0017), so no extra discriminator tag is required
(a `DarkerSemanticConventions.InstrumentationDomain` tag is the fallback if that ever changes — see
Alternatives).

**2. `IAmADarkerQueryMeter` / `QueryMeter`** — one instrument:

- `Histogram<double>` **`paramore.darker.query.duration`**, unit **`s`**, description "Duration of
  Darker query executions." Recorded as `activity.Duration.TotalSeconds`.
- Allowed tags (low cardinality): `query.type` (`paramore.darker.querytype`), `operation`
  (`query`), and `error.type` (present only on failure). Records `[..activity.TagObjects.Filter(
  allowedTags), ..serviceAttributes]`.
- `Enabled => _histogram.Enabled`.

**Count and error/outcome are derived, not separate instruments:** an OTel histogram inherently
exposes a data-point **count** (that is the "how many queries ran" metric), and the `error.type`
dimension partitions those counts into success (absent) vs failure (exception type) — exactly how
OTel's `*.duration` histograms serve as both count and latency. This satisfies FR13's "count",
"duration", and "error outcome" with a single instrument, mirroring Brighter's `DbMeter`.

**3. `IAmADarkerDbMeter` / `DbMeter`** — mirrors Brighter's `DbMeter`:

- `Histogram<double>` **`db.client.operation.duration`** (OTel database semconv), unit `s`.
- Allowed tags: `db.system`, `db.namespace`/`db.name`, `db.operation.name`, `db.collection.name`,
  `error.type`, `server.address`, `server.port`, `network.peer.*` — the tags 0017's `CreateDbSpan`
  sets from `DbSpanInfo`.
- `Enabled => _histogram.Enabled`.

**4. `DarkerSemanticConventions` (extended)** — adds `MeterName = "paramore.darker"`, the query and
DB metric names, and the per-instrument allowed-tag sets (as `FrozenSet<string>` on .NET 8+, `HashSet`
otherwise, following Brighter). Tag-*key* constants already introduced in 0017 are reused.

**5. `DarkerMetricsBuilderExtensions.AddDarkerInstrumentation(this MeterProviderBuilder)`** —
`TryAddSingleton<IAmADarkerQueryMeter, QueryMeter>()`, `TryAddSingleton<IAmADarkerDbMeter, DbMeter>()`,
then `builder.AddMeter(DarkerSemanticConventions.MeterName)`. Mirrors
`BrighterMetricsBuilderExtensions`.

**6. Processor registration (in 0017's tracer builder extension)** —
`AddDarkerInstrumentation(TracerProviderBuilder)` adds `DarkerMetricsFromTracesProcessor` **only when
both meters are registered** (`services.Any(sd => sd.ServiceType == typeof(IAmADarkerQueryMeter))`
&& the DB meter). So metrics require the user to call `AddDarkerInstrumentation` on **both** the
tracer and meter builders; tracing alone adds no processor and no metric cost.

### Technology Choices

- **Metrics-from-traces via `BaseProcessor<Activity>`** — the single-source-of-truth design (RD4);
  no timing/labelling duplicated between tracer and meter.
- **`System.Diagnostics.Metrics.Histogram<double>` + `IMeterFactory`** — the .NET-native metrics
  API; instruments created on `DarkerSemanticConventions.MeterName`.
- **Duration histogram only** (count/error derived) — matches OTel `*.duration` convention and
  Brighter; fewer instruments, no double counting.
- **OTel database metric name `db.client.operation.duration`** reused verbatim for DB metrics;
  a Darker-namespaced `paramore.darker.query.duration` for the query metric (no OTel semconv exists
  for an in-process query processor).
- **Allowed-tag filtering + resource/service attributes** (`GetServiceAttributes`) ported from
  Brighter's `MeterProviderExtensions`, so measurements carry `service.name` etc. and are protected
  from high-cardinality span tags (e.g. `query_body` is never a metric dimension).

### Implementation Approach

Structural first: extend `DarkerSemanticConventions` with the meter/metric constants and allowed-tag
sets; port `GetServiceAttributes`. Then behavioural: `QueryMeter`, `DbMeter`, the processor, and the
two builder-extension changes. Drive with `/test-first` using an in-memory metrics reader
(`MeterProvider` + `MetricReader`) plus the in-memory `ActivityListener` from 0017: assert that
executing a query records one `paramore.darker.query.duration` measurement with `query.type`/
`operation`; that a failing query adds `error.type`; that a DB span records
`db.client.operation.duration` with `db.*` tags; that `query_body`/`spancontext.*` never appear as
metric dimensions; and that with no meter registered the processor is absent / `Enabled` is false and
nothing is recorded (NFR2).

## Consequences

### Positive

- Traces and metrics come from one source and cannot disagree; no second timing path in the hot
  `QueryProcessor`/`PipelineBuilder` code.
- One `AddDarkerInstrumentation()` per builder yields query latency+throughput+error-rate and DB
  operation latency, in OTel-standard shapes.
- Instability of the experimental metrics-from-traces spec is isolated in one processor + two meters.
- Cohesive, extensible: a future metric family is a new meter + one switch arm.

### Negative

- Metrics require wiring **both** the tracer and meter builders; wiring only the meter builder yields
  no metrics (the processor lives on the tracer pipeline). Must be documented clearly.
- Metrics exist only when tracing spans are produced — if a user disables tracing entirely, metrics
  stop too (acceptable given RD4; noted).
- Reuses the experimental `db.client.operation.duration` convention, which may shift in future OTel
  releases.

### Risks and Mitigations

- **Per-span-end overhead** on the tracer pipeline — guard `OnEnd` with `Enabled` and source-name
  checks before any tag work; the processor is only added when meters exist.
- **High-cardinality metric dimensions** blowing up the time series — strict per-instrument
  allowed-tag sets; `query.type` is bounded by the number of query types; `query_body`/`spancontext.*`
  are excluded by construction.
- **Dispatch on `ActivityKind` misclassifying** if the source later emits other kinds — today the
  source emits only Internal (query) and Client (DB); if that changes, switch to an explicit
  `InstrumentationDomain` tag (kept as the documented fallback).
- **`error.type` availability** — guaranteed: 0017's `AddExceptionToSpan` tags failed spans with
  `error.type` (see Integration Points).

### Integration Points with ADR 0017

- **`error.type` tag.** 0017's `AddExceptionToSpan` sets an `error.type` span tag (the exception's
  type name, per the OTel exceptions convention) in addition to `ActivityStatusCode.Error`. This
  processor reads that tag directly as the success/failure metric dimension, so trace and metric
  share one value. (`error.type` is defined in `DarkerSemanticConventions`.)
- **Shared constants.** `MeterName` and metric-tag keys live in the `DarkerSemanticConventions`
  holder introduced by 0017; 0018 extends it.
- **Span kinds.** Relies on 0017's query span = `Internal`, DB span = `Client`.

## Alternatives Considered

- **Emit metrics directly from `QueryProcessor`/`PipelineBuilder`** (a parallel meter path) —
  rejected per RD4: duplicates timing/labelling, risks trace/metric drift, and adds cost to the hot
  path even when only tracing is wanted.
- **Separate counter instruments for count and error** — rejected: an OTel duration histogram already
  yields count, and `error.type` yields the success/failure split; extra counters would double-count
  and add cardinality for no gain (matches Brighter/OTel).
- **Dispatch via an `InstrumentationDomain` tag (Brighter's approach)** — viable but unnecessary for
  Darker, whose single source emits only two span kinds; `ActivityKind` dispatch needs no extra tag.
  Retained as the documented fallback.
- **Put the meters in core `Paramore.Darker`** — rejected: they depend on OTel SDK
  `MeterProvider`/`IMeterFactory` and are only useful with the SDK, so they belong beside the other
  wiring in `Paramore.Darker.Extensions.Diagnostics` (NFR4).

## References

- Requirements: [specs/011-telemetry/requirements.md](../../specs/011-telemetry/requirements.md) (FR13, RD4, NFR2, NFR4)
- Related ADRs: **0017** (tracing + DB spans — the source of the metrics); 0012 (JSON serializer)
- Brighter model: `Paramore.Brighter/Observability/BrighterMetricsFromTracesProcessor.cs`,
  `DbMeter.cs`, `IAmABrighterDbMeter.cs`, `MessagingMeter.cs`, `MeterProviderExtensions.cs`;
  `Paramore.Brighter.Extensions.Diagnostics/BrighterMetricsBuilderExtensions.cs`
- OTel database metrics: https://opentelemetry.io/docs/specs/semconv/database/database-metrics/
- OTel metrics API (.NET): https://learn.microsoft.com/dotnet/core/diagnostics/metrics
- OTel exceptions / `error.type`: https://opentelemetry.io/docs/specs/semconv/attributes-registry/error/
