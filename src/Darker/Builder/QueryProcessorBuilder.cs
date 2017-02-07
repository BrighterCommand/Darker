using System;
using System.Collections.Generic;

namespace Darker.Builder
{
    public sealed class QueryProcessorBuilder : INeedHandlers, INeedAQueryContext, IBuildTheQueryProcessor
    {
        private readonly Dictionary<string, object> _contextBagData = new Dictionary<string, object>();

        private IHandlerConfiguration _handlerConfiguration;
        private IQueryContextFactory _queryContextFactory;

        public static INeedHandlers With()
        {
            return new QueryProcessorBuilder();
        }

        public INeedAQueryContext Handlers(IHandlerConfiguration handlerConfiguration)
        {
            _handlerConfiguration = handlerConfiguration ?? throw new ArgumentNullException(nameof(handlerConfiguration));
            return this;
        }

        public INeedAQueryContext Handlers(IQueryHandlerRegistry handlerRegistry, IQueryHandlerFactory handlerFactory, IQueryHandlerDecoratorFactory decoratorFactory)
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

        public INeedAQueryContext Handlers(IQueryHandlerRegistry handlerRegistry, Func<Type, object> handlerFactory, Func<Type, object> decoratorFactory)
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
            return new QueryProcessor(_handlerConfiguration, _queryContextFactory, _contextBagData);
        }
    }
}