using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.Logging;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Logging;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.Logging
{
    [Collection("DarkerHostBootstrap")]
    public class When_AddJsonQueryLogging_called_should_configure_json_options
    {
        private readonly LoggerCaptureFixture _logs;

        public When_AddJsonQueryLogging_called_should_configure_json_options(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public void Callback_mutates_QueryLoggingJsonOptions_Options()
        {
            // Arrange — throwaway options so the callback's mutation does not leak (C5)
            var original = QueryLoggingJsonOptions.Options;
            try
            {
                QueryLoggingJsonOptions.Options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };

                var services = new ServiceCollection();

                // Act — the callback configures QueryLoggingJsonOptions.Options in place
                services.AddDarker()
                    .AddHandlers(r => r.Register<ExtensionsLoggingTestQuery, ExtensionsLoggingTestQuery.Result, ExtensionsLoggingTestQueryHandler>())
                    .AddJsonQueryLogging(o => o.WriteIndented = true);

                // Assert
                QueryLoggingJsonOptions.Options.WriteIndented.ShouldBeTrue();
            }
            finally
            {
                QueryLoggingJsonOptions.Options = original;
            }
        }

        [Fact]
        public void Decorated_query_logs_System_Text_Json_body()
        {
            // Arrange — throwaway options absorb the serialize-lock (C5)
            var original = QueryLoggingJsonOptions.Options;
            _logs.Clear();
            try
            {
                QueryLoggingJsonOptions.Options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    WriteIndented = false
                };

                var query = new ExtensionsLoggingTestQuery { Id = Guid.NewGuid(), Name = "Linus" };

                // Route the container's logging through the fixture's capturing provider, because
                // AddDarker resets ApplicationLogging.LoggerFactory from the container's ILoggerFactory.
                var services = new ServiceCollection();
                services.AddLogging(b => b.AddProvider(_logs.Provider));
                services.AddDarker()
                    .AddHandlers(r => r.Register<ExtensionsLoggingTestQuery, ExtensionsLoggingTestQuery.Result, ExtensionsLoggingTestQueryHandler>())
                    .AddJsonQueryLogging();
                services.AddTransient<ExtensionsLoggingTestQueryHandler>();

                var queryProcessor = services.BuildServiceProvider().GetRequiredService<IQueryProcessor>();

                // Act
                queryProcessor.Execute(query);

                // Assert — the {Query} log argument is the System.Text.Json output (not Newtonsoft)
                _logs.CapturedLogs.ShouldNotBeEmpty();
                var expectedJson = JsonSerializer.Serialize(query, QueryLoggingJsonOptions.Options);
                var start = _logs.CapturedLogs.Single(e => e.MessageTemplate == "Executing query {QueryName}: {Query}");
                start.StructuredArguments.Single(kvp => kvp.Key == "Query").Value.ShouldBe(expectedJson);
            }
            finally
            {
                QueryLoggingJsonOptions.Options = original;
            }
        }
    }
}
