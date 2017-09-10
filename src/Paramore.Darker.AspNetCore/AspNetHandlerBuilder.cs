using System;
using System.Reflection;
using Paramore.Darker.Builder;

namespace Paramore.Darker.AspNetCore
{
    internal class AspNetHandlerBuilder : IDarkerHandlerBuilder
    {
        private readonly AspNetHandlerRegistry _registry;
        private readonly QueryProcessorBuilder _builder;

        public AspNetHandlerBuilder(AspNetHandlerRegistry registry, QueryProcessorBuilder builder)
        {
            _registry = registry;
            _builder = builder;
        }

        public IQueryProcessorExtensionBuilder AddHandlersFromAssemblies(params Assembly[] assemblies)
        {
            _registry.RegisterFromAssemblies(assemblies);
            return _builder;
        }

        public IQueryProcessorExtensionBuilder AddHandlers(Action<IQueryHandlerRegistry> registerHandlers)
        {
            if (registerHandlers == null)
                throw new ArgumentNullException(nameof(registerHandlers));

            registerHandlers(_registry);

            return _builder;
        }
    }
}