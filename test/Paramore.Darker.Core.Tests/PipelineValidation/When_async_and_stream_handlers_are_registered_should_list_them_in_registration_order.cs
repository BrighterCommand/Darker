using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class AsyncAndStreamRegistryInspectionTests
    {
        [Fact]
        public void When_async_handlers_are_registered_should_list_them_in_registration_order()
        {
            // Arrange — a fixed registration, then a router with two candidates
            var registry = new QueryHandlerRegistryAsync();
            registry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncHandlerWithSyncAttribute>();
            registry.Register<DatedQuery, string>(
                (_, _) => typeof(NewDatedQueryHandlerAsync),
                typeof(NewDatedQueryHandlerAsync), typeof(LegacyDatedQueryHandlerAsync));

            // Act
            var registrations = ((IAmAHandlerRegistryInspector)registry).GetRegistrations();

            // Assert
            registrations.Count.ShouldBe(2);
            registrations[0].QueryType.ShouldBe(typeof(AsyncTestQuery));
            registrations[0].ResultType.ShouldBe(typeof(AsyncTestQuery.Result));
            registrations[0].CandidateHandlerTypes.ShouldBe([typeof(AsyncHandlerWithSyncAttribute)]);
            registrations[1].QueryType.ShouldBe(typeof(DatedQuery));
            registrations[1].ResultType.ShouldBe(typeof(string));
            registrations[1].CandidateHandlerTypes.ShouldBe([typeof(NewDatedQueryHandlerAsync), typeof(LegacyDatedQueryHandlerAsync)]);
        }

        [Fact]
        public void When_stream_handlers_are_registered_should_list_them_in_registration_order()
        {
            // Arrange — a fixed registration, then a router with two candidates
            var registry = new StreamQueryHandlerRegistry();
            registry.Register<StreamTestQuery, string, StreamTestQueryHandler>();
            registry.Register<DatedStreamQuery, string>(
                (_, _) => typeof(NewDatedStreamHandler),
                typeof(NewDatedStreamHandler), typeof(LegacyDatedStreamHandler));

            // Act
            var registrations = ((IAmAHandlerRegistryInspector)registry).GetRegistrations();

            // Assert
            registrations.Count.ShouldBe(2);
            registrations[0].QueryType.ShouldBe(typeof(StreamTestQuery));
            registrations[0].ResultType.ShouldBe(typeof(string));
            registrations[0].CandidateHandlerTypes.ShouldBe([typeof(StreamTestQueryHandler)]);
            registrations[1].QueryType.ShouldBe(typeof(DatedStreamQuery));
            registrations[1].ResultType.ShouldBe(typeof(string));
            registrations[1].CandidateHandlerTypes.ShouldBe([typeof(NewDatedStreamHandler), typeof(LegacyDatedStreamHandler)]);
        }
    }
}
