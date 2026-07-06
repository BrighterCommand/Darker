using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Paramore.Darker;
using Paramore.Darker.Observability;
using Paramore.Darker.Observability.Attributes;
using Paramore.Darker.Observability.Handlers;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Diagnostics.Tests;

/// <summary>
/// End-to-end verification (ADR 0018 §Implementation Approach) that executing a query through a
/// real <see cref="QueryProcessor"/> with both the tracer and meter builders wired records a
/// <c>paramore.darker.query.duration</c> point and, for a handler carrying <c>[QueryDbSpan]</c>,
/// a <c>db.client.operation.duration</c> point — while high-cardinality span tags never become
/// metric dimensions, a failing query surfaces <c>error.type</c>, and wiring the tracer alone
/// records nothing (NFR2/AC8).
/// </summary>
[Collection("DarkerMeter")]
public class EndToEndMetricsFromQueryExecutionTests
{
    private sealed class DbQuery : IQuery<DbResult>;

    private sealed class DbResult
    {
        public int Value { get; set; }
    }

    private sealed class DbQueryHandler : QueryHandler<DbQuery, DbResult>
    {
        [QueryDbSpan(step: 1, DbSystem.PostgreSql, "orders", "order", "select")]
        public override DbResult Execute(DbQuery query) => new DbResult { Value = 42 };
    }

    private sealed class ThrowingQuery : IQuery<DbResult>;

    private sealed class ThrowingQueryHandler : QueryHandler<ThrowingQuery, DbResult>
    {
        public override DbResult Execute(ThrowingQuery query)
            => throw new InvalidOperationException("boom");
    }

    /// <summary>
    /// Minimal <see cref="IMeterFactory"/> that creates bare <see cref="Meter"/> instances; the
    /// SDK's <see cref="MeterProvider"/> picks them up via its MeterListener once the histogram is
    /// published. In production the Generic Host supplies this.
    /// </summary>
    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = new();

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options.Name, options.Version);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (var meter in _meters)
                meter.Dispose();
        }
    }

    // WithMetrics is wired BEFORE WithTracing so the meter registrations are visible when the
    // tracer builder's AddDarkerInstrumentation checks for them (the ConfigureServices callback
    // runs immediately at call time, not lazily at provider-build time).
    private static ServiceProvider BuildStackWithMetrics(List<Metric> metrics)
    {
        var services = new ServiceCollection();
        services.TryAddSingleton<IMeterFactory, TestMeterFactory>();
        services.AddOpenTelemetry()
            .WithMetrics(b => b.AddDarkerInstrumentation().AddInMemoryExporter(metrics))
            .WithTracing(b => b.AddDarkerInstrumentation());
        return services.BuildServiceProvider();
    }

    private static QueryProcessor CreateDbQueryProcessor(IAmADarkerTracer tracer)
    {
        var registry = new QueryHandlerRegistry();
        registry.Register<DbQuery, DbResult, DbQueryHandler>();
        return CreateProcessor(tracer, registry, _ => new DbQueryHandler());
    }

    private static QueryProcessor CreateThrowingQueryProcessor(IAmADarkerTracer tracer)
    {
        var registry = new QueryHandlerRegistry();
        registry.Register<ThrowingQuery, DbResult, ThrowingQueryHandler>();
        return CreateProcessor(tracer, registry, _ => new ThrowingQueryHandler());
    }

    private static QueryProcessor CreateProcessor(
        IAmADarkerTracer tracer, QueryHandlerRegistry registry, Func<Type, IQueryHandler> handlerFactory)
    {
        var decoratorFactory = new SimpleHandlerDecoratorFactory(_ =>
            new QueryDbSpanDecorator<IQuery<DbResult>, DbResult>());
        var decoratorRegistry = new InMemoryDecoratorRegistry();

        var config = new HandlerConfiguration(
            registry, new SimpleHandlerFactory(handlerFactory), decoratorRegistry, decoratorFactory);
        return new QueryProcessor(
            config,
            new InMemoryQueryContextFactory(),
            tracer: tracer,
            instrumentationOptions: InstrumentationOptions.QueryInformation | InstrumentationOptions.DatabaseInformation);
    }

    private static IReadOnlyList<string> TagKeys(Metric metric)
    {
        var keys = new List<string>();
        foreach (var point in metric.GetMetricPoints())
            foreach (var tag in point.Tags)
                keys.Add(tag.Key);
        return keys;
    }

    [Fact]
    public void When_executing_query_with_metrics_wired_should_record_query_and_db_duration_metrics()
    {
        //Arrange
        var metrics = new List<Metric>();
        using var sp = BuildStackWithMetrics(metrics);
        var tracerProvider = sp.GetRequiredService<TracerProvider>();
        var meterProvider = sp.GetRequiredService<MeterProvider>();
        var tracer = sp.GetRequiredService<IAmADarkerTracer>();
        var processor = CreateDbQueryProcessor(tracer);

        //Act
        processor.Execute(new DbQuery());
        tracerProvider.ForceFlush();
        meterProvider.ForceFlush();

        //Assert — the query duration metric was recorded with the allowed query dimensions
        var queryMetric = metrics.Single(m => m.Name == DarkerSemanticConventions.QueryDurationMetricName);
        var queryTags = TagKeys(queryMetric);
        queryTags.ShouldContain(DarkerSemanticConventions.QueryType);
        queryTags.ShouldContain(DarkerSemanticConventions.Operation);

        // the DB client operation metric was recorded with the allowed db.* dimensions
        var dbMetric = metrics.Single(m => m.Name == DarkerSemanticConventions.DbClientOperationDurationMetricName);
        var dbTags = TagKeys(dbMetric);
        dbTags.ShouldContain(DarkerSemanticConventions.DbSystem);
        dbTags.ShouldContain(DarkerSemanticConventions.DbName);
        dbTags.ShouldContain(DarkerSemanticConventions.DbOperation);
        dbTags.ShouldContain(DarkerSemanticConventions.DbSqlTable);

        // high-cardinality span tags never leak into metric dimensions
        var allTags = metrics.SelectMany(TagKeys).ToList();
        allTags.ShouldNotContain(DarkerSemanticConventions.QueryBody);
        allTags.ShouldNotContain(k => k.StartsWith("spancontext", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void When_executing_failing_query_with_metrics_wired_should_record_error_type_dimension()
    {
        //Arrange
        var metrics = new List<Metric>();
        using var sp = BuildStackWithMetrics(metrics);
        var tracerProvider = sp.GetRequiredService<TracerProvider>();
        var meterProvider = sp.GetRequiredService<MeterProvider>();
        var tracer = sp.GetRequiredService<IAmADarkerTracer>();
        var processor = CreateThrowingQueryProcessor(tracer);

        //Act
        Should.Throw<InvalidOperationException>(() => processor.Execute(new ThrowingQuery()));
        tracerProvider.ForceFlush();
        meterProvider.ForceFlush();

        //Assert — the query duration metric surfaces the error.type dimension
        var queryMetric = metrics.Single(m => m.Name == DarkerSemanticConventions.QueryDurationMetricName);
        TagKeys(queryMetric).ShouldContain(DarkerSemanticConventions.ErrorType);
    }

    [Fact]
    public void When_executing_query_without_meter_builder_should_record_no_metrics()
    {
        //Arrange — only the tracer builder is wired (no meter builder)
        var services = new ServiceCollection();
        services.AddOpenTelemetry()
            .WithTracing(b => b.AddDarkerInstrumentation());
        using var sp = services.BuildServiceProvider();

        var tracerProvider = sp.GetRequiredService<TracerProvider>();
        var tracer = sp.GetRequiredService<IAmADarkerTracer>();
        var processor = CreateDbQueryProcessor(tracer);

        // a standalone MeterProvider observes whether any Darker metric is emitted
        var metrics = new List<Metric>();
        using var standaloneMeterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(DarkerSemanticConventions.MeterName)
            .AddInMemoryExporter(metrics)
            .Build()!;

        //Act
        processor.Execute(new DbQuery());
        tracerProvider.ForceFlush();
        standaloneMeterProvider.ForceFlush();

        //Assert — no metrics recorded (NFR2/AC8)
        metrics.ShouldBeEmpty();
    }
}
