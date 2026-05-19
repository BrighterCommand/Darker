using System;
using Paramore.Darker.QueryLogging;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests.Decorators
{
    public class QueryLoggingDecoratorTests
    {
        [Fact]
        public void When_sync_logging_attribute_should_return_sync_decorator_type()
        {
            // Arrange
            var attribute = new QueryLoggingAttribute(1);

            // Act
            var decoratorType = attribute.GetDecoratorType();

            // Assert
            decoratorType.ShouldBe(typeof(QueryLoggingDecorator<,>));
        }

        [Fact]
        public void When_async_logging_attribute_should_return_async_decorator_type()
        {
            // Arrange
            var attribute = new QueryLoggingAttributeAsync(1);

            // Act
            var decoratorType = attribute.GetDecoratorType();

            // Assert
            decoratorType.ShouldBe(typeof(QueryLoggingDecoratorAsync<,>));
        }
    }
}
