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
using System.Collections.Generic;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_no_stream_handler_registered_should_throw_ConfigurationException_on_first_move_next
    {
        private static QueryProcessor BuildProcessorWithEmptyStreamRegistry()
        {
            // Empty stream registry — MultiItemStreamQuery is not registered
            var emptyStreamRegistry = new StreamQueryHandlerRegistry();

            var syncRegistry = new QueryHandlerRegistry();
            var handlerFactory = new SimpleHandlerFactory(_ =>
                throw new InvalidOperationException("should not be called"));
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ =>
                throw new InvalidOperationException("should not be called"));
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var config = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, decoratorRegistry, decoratorFactory,
                emptyStreamRegistry);

            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }

        [Fact]
        public void When_no_stream_handler_registered_should_not_throw_at_call_time()
        {
            // Arrange
            var processor = BuildProcessorWithEmptyStreamRegistry();

            // Act — ExecuteStream is a deferred iterator; calling it must not throw
            IAsyncEnumerable<string> stream = null;
            var callSiteException = Record.Exception(() =>
            {
                stream = processor.ExecuteStream(new MultiItemStreamQuery());
            });

            // Assert — no exception thrown at call time (deferred evaluation)
            callSiteException.ShouldBeNull(
                "ExecuteStream is an async iterator; the body (including handler resolution) runs on MoveNextAsync, not here");
            stream.ShouldNotBeNull();
        }

        [Fact]
        public async Task When_no_stream_handler_registered_should_throw_ConfigurationException_on_first_MoveNextAsync()
        {
            // Arrange — handler resolution runs inside the iterator, deferred until first MoveNextAsync
            var processor = BuildProcessorWithEmptyStreamRegistry();
            var stream = processor.ExecuteStream(new MultiItemStreamQuery());

            // Act — first MoveNextAsync triggers handler resolution, which throws
            var enumerator = stream.GetAsyncEnumerator();
            Exception caughtException = null;
            try
            {
                await enumerator.MoveNextAsync();
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }
            finally
            {
                await enumerator.DisposeAsync();
            }

            // Assert — ConfigurationException surfaces on the first MoveNextAsync, not at call time
            caughtException.ShouldNotBeNull();
            caughtException.ShouldBeOfType<ConfigurationException>(
                "handler resolution inside the iterator must throw ConfigurationException for an unregistered query");
            caughtException.Message.ShouldContain(nameof(MultiItemStreamQuery));
        }
    }
}
