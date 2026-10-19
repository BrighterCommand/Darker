using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Extensions.AspNetCore.Tests.TestDoubles;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.Logging;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.AspNetCore.Tests
{
    [Collection("DarkerHostBootstrap")]
    public class NonScopedHandlerLifetimeWarningTests
    {
        [Theory]
        [InlineData(ServiceLifetime.Transient)]
        [InlineData(ServiceLifetime.Singleton)]
        public void When_queries_run_inside_a_request_should_not_adopt_the_request_scope_and_log_one_warning(ServiceLifetime handlerLifetime)
        {
            // Arrange — a non-Scoped handler lifetime, the request scope registered, and a request in flight
            var logs = new CapturingLoggerProvider();
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddProvider(logs));
            services.AddScoped<RequestScopedDependency>();
            services.AddDarker(options => options.HandlerLifetime = handlerLifetime)
                .AddHandlers(registry => registry.Register<RequestScopedSyncQuery, RequestScopedDependency, RequestScopedSyncQueryHandler>());
            services.AddDarkerRequestScope();

            // Not disposed: the container's LoggerFactory is now ApplicationLogging.LoggerFactory, and disposing it
            // would break the static loggers of tests that run after this one.
            var provider = services.BuildServiceProvider();

            using var requestScope = provider.CreateScope();
            provider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { RequestServices = requestScope.ServiceProvider };
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act
            var dependencies = Enumerable.Range(0, 3)
                .Select(_ => queryProcessor.Execute(new RequestScopedSyncQuery()))
                .ToList();

            // Assert — no handler got the request's instance
            var requestDependency = requestScope.ServiceProvider.GetRequiredService<RequestScopedDependency>();
            foreach (var dependency in dependencies)
                ReferenceEquals(dependency, requestDependency).ShouldBeFalse();
            requestDependency.IsDisposed.ShouldBeFalse();

            // Assert — one Warning across all the queries, naming the Scoped-lifetime reason
            var warnings = logs.Snapshot().Where(e => e.LogLevel == LogLevel.Warning).ToList();
            warnings.Count.ShouldBe(1);
            warnings[0].RenderedMessage.ShouldContain("Scoped handler lifetime");
        }
    }
}
