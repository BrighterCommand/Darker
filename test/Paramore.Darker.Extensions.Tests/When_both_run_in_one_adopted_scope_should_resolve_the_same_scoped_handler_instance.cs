using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    [Collection("DarkerHostBootstrap")]
    public class TwoQueriesOfOneHandlerTypeRunInOneAmbientScopeTests
    {
        [Fact]
        public void When_both_run_in_one_adopted_scope_should_resolve_the_same_scoped_handler_instance()
        {
            // Arrange — this pins a known contract, not an accident: a Scoped handler is one instance per
            // scope, so queries that share an adopted ambient scope share the handler, as every other
            // consumer of that scope shares its Scoped services (ADR 0024). Only decorators are per query.
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddScoped<ITrackedDependency, TrackedDependency>();
            services.AddSingleton<DependencyTracker>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddHandlers(registry => registry.Register<FirstSharedResultQuery, SharedRecordingResult, FirstSharedResultQueryHandler>())
                .RegisterDecorator(typeof(InstanceRecordingDecorator<,>));
            services.AddDarkerScopeProvider<AsyncLocalScopeProvider>();

            using var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var ambient = new RecordingAmbientScope(provider.CreateScope());
            using var _ = AsyncLocalScopeProvider.Use(ambient);

            // Act
            var first = queryProcessor.Execute(new FirstSharedResultQuery());
            var second = queryProcessor.Execute(new FirstSharedResultQuery());

            // Assert
            first.HandlerId.ShouldBe(second.HandlerId);
        }
    }
}
