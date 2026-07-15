using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    internal sealed class ServiceCollectionHandlerRegistry : QueryHandlerRegistry
    {
        private readonly ServiceLifetime _lifetime;
        private readonly IServiceCollection _services;

        public ServiceCollectionHandlerRegistry(IServiceCollection services, ServiceLifetime lifetime)
        {
            _services = services;
            _lifetime = lifetime;
        }

        public override void Register(Type queryType, Type resultType, Type handlerType)
        {
            _services.TryAdd(new ServiceDescriptor(handlerType, handlerType, _lifetime));

            base.Register(queryType, resultType, handlerType);
        }

        public override void Register<TQuery, TResult>(
            Func<TQuery, IQueryContext, Type?> router,
            params Type[] candidateHandlerTypes)
        {
            foreach (var candidate in candidateHandlerTypes)
                _services.TryAdd(new ServiceDescriptor(candidate, candidate, _lifetime));

            base.Register<TQuery, TResult>(router, candidateHandlerTypes);
        }
    }
}