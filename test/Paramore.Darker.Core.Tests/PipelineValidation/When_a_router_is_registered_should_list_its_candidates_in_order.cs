using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class RouterRegistrationInspectionTests
    {
        [Fact]
        public void When_a_router_is_registered_should_list_its_candidates_in_order()
        {
            // Arrange — candidates deliberately registered new-before-legacy
            var registry = new QueryHandlerRegistry();
            registry.Register<DatedQuery, string>(
                (_, _) => typeof(NewDatedQueryHandler),
                typeof(NewDatedQueryHandler), typeof(LegacyDatedQueryHandler));

            // Act
            var registrations = ((IAmAHandlerRegistryInspector)registry).GetRegistrations();

            // Assert — one registration carrying both candidates in order
            var registration = registrations.ShouldHaveSingleItem();
            registration.QueryType.ShouldBe(typeof(DatedQuery));
            registration.ResultType.ShouldBe(typeof(string));
            registration.CandidateHandlerTypes.ShouldBe([typeof(NewDatedQueryHandler), typeof(LegacyDatedQueryHandler)]);
        }

        [Fact]
        public void When_a_router_is_registered_with_no_candidates_should_list_an_empty_candidate_list()
        {
            // Arrange
            var registry = new QueryHandlerRegistry();
            registry.Register<DatedQuery, string>((_, _) => null);

            // Act
            var registrations = ((IAmAHandlerRegistryInspector)registry).GetRegistrations();

            // Assert
            var registration = registrations.ShouldHaveSingleItem();
            registration.QueryType.ShouldBe(typeof(DatedQuery));
            registration.CandidateHandlerTypes.ShouldBeEmpty();
        }

        [Fact]
        public void When_fixed_and_routed_registrations_are_mixed_should_keep_their_relative_order()
        {
            // Arrange — fixed, routed, fixed
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, SyncHandlerWithDbSpanAttribute>();
            registry.Register<DatedQuery, string>((_, _) => typeof(NewDatedQueryHandler), typeof(NewDatedQueryHandler));
            registry.Register<PartialBuildQuery, string, PartialBuildHandler>();

            // Act
            var registrations = ((IAmAHandlerRegistryInspector)registry).GetRegistrations();

            // Assert
            registrations.Select(r => r.QueryType)
                .ShouldBe([typeof(SyncTestQuery), typeof(DatedQuery), typeof(PartialBuildQuery)]);
        }
    }
}
