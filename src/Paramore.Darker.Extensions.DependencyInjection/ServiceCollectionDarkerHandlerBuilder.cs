using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Builder;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    internal class ServiceCollectionDarkerHandlerBuilder : IDarkerHandlerBuilder
    {
        private readonly ServiceCollectionDecoratorRegistry _decoratorRegistry;
        private readonly ServiceCollectionHandlerRegistry _registry;
        private readonly ServiceCollectionHandlerRegistryAsync _registryAsync;
        private readonly ServiceCollectionStreamHandlerRegistry _registryStream;

        public IServiceCollection Services { get; }

        public ServiceCollectionDarkerHandlerBuilder(ServiceCollectionHandlerRegistry registry,
            ServiceCollectionHandlerRegistryAsync registryAsync,
            ServiceCollectionStreamHandlerRegistry registryStream,
            ServiceCollectionDecoratorRegistry decoratorRegistry,
            IServiceCollection services)
        {
            _registry = registry;
            _registryAsync = registryAsync;
            _registryStream = registryStream;
            _decoratorRegistry = decoratorRegistry;
            Services = services;
        }

        public IDarkerHandlerBuilder AddHandlersFromAssemblies(params Assembly[] assemblies)
        {
            if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));

            _registry.RegisterFromAssemblies(assemblies);
            _registryAsync.RegisterFromAssemblies(assemblies);
            _registryStream.RegisterFromAssemblies(assemblies);

            return this;
        }

        public IDarkerHandlerBuilder AddHandlers(Action<IQueryHandlerRegistry> registerHandlers)
        {
            if (registerHandlers == null)
                throw new ArgumentNullException(nameof(registerHandlers));

            registerHandlers(_registry);

            return this;
        }

        public IDarkerHandlerBuilder AddAsyncHandlers(Action<IQueryHandlerRegistryAsync> registerHandlers)
        {
            if (registerHandlers == null)
                throw new ArgumentNullException(nameof(registerHandlers));

            registerHandlers(_registryAsync);

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