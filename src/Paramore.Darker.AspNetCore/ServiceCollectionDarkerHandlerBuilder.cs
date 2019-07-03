using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Builder;

namespace Paramore.Darker.AspNetCore
{
    internal class ServiceCollectionDarkerHandlerBuilder : IDarkerHandlerBuilder
    {
        public IServiceCollection Services { get; }

        private readonly ServiceCollectionHandlerRegistry _registry;

        public ServiceCollectionDarkerHandlerBuilder(IServiceCollection services, ServiceCollectionHandlerRegistry registry)
        {
            Services = services;
            _registry = registry;
        }

        public IDarkerHandlerBuilder AddHandlersFromAssemblies(params Assembly[] assemblies)
        {
            _registry.RegisterFromAssemblies(assemblies);

            return this;
           // return new AspNetBuilderWrapper(Services, _registry);
        }

        public IDarkerHandlerBuilder AddHandlers(Action<IQueryHandlerRegistry> registerHandlers)
        {
            if (registerHandlers == null)
                throw new ArgumentNullException(nameof(registerHandlers));

            registerHandlers(_registry);


            return this;
            //return new AspNetBuilderWrapper(Services, _registry);
        }

        public IQueryProcessorExtensionBuilder AddContextBagItem(string key, object item)
        {
            throw new NotImplementedException();
        }

        public IQueryProcessorExtensionBuilder RegisterDecorator(Type decoratorType)
        { 
            _registry.Register(decoratorType);
            return this;
        }
    }
}