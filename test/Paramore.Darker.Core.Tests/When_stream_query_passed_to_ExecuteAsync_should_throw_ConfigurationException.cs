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

using System;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_stream_query_passed_to_ExecuteAsync_should_throw_ConfigurationException
    {
        private static QueryProcessor BuildProcessor()
        {
            // MultiItemStreamQuery is registered only in the stream registry,
            // not in the async registry — so ExecuteAsync cannot find a handler.
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<MultiItemStreamQuery, string, MultiItemStreamHandler>();

            var syncRegistry = new QueryHandlerRegistry();
            var handlerFactory = new SimpleHandlerFactory(_ =>
                throw new InvalidOperationException("should not be called"));
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ =>
                throw new InvalidOperationException("should not be called"));
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var config = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, decoratorRegistry, decoratorFactory,
                streamRegistry);

            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }

        [Fact]
        public async Task When_stream_query_passed_to_ExecuteAsync_should_throw_ConfigurationException_for_no_async_handler()
        {
            // Arrange — IStreamQuery<string> also satisfies IQuery<string>, so this compiles
            var processor = BuildProcessor();

            // Act — passing a stream query to ExecuteAsync; stream handlers live only in the stream registry
            var exception = await Should.ThrowAsync<ConfigurationException>(
                () => processor.ExecuteAsync<string>(new MultiItemStreamQuery()));

            // Assert — clear message indicating no async handler (not a misleading error)
            exception.Message.ShouldContain(nameof(MultiItemStreamQuery));
        }
    }
}
