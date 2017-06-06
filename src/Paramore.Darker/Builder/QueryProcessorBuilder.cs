using System;
using System.Collections.Generic;

namespace Paramore.Darker.Builder
{
    public sealed class QueryProcessorBuilder : INeedHandlers, INeedRemoteQueries, INeedAQueryContext, IBuildTheQueryProcessor
    {
        private readonly Dictionary<string, object> _contextBagData = new Dictionary<string, object>();

        private IHandlerConfiguration _handlerConfiguration;
        private IRemoteQueryRegistry _remoteQueryRegistry;
        private IQueryContextFactory _queryContextFactory;

        public static INeedHandlers With()
        {
            return new QueryProcessorBuilder();
        }

        public INeedRemoteQueries Handlers(IHandlerConfiguration handlerConfiguration)
        {
            _handlerConfiguration = handlerConfiguration ?? throw new ArgumentNullException(nameof(handlerConfiguration));
            return this;
        }

        public INeedRemoteQueries Handlers(IQueryHandlerRegistry handlerRegistry, IQueryHandlerFactory handlerFactory, IQueryHandlerDecoratorFactory decoratorFactory)
        {
            if (handlerRegistry == null)
                throw new ArgumentNullException(nameof(handlerRegistry));
            if (handlerFactory == null)
                throw new ArgumentNullException(nameof(handlerFactory));
            if (decoratorFactory == null)
                throw new ArgumentNullException(nameof(decoratorFactory));

            _handlerConfiguration = new HandlerConfiguration(handlerRegistry, handlerFactory, decoratorFactory);
            return this;
        }

        public INeedRemoteQueries Handlers(IQueryHandlerRegistry handlerRegistry, Func<Type, IQueryHandler> handlerFactory, Func<Type, IQueryHandlerDecorator> decoratorFactory)
        {
            if (handlerRegistry == null)
                throw new ArgumentNullException(nameof(handlerRegistry));
            if (handlerFactory == null)
                throw new ArgumentNullException(nameof(handlerFactory));
            if (decoratorFactory == null)
                throw new ArgumentNullException(nameof(decoratorFactory));

            _handlerConfiguration = new HandlerConfiguration(handlerRegistry, new FactoryFuncWrapper(handlerFactory), new FactoryFuncWrapper(decoratorFactory));
            return this;
        }

        public INeedAQueryContext NoRemoteQueries()
        {
            _remoteQueryRegistry = new NullRemoteQueryRegistry();
            return this;
        }

        public INeedAQueryContext RemoteQueries(params IRemoteQueryRegistry[] registries)
        {
            _remoteQueryRegistry = new CompositeRemoteQueryRegistry(registries);
            return this;
        }

        public IBuildTheQueryProcessor QueryContextFactory(IQueryContextFactory queryContextFactory)
        {
            _queryContextFactory = queryContextFactory ?? throw new ArgumentNullException(nameof(queryContextFactory));
            return this;
        }

        public IBuildTheQueryProcessor InMemoryQueryContextFactory()
        {
            _queryContextFactory = new InMemoryQueryContextFactory();
            return this;
        }

        public IBuildTheQueryProcessor ContextBagItem(string key, object item)
        {
            // todo dupe check
            _contextBagData.Add(key, item);
            return this;
        }

        public IQueryProcessor Build()
        {
            return new QueryProcessor(_remoteQueryRegistry, _handlerConfiguration, _queryContextFactory, _contextBagData);
        }
    }
}