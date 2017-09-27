using System;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Testing.Ports;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests
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
                handlerRegistry.Register(typeof(TestQueryA), typeof(Guid), typeof(IQueryHandler<TestQueryA, Guid>));

                // Act
                var handlerType = handlerRegistry.Get(typeof(TestQueryA));

                // Assert
                handlerType.ShouldBe(typeof(IQueryHandler<TestQueryA, Guid>));
            }

            [Fact]
            public void ReturnsNullForNotRegisteredHandler()
            {
                // Arrange
                var handlerRegistry = new QueryHandlerRegistry();
                handlerRegistry.Register(typeof(TestQueryA), typeof(Guid), typeof(IQueryHandler<TestQueryA, Guid>));

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
                handlerRegistry.Register<TestQueryA, Guid, IQueryHandler<TestQueryA, Guid>>();

                // Act
                var exception = Assert.Throws<ConfigurationException>(() => handlerRegistry.Register(
                    typeof(TestQueryA), typeof(Guid), typeof(IQueryHandler<TestQueryA, Guid>)));

                // Assert
                exception.Message.ShouldBe($"Registry already contains an entry for {typeof(TestQueryA).Name}");
                handlerRegistry.Get(typeof(TestQueryA)).ShouldNotBeNull();
                handlerRegistry.Get(typeof(TestQueryB)).ShouldBeNull();
                handlerRegistry.Get(typeof(TestQueryC)).ShouldBeNull();
            }

#if !NET452
            [Fact]
            public void ThrowsConfigurationExceptionWhenResultTypeDoesnotMatch()
            {
                // Arrange
                var handlerRegistry = new QueryHandlerRegistry();

                // Act
                var exception = Assert.Throws<ConfigurationException>(() => handlerRegistry.Register(
                    typeof(TestQueryA), typeof(string), typeof(IQueryHandler<TestQueryA, Guid>)));

                // Assert
                exception.Message.ShouldBe($"Result type not valid for query {typeof(TestQueryA).Name}");
            }
#endif
        }

        public class WhenRegisteringUsingGenerics
        {
            [Fact]
            public void ReturnsRegisteredHandler()
            {
                // Arrange
                var handlerRegistry = new QueryHandlerRegistry();
                handlerRegistry.Register<TestQueryA, Guid, IQueryHandler<TestQueryA, Guid>>();

                // Act
                var handlerType = handlerRegistry.Get(typeof(TestQueryA));

                // Assert
                handlerType.ShouldBe(typeof(IQueryHandler<TestQueryA, Guid>));
            }

            [Fact]
            public void ReturnsNullForNotRegisteredHandler()
            {
                // Arrange
                var handlerRegistry = new QueryHandlerRegistry();
                handlerRegistry.Register<TestQueryA, Guid, IQueryHandler<TestQueryA, Guid>>();

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
                handlerRegistry.Register<TestQueryA, Guid, IQueryHandler<TestQueryA, Guid>>();

                // Act
                var exception = Assert.Throws<ConfigurationException>(() => handlerRegistry.Register<TestQueryA, Guid, IQueryHandler<TestQueryA, Guid>>());

                // Assert
                exception.Message.ShouldBe($"Registry already contains an entry for {typeof(TestQueryA).Name}");
                handlerRegistry.Get(typeof(TestQueryA)).ShouldNotBeNull();
                handlerRegistry.Get(typeof(TestQueryB)).ShouldBeNull();
                handlerRegistry.Get(typeof(TestQueryC)).ShouldBeNull();
            }
        }

        public class WhenRegisteringFromAssemblies
        {
            [Fact]
            public void FindsAndRegistersAllHandlersInSingleAssembly()
            {
                // Arrange
                var handlerRegistry = new QueryHandlerRegistry();

                // Act
                handlerRegistry.RegisterFromAssemblies(new[] { typeof(TestQueryHandler).Assembly });

                // Assert
                handlerRegistry.Get(typeof(TestQueryA)).ShouldNotBeNull();
                handlerRegistry.Get(typeof(TestQueryB)).ShouldBeNull();
                handlerRegistry.Get(typeof(TestQueryC)).ShouldBeNull();
            }
        }
    }
}