using System;
using System.Threading.Tasks;
using Paramore.Darker.Testing;
using Paramore.Darker.Testing.Ports;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests
{
    public class FakeQueryProcessorAsyncTests
    {
        [Fact]
        public async Task When_FakeQueryProcessor_used_as_IQueryProcessorAsync_should_execute_async_queries()
        {
            // Arrange
            var expectedId = Guid.NewGuid();
            var fake = new FakeQueryProcessor();
            fake.SetupResultFor<TestQueryA>(expectedId);

            IQueryProcessorAsync asyncProcessor = fake;

            // Act
            var result = await asyncProcessor.ExecuteAsync(new TestQueryA(expectedId));

            // Assert
            result.ShouldBe(expectedId);
        }
    }
}
