using System;
using System.Linq;
using Paramore.Darker.Core.Tests.Exported;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class SyncRegistryInspectionTests
    {
        [Fact]
        public void When_handlers_are_registered_should_list_them_in_registration_order()
        {
            // Arrange — one generic and one Type-based registration, in that order
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, SyncHandlerWithDbSpanAttribute>();
            registry.Register(typeof(DatedQuery), typeof(string), typeof(NewDatedQueryHandler));

            // Act
            var registrations = ((IAmAHandlerRegistryInspector)registry).GetRegistrations();

            // Assert
            registrations.Count.ShouldBe(2);
            registrations[0].QueryType.ShouldBe(typeof(SyncTestQuery));
            registrations[0].ResultType.ShouldBe(typeof(SyncTestQuery.Result));
            registrations[0].CandidateHandlerTypes.ShouldBe([typeof(SyncHandlerWithDbSpanAttribute)]);
            registrations[1].QueryType.ShouldBe(typeof(DatedQuery));
            registrations[1].ResultType.ShouldBe(typeof(string));
            registrations[1].CandidateHandlerTypes.ShouldBe([typeof(NewDatedQueryHandler)]);
        }

        [Fact]
        public void When_handlers_are_registered_from_assemblies_should_list_the_scanned_handlers()
        {
            // Arrange — scanning the test assembly finds the public handlers in Exported/
            var registry = new QueryHandlerRegistry();
            registry.RegisterFromAssemblies([typeof(TestQueryHandler).Assembly]);

            // Act
            var registrations = ((IAmAHandlerRegistryInspector)registry).GetRegistrations();

            // Assert
            var registration = registrations.Single(r => r.QueryType == typeof(TestQueryA));
            registration.ResultType.ShouldBe(typeof(Guid));
            registration.CandidateHandlerTypes.ShouldBe([typeof(TestQueryHandler)]);
        }

        [Fact]
        public void When_a_null_handler_type_is_registered_should_list_it_with_a_null_candidate()
        {
            // Arrange
            var registry = new QueryHandlerRegistry();
            registry.Register(typeof(PartialBuildQuery), typeof(string), null);

            // Act
            var registrations = ((IAmAHandlerRegistryInspector)registry).GetRegistrations();

            // Assert
            var registration = registrations.ShouldHaveSingleItem();
            registration.QueryType.ShouldBe(typeof(PartialBuildQuery));
            registration.CandidateHandlerTypes.ShouldBe([(Type)null]);
        }
    }
}
