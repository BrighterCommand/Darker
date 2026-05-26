using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Testing;
using Paramore.Darker.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests
{
    public class FakeQueryProcessorContextTests
    {
        [Fact]
        public void When_Execute_called_with_context_should_store_context_as_LastProvidedContext()
        {
            // Arrange
            var processor = new FakeQueryProcessor();
            var context = new QueryContext { Bag = { ["key"] = "value" } };
            var query = new SyncTestQuery(Guid.NewGuid());

            // Act
            processor.Execute(query, context);

            // Assert
            processor.LastProvidedContext.ShouldBeSameAs(context);
        }

        [Fact]
        public async Task When_ExecuteAsync_called_with_context_should_store_context_as_LastProvidedContext()
        {
            // Arrange
            var processor = new FakeQueryProcessor();
            var context = new QueryContext { Bag = { ["key"] = "value" } };
            var query = new SyncTestQuery(Guid.NewGuid());

            // Act
            await processor.ExecuteAsync(query, context, CancellationToken.None);

            // Assert
            processor.LastProvidedContext.ShouldBeSameAs(context);
        }

        [Fact]
        public void When_Execute_called_without_context_should_leave_LastProvidedContext_null()
        {
            // Arrange
            var processor = new FakeQueryProcessor();
            var query = new SyncTestQuery(Guid.NewGuid());

            // Act
            processor.Execute(query);

            // Assert
            processor.LastProvidedContext.ShouldBeNull();
        }
    }
}
