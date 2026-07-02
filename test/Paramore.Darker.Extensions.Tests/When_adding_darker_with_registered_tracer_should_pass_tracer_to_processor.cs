using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Core.Tests.Exported;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    [Collection("DarkerActivitySource")]
    public class When_adding_darker_with_registered_tracer_should_pass_tracer_to_processor
    {
        private static ActivityListener CreateListener(List<Activity> completed)
        {
            var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == DarkerSemanticConventions.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
                ActivityStopped = a => completed.Add(a),
            };
            ActivitySource.AddActivityListener(listener);
            return listener;
        }

        [Fact]
        public void When_tracer_registered_and_listener_subscribed_should_produce_query_span()
        {
            // Arrange
            var completed = new List<Activity>();
            using var tracer = new DarkerTracer();
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton<IAmADarkerTracer>(tracer);
            services.AddDarker(options => options.InstrumentationOptions = InstrumentationOptions.QueryInformation)
                    .AddHandlersFromAssemblies(typeof(TestQueryHandler).Assembly);
            using var provider = services.BuildServiceProvider();
            using var listener = CreateListener(completed);

            var processor = provider.GetRequiredService<IQueryProcessor>();

            // Act
            processor.Execute(new TestQueryA(Guid.NewGuid()));

            // Assert
            completed.Count.ShouldBe(1);
            completed[0].DisplayName.ShouldBe("TestQueryA query");
            completed[0].Status.ShouldBe(ActivityStatusCode.Ok);
        }

        [Fact]
        public void When_darker_options_created_should_default_instrumentation_options_to_All()
        {
            // Arrange / Act
            var options = new DarkerOptions();

            // Assert
            options.InstrumentationOptions.ShouldBe(InstrumentationOptions.All);
        }

        [Fact]
        public void When_no_tracer_registered_and_listener_subscribed_should_produce_no_span()
        {
            // Arrange
            var completed = new List<Activity>();
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker()
                    .AddHandlersFromAssemblies(typeof(TestQueryHandler).Assembly);
            using var provider = services.BuildServiceProvider();
            using var listener = CreateListener(completed);

            var processor = provider.GetRequiredService<IQueryProcessor>();

            // Act
            processor.Execute(new TestQueryA(Guid.NewGuid()));

            // Assert
            completed.ShouldBeEmpty();
        }
    }
}
