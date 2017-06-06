using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
            var result = await _queryProcessor.ExecuteAsync(new TestQueryA());

            // Assert
            result.ShouldBe(expecedResult);
        }
        
        [Fact]
        public async Task ExecutesRemoteQueriesBeforeInProcessQueries()
        {
            // Arrange
            var expecedResult = Guid.NewGuid();
            _remoteQueryRegistry.Register<TestQueryA, Guid>(expecedResult);
            _queryHandlerRegistry.Register<TestQueryA, Guid, InMemoryRemoteQueryHandler<TestQueryA, Guid>>();

            // Act
            var result = await _queryProcessor.ExecuteAsync(new TestQueryA());

            // Assert
            result.ShouldBe(expecedResult);
        }

        [Fact]
        public void ThrowsInvalidOperationExceptionWhenTryingToExecuteARemoteQuerySynchronously()
        {
            // Arrange
            var expecedResult = Guid.NewGuid();
            _remoteQueryRegistry.Register<TestQueryA, Guid>(expecedResult);

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => _queryProcessor.Execute(new TestQueryA()));

            // Assert
            exception.Message.ShouldBe("Remote queries only support async execution. Please use ExecuteAsync instead.");
        }

        public class TestQueryA : IQuery<Guid>
        {
        }

        public class TestQueryB : IQuery<Guid>
        {
        }

        public sealed class InMemoryRemoteQueryHandler<TQuery, TResult> : QueryHandlerAsync<TQuery, TResult>
            where TQuery : IQuery<TResult>
        {
            private readonly TResult _result;

            public InMemoryRemoteQueryHandler(TResult result)
            {
                _result = result;
            }

            public override Task<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(_result);
            }
        }

        public sealed class InMemoryRemoteQueryRegistry : IRemoteQueryRegistry
        {
            private readonly IDictionary<Type, IQueryHandler> _handlers = new Dictionary<Type, IQueryHandler>();

            public bool CanHandle(Type query) => _handlers.ContainsKey(query);

            public IQueryHandler ResolveHandler(Type query) => _handlers[query];

            public void Register<TQuery, TResult>(TResult result) where TQuery : IQuery<TResult>
            {
                _handlers.Add(typeof(TQuery), new InMemoryRemoteQueryHandler<TQuery, TResult>(result));
            }

            public void Register<TQuery, TResult>(IQueryHandler<TQuery, TResult> handler) where TQuery : IQuery<TResult>
            {
                _handlers.Add(typeof(TQuery), handler);
            }
        }
    }
}