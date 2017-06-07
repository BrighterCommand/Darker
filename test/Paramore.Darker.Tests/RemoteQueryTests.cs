using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Testing;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests
{
    public class RemoteQueryTests
    {
        private readonly InMemoryRemoteQueryRegistry _remoteQueryRegistry;
        private readonly IQueryHandlerRegistry _queryHandlerRegistry;
        private readonly IQueryProcessor _queryProcessor;

        public RemoteQueryTests()
        {
            _remoteQueryRegistry = new InMemoryRemoteQueryRegistry();
            _queryHandlerRegistry = new QueryHandlerRegistry();

            var handlerConfiguration = new HandlerConfiguration(_queryHandlerRegistry, new NullHandlerFactory(), new NullDecoratorFactory());
            _queryProcessor = new QueryProcessor(_remoteQueryRegistry, handlerConfiguration, new InMemoryQueryContextFactory());
        }
 
        [Fact]
        public async Task ExecutesRemoteQueries()
        {
            // Arrange
            var expecedResult = Guid.NewGuid();
            _remoteQueryRegistry.Register<TestQueryA, Guid>(expecedResult);
            _remoteQueryRegistry.Register<TestQueryB, Guid>(Guid.NewGuid());

            // Act
            var result = await _queryProcessor.ExecuteRemoteAsync(new TestQueryA());

            // Assert
            result.ShouldBe(expecedResult);
        }

        [Fact]
        public async Task ThrowsMissingHandlerExceptionWhenNoHandlerIsRegistered()
        {
            // Act
            var exception = await Assert.ThrowsAsync<MissingHandlerException>(async () => await _queryProcessor.ExecuteRemoteAsync(new TestQueryA()));

            // Assert
            exception.Message.ShouldBe($"No handler registered for remote query: {typeof(TestQueryA).FullName}");
        }

        public class TestQueryA : IRemoteQuery<Guid>
        {
        }

        public class TestQueryB : IRemoteQuery<Guid>
        {
        }

        public sealed class InMemoryRemoteQueryHandler<TQuery, TResult> : IQueryHandler
            where TQuery : IRemoteQuery<TResult>
        {
            private readonly TResult _result;
            
            public IQueryContext Context { get; set; }

            public InMemoryRemoteQueryHandler(TResult result)
            {
                _result = result;
            }

            public Task<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(_result);
            }
        }

        public sealed class InMemoryRemoteQueryRegistry : IRemoteQueryRegistry
        {
            private readonly IDictionary<Type, IQueryHandler> _handlers = new Dictionary<Type, IQueryHandler>();

            public bool CanHandle(Type query) => _handlers.ContainsKey(query);

            public IQueryHandler ResolveHandler(Type query) => _handlers[query];

            public void Register<TQuery, TResult>(TResult result) where TQuery : IRemoteQuery<TResult>
            {
                _handlers.Add(typeof(TQuery), new InMemoryRemoteQueryHandler<TQuery, TResult>(result));
            }

            public void Register<TQuery, TResult>(IQueryHandler handler) where TQuery : IRemoteQuery<TResult>
            {
                _handlers.Add(typeof(TQuery), handler);
            }
        }
    }
}