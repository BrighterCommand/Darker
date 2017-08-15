using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker;
using Paramore.Darker.Builder;

namespace SampleApi
{
    public static class ServiceCollectionExtensions
    {
        public static IQueryProcessorExtensionBuilder AddDarker(this IServiceCollection services)
        {           
            // todo: executing assembly isn't good enough, need to be able to configure assemblies
            var assembly = Assembly.GetExecutingAssembly();
            
            // todo: must be configurable
            var queryContextFactory = new InMemoryQueryContextFactory();
            
            var handlerRegistry = new QueryHandlerRegistry();
            
            new HandlerSettings(services, handlerRegistry)
                .WithQueriesAndHandlersFromAssembly(assembly);

            var factory = new HandlerFactory(services);
            var decoratorRegistry = new DecoratorRegistry(services);
            var handlerConfiguration = new HandlerConfiguration(handlerRegistry, decoratorRegistry, factory);

            var builder = QueryProcessorBuilder.With()
                .Handlers(handlerConfiguration)
                .QueryContextFactory(queryContextFactory);

            var queryProcessor = builder.Build();
            
            services.AddSingleton(queryProcessor);
            
            return (QueryProcessorBuilder)builder;
        }
        
        private sealed class HandlerSettings
        {
            private readonly IServiceCollection _services;
            private readonly IQueryHandlerRegistry _handlerRegistry;

            public HandlerSettings(IServiceCollection services, IQueryHandlerRegistry handlerRegistry)
            {
                _services = services;
                _handlerRegistry = handlerRegistry;
            }

            public HandlerSettings WithQueriesAndHandlersFromAssembly(Assembly assembly)
            {
                var subscribers =
                    from t in assembly.GetExportedTypes()
                    let ti = t.GetTypeInfo()
                    where ti.IsClass && !ti.IsAbstract && !ti.IsInterface
                    from i in t.GetInterfaces()
                    where i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)
                    select new { QueryType = i.GetGenericArguments().First(), ResultType = i.GetGenericArguments().ElementAt(1), HandlerType = t };

                foreach (var subscriber in subscribers)
                {
                    _handlerRegistry.Register(subscriber.QueryType, subscriber.ResultType, subscriber.HandlerType);
                    _services.AddTransient(subscriber.HandlerType);
                }

                return this;
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
