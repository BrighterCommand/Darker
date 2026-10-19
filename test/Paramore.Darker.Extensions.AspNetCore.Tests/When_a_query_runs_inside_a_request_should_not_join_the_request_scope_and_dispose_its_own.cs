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
    public class AlwaysNewRequestScopeTests
    {
        [Fact]
        public void When_a_query_runs_inside_a_request_should_not_join_the_request_scope_and_dispose_its_own()
        {
            // Arrange — Scoped handlers, the request scope registered with AlwaysNew, and a request in flight
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddScoped<RequestScopedDependency>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddHandlers(registry => registry.Register<RequestScopedSyncQuery, RequestScopedDependency, RequestScopedSyncQueryHandler>());
            services.AddDarkerRequestScope(ScopeAffinity.AlwaysNew);
            var provider = services.BuildServiceProvider();

            using var requestScope = provider.CreateScope();
            provider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { RequestServices = requestScope.ServiceProvider };
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act
            var handlerDependency = queryProcessor.Execute(new RequestScopedSyncQuery());

            // Assert — the handler did not get the request's instance, and Darker disposed its own
            var requestDependency = requestScope.ServiceProvider.GetRequiredService<RequestScopedDependency>();
            ReferenceEquals(handlerDependency, requestDependency).ShouldBeFalse();
            handlerDependency.IsDisposed.ShouldBeTrue();
            requestDependency.IsDisposed.ShouldBeFalse();
        }
    }
}
