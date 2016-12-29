using System;
using System.Collections.Generic;

namespace Darker.Builder
{
    public sealed class QueryProcessorBuilder : INeedHandlers, INeedARequestContext, IBuildTheQueryProcessor
    {
        private IHandlerConfiguration _handlerConfiguration;
        private IRequestContextFactory _requestContextFactory;
        private Dictionary<string, object> _contextBagData;

        private QueryProcessorBuilder()
        {
            _contextBagData = new Dictionary<string, object>();
        }

        public static INeedHandlers With()
        {
            return new QueryProcessorBuilder();
        }

        public INeedARequestContext Handlers(IHandlerConfiguration handlerConfiguration)
        {
            _handlerConfiguration = handlerConfiguration ?? throw new ArgumentNullException(nameof(handlerConfiguration));
            return this;
        }

        public INeedARequestContext Handlers(IQueryHandlerRegistry handlerRegistry, IQueryHandlerFactory handlerFactory, IQueryHandlerDecoratorFactory decoratorFactory)
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

        public INeedARequestContext Handlers(IQueryHandlerRegistry handlerRegistry, Func<Type, object> handlerFactory, Func<Type, object> decoratorFactory)
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

        public IBuildTheQueryProcessor RequestContextFactory(IRequestContextFactory requestContextFactory)
        {
            _requestContextFactory = requestContextFactory ?? throw new ArgumentNullException(nameof(requestContextFactory));
            return this;
        }

        public IBuildTheQueryProcessor InMemoryRequestContextFactory()
        {
            _requestContextFactory = new InMemoryRequestContextFactory();
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
            return new QueryProcessor(_handlerConfiguration, _requestContextFactory, _contextBagData);
        }
    }
}