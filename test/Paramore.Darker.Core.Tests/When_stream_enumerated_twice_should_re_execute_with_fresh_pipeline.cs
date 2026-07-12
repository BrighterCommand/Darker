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
    public class When_stream_enumerated_twice_should_re_execute_with_fresh_pipeline
    {
        [Fact]
        public async Task When_stream_is_enumerated_twice_should_yield_all_items_each_time()
        {
            // Arrange — count how many handler instances are created across both enumerations
            int handlerCreationCount = 0;
            var handlerFactory = new SimpleHandlerFactory(_ =>
            {
                handlerCreationCount++;
                return new MultiItemStreamHandler();
            });
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<MultiItemStreamQuery, string, MultiItemStreamHandler>();

            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, decoratorRegistry, decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, decoratorRegistry, decoratorFactory,
                streamRegistry);

            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());
            var query = new MultiItemStreamQuery();

            // Act — obtain the IAsyncEnumerable once, then enumerate it twice
            var stream = processor.ExecuteStream(query);

            var firstPass = new List<string>();
            await foreach (var item in stream)
                firstPass.Add(item);

            var secondPass = new List<string>();
            await foreach (var item in stream)
                secondPass.Add(item);

            // Assert — both passes yield the full item sequence (cold, not cached)
            firstPass.ShouldBe(MultiItemStreamHandler.Items, ignoreOrder: false,
                "first pass must yield all handler items in order");
            secondPass.ShouldBe(MultiItemStreamHandler.Items, ignoreOrder: false,
                "second pass must re-execute the handler and yield all items again — the stream is cold, not cached");

            // Assert — a fresh handler was created per enumeration (fresh PipelineBuilder per GetAsyncEnumerator)
            handlerCreationCount.ShouldBe(2,
                "each await foreach calls GetAsyncEnumerator, which starts a fresh async iterator body creating a new PipelineBuilder and handler");
        }
    }
}
