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
    public class StreamEnumeratedAfterRequestEndsTests
    {
        [Fact]
        public async Task When_the_request_ends_before_the_first_move_next_should_enumerate_in_an_owned_scope()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddScoped<RequestScopedDependency>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddHandlers(registry => registry.Register<RequestScopedSyncQuery, RequestScopedDependency, RequestScopedSyncQueryHandler>())
                .AddStreamHandlers(registry => registry.Register<RequestScopedStreamQuery, RequestScopedDependency, RequestScopedStreamQueryHandler>());
            services.AddDarkerRequestScope();

            using var provider = services.BuildServiceProvider();
            var requestScope = provider.CreateScope();
            var accessor = provider.GetRequiredService<IHttpContextAccessor>();
            accessor.HttpContext = new DefaultHttpContext { RequestServices = requestScope.ServiceProvider };
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // a query inside the request first, so the provider has served this request's scope
            var requestDependency = queryProcessor.Execute(new RequestScopedSyncQuery());
            var enumerator = queryProcessor.ExecuteStream(new RequestScopedStreamQuery(), cancellationToken: TestContext.Current.CancellationToken).GetAsyncEnumerator(TestContext.Current.CancellationToken);

            // Act
            accessor.HttpContext = null;
            requestScope.Dispose();

            var yielded = new List<RequestScopedDependency>();
            await Should.NotThrowAsync(async () =>
            {
                while (await enumerator.MoveNextAsync())
                    yielded.Add(enumerator.Current);
            });
            await enumerator.DisposeAsync();

            // Assert
            yielded.Count.ShouldBe(1);
            ReferenceEquals(yielded[0], requestDependency).ShouldBeFalse();
            yielded[0].IsDisposed.ShouldBeTrue();
        }
    }
}
