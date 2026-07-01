using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_creating_query_with_explicit_id_should_use_supplied_id
    {
        [Fact]
        public void Should_use_supplied_id_when_constructed_with_explicit_id_ctor()
        {
            // Arrange
            const string expectedId = "order-42";

            // Act
            var query = new QueryWithExplicitId(expectedId);

            // Assert
            query.Id.ShouldBe(expectedId);
        }

        [Fact]
        public void Should_use_init_setter_to_override_default_id()
        {
            // Arrange
            const string expectedId = "x";

            // Act
            var query = new QueryWithDefaultId { Id = expectedId };

            // Assert
            query.Id.ShouldBe(expectedId);
        }
    }
}
