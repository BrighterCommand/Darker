# Ralph Tasks: 011-telemetry (Tracing + Database Spans — ADR 0017)

> Auto-generated from the approved design for unattended TDD execution.
> Each task is self-contained with all context a fresh Claude session needs.
> Scope: ADR 0017 only. Metrics (ADR 0018) are a separate task list.

## Spec Context

- **Spec**: 011-telemetry
- **Requirements**: specs/011-telemetry/requirements.md
- **ADRs**: docs/adr/0017-query-tracing-and-database-spans.md (metrics deferred to docs/adr/0018-metrics-from-query-traces.md)

## Conventions for every task

- Core namespace for new types: `Paramore.Darker.Observability`.
- Tests: `test/Paramore.Darker.Core.Tests/`, one public class per file, file/class named `When_[condition]_should_[expected_behavior]`, `[Fact]`, `Shouldly`, xUnit v3. Prefer REAL test doubles (`QueryHandlerRegistry`, `SimpleHandlerFactory`, `SimpleHandlerDecoratorFactory`, `InMemoryDecoratorRegistry`, `InMemoryQueryContextFactory`/`TrackingQueryContextFactory`) over mocks; shared doubles live in `test/Paramore.Darker.Core.Tests/TestDoubles/`. To capture spans use an in-memory `System.Diagnostics.ActivityListener` with `Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData` and `ShouldListenTo = s => s.Name == "paramore.darker"` — no OpenTelemetry SDK in core tests.
- After every task, `dotnet build Darker.Filter.slnf -c Release` and the task's RALPH-VERIFY filter must both pass.

## Tasks

- [x] **Add `DarkerSemanticConventions` constants holder**
  - **Behavior**: A static `DarkerSemanticConventions` class exposes the source name and all attribute/event key strings so a typo cannot silently break tracing.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_reading_semantic_conventions_should_expose_stable_attribute_keys.cs`
  - **Test should verify**:
    - `DarkerSemanticConventions.SourceName == "paramore.darker"`.
    - `QueryId == "paramore.darker.queryid"`, `QueryType == "paramore.darker.querytype"`, `Operation == "paramore.darker.operation"`, `QueryBody == "paramore.darker.query_body"`.
    - `HandlerName == "paramore.darker.handlername"`, `HandlerType == "paramore.darker.handlertype"`, `IsSink == "paramore.darker.is_sink"`, `ErrorType == "error.type"`, and a `SpanContextPrefix == "spancontext."`.
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/DarkerSemanticConventions.cs` - new static class of `public const string` fields: `SourceName`, `QueryId`, `QueryType`, `Operation`, `QueryBody`, `SpanContextPrefix`, `HandlerName`, `HandlerType`, `IsSink`, `ErrorType`, plus the `db.*` keys (`DbSystem = "db.system"`, `DbName = "db.name"`, `DbOperation = "db.operation"`, `DbCollectionName`/`DbSqlTable`, `ServerAddress = "server.address"`, `DbStatement`, `DbUser`).
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_reading_semantic_conventions_should_expose_stable_attribute_keys"`
  - **References**: ADR 0017 §Key Components 4 (`DarkerSemanticConventions`); requirements FR1, FR6, FR8, FR10, NFR5.

- [ ] **Add `InstrumentationOptions` `[Flags]` enum**
  - **Behavior**: An `InstrumentationOptions` flags enum names the attribute groups that may be emitted, with `All` being the union of every group.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_combining_instrumentation_options_should_expose_flag_groups.cs`
  - **Test should verify**:
    - `None == 0`, `QueryInformation == 1`, `QueryBody == 2`, `QueryContext == 4`, `DatabaseInformation == 8`.
    - `All == (QueryInformation | QueryBody | QueryContext | DatabaseInformation)` and `All.HasFlag(QueryBody)` is true.
    - `None.HasFlag(QueryInformation)` is false.
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/InstrumentationOptions.cs` - new `[Flags] public enum InstrumentationOptions` exactly as above.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_combining_instrumentation_options_should_expose_flag_groups"`
  - **References**: ADR 0017 §Key Components 3 (`InstrumentationOptions`); requirements FR5, FR9.

- [ ] **Add `Query<TResult>` base class with defaulted-GUID `Id`**
  - **Behavior**: A query deriving from `Query<TResult>` and constructed with the parameterless ctor exposes a non-empty GUID string `Id` that is unique per instance.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_creating_query_without_id_should_default_to_guid.cs`
  - **Test should verify**:
    - A test query derived from `Query<TResult>` has a non-null, non-empty `Id` that parses as a `Guid`.
    - Two instances have different `Id` values.
    - The type is assignable to `IQuery<TResult>` (base class implements the marker; `IQuery` itself gains no member).
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/Query.cs` - `public abstract class Query<TResult> : IQuery<TResult>` with `protected Query() { }` and `public string Id { get; init; } = Guid.NewGuid().ToString();`.
  - **References**: ADR 0017 §Key Components 2 (`Query` base class), §Alternatives (why not `IQuery`); requirements FR6a, RD1, AC2a. Read `src/Paramore.Darker/IQuery.cs`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_creating_query_without_id_should_default_to_guid"`

- [ ] **Allow `Query<TResult>` to take an explicit id**
  - **Behavior**: A query deriving from `Query<TResult>` constructed with `Query(string id)` exposes exactly that id instead of a generated GUID.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_creating_query_with_explicit_id_should_use_supplied_id.cs`
  - **Test should verify**:
    - Passing `"order-42"` to the base ctor yields `Id == "order-42"`.
    - The `init` setter also allows `new TestQuery { Id = "x" }` to override the default.
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/Query.cs` - add `protected Query(string id) => Id = id;`.
  - **References**: ADR 0017 §Key Components 2 (`Query` base class); requirements FR6a, AC2a.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_creating_query_with_explicit_id_should_use_supplied_id"`

- [ ] **Surface `Span` and `Tracer` on `IQueryContext` (default null)**
  - **Behavior**: `IQueryContext` gains nullable `Activity? Span` and `IAmADarkerTracer? Tracer` get/set properties; `QueryContext` defaults both to null so existing behaviour is unchanged. (This task also brings `System.Diagnostics.DiagnosticSource` into core so `Activity` is available.)
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_creating_query_context_should_default_span_and_tracer_to_null.cs`
  - **Test should verify**:
    - A new `QueryContext` has `Span == null` and `Tracer == null`.
    - `Span` and `Tracer` are settable and round-trip the assigned values through the `IQueryContext` reference.
  - **Implementation files**:
    - `src/Paramore.Darker/IQueryContext.cs` - add `Activity? Span { get; set; }` and `IAmADarkerTracer? Tracer { get; set; }` (add `using System.Diagnostics;` and `using Paramore.Darker.Observability;`).
    - `src/Paramore.Darker/QueryContext.cs` - implement both auto-properties, defaulting null.
    - `src/Paramore.Darker/Observability/IAmADarkerTracer.cs` - add a minimal `public interface IAmADarkerTracer : IDisposable { }` placeholder so the property type compiles (fleshed out in a later task).
    - `Directory.Packages.props` - add `<PackageVersion Include="System.Diagnostics.DiagnosticSource" Version="10.0.9" />` (match the existing 10.0.x lines for Microsoft.Extensions.* / System.Text.Json).
    - `src/Paramore.Darker/Paramore.Darker.csproj` - add `<PackageReference Include="System.Diagnostics.DiagnosticSource" />`.
  - **References**: ADR 0017 §Key Components 5 (`IQueryContext` extension), §NFR4 dependency hygiene; requirements FR11, NFR1, NFR4. Read `src/Paramore.Darker/IQueryContext.cs`, `src/Paramore.Darker/QueryContext.cs`, `Directory.Packages.props`, `src/Paramore.Darker/Paramore.Darker.csproj`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_creating_query_context_should_default_span_and_tracer_to_null"`

- [ ] **Add `DbSystem` enum with OTel `db.system` string mapping**
  - **Behavior**: A `DbSystem` enum lists common OTel `db.system` values and maps each to its canonical `db.system` string, with an escape value for anything unlisted.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_mapping_db_system_should_return_otel_string.cs`
  - **Test should verify**:
    - `DbSystem.PostgreSql.ToDbSystemString() == "postgresql"`, `DbSystem.MsSql.ToDbSystemString() == "mssql"`, `DbSystem.MySql.ToDbSystemString() == "mysql"`, `DbSystem.Sqlite.ToDbSystemString() == "sqlite"`.
    - `DbSystem.Other.ToDbSystemString()` returns a non-null fallback (e.g. `"other_sql"`).
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/DbSystem.cs` - `public enum DbSystem` (MsSql, PostgreSql, MySql, Sqlite, Oracle, Db2, MongoDb, Redis, Cassandra, Other) plus a `public static class DbSystemExtensions { public static string ToDbSystemString(this DbSystem system) => ... }`.
  - **References**: ADR 0017 §Key Components 8 (DB-span support, `DbSystem`); requirements FR12, RD3. OTel db.system: https://opentelemetry.io/docs/specs/semconv/db/database-spans/.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_mapping_db_system_should_return_otel_string"`

- [ ] **Add `DbSpanInfo` record**
  - **Behavior**: A `DbSpanInfo` record carries the attributes needed to shape a DB span: required system/name/operation plus optional table, server address, statement, user, and a free attribute bag.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_creating_db_span_info_should_carry_supplied_values.cs`
  - **Test should verify**:
    - Constructing with `DbSystem.PostgreSql`, `dbName: "orders"`, `dbOperation: "select"`, `dbTable: "order"` exposes those values.
    - Optional members (`ServerAddress`, `DbStatement`, `DbUser`, `DbAttributes`) default to null/empty and are settable.
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/DbSpanInfo.cs` - `public record DbSpanInfo(DbSystem DbSystem, string DbName, string DbOperation, string? DbTable = null)` with additional `init` members `string? ServerAddress`, `string? DbStatement`, `string? DbUser`, and `IDictionary<string, string>? DbAttributes`.
  - **References**: ADR 0017 §Key Components 8 (`DbSpanInfo` mirrors Brighter `BoxSpanInfo`); requirements FR12, RD3.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_creating_db_span_info_should_carry_supplied_values"`

- [ ] **Add `DarkerTracer` / `IAmADarkerTracer` skeleton owning the `ActivitySource`**
  - **Behavior**: `DarkerTracer` owns one `ActivitySource` named `paramore.darker`; it is disposable and, because there is no listener in this test, `CreateQuerySpan` returns null (zero-overhead path).
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_creating_tracer_without_listener_should_expose_source_and_return_null_span.cs`
  - **Test should verify**:
    - `tracer.ActivitySource.Name == DarkerSemanticConventions.SourceName`.
    - With no `ActivityListener` registered, `tracer.CreateQuerySpan(new SomeQuery())` returns null.
    - `DarkerTracer` implements `IAmADarkerTracer : IDisposable` and disposes without throwing.
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/IAmADarkerTracer.cs` - expand to `ActivitySource ActivitySource { get; }`, `Activity? CreateQuerySpan<TResult>(IQuery<TResult> query, Activity? parentActivity = null, InstrumentationOptions options = InstrumentationOptions.All)`, `Activity? CreateDbSpan(DbSpanInfo info, Activity? parentActivity, InstrumentationOptions options = InstrumentationOptions.All)`, `void AddExceptionToSpan(Activity? span, Exception exception)`, `void EndSpan(Activity? span)`.
    - `src/Paramore.Darker/Observability/DarkerTracer.cs` - new `public sealed class DarkerTracer : IAmADarkerTracer`; ctor builds `new ActivitySource(DarkerSemanticConventions.SourceName, <assembly version>)` and accepts an optional `TimeProvider` (defaulting `TimeProvider.System`); `CreateQuerySpan` guards with `ActivitySource.HasListeners()` and returns null when no listener (fuller body added in later tasks); other methods stubbed to no-op-safe; `Dispose()` disposes the source.
  - **References**: ADR 0017 §Key Components 1 (`IAmADarkerTracer`/`DarkerTracer`), §Technology Choices (`TimeProvider`, `HasListeners` guard); requirements FR1, NFR2. Brighter model `Paramore.Brighter/Observability/BrighterTracer.cs`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_creating_tracer_without_listener_should_expose_source_and_return_null_span"`

- [ ] **`CreateQuerySpan` produces span with name/kind/parent**
  - **Behavior**: With a listener sampling AllData, `CreateQuerySpan` starts an `Internal` activity named `"<QueryType> query"`; when a parent activity is passed it nests under it, and when parent is null it uses ambient `Activity.Current`. The tracer sets `Activity.Current` to the new span.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_creating_query_span_should_set_name_kind_and_parent.cs`
  - **Test should verify**:
    - With an in-memory listener, the returned activity has `DisplayName == "<QueryTypeName> query"` and `Kind == ActivityKind.Internal`.
    - Passing an explicit parent activity makes the query span's `ParentId == parent.Id`.
    - After the call `Activity.Current` equals the returned span.
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/DarkerTracer.cs` - implement `CreateQuerySpan` core: `var activity = ActivitySource.StartActivity($"{query.GetType().Name} query", ActivityKind.Internal, parentActivity?.Id); if (activity != null) Activity.Current = activity; return activity;` (attributes added in following tasks).
  - **References**: ADR 0017 §Architecture Overview (span shape), §Key Components 1 (parenting); requirements FR2, FR3, AC1.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_creating_query_span_should_set_name_kind_and_parent"`

- [ ] **`CreateQuerySpan` records `QueryInformation` attributes**
  - **Behavior**: When `options` includes `QueryInformation` and `Activity.IsAllDataRequested` is true, the span carries `paramore.darker.queryid` (from a `Query<TResult>.Id`, else absent), `paramore.darker.querytype` (full type name), and `paramore.darker.operation` (`"query"`).
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_creating_query_span_with_query_information_should_tag_id_type_and_operation.cs`
  - **Test should verify**:
    - For a query deriving from `Query<TResult>`, the span tag `paramore.darker.queryid` equals the query's `Id`.
    - `paramore.darker.querytype` equals the query's `GetType().FullName` and `paramore.darker.operation == "query"`.
    - For a query implementing `IQuery<TResult>` directly (no base), the `queryid` tag is absent while `querytype`/`operation` are present.
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/DarkerTracer.cs` - after starting the activity, guard `if (activity?.IsAllDataRequested == true && options.HasFlag(InstrumentationOptions.QueryInformation))`; read id via closed-generic pattern `var id = query is Query<TResult> q ? q.Id : null;` and set the three tags via `DarkerSemanticConventions` keys (only set `QueryId` when `id != null`).
  - **References**: ADR 0017 §Key Components 1 (closed-generic id read), 4 (conventions); requirements FR6, FR6a, FR9, AC2. Read `src/Paramore.Darker/Observability/Query.cs`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_creating_query_span_with_query_information_should_tag_id_type_and_operation"`

- [ ] **`CreateQuerySpan` records `query_body` only when `QueryBody` set**
  - **Behavior**: When `options` includes `QueryBody`, the span carries `paramore.darker.query_body` = the query serialised as JSON using the runtime-type serializer; when the flag is absent, no body tag is emitted.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_creating_query_span_with_query_body_should_serialise_runtime_type.cs`
  - **Test should verify**:
    - With `QueryBody` set, `paramore.darker.query_body` is present and its JSON contains the concrete query's property values (not `"{}"`), confirming runtime-type serialisation.
    - With `QueryBody` NOT set (e.g. `QueryInformation` only), the body tag is absent.
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/DarkerTracer.cs` - add a guarded block `if (activity?.IsAllDataRequested == true && options.HasFlag(InstrumentationOptions.QueryBody))` that serialises via `JsonSerializer.Serialize(query, query.GetType(), <options>)`, reusing the `QueryLoggingJsonOptions.Options` approach (add the same `IL2026`/`IL3050` `UnconditionalSuppressMessage` guards used by `QueryLoggingDecorator`).
  - **References**: ADR 0017 §Technology Choices (existing serializer, runtime-type overload), ADR 0012; requirements FR7, FR9. Read `src/Paramore.Darker/Logging/Handlers/QueryLoggingDecorator.cs` and `.../Logging/QueryLoggingJsonOptions.cs`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_creating_query_span_with_query_body_should_serialise_runtime_type"`

- [ ] **`CreateQuerySpan` copies `spancontext.*` bag entries when `QueryContext` set**
  - **Behavior**: When `options` includes `QueryContext`, entries in a supplied `IQueryContext.Bag` whose key begins with `spancontext.` are copied onto the span as attributes; without the flag they are not.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_creating_query_span_with_query_context_should_copy_spancontext_bag_entries.cs`
  - **Test should verify**:
    - A bag containing `spancontext.tenant = "acme"` and a non-prefixed `other = "x"` yields a span with tag `spancontext.tenant == "acme"` and no `other` tag.
    - Without `QueryContext` in `options`, no `spancontext.*` tag appears.
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/IAmADarkerTracer.cs` + `DarkerTracer.cs` - extend `CreateQuerySpan` to also accept the `IQueryContext` (or its `Bag`) so bag copying can occur; add the guarded copy loop keyed on `DarkerSemanticConventions.SpanContextPrefix`. (Adjust the interface signature and the earlier skeleton accordingly; keep `parentActivity`/`options` optional. Final signature: `CreateQuerySpan<TResult>(IQuery<TResult> query, Activity? parentActivity, IQueryContext? context, InstrumentationOptions options)`.)
  - **References**: ADR 0017 §Key Components 3 (`QueryContext` flag), 6 (processor passes context); requirements FR8, FR9, AC3.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_creating_query_span_with_query_context_should_copy_spancontext_bag_entries"`

- [ ] **`AddExceptionToSpan` sets Error status, records the exception, tags `error.type`**
  - **Behavior**: `AddExceptionToSpan(span, ex)` sets `ActivityStatusCode.Error`, records the exception per the OTel exceptions convention, and adds an `error.type` tag equal to the exception's type name.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_recording_exception_on_span_should_set_error_status_and_error_type.cs`
  - **Test should verify**:
    - After the call, the span's `Status == ActivityStatusCode.Error`.
    - The span has an `exception` `ActivityEvent` recorded.
    - The span tag `error.type` equals `typeof(TException).Name` (or FullName — assert on what the impl sets consistently).
    - Passing a null span is a safe no-op.
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/DarkerTracer.cs` - implement `AddExceptionToSpan`: null-guard, `span.SetStatus(ActivityStatusCode.Error)`, `span.AddException(exception)` (or an `ActivityEvent`-based fallback for netstandard2.0, which lacks `Activity.AddException`), and `span.SetTag(DarkerSemanticConventions.ErrorType, exception.GetType().Name)`.
  - **References**: ADR 0017 §Key Components 1, 6 (`error.type` shared with metrics); requirements FR4, AC5, NFR5. OTel exceptions: https://opentelemetry.io/docs/specs/semconv/exceptions/exceptions-spans/.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_recording_exception_on_span_should_set_error_status_and_error_type"`

- [ ] **`EndSpan` sets Ok-if-unset, disposes, and restores prior `Activity.Current`**
  - **Behavior**: `EndSpan(span)` sets status Ok only if the span has no status yet, stops the activity, and restores the `Activity.Current` that was current before the span was started.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_ending_span_should_set_ok_and_restore_previous_current.cs`
  - **Test should verify**:
    - Capture `Activity.Current` (may be null), create a query span (which sets Current to it), then `EndSpan` restores `Activity.Current` to the captured value.
    - A span with no explicit status ends with `Status == ActivityStatusCode.Ok`.
    - A span already set to `Error` (via `AddExceptionToSpan`) is NOT overwritten to Ok by `EndSpan`.
    - Null span is a safe no-op.
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/DarkerTracer.cs` - track the previous `Activity.Current` when starting a span (e.g. capture in `CreateQuerySpan` and stash via a custom property so `EndSpan` can revert, mirroring `BrighterTracer`); `EndSpan` sets Ok if `span.Status == ActivityStatusCode.Unset`, calls `span.Stop()`/`Dispose()`, and reverts `Activity.Current`.
  - **References**: ADR 0017 §Key Components 1, §Risks (async `Activity.Current` leakage); requirements FR3, NFR3, AC6.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_ending_span_should_set_ok_and_restore_previous_current"`

- [ ] **`DarkerTracer.WriteQueryEvent` static step-event writer**
  - **Behavior**: The static `WriteQueryEvent(span, stepName, isAsync, options, isSink)` adds one `ActivityEvent` named after the step with tags `paramore.darker.handlername`, `paramore.darker.handlertype` (sync/async), and `paramore.darker.is_sink`; it is a no-op when the span is null or `QueryInformation` is not set.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_writing_query_event_should_add_event_with_handler_tags.cs`
  - **Test should verify**:
    - Calling with `stepName: "MyHandler", isAsync: true, isSink: true` adds exactly one `ActivityEvent` named `"MyHandler"` with `handlername == "MyHandler"`, `handlertype == "async"`, `is_sink == true`.
    - `isAsync: false` yields `handlertype == "sync"`; `isSink: false` yields `is_sink == false`.
    - Null span is a safe no-op (no throw).
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/DarkerTracer.cs` - add `public static void WriteQueryEvent(Activity? span, string stepName, bool isAsync, InstrumentationOptions options, bool isSink = false)` guarded by `span?.IsAllDataRequested == true && options.HasFlag(QueryInformation)`; build an `ActivityEvent` with `ActivityTagsCollection` using `DarkerSemanticConventions` keys and `span.AddEvent(...)`.
  - **References**: ADR 0017 §Key Components 1, 7 (events woven, static writer); requirements FR10, FR10a, AC4.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_writing_query_event_should_add_event_with_handler_tags"`

- [ ] **`CreateDbSpan` produces a Client DB span nested under the parent**
  - **Behavior**: `CreateDbSpan(info, parent, options)` starts a `Client` activity named `"<operation> <dbName> <dbTable>"` (or `"<operation> <dbName>"` when no table), parented to the supplied span, and — when `DatabaseInformation` is set — tags it with the `db.*` attributes from `DbSpanInfo`; returns null when there is no listener.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_creating_db_span_should_nest_under_parent_with_db_attributes.cs`
  - **Test should verify**:
    - With a listener and a parent query span, the DB span's `ParentId == parent.Id` and `Kind == ActivityKind.Client`.
    - `DisplayName == "select orders order"` for operation `select`, name `orders`, table `order`; and `"select orders"` when table is null.
    - With `DatabaseInformation` set, tags `db.system == "postgresql"`, `db.name == "orders"`, `db.operation == "select"` are present.
    - With no listener, returns null.
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/DarkerTracer.cs` - implement `CreateDbSpan` using `ActivitySource.StartActivity(name, ActivityKind.Client, parentActivity?.Id)`, then guarded `db.*` tagging via `DbSpanInfo`/`DbSystem.ToDbSystemString()` and `DarkerSemanticConventions`.
  - **References**: ADR 0017 §Architecture Overview (DB span shape), §Key Components 8; requirements FR12, RD3, AC7.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_creating_db_span_should_nest_under_parent_with_db_attributes"`

- [ ] **`QueryProcessor.Execute` owns the sync query-span lifecycle**
  - **Behavior**: `Execute` (sync) creates the query span parented to `queryContext.Span`, sets `queryContext.Span`/`Tracer`, ends the span in `finally`, records any exception once via `AddExceptionToSpan`, and — with no tracer/listener — behaves exactly as today.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_executing_sync_query_with_tracer_should_create_and_end_query_span.cs`
  - **Test should verify**:
    - With a real `DarkerTracer` + in-memory listener, executing a sync query produces one completed span named `"<QueryType> query"` whose id is surfaced on the handler's `Context.Span` during execution.
    - A throwing handler yields a span with `Status == Error` and a recorded exception, and the exception still propagates (unwrapped from `TargetInvocationException`).
    - After execution `Activity.Current` is restored to its pre-call value.
    - Constructing `QueryProcessor` WITHOUT a tracer leaves the existing suite behaviour unchanged (no span created; add a no-listener assertion).
  - **Implementation files**:
    - `src/Paramore.Darker/QueryProcessor.cs` - add optional ctor params `IAmADarkerTracer? tracer = null`, `InstrumentationOptions instrumentationOptions = InstrumentationOptions.All`; in `Execute`, after `InitQueryContext`, do `var span = tracer?.CreateQuerySpan(query, queryContext.Span, queryContext, instrumentationOptions); queryContext.Span = span; queryContext.Tracer = tracer;` then wrap invoke in `try/catch(...AddExceptionToSpan)/finally(EndSpan)`, recording the exception once inside the existing `TargetInvocationException`-unwrapping catch.
  - **References**: ADR 0017 §Key Components 6 (`QueryProcessor` changes); requirements FR2, FR3, FR4, FR11, AC1, AC5, AC6, NFR1. Read `src/Paramore.Darker/QueryProcessor.cs`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_executing_sync_query_with_tracer_should_create_and_end_query_span"`

- [ ] **`QueryProcessor.ExecuteAsync` owns the async query-span lifecycle**
  - **Behavior**: `ExecuteAsync` mirrors the sync span lifecycle with correct `Activity.Current` flow across `await`: span created parented to `queryContext.Span`, ended in `finally`, exception recorded once, and no span/overhead when no tracer/listener.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_executing_async_query_with_tracer_should_create_and_end_query_span.cs`
  - **Test should verify**:
    - With a `DarkerTracer` + listener, awaiting an async query produces one completed `"<QueryType> query"` span, surfaced on `Context.Span` during handler execution.
    - A throwing async handler yields `Status == Error` + recorded exception, exception propagates unwrapped.
    - After `await`, `Activity.Current` is restored (assert unchanged from before the call).
  - **Implementation files**:
    - `src/Paramore.Darker/QueryProcessor.cs` - apply the same span create/end/exception logic in `ExecuteAsync`, ending the span in `finally` after the `await`.
  - **References**: ADR 0017 §Key Components 6, §Risks (async `Activity.Current` leakage); requirements FR2, FR3, FR4, NFR3, AC5, AC6.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_executing_async_query_with_tracer_should_create_and_end_query_span"`

- [ ] **`PipelineBuilder.Build` weaves a step event per sync decorator and the handler (sink)**
  - **Behavior**: When the context carries a span+tracer, the sync `Func` chain writes one `WriteQueryEvent` per decorator step and one for the handler with `isSink: true`; with no span it is a pass-through with no behaviour change.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_building_sync_pipeline_with_span_should_write_event_per_step.cs`
  - **Test should verify**:
    - Executing a sync query with one decorator + handler yields two events on the span: the decorator (`is_sink == false`) and the handler (`is_sink == true`), each with `handlertype == "sync"`.
    - The handler event names match the handler type name; the decorator event names match the decorator type name.
    - With no span on the context, the pipeline runs identically and adds no events (no throw).
  - **Implementation files**:
    - `src/Paramore.Darker/PipelineBuilder.cs` - in `Build`, read `queryContext.Span`/`Tracer` (and the processor's options via the context/handler); wrap the innermost handler closure with `DarkerTracer.WriteQueryEvent(span, handlerType.Name, isAsync: false, options, isSink: true)` before invoking, and each decorator-wrapping closure with `WriteQueryEvent(span, decorator.GetType().Name, false, options)` before calling `next`. Exceptions are NOT recorded here (processor owns that).
  - **References**: ADR 0017 §Key Components 7 (`PipelineBuilder` changes), §Risks (double exception events, threading tracer via context); requirements FR10, FR10a, AC4. Read `src/Paramore.Darker/PipelineBuilder.cs`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_building_sync_pipeline_with_span_should_write_event_per_step"`

- [ ] **`PipelineBuilder.BuildAsync` weaves a step event per async decorator and the handler (sink)**
  - **Behavior**: The async `Func` chain writes one `WriteQueryEvent` per async decorator step and one for the handler with `isSink: true` and `handlertype == "async"`; with no span it is a pass-through.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_building_async_pipeline_with_span_should_write_event_per_step.cs`
  - **Test should verify**:
    - Awaiting an async query with one async decorator + handler yields two events: decorator (`is_sink == false`) and handler (`is_sink == true`), each with `handlertype == "async"`.
    - Event names match the async decorator and handler type names.
    - With no span, the async pipeline runs identically and adds no events.
  - **Implementation files**:
    - `src/Paramore.Darker/PipelineBuilder.cs` - apply the same weaving in `BuildAsync` with `isAsync: true`.
  - **References**: ADR 0017 §Key Components 7; requirements FR10, FR10a, AC4.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_building_async_pipeline_with_span_should_write_event_per_step"`

- [ ] **`[QueryDbSpan]` attribute + sync `QueryDbSpanDecorator` opens a child DB span**
  - **Behavior**: `[QueryDbSpan(step, DbSystem, dbName, dbTable, operation)]` on a sync handler's `Execute` weaves a `QueryDbSpanDecorator<TQuery,TResult>` that reads `Context.Tracer`+`Context.Span`, opens a child DB span via `CreateDbSpan`, invokes `next`, and ends the DB span; the processor still records any exception.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_executing_sync_handler_with_db_span_attribute_should_create_child_db_span.cs`
  - **Test should verify**:
    - A sync handler decorated with `[QueryDbSpan(1, DbSystem.PostgreSql, "orders", "order", "select")]` produces a `Client` DB span nested under the query span (`ParentId == querySpan.Id`) with `db.*` tags.
    - The DB span starts and ends within the handler invocation (assert it is stopped after execution).
    - With no listener/tracer, the handler runs unchanged (no DB span, no throw).
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/Attributes/QueryDbSpanAttribute.cs` - `public sealed class QueryDbSpanAttribute : QueryHandlerAttribute` with ctor `(int step, DbSystem system, string dbName, string dbTable, string operation)`; `GetAttributeParams()` returns those five values; `GetDecoratorType()` returns `typeof(QueryDbSpanDecorator<,>)`.
    - `src/Paramore.Darker/Observability/Handlers/QueryDbSpanDecorator.cs` - `public class QueryDbSpanDecorator<TQuery,TResult> : IQueryHandlerDecorator<TQuery,TResult> where TQuery : IQuery<TResult>`; `InitializeFromAttributeParams` reads the five params into a `DbSpanInfo`; `Execute` opens `Context.Tracer?.CreateDbSpan(info, Context.Span)`, calls `next`, ends via `Context.Tracer?.EndSpan(dbSpan)` in `finally` (no exception recording here).
  - **References**: ADR 0017 §Key Components 8 (DB decorator, `GetAttributeParams`/`InitializeFromAttributeParams`); requirements FR12a, RD3, AC7. Read `src/Paramore.Darker/Logging/Attributes/QueryLoggingAttribute.cs`, `src/Paramore.Darker/Logging/Handlers/QueryLoggingDecorator.cs`, `src/Paramore.Darker/QueryHandlerAttribute.cs`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_executing_sync_handler_with_db_span_attribute_should_create_child_db_span"`

- [ ] **`[QueryDbSpanAsync]` attribute + async `QueryDbSpanDecoratorAsync` opens a child DB span**
  - **Behavior**: `[QueryDbSpanAsync(step, DbSystem, dbName, dbTable, operation)]` on an async handler's `ExecuteAsync` weaves a `QueryDbSpanDecoratorAsync<TQuery,TResult>` that opens a child DB span around the awaited `next` and ends it.
  - **Test file**: `test/Paramore.Darker.Core.Tests/When_executing_async_handler_with_db_span_attribute_should_create_child_db_span.cs`
  - **Test should verify**:
    - An async handler decorated with `[QueryDbSpanAsync(1, DbSystem.MsSql, "orders", "order", "select")]` produces a `Client` DB span nested under the query span with `db.*` tags.
    - The DB span is stopped after the awaited handler completes.
    - With no listener/tracer the async handler runs unchanged.
  - **Implementation files**:
    - `src/Paramore.Darker/Observability/Attributes/QueryDbSpanAttributeAsync.cs` - `public sealed class QueryDbSpanAttributeAsync : QueryHandlerAttributeAsync` mirroring the sync attribute, `GetDecoratorType()` returns `typeof(QueryDbSpanDecoratorAsync<,>)`.
    - `src/Paramore.Darker/Observability/Handlers/QueryDbSpanDecoratorAsync.cs` - `public class QueryDbSpanDecoratorAsync<TQuery,TResult> : IQueryHandlerDecoratorAsync<TQuery,TResult>`; `ExecuteAsync` opens the DB span, `await next(...)`, ends it in `finally`.
  - **References**: ADR 0017 §Key Components 8; requirements FR12a, RD3, AC7. Read `src/Paramore.Darker/Logging/Attributes/QueryLoggingAttributeAsync.cs`, `src/Paramore.Darker/Logging/Handlers/QueryLoggingDecoratorAsync.cs`, `src/Paramore.Darker/QueryHandlerAttributeAsync.cs`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Core.Tests/ --filter "FullyQualifiedName~When_executing_async_handler_with_db_span_attribute_should_create_child_db_span"`

- [ ] **Create `Paramore.Darker.Extensions.Diagnostics` assembly + OTel CPM packages + solution/test wiring**
  - **Behavior**: A new SDK-wiring assembly and its test project exist, reference the OpenTelemetry SDK via CPM, and are registered in both solution files; nothing else changes behaviourally yet.
  - **Test file**: (none yet — project-creation task) build the new test project instead.
  - **Test should verify**:
    - N/A for this structural task; the new test project compiles and references the Diagnostics assembly + OpenTelemetry SDK test packages.
  - **Implementation files**:
    - `Directory.Packages.props` - add `PackageVersion` entries for `OpenTelemetry` and `OpenTelemetry.Extensions.Hosting` (and, for tests, `OpenTelemetry.Exporter.InMemory`) at a current stable version.
    - `src/Paramore.Darker.Extensions.Diagnostics/Paramore.Darker.Extensions.Diagnostics.csproj` - new SDK project (TFMs `netstandard2.0;net8.0;net9.0`), `ProjectReference` to `../Paramore.Darker/Paramore.Darker.csproj`, `PackageReference` to `OpenTelemetry`.
    - `test/Paramore.Darker.Extensions.Diagnostics.Tests/Paramore.Darker.Extensions.Diagnostics.Tests.csproj` - new xUnit v3 test project referencing the Diagnostics assembly, `Shouldly`, and `OpenTelemetry.Exporter.InMemory`.
    - `Darker.slnx` and `Darker.Filter.slnf` - add both new projects.
  - **References**: ADR 0017 §Key Components 9, §NFR4; requirements FR14, NFR4. Read `Darker.slnx`, `Darker.Filter.slnf`, `Directory.Packages.props`, `src/Paramore.Darker.Extensions.DependencyInjection/Paramore.Darker.Extensions.DependencyInjection.csproj`. Brighter model `Paramore.Brighter.Extensions.Diagnostics/BrighterTracerBuilderExtensions.cs`.
  - **RALPH-VERIFY**: `dotnet build test/Paramore.Darker.Extensions.Diagnostics.Tests/ -c Release`

- [ ] **`AddDarkerInstrumentation(this TracerProviderBuilder)` registers source + tracer**
  - **Behavior**: `AddDarkerInstrumentation()` on a `TracerProviderBuilder` constructs a `DarkerTracer`, `TryAddSingleton<IAmADarkerTracer>` it, and adds the `paramore.darker` source so Darker query spans are collected. (No metrics processor — deferred to ADR 0018.)
  - **Test file**: `test/Paramore.Darker.Extensions.Diagnostics.Tests/When_adding_darker_instrumentation_should_register_source_and_tracer.cs`
  - **Test should verify**:
    - Building a `TracerProvider` with `.AddDarkerInstrumentation()` and an in-memory exporter, then creating+ending a span on a `DarkerTracer` resolved from the service provider, results in the span being exported (source is subscribed).
    - The service collection has a singleton `IAmADarkerTracer` registered.
  - **Implementation files**:
    - `src/Paramore.Darker.Extensions.Diagnostics/DarkerTracerBuilderExtensions.cs` - `public static TracerProviderBuilder AddDarkerInstrumentation(this TracerProviderBuilder builder)` that builds a `DarkerTracer`, registers it via `builder.ConfigureServices(s => s.TryAddSingleton<IAmADarkerTracer>(tracer))`, and calls `builder.AddSource(tracer.ActivitySource.Name)`.
  - **References**: ADR 0017 §Key Components 9 (source + tracer ONLY; metrics processor belongs to 0018); requirements FR14, AC8. Brighter `BrighterTracerBuilderExtensions.cs`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Extensions.Diagnostics.Tests/ --filter "FullyQualifiedName~When_adding_darker_instrumentation_should_register_source_and_tracer"`

- [ ] **`AddDarker` threads the registered tracer + `InstrumentationOptions` into `QueryProcessor`**
  - **Behavior**: When an `IAmADarkerTracer` is registered and `DarkerOptions.InstrumentationOptions` is set, `AddDarker` passes both to the `QueryProcessor` ctor; when no tracer is registered, the processor is built without one (unchanged behaviour).
  - **Test file**: `test/Paramore.Darker.Extensions.Tests/When_adding_darker_with_registered_tracer_should_pass_tracer_to_processor.cs`
  - **Test should verify**:
    - With an `IAmADarkerTracer` registered in the container and a subscribed listener, resolving `IQueryProcessor` and executing a query produces a query span (proving the tracer was threaded in).
    - `DarkerOptions.InstrumentationOptions` defaults to `All` and is forwarded (e.g. setting it to `None` suppresses attribute groups).
    - With NO tracer registered, resolving and executing a query creates no span (existing behaviour preserved).
  - **Implementation files**:
    - `src/Paramore.Darker.Extensions.DependencyInjection/DarkerOptions.cs` - add `public InstrumentationOptions InstrumentationOptions { get; set; } = InstrumentationOptions.All;`.
    - `src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionExtensions.cs` - in `BuildQueryProcessor`, resolve `provider.GetService<IAmADarkerTracer>()` and pass it plus `options.InstrumentationOptions` to the `QueryProcessor` ctor.
  - **References**: ADR 0017 §Key Components 6, 9 (DI resolves tracer + options); requirements FR15, AC8, NFR1. Read `src/Paramore.Darker.Extensions.DependencyInjection/ServiceCollectionExtensions.cs`, `.../DarkerOptions.cs`.
  - **RALPH-VERIFY**: `dotnet test test/Paramore.Darker.Extensions.Tests/ --filter "FullyQualifiedName~When_adding_darker_with_registered_tracer_should_pass_tracer_to_processor"`
