// Copyright (c) 2025, Ian Cooper
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the
// following conditions are met:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
// Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_consuming_stream_query_should_produce_items_lazily
    {
        private static QueryProcessor BuildProcessor(int[] producedCount)
        {
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<LazyStreamQuery, int, LazyTrackingStreamHandler>();

            var syncRegistry = new QueryHandlerRegistry();
            var handlerFactory = new SimpleHandlerFactory(_ => new LazyTrackingStreamHandler(producedCount));
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var config = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, decoratorRegistry, decoratorFactory,
                streamRegistry);

            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }

        [Fact]
        public async Task When_consuming_a_stream_query_should_observe_first_item_before_handler_produces_last()
        {
            // Arrange
            var producedCount = new int[1];
            var processor = BuildProcessor(producedCount);
            var query = new LazyStreamQuery();

            // Act — use manual enumerator to capture the production count the instant the first item arrives
            var enumerator = processor.ExecuteStream(query).GetAsyncEnumerator();
            try
            {
                await enumerator.MoveNextAsync();
                var firstItem = enumerator.Current;
                var producedWhenFirstObserved = producedCount[0];

                // Exhaust remaining items
                while (await enumerator.MoveNextAsync()) { }

                // Assert — only 1 item produced when consumer observes the first item (no eager buffering)
                producedWhenFirstObserved.ShouldBe(1,
                    "the framework must not buffer the stream; only the item just yielded should have been produced");
                producedWhenFirstObserved.ShouldBeLessThan(LazyTrackingStreamHandler.TotalItems);
                firstItem.ShouldBe(1);
                producedCount[0].ShouldBe(LazyTrackingStreamHandler.TotalItems);
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }
    }
}
