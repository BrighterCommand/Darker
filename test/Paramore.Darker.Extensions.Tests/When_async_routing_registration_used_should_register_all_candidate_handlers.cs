// MIT License
// Copyright (c) 2024 Ian Cooper

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Core.Tests.Exported;
using Paramore.Darker.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    public class AsyncRoutingDiRegistrationTests
    {
        private sealed class LegacyAsyncHandler : IQueryHandlerAsync<ExportedDatedQuery, string>
        {
            public IQueryContext Context { get; set; }
            public Task<string> ExecuteAsync(ExportedDatedQuery query, CancellationToken cancellationToken = default)
                => Task.FromResult("legacy");
            public Task<string> FallbackAsync(ExportedDatedQuery query, CancellationToken cancellationToken = default)
                => throw new NotImplementedException();
        }

        private sealed class NewAsyncHandler : IQueryHandlerAsync<ExportedDatedQuery, string>
        {
            public IQueryContext Context { get; set; }
            public Task<string> ExecuteAsync(ExportedDatedQuery query, CancellationToken cancellationToken = default)
                => Task.FromResult("new");
            public Task<string> FallbackAsync(ExportedDatedQuery query, CancellationToken cancellationToken = default)
                => throw new NotImplementedException();
        }

        [Fact]
        public async Task When_async_routing_registration_used_should_register_all_candidate_handler_types_in_the_container()
        {
            // Arrange
            var cutover = new DateTime(2024, 1, 1);

            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker()
                .AddAsyncHandlers(r => r.Register<ExportedDatedQuery, string>(
                    (q, ctx) => q.Date < cutover
                        ? typeof(LegacyAsyncHandler)
                        : typeof(NewAsyncHandler),
                    typeof(LegacyAsyncHandler), typeof(NewAsyncHandler)));

            var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act — same query type, different date fields
            var legacyResult = await queryProcessor.ExecuteAsync(new ExportedDatedQuery(new DateTime(2020, 6, 1)));
            var newResult = await queryProcessor.ExecuteAsync(new ExportedDatedQuery(new DateTime(2025, 6, 1)));

            // Assert — both candidate handlers were registered in the container and are reachable
            legacyResult.ShouldBe("legacy");
            newResult.ShouldBe("new");
        }
    }
}
