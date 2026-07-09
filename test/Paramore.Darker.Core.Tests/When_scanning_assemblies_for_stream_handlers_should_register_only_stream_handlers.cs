using Paramore.Darker.Core.Tests.Exported;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_scanning_assemblies_for_stream_handlers_should_register_only_stream_handlers
    {
        [Fact]
        public void When_scanning_assembly_should_register_public_stream_handler_implementations()
        {
            // Arrange
            var registry = new StreamQueryHandlerRegistry();

            // Act — scan this test assembly which contains ExportedStreamQueryHandler
            registry.RegisterFromAssemblies(new[] { typeof(ExportedStreamQueryHandler).Assembly });

            // Assert — the exported stream handler is found
            registry.Get(typeof(ExportedStreamQuery)).ShouldBe(typeof(ExportedStreamQueryHandler));
        }

        [Fact]
        public void When_scanning_assembly_should_not_register_async_query_handlers()
        {
            // Arrange
            var registry = new StreamQueryHandlerRegistry();

            // Act
            registry.RegisterFromAssemblies(new[] { typeof(TestQueryHandlerAsync).Assembly });

            // Assert — IQueryHandlerAsync implementations are NOT in the stream registry
            registry.Get(typeof(TestQueryA)).ShouldBeNull();
        }

        [Fact]
        public void When_scanning_assembly_should_not_register_sync_query_handlers()
        {
            // Arrange
            var registry = new StreamQueryHandlerRegistry();

            // Act
            registry.RegisterFromAssemblies(new[] { typeof(TestQueryHandler).Assembly });

            // Assert — IQueryHandler implementations are NOT in the stream registry
            // TestQueryHandler handles TestQueryA; it should not appear in the stream registry
            registry.Get(typeof(TestQueryA)).ShouldBeNull();
        }
    }
}
