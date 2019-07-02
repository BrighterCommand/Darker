using System;
using System.Reflection;
using Paramore.Darker.Builder;

namespace Paramore.Darker.AspNetCore
{
    internal class ServiceCollectionDarkerHandlerBuilder : IDarkerHandlerBuilder
    {
        private readonly DarkerContextBag _contextBag;
        private readonly ServiceCollectionDecoratorRegistry _decoratorRegistry;
        private readonly ServiceCollectionHandlerRegistry _registry;

        public ServiceCollectionDarkerHandlerBuilder(ServiceCollectionHandlerRegistry registry,
            ServiceCollectionDecoratorRegistry decoratorRegistry, DarkerContextBag contextContextBag)
        {
            _registry = registry;
            _decoratorRegistry = decoratorRegistry;
            _contextBag = contextContextBag;
        }

        public IDarkerHandlerBuilder AddHandlersFromAssemblies(params Assembly[] assemblies)
        {
            if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));

            _registry.RegisterFromAssemblies(assemblies);

            return this;
        }

        public IDarkerHandlerBuilder AddHandlers(Action<IQueryHandlerRegistry> registerHandlers)
        {
            if (registerHandlers == null)
                throw new ArgumentNullException(nameof(registerHandlers));

            registerHandlers(_registry);

            return this;
        }

        public IQueryProcessorExtensionBuilder AddContextBagItem(string key, object item)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (item == null) throw new ArgumentNullException(nameof(item));

            _contextBag.Add(key, item);

            return this;
        }

        public IQueryProcessorExtensionBuilder RegisterDecorator(Type decoratorType)
        {
            if (decoratorType == null) throw new ArgumentNullException(nameof(decoratorType));

            _decoratorRegistry.Register(decoratorType);
            return this;
        }
    }
}