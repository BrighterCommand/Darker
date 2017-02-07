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
                handlerRegistry.Register(typeof(TestQueryA), typeof(TestQueryA.Result), typeof(IQueryHandler<TestQueryA, TestQueryA.Result>));

                // Act
                var handlerType = handlerRegistry.Get(typeof(TestQueryA));

                // Assert
                handlerType.ShouldBe(typeof(IQueryHandler<TestQueryA, TestQueryA.Result>));
            }

            [Fact]
            public void ReturnsNullForNotRegisteredHandler()
            {
                // Arrange
                var handlerRegistry = new QueryHandlerRegistry();
                handlerRegistry.Register(typeof(TestQueryA), typeof(TestQueryA.Result), typeof(IQueryHandler<TestQueryA, TestQueryA.Result>));

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
                handlerRegistry.Register<TestQueryA, TestQueryA.Result, IQueryHandler<TestQueryA, TestQueryA.Result>>();

                // Act
                var exception = Assert.Throws<ConfigurationException>(() => handlerRegistry.Register(
                    typeof(TestQueryA), typeof(TestQueryA.Result), typeof(IQueryHandler<TestQueryA, TestQueryA.Result>)));

                // Assert
                exception.Message.ShouldBe($"Registry already contains an entry for {typeof(TestQueryA).Name}");
                handlerRegistry.Get(typeof(TestQueryA)).ShouldNotBeNull();
                handlerRegistry.Get(typeof(TestQueryB)).ShouldBeNull();
                handlerRegistry.Get(typeof(TestQueryC)).ShouldBeNull();
            }

            [Fact]
            public void ThrowsConfigurationExceptionWhenResultTypeDoesnotMatch()
            {
                // Arrange
                var handlerRegistry = new QueryHandlerRegistry();

                // Act
                var exception = Assert.Throws<ConfigurationException>(() => handlerRegistry.Register(
                    typeof(TestQueryA), typeof(string), typeof(IQueryHandler<TestQueryA, TestQueryA.Result>)));

                // Assert
                exception.Message.ShouldBe($"Result type not valid for query {typeof(TestQueryA).Name}");
            }
        }

        public class WhenRegisteringUsingGenerics
        {
            [Fact]
            public void ReturnsRegisteredHandler()
            {
                // Arrange
                var handlerRegistry = new QueryHandlerRegistry();
                handlerRegistry.Register<TestQueryA, TestQueryA.Result, IQueryHandler<TestQueryA, TestQueryA.Result>>();

                // Act
                var handlerType = handlerRegistry.Get(typeof(TestQueryA));

                // Assert
                handlerType.ShouldBe(typeof(IQueryHandler<TestQueryA, TestQueryA.Result>));
            }

            [Fact]
            public void ReturnsNullForNotRegisteredHandler()
            {
                // Arrange
                var handlerRegistry = new QueryHandlerRegistry();
                handlerRegistry.Register<TestQueryA, TestQueryA.Result, IQueryHandler<TestQueryA, TestQueryA.Result>>();

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
                handlerRegistry.Register<TestQueryA, TestQueryA.Result, IQueryHandler<TestQueryA, TestQueryA.Result>>();

                // Act
                var exception = Assert.Throws<ConfigurationException>(() => handlerRegistry.Register<TestQueryA, TestQueryA.Result, IQueryHandler<TestQueryA, TestQueryA.Result>>());

                // Assert
                exception.Message.ShouldBe($"Registry already contains an entry for {typeof(TestQueryA).Name}");
                handlerRegistry.Get(typeof(TestQueryA)).ShouldNotBeNull();
                handlerRegistry.Get(typeof(TestQueryB)).ShouldBeNull();
                handlerRegistry.Get(typeof(TestQueryC)).ShouldBeNull();
            }
        }

        public class TestQueryA : IQuery<TestQueryA.Result>
        {
            public class Result { }
        }

        public class TestQueryB : IQuery<TestQueryB.Result>
        {
            public class Result { }
        }

        public class TestQueryC : IQuery<TestQueryC.Result>
        {
            public class Result { }
        }
    }
}