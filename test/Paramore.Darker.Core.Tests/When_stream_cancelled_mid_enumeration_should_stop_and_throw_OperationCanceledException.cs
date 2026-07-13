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
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_stream_cancelled_mid_enumeration_should_stop_and_throw_OperationCanceledException
    {
        private static QueryProcessor BuildProcessor()
        {
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<MultiItemStreamQuery, string, MultiItemStreamHandler>();

            var syncRegistry = new QueryHandlerRegistry();
            var handlerFactory = new SimpleHandlerFactory(_ => new MultiItemStreamHandler());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var config = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, decoratorRegistry, decoratorFactory,
                streamRegistry);

            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }

        [Fact]
        public async Task When_stream_cancelled_mid_enumeration_should_stop_and_propagate_OperationCanceledException()
        {
            // Arrange — MultiItemStreamHandler yields 3 items and checks ThrowIfCancellationRequested before each
            var processor = BuildProcessor();
            using var cts = new CancellationTokenSource();
            var receivedItems = new List<string>();
            OperationCanceledException caughtException = null;

            // Act — WithCancellation flows the token into the handler via [EnumeratorCancellation]
            try
            {
                await foreach (var item in processor.ExecuteStream(new MultiItemStreamQuery())
                                                     .WithCancellation(cts.Token))
                {
                    receivedItems.Add(item);
                    cts.Cancel(); // signal cancellation after observing the first item
                }
            }
            catch (OperationCanceledException ex)
            {
                caughtException = ex;
            }

            // Assert
            caughtException.ShouldNotBeNull("cancellation must propagate as OperationCanceledException");
            receivedItems.Count.ShouldBe(1, "enumeration must stop after the cancellation signal");
            receivedItems[0].ShouldBe(MultiItemStreamHandler.Items[0]);
        }
    }
}
