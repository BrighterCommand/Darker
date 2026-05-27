using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests.Integrations
{
    public class When_AddJsonQueryLogging_called_should_register_serializer_settings
    {
        [Fact]
        public void AddJsonQueryLogging_should_register_JsonSerializerSettings_in_DI()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker()
                .AddHandlers(r => r.Register<SyncTestQuery, SyncTestQuery.Result, LoggingQueryHandler>())
                .AddJsonQueryLogging();
            services.AddTransient<LoggingQueryHandler>();

            var provider = services.BuildServiceProvider();

            // Act — resolve the settings that AddJsonQueryLogging should have registered
            var settings = provider.GetService<JsonSerializerSettings>();

            // Assert — settings are registered so logging decorator can be injected
            settings.ShouldNotBeNull();
        }

        [Fact]
        public void AddJsonQueryLogging_should_allow_logging_decorated_query_to_execute()
        {
            // Arrange
            var id = Guid.NewGuid();
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker()
                .AddHandlers(r => r.Register<SyncTestQuery, SyncTestQuery.Result, LoggingQueryHandler>())
                .AddJsonQueryLogging();
            services.AddTransient<LoggingQueryHandler>();

            var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act — handler with [QueryLogging] requires JsonSerializerSettings to be injected into decorator
            var result = queryProcessor.Execute(new SyncTestQuery(id));

            // Assert
            result.ShouldNotBeNull();
            result.Value.ShouldBe(id);
        }
    }
}
