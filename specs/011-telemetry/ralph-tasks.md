# Ralph Tasks: 011-telemetry (Metrics from Query Traces — ADR 0018)

> Auto-generated from the approved design for unattended TDD execution.
> Each task is self-contained with all context a fresh Claude session needs.
> Scope: ADR 0018 only. Tracing (ADR 0017) is the separate ralph-tasks-0017.md list.

## Spec Context

- **Spec**: 011-telemetry
- **Requirements**: specs/011-telemetry/requirements.md (FR13, RD4, NFR2, NFR4, AC8)
- **ADRs**: docs/adr/0018-metrics-from-query-traces.md (**depends on** docs/adr/0017-query-tracing-and-database-spans.md — the query/DB spans are the single source the metrics are derived from; 0018 does not change 0017's tracing behaviour)

## Conventions for every task

- **Namespaces**: all new *behavioural* types (meters, processor, builder extension, tag/service helpers) live in `Paramore.Darker.Extensions.Diagnostics` under `src/Paramore.Darker.Extensions.Diagnostics/`, because they depend on the OpenTelemetry SDK (`BaseProcessor<Activity>`, `MeterProviderBuilder`, `IMeterFactory`, `MeterProvider`) — NFR4 keeps those out of core. The single exception is **constants**: the meter name, metric names, service-attribute keys, and the per-instrument allowed-tag sets are added to core `src/Paramore.Darker/Observability/DarkerSemanticConventions.cs` (System.Diagnostics/BCL only). Reuse the existing tag-key constants from 0017 (`QueryType`, `Operation`, `ErrorType`, `DbSystem`, `DbName`, `DbOperation`, `DbSqlTable`, `DbCollectionName`, `ServerAddress`).
- **Allowed-tag sets** are `FrozenSet<string>` on net8.0+ and `HashSet<string>` on netstandard2.0 — use the same `#if NET8_0_OR_GREATER` / `#if NETSTANDARD2_0` conditional-compilation pattern already in `DarkerTracer.cs` and Brighter's `DbMeter.cs`/`TagObjectsExtensions.cs`.
- **Meter/MeterProvider state is process-global** (exactly like the `ActivitySource` listeners in 0017). The first meter-test task creates a non-parallel xUnit collection `test/Paramore.Darker.Extensions.Diagnostics.Tests/DarkerMeterCollection.cs` — a `[CollectionDefinition("DarkerMeter", DisableParallelization = true)]` modelled on `test/Paramore.Darker.Core.Tests/DarkerActivitySourceCollection.cs`. EVERY test that builds a `MeterProvider`, registers a `MeterListener`, OR builds a `TracerProvider` (which registers a process-global `ActivityListener`) MUST carry `[Collection("DarkerMeter")]`. Because that collection disables parallelization it also prevents a leaked trace `ActivityListener` from one test bleeding into another in this assembly.
- **Never use `InstrumentationOptions.All` in tests** — `All` includes `QueryBody`, which serialises via the shared, lock-prone `QueryLoggingJsonOptions.Options` singleton. Use body-free options (`QueryInformation`, `DatabaseInformation`, or their combination) instead.
- **Tests**: xUnit v3, one public class per file, file/class named `When_[condition]_should_[expected_behavior]`, `[Fact]`, `Shouldly`. Prefer REAL implementations over mocks. Capture metrics with the OpenTelemetry in-memory exporter already referenced by the test project (`OpenTelemetry.Exporter.InMemory` 1.11.0 is in `Directory.Packages.props` and the test csproj, and supports metrics): `AddInMemoryExporter(List<Metric> exportedMetrics)` on the `MeterProviderBuilder`, then `meterProvider.ForceFlush()` before asserting — mirror the trace exporter-flush style in the existing `When_adding_darker_instrumentation_should_register_source_and_tracer.cs`. Every metric `Metric` exposes `MetricPoints`; read each point's tags via its enumerator to assert dimensions.
- **After every task**: run `dotnet build Darker.Filter.slnf -c Release`, then the task's RALPH-VERIFY filter. Because these touch DI/OTel wiring and process-global meter/listener state, also run the FULL Diagnostics test project **twice** to prove determinism: `dotnet test test/Paramore.Darker.Extensions.Diagnostics.Tests/ -c Release` (x2). When the tracer-builder / DI wiring changes (the conditional-processor task), additionally run the full `dotnet test Darker.Filter.slnf -c Release`.

## Tasks

- [x] **Extend `DarkerSemanticConventions` with meter/metric constants and allowed-tag sets**
  - **Behavior**: The core `DarkerSemanticConventions` holder exposes the meter name, the two metric names, the service-attribute resource keys, and the two per-instrument allowed-tag sets, so meters filter dimensions against a single typo-proof source.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_reading_metric_semantic_conventions_should_expose_meter_and_metric_names.cs`
  - **Test should verify**:
    - `DarkerSemanticConventions.MeterName == "paramore.darker"`.
    - `QueryDurationMetricName == "paramore.darker.query.duration"` and `DbClientOperationDurationMetricName == "db.client.operation.duration"`.
    - `ServiceName == "service.name"`, `ServiceVersion == "service.version"`, `ServiceInstanceId == "service.instance.id"`, `ServiceNamespace == "service.namespace"`.
    - `QueryDurationAllowedTags` contains exactly `QueryType`, `Operation`, `ErrorType` (and NOT `QueryId` or `QueryBody`).
    - `DbClientOperationDurationAllowedTags` contains exactly `DbSystem`, `DbName`, `DbOperation`, `DbSqlTable`, `DbCollectionName`, `ServerAddress`, `ErrorType` (and NOT `DbStatement` or `DbUser` — high cardinality).
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/DarkerSemanticConventions.cs` — add `public const string MeterName = "paramore.darker";`, `QueryDurationMetricName`, `DbClientOperationDurationMetricName`, the four `Service*` keys, and two static readonly allowed-tag sets built with the `#if NET8_0_OR_GREATER` `FrozenSet<string>` (`.ToFrozenSet()`) / `#else HashSet<string>` pattern, seeded from the existing 0017 key constants. Add `using System.Collections.Generic;` and a guarded `using System.Collections.Frozen;`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_reading_metric_semantic_conventions_should_expose_meter_and_metric_names"`
  - **References**: ADR 0018 §Key Components 4, §Integration Points (reused 0017 keys); requirements FR13, RD4. Read `src/Paramore.Darker/Observability/DarkerSemanticConventions.cs` (existing 0017 constants), and `../Brighter/src/Paramore.Brighter/Observability/DbMeter.cs` + `BrighterSemanticConventions.cs` for the FrozenSet/HashSet shape.

- [x] **Add tag-enrichment helpers (`Filter` + `GetServiceAttributes`) and the `DarkerMeter` test collection**
  - **Behavior**: A `Filter` extension keeps only allowed-key tags from an activity's `TagObjects`, and a `GetServiceAttributes` extension reads `service.*` resource attributes off a `MeterProvider`, so recorded measurements carry service identity and are protected from high-cardinality span tags.
  - **Test file**: `test/Paramore.Darker.Extensions.Diagnostics.Tests/When_enriching_metric_tags_should_filter_to_allowed_keys_and_read_service_attributes.cs` (this is the FIRST meter test — it builds a `MeterProvider`, so it MUST carry `[Collection("DarkerMeter")]`)
  - **Test should verify**:
    - `Filter` over a tag list `{querytype, queryid, query_body, error.type}` against `DarkerSemanticConventions.QueryDurationAllowedTags` returns exactly `querytype` and `error.type` (drops `queryid`/`query_body`).
    - Building a `MeterProvider` via `Sdk.CreateMeterProviderBuilder().ConfigureResource(r => r.AddService("svc-a"))...` and calling `GetServiceAttributes()` returns a pair whose key is `service.name` and value `"svc-a"`.
  - **Implementation files**:
    - `test/Paramore.Darker.Extensions.Diagnostics.Tests/DarkerMeterCollection.cs` — new `[CollectionDefinition("DarkerMeter", DisableParallelization = true)] public sealed class DarkerMeterCollection {}`, modelled on `test/Paramore.Darker.Core.Tests/DarkerActivitySourceCollection.cs`.
    - `src/Paramore.Darker.Extensions.Diagnostics/Observability/TagObjectsExtensions.cs` — `internal static KeyValuePair<string, object?>[] Filter(this IEnumerable<KeyValuePair<string, object?>> tags, FrozenSet<string>/HashSet<string> allowedTags)`, ported verbatim from Brighter's `TagObjectsExtensions` (allocation-free buffer, trimmed to `insertIndex`).
    - `src/Paramore.Darker.Extensions.Diagnostics/Observability/MeterProviderExtensions.cs` — `internal static KeyValuePair<string, object?>[] GetServiceAttributes(this MeterProvider meterProvider)` ported from Brighter's `MeterProviderExtensions` (`GetResource().Attributes.Where(service.* keys).Cast<...>().ToArray()`), using `DarkerSemanticConventions.Service*` keys.
    - `src/Paramore.Darker.Extensions.Diagnostics/Paramore.Darker.Extensions.Diagnostics.csproj` — add `<ItemGroup><InternalsVisibleTo Include="Paramore.Darker.Extensions.Diagnostics.Tests" /></ItemGroup>` so the internal helpers are testable.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Extensions.Diagnostics.Tests/ --filter "FullyQualifiedName~When_enriching_metric_tags_should_filter_to_allowed_keys_and_read_service_attributes"`
  - **References**: ADR 0018 §Technology Choices (allowed-tag filtering + `GetServiceAttributes`), §Key Components 2; requirements FR13, NFR2. Read `../Brighter/src/Paramore.Brighter/Observability/TagObjectsExtensions.cs` and `MeterProviderExtensions.cs`; `test/Paramore.Darker.Core.Tests/DarkerActivitySourceCollection.cs`.

- [x] **Add `IAmADarkerQueryMeter` + `QueryMeter` recording `paramore.darker.query.duration`**
  - **Behavior**: `QueryMeter` owns one `Histogram<double>` `paramore.darker.query.duration` (unit `s`); `RecordQueryOperation(Activity)` records `activity.Duration.TotalSeconds` with the allowed query tags plus the resource service attributes; `Enabled` reflects the histogram's listener state.
  - **Test file**: `test/Paramore.Darker.Extensions.Diagnostics.Tests/When_recording_query_operation_should_record_duration_with_allowed_query_tags.cs` (`[Collection("DarkerMeter")]`)
  - **Test should verify**:
    - Building a `MeterProvider` with `.AddMeter(DarkerSemanticConventions.MeterName)` + `AddInMemoryExporter(metrics)`, resolving/constructing a `QueryMeter` (with the SDK's `IMeterFactory` + `MeterProvider`), starting+stopping a `paramore.darker` `Internal` activity tagged `querytype`, `operation="query"`, and a stray `query_body`, then calling `RecordQueryOperation(activity)` and `ForceFlush()`, exports exactly one `paramore.darker.query.duration` metric with a single point whose tags include `querytype` and `operation` but NOT `query_body`.
    - A recorded activity additionally tagged `error.type` surfaces `error.type` as a metric dimension.
    - `queryMeter.Enabled` is true while the meter is subscribed.
  - **Implementation files**:
    - `src/Paramore.Darker.Extensions.Diagnostics/Observability/IAmADarkerQueryMeter.cs` — `public interface IAmADarkerQueryMeter { void RecordQueryOperation(Activity activity); bool Enabled { get; } }`.
    - `src/Paramore.Darker.Extensions.Diagnostics/Observability/QueryMeter.cs` — `public sealed class QueryMeter : IAmADarkerQueryMeter`; ctor `(IMeterFactory meterFactory, MeterProvider meterProvider)` (mirrors Brighter `DbMeter`); caches `_serviceAttributes = meterProvider.GetServiceAttributes()`; creates the histogram on `DarkerSemanticConventions.MeterName` with name `QueryDurationMetricName`, unit `"s"`, description "Duration of Darker query executions."; `RecordQueryOperation` records `[..activity.TagObjects.Filter(DarkerSemanticConventions.QueryDurationAllowedTags), .._serviceAttributes]`; `Enabled => _histogram.Enabled`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Extensions.Diagnostics.Tests/ --filter "FullyQualifiedName~When_recording_query_operation_should_record_duration_with_allowed_query_tags"`
  - **References**: ADR 0018 §Key Components 2 (query meter, count/error derived); requirements FR13, RD4, NFR2. Read `../Brighter/src/Paramore.Brighter/Observability/DbMeter.cs` + `IAmABrighterDbMeter.cs`.

- [x] **Add `IAmADarkerDbMeter` + `DbMeter` recording `db.client.operation.duration`**
  - **Behavior**: `DbMeter` owns one `Histogram<double>` `db.client.operation.duration` (unit `s`); `RecordClientOperation(Activity)` records the DB span duration with the allowed `db.*`/`server.address`/`error.type` tags plus service attributes; `Enabled` reflects the histogram.
  - **Test file**: `test/Paramore.Darker.Extensions.Diagnostics.Tests/When_recording_db_operation_should_record_duration_with_allowed_db_tags.cs` (`[Collection("DarkerMeter")]`)
  - **Test should verify**:
    - With a subscribed `MeterProvider` + in-memory exporter, starting+stopping a `paramore.darker` `Client` activity tagged `db.system="postgresql"`, `db.name="orders"`, `db.operation="select"`, `db.sql.table="order"`, `server.address="db-host"`, and a stray `db.statement`, then `RecordClientOperation(activity)` + flush, exports one `db.client.operation.duration` metric whose point tags include `db.system`, `db.name`, `db.operation`, `db.sql.table`, `server.address` but NOT `db.statement`.
    - An activity tagged `error.type` surfaces `error.type` as a dimension.
    - `dbMeter.Enabled` is true while subscribed.
  - **Implementation files**:
    - `src/Paramore.Darker.Extensions.Diagnostics/Observability/IAmADarkerDbMeter.cs` — `public interface IAmADarkerDbMeter { void RecordClientOperation(Activity activity); bool Enabled { get; } }`.
    - `src/Paramore.Darker.Extensions.Diagnostics/Observability/DbMeter.cs` — `public sealed class DbMeter : IAmADarkerDbMeter`; ctor `(IMeterFactory meterFactory, MeterProvider meterProvider)`; histogram name `DbClientOperationDurationMetricName`, unit `"s"`, description "Duration of database client operations."; records `[..activity.TagObjects.Filter(DarkerSemanticConventions.DbClientOperationDurationAllowedTags), .._serviceAttributes]`; `Enabled => _histogram.Enabled`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Extensions.Diagnostics.Tests/ --filter "FullyQualifiedName~When_recording_db_operation_should_record_duration_with_allowed_db_tags"`
  - **References**: ADR 0018 §Key Components 3 (DB meter, exact keys `CreateDbSpan` sets); requirements FR13, RD4. Read `src/Paramore.Darker/Observability/DarkerTracer.cs` (`CreateDbSpan` tag keys) and `../Brighter/src/Paramore.Brighter/Observability/DbMeter.cs`.

- [x] **Add `DarkerMetricsFromTracesProcessor` dispatching span ends to the right meter**
  - **Behavior**: `DarkerMetricsFromTracesProcessor : BaseProcessor<Activity>` short-circuits when neither meter is enabled, ignores spans from other sources, and on our source dispatches by `ActivityKind` — `Internal` ⇒ `queryMeter.RecordQueryOperation`, `Client` ⇒ `dbMeter.RecordClientOperation`. It holds no metric state.
  - **Test file**: `test/Paramore.Darker.Extensions.Diagnostics.Tests/When_ending_span_through_processor_should_dispatch_to_meter_by_activity_kind.cs` (`[Collection("DarkerMeter")]`)
  - **Test should verify**:
    - With both meters enabled (subscribed `MeterProvider` + in-memory exporter), calling `OnEnd` with a stopped `paramore.darker` `Internal` activity records one `paramore.darker.query.duration` and no `db.client.operation.duration`; a `Client` activity records `db.client.operation.duration` and no query metric.
    - An activity from a DIFFERENT `ActivitySource` name records nothing.
    - When neither meter is enabled (no `MeterProvider` subscribing `paramore.darker`), `OnEnd` records nothing (short-circuit) and does not throw.
  - **Implementation files**:
    - `src/Paramore.Darker.Extensions.Diagnostics/Observability/DarkerMetricsFromTracesProcessor.cs` — `public sealed class DarkerMetricsFromTracesProcessor : BaseProcessor<Activity>`; ctor `(IAmADarkerTracer tracer, IAmADarkerQueryMeter queryMeter, IAmADarkerDbMeter dbMeter)` caching `tracer.ActivitySource.Name`; `public override void OnEnd(Activity? activity)`: return if `!(queryMeter.Enabled || dbMeter.Enabled)`, if `activity is null`, if `activity.Source.Name != _sourceName`; then `switch (activity.Kind) { case ActivityKind.Internal: queryMeter.RecordQueryOperation(activity); break; case ActivityKind.Client: dbMeter.RecordClientOperation(activity); break; }`; call `base.OnEnd(activity)`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Extensions.Diagnostics.Tests/ --filter "FullyQualifiedName~When_ending_span_through_processor_should_dispatch_to_meter_by_activity_kind"`
  - **References**: ADR 0018 §Key Components 1, §Architecture Overview (dispatch on `ActivityKind`), §Risks (Enabled/source guards first); requirements FR13, NFR2. Read `../Brighter/src/Paramore.Brighter/Observability/BrighterMetricsFromTracesProcessor.cs`; `src/Paramore.Darker/Observability/IAmADarkerTracer.cs`.

- [x] **Add `DarkerMetricsBuilderExtensions.AddDarkerInstrumentation(this MeterProviderBuilder)`**
  - **Behavior**: `AddDarkerInstrumentation()` on a `MeterProviderBuilder` registers the two meters as singletons and adds the `paramore.darker` meter so their instruments are collected.
  - **Test file**: `test/Paramore.Darker.Extensions.Diagnostics.Tests/When_adding_darker_instrumentation_to_meter_builder_should_register_meters_and_meter.cs` (`[Collection("DarkerMeter")]`)
  - **Test should verify**:
    - Building a `ServiceCollection` with `AddOpenTelemetry().WithMetrics(b => b.AddDarkerInstrumentation().AddInMemoryExporter(metrics))`, then resolving `GetRequiredService<MeterProvider>()` to activate collection, resolving `IAmADarkerQueryMeter` and `IAmADarkerDbMeter` returns non-null singletons whose `Enabled` is true.
    - Resolving `IAmADarkerQueryMeter` twice returns the same instance.
  - **Implementation files**:
    - `src/Paramore.Darker.Extensions.Diagnostics/DarkerMetricsBuilderExtensions.cs` — `public static MeterProviderBuilder AddDarkerInstrumentation(this MeterProviderBuilder builder)` that `builder.ConfigureServices(services => { services.TryAddSingleton<IAmADarkerQueryMeter, QueryMeter>(); services.TryAddSingleton<IAmADarkerDbMeter, DbMeter>(); })` and `builder.AddMeter(DarkerSemanticConventions.MeterName)`. Mirror Brighter's `BrighterMetricsBuilderExtensions`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Extensions.Diagnostics.Tests/ --filter "FullyQualifiedName~When_adding_darker_instrumentation_to_meter_builder_should_register_meters_and_meter"`
  - **References**: ADR 0018 §Key Components 5; requirements FR13, NFR4. Read `../Brighter/src/Paramore.Brighter.Extensions.Diagnostics/BrighterMetricsBuilderExtensions.cs`; existing `src/Paramore.Darker.Extensions.Diagnostics/DarkerTracerBuilderExtensions.cs`.

- [x] **Make the tracer-builder `AddDarkerInstrumentation` add the metrics processor only when both meters are registered**
  - **Behavior**: `AddDarkerInstrumentation(this TracerProviderBuilder)` adds a `DarkerMetricsFromTracesProcessor` to the tracer pipeline only when BOTH `IAmADarkerQueryMeter` and `IAmADarkerDbMeter` are registered (i.e. the meter builder was also wired); tracing alone adds no processor and no metric cost.
  - **Test file**: `test/Paramore.Darker.Extensions.Diagnostics.Tests/When_adding_tracer_instrumentation_should_add_metrics_processor_only_when_meters_registered.cs` (`[Collection("DarkerMeter")]`)
  - **Test should verify**:
    - Wiring BOTH `WithTracing(t => t.AddDarkerInstrumentation()...)` and `WithMetrics(m => m.AddDarkerInstrumentation().AddInMemoryExporter(metrics))`, then creating+ending a query span on the resolved `IAmADarkerTracer` and flushing, records a `paramore.darker.query.duration` measurement (proves the processor was added to the tracer pipeline).
    - Wiring ONLY `WithTracing(t => t.AddDarkerInstrumentation()...)` (no meter builder) creates+ends a span successfully and records NO metrics (no processor added, no throw) — NFR2/AC8.
  - **Implementation files**:
    - `src/Paramore.Darker.Extensions.Diagnostics/DarkerTracerBuilderExtensions.cs` — inside `AddDarkerInstrumentation`, after registering the tracer, add `builder.ConfigureServices(services => { if (services.Any(sd => sd.ServiceType == typeof(IAmADarkerQueryMeter)) && services.Any(sd => sd.ServiceType == typeof(IAmADarkerDbMeter))) builder.AddProcessor(sp => new DarkerMetricsFromTracesProcessor(sp.GetRequiredService<IAmADarkerTracer>(), sp.GetRequiredService<IAmADarkerQueryMeter>(), sp.GetRequiredService<IAmADarkerDbMeter>())); });`. Add `using System.Linq;` and `using Microsoft.Extensions.DependencyInjection;`. Do not otherwise change the existing source/tracer registration (0017 behaviour preserved).
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Extensions.Diagnostics.Tests/ --filter "FullyQualifiedName~When_adding_tracer_instrumentation_should_add_metrics_processor_only_when_meters_registered"`
  - **References**: ADR 0018 §Key Components 6 (conditional processor), §Consequences/Negative (both builders required); requirements FR13, NFR2, AC8. Read existing `src/Paramore.Darker.Extensions.Diagnostics/DarkerTracerBuilderExtensions.cs`; `../Brighter/src/Paramore.Brighter.Extensions.Diagnostics/BrighterTracerBuilderExtensions.cs`.

- [ ] **End-to-end: executing a query records query + DB duration metrics with correct dimensions, and nothing when unwired**
  - **Behavior**: With both the tracer and meter builders wired and an in-memory metric reader, executing a query through a real `QueryProcessor` records one `paramore.darker.query.duration`; a DB span records `db.client.operation.duration`; a failing query adds `error.type`; high-cardinality span tags never become metric dimensions; and with no meter builder wired, nothing is recorded.
  - **Test file**: `test/Paramore.Darker.Extensions.Diagnostics.Tests/When_executing_query_with_metrics_wired_should_record_query_and_db_duration_metrics.cs` (`[Collection("DarkerMeter")]`)
  - **Test should verify**:
    - Wiring `WithTracing(t => t.AddDarkerInstrumentation())` + `WithMetrics(m => m.AddDarkerInstrumentation().AddInMemoryExporter(metrics))`, resolving the `IAmADarkerTracer`, building a `QueryProcessor` with that tracer and body-free `InstrumentationOptions` (`QueryInformation | DatabaseInformation`, NOT `All`), executing a query whose handler carries `[QueryDbSpan(...)]`, then `ForceFlush()`: exports one `paramore.darker.query.duration` point with `querytype`/`operation` dimensions and one `db.client.operation.duration` point with `db.*` dimensions.
    - No exported metric point carries a `query_body` or `spancontext.*` dimension.
    - Executing a query whose handler throws yields a `paramore.darker.query.duration` point with an `error.type` dimension.
    - Wiring ONLY the tracer builder (no meter builder) and executing a query records zero metrics (NFR2, AC8).
  - **Implementation files**:
    - (none — behaviour is fully delivered by the previous tasks; this task only adds the end-to-end test, plus any shared test-double query/handler under `test/Paramore.Darker.Extensions.Diagnostics.Tests/TestDoubles/` if one does not already exist.)
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Extensions.Diagnostics.Tests/ --filter "FullyQualifiedName~When_executing_query_with_metrics_wired_should_record_query_and_db_duration_metrics"`
  - **References**: ADR 0018 §Implementation Approach (end-to-end assertions), §Consequences; requirements FR13, RD4, NFR2, AC8. Read `src/Paramore.Darker/QueryProcessor.cs` (tracer/options ctor params from 0017), `src/Paramore.Darker/Observability/Attributes/QueryDbSpanAttribute.cs`, and the existing `test/Paramore.Darker.Extensions.Diagnostics.Tests/When_adding_darker_instrumentation_should_register_source_and_tracer.cs` for the exporter-flush rig.
