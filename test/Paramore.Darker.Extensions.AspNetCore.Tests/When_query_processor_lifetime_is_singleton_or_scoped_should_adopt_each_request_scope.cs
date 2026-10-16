using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Extensions.AspNetCore.Tests.TestDoubles;
using Paramore.Darker.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.AspNetCore.Tests
{
    [Collection("DarkerHostBootstrap")]
    public class When_query_processor_lifetime_is_singleton_or_scoped_should_adopt_each_request_scope
    {
        [Theory]
        [InlineData(ServiceLifetime.Singleton)]
        [InlineData(ServiceLifetime.Scoped)]
        public void When_requests_run_in_succession_should_adopt_each_requests_own_scope(ServiceLifetime processorLifetime)
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddScoped<RequestScopedDependency>();
            services.AddDarker(options =>
                {
                    options.HandlerLifetime = ServiceLifetime.Scoped;
                    options.QueryProcessorLifetime = processorLifetime;
                })
                .AddHandlers(registry => registry.Register<RequestScopedSyncQuery, RequestScopedDependency, RequestScopedSyncQueryHandler>());
            services.AddDarkerRequestScope();
            var provider = services.BuildServiceProvider();
            var accessor = provider.GetRequiredService<IHttpContextAccessor>();

            // Act — two sequential requests, each with a scope of its own
            using var firstRequestScope = provider.CreateScope();
            accessor.HttpContext = new DefaultHttpContext { RequestServices = firstRequestScope.ServiceProvider };
            var firstHandlerDependency = QueryProcessorFor(processorLifetime, provider, firstRequestScope)
                .Execute(new RequestScopedSyncQuery());

            using var secondRequestScope = provider.CreateScope();
            accessor.HttpContext = new DefaultHttpContext { RequestServices = secondRequestScope.ServiceProvider };
            var secondHandlerDependency = QueryProcessorFor(processorLifetime, provider, secondRequestScope)
                .Execute(new RequestScopedSyncQuery());

            // Assert — each query resolved its own request's instance, and the two requests differ
            var firstRequestDependency = firstRequestScope.ServiceProvider.GetRequiredService<RequestScopedDependency>();
            var secondRequestDependency = secondRequestScope.ServiceProvider.GetRequiredService<RequestScopedDependency>();
            ReferenceEquals(firstHandlerDependency, firstRequestDependency).ShouldBeTrue();
            ReferenceEquals(secondHandlerDependency, secondRequestDependency).ShouldBeTrue();
            ReferenceEquals(firstHandlerDependency, secondHandlerDependency).ShouldBeFalse();
        }

        // A Scoped processor is resolved from the request scope; a Singleton one from the root.
        private static IQueryProcessor QueryProcessorFor(
            ServiceLifetime processorLifetime, ServiceProvider root, IServiceScope requestScope)
            => processorLifetime == ServiceLifetime.Scoped
                ? requestScope.ServiceProvider.GetRequiredService<IQueryProcessor>()
                : root.GetRequiredService<IQueryProcessor>();
    }
}
