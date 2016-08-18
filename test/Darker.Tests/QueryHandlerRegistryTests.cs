using Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Darker.Tests
{
    public class QueryHandlerRegistryTests
    {
        public class WhenRegisteringUsingTypes
        {
            [Fact]
            public void ReturnsRegisteredHandler()
            {
                // Arrange
                var handlerRegistry = new QueryHandlerRegistry();
                handlerRegistry.Register(typeof(TestQueryA), typeof(TestQueryA.Response), typeof(IQueryHandler<TestQueryA, TestQueryA.Response>));

                // Act
                var handlerType = handlerRegistry.Get(typeof(TestQueryA));

                // Assert
                handlerType.ShouldBe(typeof(IQueryHandler<TestQueryA, TestQueryA.Response>));
            }

            [Fact]
            public void ReturnsNullForNotRegisteredHandler()
            {
                // Arrange
                var handlerRegistry = new QueryHandlerRegistry();
                handlerRegistry.Register(typeof(TestQueryA), typeof(TestQueryA.Response), typeof(IQueryHandler<TestQueryA, TestQueryA.Response>));

                // Act
                var handlerType = handlerRegistry.Get(typeof(TestQueryB));

                // Assert
                handlerType.ShouldBeNull();
            }

            [Fact]
            public void ThrowsConfigurationExceptionWhenAddingADuplicatedRegistration()
            {
                // Arrange
                var handlerRegistry = new QueryHandlerRegistry();
                handlerRegistry.Register<TestQueryA, TestQueryA.Response, IQueryHandler<TestQueryA, TestQueryA.Response>>();

                // Act
                var exception = Assert.Throws<ConfigurationException>(() => handlerRegistry.Register(typeof(TestQueryA), typeof(TestQueryA.Response), typeof(IQueryHandler<TestQueryA, TestQueryA.Response>)));

                // Assert
                exception.Message.ShouldBe($"Registry already contains an entry for {typeof(TestQueryA).Name}");
                handlerRegistry.Get(typeof(TestQueryA)).ShouldNotBeNull();
                handlerRegistry.Get(typeof(TestQueryB)).ShouldBeNull();
                handlerRegistry.Get(typeof(TestQueryC)).ShouldBeNull();
            }

            [Fact]
            public void ThrowsConfigurationExceptionWhenResponseTypeDoesnotMatch()
            {
                // Arrange
                var handlerRegistry = new QueryHandlerRegistry();

                // Act
                var exception = Assert.Throws<ConfigurationException>(() => handlerRegistry.Register(
                    typeof(TestQueryA), typeof(string), typeof(IQueryHandler<TestQueryA, TestQueryA.Response>)));

                // Assert
                exception.Message.ShouldBe($"Response type not valid for request {typeof(TestQueryA).Name}");
            }
        }

        public class WhenRegisteringUsingGenerics
        {
            [Fact]
            public void ReturnsRegisteredHandler()
            {
                // Arrange
                var handlerRegistry = new QueryHandlerRegistry();
                handlerRegistry.Register<TestQueryA, TestQueryA.Response, IQueryHandler<TestQueryA, TestQueryA.Response>>();

                // Act
                var handlerType = handlerRegistry.Get(typeof(TestQueryA));

                // Assert
                handlerType.ShouldBe(typeof(IQueryHandler<TestQueryA, TestQueryA.Response>));
            }

            [Fact]
            public void ReturnsNullForNotRegisteredHandler()
            {
                // Arrange
                var handlerRegistry = new QueryHandlerRegistry();
                handlerRegistry.Register<TestQueryA, TestQueryA.Response, IQueryHandler<TestQueryA, TestQueryA.Response>>();

                // Act
                var handlerType = handlerRegistry.Get(typeof(TestQueryB));

                // Assert
                handlerType.ShouldBeNull();
            }

            [Fact]
            public void ThrowsConfigurationExceptionWhenAddingADuplicatedRegistration()
            {
                // Arrange
                var handlerRegistry = new QueryHandlerRegistry();
                handlerRegistry.Register<TestQueryA, TestQueryA.Response, IQueryHandler<TestQueryA, TestQueryA.Response>>();

                // Act
                var exception = Assert.Throws<ConfigurationException>(() => handlerRegistry.Register<TestQueryA, TestQueryA.Response, IQueryHandler<TestQueryA, TestQueryA.Response>>());

                // Assert
                exception.Message.ShouldBe($"Registry already contains an entry for {typeof(TestQueryA).Name}");
                handlerRegistry.Get(typeof(TestQueryA)).ShouldNotBeNull();
                handlerRegistry.Get(typeof(TestQueryB)).ShouldBeNull();
                handlerRegistry.Get(typeof(TestQueryC)).ShouldBeNull();
            }
        }

        public class TestQueryA : IQueryRequest<TestQueryA.Response>
        {
            public class Response : IQueryResponse { }
        }

        public class TestQueryB : IQueryRequest<TestQueryB.Response>
        {
            public class Response : IQueryResponse { }
        }

        public class TestQueryC : IQueryRequest<TestQueryC.Response>
        {
            public class Response : IQueryResponse { }
        }
    }
}