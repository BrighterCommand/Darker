using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Policies.Attributes;
using Paramore.Darker.Policies.Handlers;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Decorators
{
    public class RetryableQueryDecoratorTests
    {
        [Fact]
        public void When_sync_retryable_attribute_should_return_sync_decorator_type()
        {
            // Arrange
            var attribute = new RetryableQueryAttribute(1, "MyPolicy");

            // Act
            var decoratorType = attribute.GetDecoratorType();

            // Assert
            decoratorType.ShouldBe(typeof(RetryableQueryDecorator<,>));
        }

        [Fact]
        public void When_async_retryable_attribute_should_return_async_decorator_type()
        {
            // Arrange
            var attribute = new RetryableQueryAttributeAsync(1, "MyPolicy");

            // Act
            var decoratorType = attribute.GetDecoratorType();

            // Assert
            decoratorType.ShouldBe(typeof(RetryableQueryDecoratorAsync<,>));
        }
    }
}
