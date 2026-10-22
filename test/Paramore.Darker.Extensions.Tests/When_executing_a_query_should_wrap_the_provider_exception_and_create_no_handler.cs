using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    public class ScopeProviderFaultTests
    {
        [Fact]
        public void When_executing_a_query_should_wrap_the_provider_exception_and_create_no_handler()
        {
            // Arrange
            var provider = Build();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act
            var exception = Should.Throw<AmbientScopeSourceException>(() => queryProcessor.Execute(new TrackedQuery()));

            // Assert
            AssertWrapsProviderException(provider, exception);
        }

        [Fact]
        public async Task When_executing_an_async_query_should_wrap_the_provider_exception_and_create_no_handler()
        {
            // Arrange
            var provider = Build();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act
            var exception = await Should.ThrowAsync<AmbientScopeSourceException>(
                async () => await queryProcessor.ExecuteAsync(new TrackedQuery(), cancellationToken: TestContext.Current.CancellationToken));

            // Assert
            AssertWrapsProviderException(provider, exception);
        }

        [Fact]
        public async Task When_executing_a_stream_query_should_wrap_the_provider_exception_at_the_first_move_next_and_create_no_handler()
        {
            // Arrange
            var provider = Build();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var query = new TrackedStreamQuery();
            query.Release();

            // Act — the stream is lazy, so the pipeline is built, and fails, at the first MoveNextAsync
            await using var enumerator = queryProcessor.ExecuteStream(query, cancellationToken: TestContext.Current.CancellationToken).GetAsyncEnumerator(TestContext.Current.CancellationToken);
            var exception = await Should.ThrowAsync<AmbientScopeSourceException>(async () => await enumerator.MoveNextAsync());

            // Assert
            AssertWrapsProviderException(provider, exception);
        }

        private static void AssertWrapsProviderException(ServiceProvider provider, AmbientScopeSourceException exception)
        {
            var scopeProvider = (ThrowingScopeProvider)provider.GetRequiredService<IAmAScopeProvider>();

            exception.InnerException.ShouldBeSameAs(scopeProvider.Thrown);
            exception.Message.ShouldContain(nameof(ThrowingScopeProvider));
            provider.GetRequiredService<DependencyTracker>().ConstructionCount.ShouldBe(0);
        }

        private static ServiceProvider Build()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton<DependencyTracker>();
            services.AddScoped<ITrackedDependency, TrackedDependency>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddHandlers(registry => registry.Register<TrackedQuery, TrackedQuery.Result, TrackedQueryHandler>())
                .AddAsyncHandlers(registry => registry.Register<TrackedQuery, TrackedQuery.Result, TrackedQueryHandlerAsync>())
                .AddStreamHandlers(registry => registry.Register<TrackedStreamQuery, string, TrackedStreamQueryHandler>())
                .RegisterDecorator(typeof(TrackedStreamDecorator<,>));
            services.AddDarkerScopeProvider<ThrowingScopeProvider>();

            return services.BuildServiceProvider();
        }
    }
}
