# Telemetry

**Spec ID:** 011-telemetry
**Created:** 2026-07-01
**Status:** Ralph tasks generated for ADR 0017 (25 tasks); ready for `/spec:ralph-implement`. Metrics (0018) tasks pending.

## Overview

Add OpenTelemetry **traces and metrics** to Darker's query pipeline, modelled on Brighter's
semantic-conventions approach (Brighter ADR 0010 / `BrighterTracer`). A `DarkerTracer` wraps a
single `ActivitySource` (`paramore.darker`); `QueryProcessor` starts a `<QueryType> query` span
covering the whole pipeline, records an event per decorator/handler, records exceptions, and
exposes the span through `IQueryContext` so handlers can nest their DB call. Attribute verbosity
(trace-only vs revealing query parameters) is controlled by an `InstrumentationOptions` flags
enum. A separate `Paramore.Darker.Extensions.Diagnostics` assembly provides
`AddDarkerInstrumentation()` for the OpenTelemetry `TracerProviderBuilder` / `MeterProviderBuilder`.
Instrumentation is strictly additive and opt-in; core Darker takes no OpenTelemetry SDK dependency.

Scope note: Darker is in-process query-side only — no messaging/outbox/dispatcher spans — so only
the "Command Processor span/attributes/events" portion of Brighter's model applies. Resolved
decisions (see `requirements.md`): a base `Query` class gives queries a `string Id` defaulting to
a GUID, non-breaking for direct `IQuery<TResult>` implementers (RD1); span
events are woven into `PipelineBuilder`'s `Func` chain since Darker has no Russian-doll base class
(RD2); a `CreateDbSpan` helper plus a DB-span decorator make handler DB spans easy (RD3); metrics
are derived from spans via a `DarkerMetricsFromTracesProcessor` (RD4).

## Status Checklist

- [x] Requirements (`/spec:requirements`) — ✅ approved
- [x] Design / ADR (`/spec:design`) — ✅ approved: 0017 tracing+DB, 0018 metrics
- [ ] Adversarial Review
- [x] Task Breakdown — `ralph-tasks.md` generated for ADR 0017 (25 tasks, unattended path); 0018 metrics tasks pending
- [ ] Implementation (`/spec:ralph-implement`)

## Documents

| Phase | File | Status |
|-------|------|--------|
| Requirements | `requirements.md` | ✅ Approved |
| Design | `docs/adr/0017-query-tracing-and-database-spans.md` | ✅ Accepted |
| Design | `docs/adr/0018-metrics-from-query-traces.md` | ✅ Accepted |
| Tasks | `ralph-tasks.md` | 📝 Generated (ADR 0017, 25 tasks); 0018 pending |
