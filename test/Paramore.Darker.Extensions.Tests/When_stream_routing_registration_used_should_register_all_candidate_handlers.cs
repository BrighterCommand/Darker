// MIT License
// Copyright (c) 2024 Ian Cooper

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    public class StreamRoutingDiRegistrationTests
    {
        private sealed class LegacyStreamHandler : IStreamQueryHandler<ExportedDatedStreamQuery, string>
        {
            public IQueryContext Context { get; set; }

            public async IAsyncEnumerable<string> ExecuteAsync(
                ExportedDatedStreamQuery query,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                yield return "legacy";
                await Task.CompletedTask;
            }
        }

        private sealed class NewStreamHandler : IStreamQueryHandler<ExportedDatedStreamQuery, string>
        {
            public IQueryContext Context { get; set; }

            public async IAsyncEnumerable<string> ExecuteAsync(
                ExportedDatedStreamQuery query,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                yield return "new";
                await Task.CompletedTask;
            }
        }

        [Fact]
        public async Task When_stream_routing_registration_used_should_register_all_candidate_handler_types_in_the_container()
        {
            // Arrange
            var cutover = new DateTime(2024, 1, 1);

            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker()
                .AddStreamHandlers(r => r.Register<ExportedDatedStreamQuery, string>(
                    (q, ctx) => q.Date < cutover
                        ? typeof(LegacyStreamHandler)
                        : typeof(NewStreamHandler),
                    typeof(LegacyStreamHandler), typeof(NewStreamHandler)));

            var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act — same stream query type, different date fields
            var legacyItems = new List<string>();
            await foreach (var item in queryProcessor.ExecuteStream(new ExportedDatedStreamQuery(new DateTime(2020, 6, 1))))
                legacyItems.Add(item);

            var newItems = new List<string>();
            await foreach (var item in queryProcessor.ExecuteStream(new ExportedDatedStreamQuery(new DateTime(2025, 6, 1))))
                newItems.Add(item);

            // Assert — both candidate handlers were registered in the container and are reachable
            legacyItems.ShouldBe(new[] { "legacy" });
            newItems.ShouldBe(new[] { "new" });
        }
    }
}
