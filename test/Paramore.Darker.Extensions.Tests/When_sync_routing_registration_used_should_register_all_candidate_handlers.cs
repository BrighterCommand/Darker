using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Core.Tests.Exported;
using Paramore.Darker.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    public class SyncRoutingDiRegistrationTests
    {
        private sealed class LegacyHandler : IQueryHandler<ExportedDatedQuery, string>
        {
            public IQueryContext Context { get; set; }
            public string Execute(ExportedDatedQuery query) => "legacy";
            public string Fallback(ExportedDatedQuery query) => throw new NotImplementedException();
        }

        private sealed class NewHandler : IQueryHandler<ExportedDatedQuery, string>
        {
            public IQueryContext Context { get; set; }
            public string Execute(ExportedDatedQuery query) => "new";
            public string Fallback(ExportedDatedQuery query) => throw new NotImplementedException();
        }

        [Fact]
        public void When_sync_routing_registration_used_should_register_all_candidate_handlers_and_route_correctly()
        {
            // Arrange
            var cutover = new DateTime(2024, 1, 1);

            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker()
                .AddHandlers(r => r.Register<ExportedDatedQuery, string>(
                    (q, ctx) => q.Date < cutover
                        ? typeof(LegacyHandler)
                        : typeof(NewHandler),
                    typeof(LegacyHandler), typeof(NewHandler)));

            var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act — same query type, different date fields
            var legacyResult = queryProcessor.Execute(new ExportedDatedQuery(new DateTime(2020, 6, 1)));
            var newResult = queryProcessor.Execute(new ExportedDatedQuery(new DateTime(2025, 6, 1)));

            // Assert — both candidate handlers were registered in the container and are reachable
            legacyResult.ShouldBe("legacy");
            newResult.ShouldBe("new");
        }
    }
}
