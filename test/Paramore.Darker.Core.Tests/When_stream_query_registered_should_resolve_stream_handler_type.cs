using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_stream_query_registered_should_resolve_stream_handler_type
    {
        [Fact]
        public void When_registering_a_stream_handler_should_return_handler_type_on_get()
        {
            // Arrange
            var registry = new StreamQueryHandlerRegistry();
            registry.Register<StreamTestQuery, string, StreamTestQueryHandler>();

            // Act
            var handlerType = registry.Get(typeof(StreamTestQuery), null, null);

            // Assert
            handlerType.ShouldBe(typeof(StreamTestQueryHandler));
        }

        [Fact]
        public void When_querying_unregistered_type_should_return_null()
        {
            // Arrange
            var registry = new StreamQueryHandlerRegistry();
            registry.Register<StreamTestQuery, string, StreamTestQueryHandler>();

            // Act
            var handlerType = registry.Get(typeof(StreamTestQueryOfDifferentResult), null, null);

            // Assert
            handlerType.ShouldBeNull();
        }

        [Fact]
        public void When_registering_duplicate_query_type_should_throw_ConfigurationException()
        {
            // Arrange
            var registry = new StreamQueryHandlerRegistry();
            registry.Register<StreamTestQuery, string, StreamTestQueryHandler>();

            // Act / Assert
            var exception = Assert.Throws<ConfigurationException>(
                () => registry.Register<StreamTestQuery, string, StreamTestQueryHandler>());

            exception.Message.ShouldBe($"Registry already contains an entry for {typeof(StreamTestQuery).Name}");
        }

        [Fact]
        public void When_registering_with_mismatched_result_type_should_throw_ConfigurationException()
        {
            // Arrange
            var registry = new StreamQueryHandlerRegistry();

            // Act / Assert — StreamTestQuery yields string, but we claim int as the result type
            var exception = Assert.Throws<ConfigurationException>(
                () => registry.Register(typeof(StreamTestQuery), typeof(int), typeof(StreamTestQueryHandler)));

            exception.Message.ShouldBe($"Result type not valid for query {typeof(StreamTestQuery).Name}");
        }
    }
}
