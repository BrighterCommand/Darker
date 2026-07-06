using System;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_creating_query_without_id_should_default_to_guid
    {
        [Fact]
        public void Should_have_non_empty_guid_id_when_constructed_with_parameterless_ctor()
        {
            // Arrange
            var query = new QueryWithDefaultId();

            // Act
            var id = query.Id;

            // Assert
            id.ShouldNotBeNull();
            id.ShouldNotBeEmpty();
            Guid.TryParse(id, out _).ShouldBeTrue();
        }

        [Fact]
        public void Should_have_unique_id_per_instance()
        {
            // Arrange
            var query1 = new QueryWithDefaultId();
            var query2 = new QueryWithDefaultId();

            // Act
            var id1 = query1.Id;
            var id2 = query2.Id;

            // Assert
            id1.ShouldNotBe(id2);
        }

        [Fact]
        public void Should_be_assignable_to_IQuery()
        {
            // Arrange
            var query = new QueryWithDefaultId();

            // Act — assign to the marker interface
            IQuery<QueryWithDefaultId.Result> asIQuery = query;

            // Assert
            asIQuery.ShouldNotBeNull();
            asIQuery.ShouldBeAssignableTo<IQuery<QueryWithDefaultId.Result>>();
        }
    }
}
