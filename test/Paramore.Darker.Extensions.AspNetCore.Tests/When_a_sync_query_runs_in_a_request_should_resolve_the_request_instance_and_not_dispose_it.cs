using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    public class RequestScopeSharingTests
    {
        [Fact]
        public void When_a_sync_query_runs_in_a_request_should_resolve_the_request_instance_and_not_dispose_it()
        {
            // Arrange
            using var request = new RequestHost();

            // Act
            var handlerDependency = request.QueryProcessor.Execute(new RequestScopedSyncQuery());

            // Assert
            request.ShouldShareAndLeaveUndisposed(handlerDependency);
        }

        [Fact]
        public async Task When_an_async_query_runs_in_a_request_should_resolve_the_request_instance_and_not_dispose_it()
        {
            // Arrange
            using var request = new RequestHost();

            // Act
            var handlerDependency = await request.QueryProcessor.ExecuteAsync(new RequestScopedAsyncQuery(), cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            request.ShouldShareAndLeaveUndisposed(handlerDependency);
        }

        [Fact]
        public async Task When_a_stream_query_runs_in_a_request_should_resolve_the_request_instance_and_not_dispose_it()
        {
            // Arrange
            using var request = new RequestHost();

            // Act
            var yielded = new List<RequestScopedDependency>();
            await foreach (var item in request.QueryProcessor.ExecuteStream(new RequestScopedStreamQuery(), cancellationToken: TestContext.Current.CancellationToken))
                yielded.Add(item);

            // Assert
            yielded.Count.ShouldBe(1);
            request.ShouldShareAndLeaveUndisposed(yielded[0]);
        }

        /// <summary>A container with Scoped handlers and one in-flight "request" whose scope the test owns.</summary>
        private sealed class RequestHost : IDisposable
        {
            private readonly ServiceProvider _provider;
            private readonly IServiceScope _requestScope;

            public RequestHost()
            {
                var services = new ServiceCollection();
                services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
                services.AddScoped<RequestScopedDependency>();
                services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                    .AddHandlers(registry => registry.Register<RequestScopedSyncQuery, RequestScopedDependency, RequestScopedSyncQueryHandler>())
                    .AddAsyncHandlers(registry => registry.Register<RequestScopedAsyncQuery, RequestScopedDependency, RequestScopedAsyncQueryHandler>())
                    .AddStreamHandlers(registry => registry.Register<RequestScopedStreamQuery, RequestScopedDependency, RequestScopedStreamQueryHandler>());
                services.AddDarkerRequestScope();

                _provider = services.BuildServiceProvider();
                _requestScope = _provider.CreateScope();
                _provider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                    new DefaultHttpContext { RequestServices = _requestScope.ServiceProvider };

                QueryProcessor = _provider.GetRequiredService<IQueryProcessor>();
            }

            public IQueryProcessor QueryProcessor { get; }

            public void ShouldShareAndLeaveUndisposed(RequestScopedDependency handlerDependency)
            {
                var requestDependency = _requestScope.ServiceProvider.GetRequiredService<RequestScopedDependency>();

                // the handler got the request's own instance, and Darker did not dispose it
                ReferenceEquals(handlerDependency, requestDependency).ShouldBeTrue();
                requestDependency.IsDisposed.ShouldBeFalse();

                // only the owner of the request scope disposes it
                _requestScope.Dispose();
                requestDependency.IsDisposed.ShouldBeTrue();
            }

            public void Dispose() => _requestScope.Dispose();
        }
    }
}
