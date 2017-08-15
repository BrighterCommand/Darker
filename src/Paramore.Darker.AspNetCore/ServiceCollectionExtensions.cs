using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Builder;

namespace Paramore.Darker.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static IQueryProcessorExtensionBuilder AddDarker(this IServiceCollection services, Action<DarkerOptions> configure)
        {
            var options = new DarkerOptions();
            configure(options);

            var handlerRegistry = new QueryHandlerRegistry();
            var factory = new HandlerFactory(services);
            var decoratorRegistry = new DecoratorRegistry(services);
            var handlerConfiguration = new HandlerConfiguration(handlerRegistry, decoratorRegistry, factory);

            RegisterQueriesAndHandlersFromAssemblies(services, handlerRegistry, options.DiscoverQueriesAndHandlersFromAssemblies);

            var builder = QueryProcessorBuilder.With()
                .Handlers(handlerConfiguration)
                .QueryContextFactory(options.QueryContextFactory);

            var queryProcessor = builder.Build();

            services.AddSingleton(queryProcessor);

            return (QueryProcessorBuilder)builder;
        }

        private static void RegisterQueriesAndHandlersFromAssemblies(IServiceCollection services, IQueryHandlerRegistry handlerRegistry, IEnumerable<Assembly> assemblies)
        {
            var subscribers =
                from t in assemblies.SelectMany(a => a.ExportedTypes)
                let ti = t.GetTypeInfo()
                where ti.IsClass && !ti.IsAbstract && !ti.IsInterface
                from i in t.GetTypeInfo().ImplementedInterfaces
                where i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)
                select new
                {
                    QueryType = i.GenericTypeArguments.ElementAt(0),
                    ResultType = i.GenericTypeArguments.ElementAt(1),
                    HandlerType = t
                };

            foreach (var subscriber in subscribers)
            {
                handlerRegistry.Register(subscriber.QueryType, subscriber.ResultType, subscriber.HandlerType);
                services.AddTransient(subscriber.HandlerType);
            }
        }

        private sealed class HandlerConfiguration : IHandlerConfiguration
        {
            public IQueryHandlerRegistry HandlerRegistry { get; }
            public IQueryHandlerFactory HandlerFactory { get; }
            public IQueryHandlerDecoratorRegistry DecoratorRegistry { get; }
            public IQueryHandlerDecoratorFactory DecoratorFactory { get; }

            public HandlerConfiguration(IQueryHandlerRegistry handlerRegistry, IQueryHandlerDecoratorRegistry decoratorRegistry, HandlerFactory factory)
            {
                HandlerRegistry = handlerRegistry;
                HandlerFactory = factory;
                DecoratorRegistry = decoratorRegistry;
                DecoratorFactory = factory;
            }
        }

        private sealed class DecoratorRegistry : IQueryHandlerDecoratorRegistry
        {
            private readonly IServiceCollection _services;

            public DecoratorRegistry(IServiceCollection services)
            {
                _services = services;
            }

            public void Register(Type decoratorType)
            {
                _services.AddTransient(decoratorType);
            }
        }

        private sealed class HandlerFactory : IQueryHandlerFactory, IQueryHandlerDecoratorFactory
        {
            private readonly IServiceCollection _services;

            // todo what's the perf implication of building per call?
            private IServiceProvider _serviceProvider => _services.BuildServiceProvider();

            public HandlerFactory(IServiceCollection services)
            {
                _services = services;
            }

            IQueryHandler IQueryHandlerFactory.Create(Type handlerType)
            {
                return (IQueryHandler)_serviceProvider.GetService(handlerType);
            }

            void IQueryHandlerFactory.Release(IQueryHandler handler)
            {
                // no op
            }

            T IQueryHandlerDecoratorFactory.Create<T>(Type decoratorType)
            {
                return (T)_serviceProvider.GetService(decoratorType);
            }

            void IQueryHandlerDecoratorFactory.Release<T>(T handler)
            {
                // no op
            }
        }
    }
}
