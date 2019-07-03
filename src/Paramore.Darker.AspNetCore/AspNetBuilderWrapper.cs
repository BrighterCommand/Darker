using System;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Builder;

namespace Paramore.Darker.AspNetCore
{
    internal class AspNetBuilderWrapper : IQueryProcessorAspNetExtensionBuilder
    {
        private readonly ServiceCollectionHandlerRegistry _registry;
        public IServiceCollection Services { get; }

        private readonly QueryProcessorBuilder _builder;

        //public AspNetBuilderWrapper(IServiceCollection services, QueryProcessorBuilder builder, )
        //{
        //    Services = services;
        //    _builder = builder;
        //}

        public AspNetBuilderWrapper(IServiceCollection services, ServiceCollectionHandlerRegistry registry)
        {
            _registry = registry;
            Services = services;
        }

        public IQueryProcessorExtensionBuilder AddContextBagItem(string key, object item)
        {
            _registry.
            return _builder.AddContextBagItem(key, item);
        }

        public IQueryProcessorExtensionBuilder RegisterDecorator(Type decoratorType)
        {
            _registry.Register(decoratorType);
            
        }
    }
}