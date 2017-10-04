using System;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Builder;

namespace Paramore.Darker.AspNetCore
{
    internal class AspNetBuilderWrapper : IQueryProcessorAspNetExtensionBuilder
    {
        public IServiceCollection Services { get; }

        private readonly QueryProcessorBuilder _builder;

        public AspNetBuilderWrapper(IServiceCollection services, QueryProcessorBuilder builder)
        {
            Services = services;
            _builder = builder;
        }

        public IQueryProcessorExtensionBuilder AddContextBagItem(string key, object item)
        {
            return _builder.AddContextBagItem(key, item);
        }

        public IQueryProcessorExtensionBuilder RegisterDecorator(Type decoratorType)
        {
            return _builder.RegisterDecorator(decoratorType);
        }
    }
}