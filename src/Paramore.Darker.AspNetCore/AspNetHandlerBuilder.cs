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
        private readonly QueryProcessorBuilder _builder;

        public AspNetHandlerBuilder(IServiceCollection services, AspNetHandlerRegistry registry, QueryProcessorBuilder builder)
        {
            Services = services;
            _registry = registry;
            _builder = builder;
        }

        public IQueryProcessorAspNetExtensionBuilder AddHandlersFromAssemblies(params Assembly[] assemblies)
        {
            _registry.RegisterFromAssemblies(assemblies);
            return new AspNetBuilderWrapper(Services, _builder);
        }

        public IQueryProcessorAspNetExtensionBuilder AddHandlers(Action<IQueryHandlerRegistry> registerHandlers)
        {
            if (registerHandlers == null)
                throw new ArgumentNullException(nameof(registerHandlers));

            registerHandlers(_registry);

            return new AspNetBuilderWrapper(Services, _builder);
        }
    }
}