using System;
using System.Collections.Generic;
using Paramore.Darker.Decorators;

namespace Paramore.Darker.Builder
{
    public sealed class QueryProcessorBuilder : INeedHandlers, INeedAQueryContext, IBuildTheQueryProcessor, IQueryProcessorExtensionBuilder
    {
        private readonly Dictionary<string, object> _contextBagData = new Dictionary<string, object>();

        private IHandlerConfiguration _handlerConfiguration;
        private IQueryContextFactory _queryContextFactory;

        private QueryProcessorBuilder()
        {
        }
        
        public static INeedHandlers With()
        {
            return new QueryProcessorBuilder();
        }

        public INeedAQueryContext Handlers(IHandlerConfiguration handlerConfiguration)
        {
            _handlerConfiguration = handlerConfiguration ?? throw new ArgumentNullException(nameof(handlerConfiguration));
            return this;
        }

        public INeedAQueryContext Handlers(IQueryHandlerRegistry handlerRegistry, IQueryHandlerFactory handlerFactory,
            IQueryHandlerDecoratorRegistry decoratorRegistry, IQueryHandlerDecoratorFactory decoratorFactory)
        {
            if (handlerRegistry == null)
                throw new ArgumentNullException(nameof(handlerRegistry));
            if (handlerFactory == null)
                throw new ArgumentNullException(nameof(handlerFactory));
            if (decoratorRegistry == null)
                throw new ArgumentNullException(nameof(decoratorRegistry));
            if (decoratorFactory == null)
                throw new ArgumentNullException(nameof(decoratorFactory));

            _handlerConfiguration = new HandlerConfiguration(
                handlerRegistry,
                handlerFactory,
                decoratorRegistry,
                decoratorFactory);

            return this;
        }

        public INeedAQueryContext Handlers(IQueryHandlerRegistry handlerRegistry, Func<Type, IQueryHandler> handlerFactory,
            Action<Type> decoratorRegistry, Func<Type, IQueryHandlerDecorator> decoratorFactory)
        {
            if (handlerRegistry == null)
                throw new ArgumentNullException(nameof(handlerRegistry));
            if (handlerFactory == null)
                throw new ArgumentNullException(nameof(handlerFactory));
            if (decoratorRegistry == null)
                throw new ArgumentNullException(nameof(decoratorRegistry));
            if (decoratorFactory == null)
                throw new ArgumentNullException(nameof(decoratorFactory));

            _handlerConfiguration = new HandlerConfiguration(
                handlerRegistry,
                new FactoryFuncWrapper(handlerFactory),
                new RegistryActionWrapper(decoratorRegistry),
                new FactoryFuncWrapper(decoratorFactory));
            
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

        public IQueryProcessorExtensionBuilder AddContextBagItem(string key, object item)
        {
            // todo dupe check
            _contextBagData.Add(key, item);
            return this;
        }

        public IQueryProcessorExtensionBuilder RegisterDecorator(Type decoratorType)
        {
            _handlerConfiguration.DecoratorRegistry.Register(decoratorType);
            return this;
        }

        public IQueryProcessor Build()
        {
            RegisterDefaultDecorators();
            return new QueryProcessor(_handlerConfiguration, _queryContextFactory, _contextBagData);
        }

        private void RegisterDefaultDecorators()
        {
            RegisterDecorator(typeof(FallbackPolicyDecorator<,>));
        }
    }
}