using System.Linq;
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
    public class OwnedScopeOutsideRequestTests
    {
        [Fact]
        public void When_there_is_no_http_context_should_use_an_owned_scope_each_query_and_log_no_warning()
        {
            // Arrange — Scoped handlers and the request scope registered, but no request is in flight
            var logs = new CapturingLoggerProvider();
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddProvider(logs));
            services.AddScoped<RequestScopedDependency>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddHandlers(registry => registry.Register<RequestScopedSyncQuery, RequestScopedDependency, RequestScopedSyncQueryHandler>());
            services.AddDarkerRequestScope();

            // Not disposed: the container's LoggerFactory is now ApplicationLogging.LoggerFactory, and disposing it
            // would break the static loggers of tests that run after this one.
            var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act — IHttpContextAccessor.HttpContext is null throughout
            var dependencies = Enumerable.Range(0, 3)
                .Select(_ => queryProcessor.Execute(new RequestScopedSyncQuery()))
                .ToList();

            // Assert — each query had a scope of its own and disposed it
            dependencies.Distinct().Count().ShouldBe(3);
            foreach (var dependency in dependencies)
                dependency.IsDisposed.ShouldBeTrue();

            // Assert — no request is the normal answer outside a request, so nothing is logged
            var warnings = logs.Snapshot().Where(e => e.LogLevel == LogLevel.Warning).ToList();
            warnings.ShouldBeEmpty();
        }
    }
}
