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
using System.Reflection;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_stream_handler_throws_during_enumeration_should_surface_original_exception
    {
        private static QueryProcessor BuildProcessor()
        {
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<FaultingStreamQuery, string, FaultingStreamHandler>();

            var syncRegistry = new QueryHandlerRegistry();
            var handlerFactory = new SimpleHandlerFactory(_ => new FaultingStreamHandler());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var config = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, decoratorRegistry, decoratorFactory,
                streamRegistry);

            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }

        [Fact]
        public async Task When_stream_handler_throws_during_enumeration_should_surface_original_exception_not_wrapped()
        {
            // Arrange — FaultingStreamHandler yields 2 items then throws InvalidOperationException
            var processor = BuildProcessor();
            var receivedItems = new List<string>();
            Exception caughtException = null;

            // Act
            try
            {
                await foreach (var item in processor.ExecuteStream(new FaultingStreamQuery()))
                    receivedItems.Add(item);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            // Assert — original exception type and message reach the caller unwrapped
            caughtException.ShouldNotBeNull();
            caughtException.ShouldBeOfType<InvalidOperationException>(
                "iterator body faults propagate directly from MoveNextAsync, not via TargetInvocationException");
            caughtException.Message.ShouldBe(FaultingStreamHandler.ExceptionMessage);
            caughtException.ShouldNotBeOfType<TargetInvocationException>();

            // Items produced before the fault were still observed
            receivedItems.Count.ShouldBe(FaultingStreamHandler.ItemsBeforeFault);
            receivedItems[0].ShouldBe("item-1");
            receivedItems[1].ShouldBe("item-2");
        }
    }
}
