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
    public class When_request_scope_registered_before_or_after_AddDarker_should_adopt_either_way
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void When_a_query_runs_inside_a_request_should_join_the_request_scope_whatever_the_call_order(bool registerRequestScopeFirst)
        {
            // Arrange — Scoped handlers, the request scope registered before or after AddDarker, and a request in flight
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddScoped<RequestScopedDependency>();

            if (registerRequestScopeFirst)
                services.AddDarkerRequestScope();

            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddHandlers(registry => registry.Register<RequestScopedSyncQuery, RequestScopedDependency, RequestScopedSyncQueryHandler>());

            if (!registerRequestScopeFirst)
                services.AddDarkerRequestScope();

            var provider = services.BuildServiceProvider();

            using var requestScope = provider.CreateScope();
            provider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { RequestServices = requestScope.ServiceProvider };
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act
            var handlerDependency = queryProcessor.Execute(new RequestScopedSyncQuery());

            // Assert — the handler got the request's instance, and Darker did not dispose it
            var requestDependency = requestScope.ServiceProvider.GetRequiredService<RequestScopedDependency>();
            ReferenceEquals(handlerDependency, requestDependency).ShouldBeTrue();
            requestDependency.IsDisposed.ShouldBeFalse();
        }
    }
}
