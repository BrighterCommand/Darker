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
    public class When_stream_handler_has_mismatched_decorator_attribute_should_throw_ConfigurationException
    {
        private static QueryProcessor BuildProcessorForSync<THandler>(QueryHandlerRegistry syncRegistry)
            where THandler : class, IQueryHandler
        {
            var handlerFactory = new SimpleHandlerFactory(type => (IQueryHandler)Activator.CreateInstance(type));
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var config = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }

        private static QueryProcessor BuildProcessorForAsync<THandler>(QueryHandlerRegistryAsync asyncRegistry)
            where THandler : class, IQueryHandler
        {
            var handlerFactory = new SimpleHandlerFactory(type => (IQueryHandler)Activator.CreateInstance(type));
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }

        private static QueryProcessor BuildProcessorForStream<THandler>(StreamQueryHandlerRegistry streamRegistry)
            where THandler : class, IQueryHandler
        {
            var handlerFactory = new SimpleHandlerFactory(type => (IQueryHandler)Activator.CreateInstance(type));
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, decoratorRegistry, decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, decoratorRegistry, decoratorFactory,
                streamRegistry);

            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }

        [Fact]
        public void When_sync_handler_has_stream_attribute_should_throw_configuration_exception()
        {
            // Arrange — sync handler with [StreamStepEvent] (StreamQueryHandlerAttribute) on Execute
            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<SyncTestQuery, SyncTestQuery.Result, SyncHandlerWithStreamAttribute>();
            var processor = BuildProcessorForSync<SyncHandlerWithStreamAttribute>(syncRegistry);

            // Act
            var exception = Should.Throw<ConfigurationException>(
                () => processor.Execute(new SyncTestQuery(Guid.NewGuid())));

            // Assert
            exception.Message.ShouldContain("stream", Case.Insensitive);
        }

        [Fact]
        public async Task When_async_handler_has_stream_attribute_should_throw_configuration_exception()
        {
            // Arrange — async handler with [StreamStepEvent] (StreamQueryHandlerAttribute) on ExecuteAsync
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncHandlerWithStreamAttribute>();
            var processor = BuildProcessorForAsync<AsyncHandlerWithStreamAttribute>(asyncRegistry);

            // Act
            var exception = await Should.ThrowAsync<ConfigurationException>(
                () => processor.ExecuteAsync(new AsyncTestQuery(Guid.NewGuid())));

            // Assert
            exception.Message.ShouldContain("stream", Case.Insensitive);
        }

        [Fact]
        public async Task When_stream_handler_has_sync_attribute_should_throw_configuration_exception()
        {
            // Arrange — stream handler with [FallbackPolicy] (QueryHandlerAttribute) on ExecuteAsync
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<StreamTestQuery, string, StreamHandlerWithSyncAttribute>();
            var processor = BuildProcessorForStream<StreamHandlerWithSyncAttribute>(streamRegistry);

            // Act — BuildStream runs inside the iterator; exception is deferred to first MoveNextAsync
            var stream = processor.ExecuteStream(new StreamTestQuery());
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

            // Assert
            caughtException.ShouldNotBeNull();
            caughtException.ShouldBeOfType<ConfigurationException>();
            caughtException.Message.ShouldContain("sync", Case.Insensitive);
            caughtException.Message.ShouldContain("stream", Case.Insensitive);
        }

        [Fact]
        public async Task When_stream_handler_has_async_attribute_should_throw_configuration_exception()
        {
            // Arrange — stream handler with [FallbackPolicyAttributeAsync] (QueryHandlerAttributeAsync) on ExecuteAsync
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<StreamTestQuery, string, StreamHandlerWithAsyncAttribute>();
            var processor = BuildProcessorForStream<StreamHandlerWithAsyncAttribute>(streamRegistry);

            // Act — BuildStream runs inside the iterator; exception is deferred to first MoveNextAsync
            var stream = processor.ExecuteStream(new StreamTestQuery());
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

            // Assert
            caughtException.ShouldNotBeNull();
            caughtException.ShouldBeOfType<ConfigurationException>();
            caughtException.Message.ShouldContain("async", Case.Insensitive);
            caughtException.Message.ShouldContain("stream", Case.Insensitive);
        }
    }
}
