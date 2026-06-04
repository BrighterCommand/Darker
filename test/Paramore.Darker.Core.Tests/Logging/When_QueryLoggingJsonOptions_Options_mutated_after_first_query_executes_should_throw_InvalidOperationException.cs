using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Paramore.Darker.Builder;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Logging;
using Paramore.Darker.Logging.Handlers;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Logging
{
    /// <summary>
    /// FR14 / AC3 — <c>QueryLoggingJsonOptions.Options</c> is freely mutable until the first query is
    /// logged, after which System.Text.Json marks the instance read-only and any further mutation throws
    /// <see cref="InvalidOperationException"/>. A single sequential <c>[Fact]</c> walks the four ordered
    /// steps. The irreversible lock is isolated on a throwaway options instance (C5) so it can never leak
    /// into another test through the shared default — which is also why the fragile "run last" ordering
    /// hazard (review finding #14) does not apply here.
    /// </summary>
    [Collection("QueryLoggingJsonOptionsOrdering")]
    public class When_QueryLoggingJsonOptions_Options_mutated_after_first_query_executes_should_throw_InvalidOperationException
    {
        private readonly LoggerCaptureFixture _logs;

        public When_QueryLoggingJsonOptions_Options_mutated_after_first_query_executes_should_throw_InvalidOperationException(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public void Mutating_options_after_first_query_executes_should_throw_InvalidOperationException()
        {
            var original = QueryLoggingJsonOptions.Options;
            _logs.Clear();
            try
            {
                // Throwaway instance: the lock the first query triggers below lands here, never on the
                // shared default, so it cannot leak across tests (C5).
                QueryLoggingJsonOptions.Options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };

                // Step 1 — before any query is logged the options instance is mutable.
                Should.NotThrow(() => QueryLoggingJsonOptions.Options.MaxDepth = 32);

                // Step 2 — the AddJsonQueryLogging callback mutates the (still unlocked) options and
                // registers the logging decorators on the builder.
                var builder = (QueryProcessorBuilder)QueryProcessorBuilder.With()
                    .Handlers(
                        BuildHandlerRegistry(),
                        new SimpleHandlerFactory(_ => new OrderingTestQueryHandler()),
                        new InMemoryDecoratorRegistry(),
                        new SimpleHandlerDecoratorFactory(
                            _ => new QueryLoggingDecorator<IQuery<OrderingTestQuery.Result>, OrderingTestQuery.Result>()))
                    .InMemoryQueryContextFactory();

                Should.NotThrow(() => builder.AddJsonQueryLogging(o => o.WriteIndented = true));

                var queryProcessor = builder.Build();

                // Step 3 — executing the first query serialises with the configured options, which locks
                // the instance. The captured {Query} proves WriteIndented took effect.
                queryProcessor.Execute(new OrderingTestQuery());

                _logs.CapturedLogs.ShouldNotBeEmpty();

                var start = _logs.CapturedLogs.Single(e => e.MessageTemplate == "Executing query {QueryName}: {Query}");
                var capturedQuery = (string)start.StructuredArguments.Single(kvp => kvp.Key == "Query").Value;
                capturedQuery.Replace("\r\n", "\n").ShouldBe("{\n  \"Marker\": \"x\"\n}");

                // Step 4 — after the lock, the same configure path now throws, surfaced unmodified.
                Should.Throw<InvalidOperationException>(() => builder.AddJsonQueryLogging(o => o.WriteIndented = false));
            }
            finally
            {
                // The lock itself is irreversible (it lived on the throwaway instance); restoring the
                // reference hands the shared, still-unlocked default back to the rest of the suite.
                QueryLoggingJsonOptions.Options = original;
            }
        }

        private static QueryHandlerRegistry BuildHandlerRegistry()
        {
            var registry = new QueryHandlerRegistry();
            registry.Register<OrderingTestQuery, OrderingTestQuery.Result, OrderingTestQueryHandler>();
            return registry;
        }
    }
}
