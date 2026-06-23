using Paramore.Darker.Policies.Attributes;
using Paramore.Darker.Policies.Handlers;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Decorators
{
    public class UseResiliencePipelineAttributeTests
    {
        [Fact]
        public void When_sync_attribute_should_return_policy_and_useTypePipeline_params()
        {
            // Arrange
            var attribute = new UseResiliencePipelineAttribute(1, "MyPipeline", useTypePipeline: true);

            // Act
            var attributeParams = attribute.GetAttributeParams();

            // Assert
            attributeParams.ShouldBe(new object[] { "MyPipeline", true });
        }

        [Fact]
        public void When_async_attribute_should_return_policy_and_useTypePipeline_params()
        {
            // Arrange
            var attribute = new UseResiliencePipelineAttributeAsync(1, "MyPipeline", useTypePipeline: true);

            // Act
            var attributeParams = attribute.GetAttributeParams();

            // Assert
            attributeParams.ShouldBe(new object[] { "MyPipeline", true });
        }

        [Fact]
        public void When_sync_attribute_should_return_sync_decorator_type()
        {
            // Arrange
            var attribute = new UseResiliencePipelineAttribute(1, "MyPipeline");

            // Act
            var decoratorType = attribute.GetDecoratorType();

            // Assert
            decoratorType.ShouldBe(typeof(UseResiliencePipelineHandler<,>));
        }

        [Fact]
        public void When_async_attribute_should_return_async_decorator_type()
        {
            // Arrange
            var attribute = new UseResiliencePipelineAttributeAsync(1, "MyPipeline");

            // Act
            var decoratorType = attribute.GetDecoratorType();

            // Assert
            decoratorType.ShouldBe(typeof(UseResiliencePipelineHandlerAsync<,>));
        }

        [Fact]
        public void When_useTypePipeline_not_specified_should_default_to_false()
        {
            // Arrange
            var attribute = new UseResiliencePipelineAttribute(1, "MyPipeline");

            // Act
            var attributeParams = attribute.GetAttributeParams();

            // Assert — second param is the useTypePipeline flag, defaulting to false
            attributeParams[1].ShouldBe(false);
        }
    }
}
