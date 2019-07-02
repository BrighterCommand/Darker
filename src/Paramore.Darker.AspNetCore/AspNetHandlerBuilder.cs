using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Builder;

namespace Paramore.Darker.AspNetCore
{
    internal class AspNetHandlerBuilder : IDarkerHandlerBuilder
    {
        public IServiceCollection Services { get; }

        private readonly AspNetHandlerRegistry _registry;

        public AspNetHandlerBuilder(IServiceCollection services, AspNetHandlerRegistry registry)
        {
            Services = services;
            _registry = registry;
        }

        public IQueryProcessorAspNetExtensionBuilder AddHandlersFromAssemblies(params Assembly[] assemblies)
        {
            _registry.RegisterFromAssemblies(assemblies);
            return new AspNetBuilderWrapper(Services);
        }

        public IQueryProcessorAspNetExtensionBuilder AddHandlers(Action<IQueryHandlerRegistry> registerHandlers)
        {
            if (registerHandlers == null)
                throw new ArgumentNullException(nameof(registerHandlers));

            registerHandlers(_registry);

            return new AspNetBuilderWrapper(Services);
        }
    }
}