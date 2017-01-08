using System.Linq;
using System.Reflection;
using Darker.Decorators;
using LightInject;

namespace Darker.LightInject
{
    public sealed class HandlerSettings
    {
        private readonly ServiceContainer _container;
        private readonly IQueryHandlerRegistry _handlerRegistry;

        public HandlerSettings(ServiceContainer container, IQueryHandlerRegistry handlerRegistry)
        {
            _container = container;
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
                select new { Request = i.GetGenericArguments().First(), ResponseType = i.GetGenericArguments().ElementAt(1), Handler = t };

            foreach (var subscriber in subscribers)
            {
                _handlerRegistry.Register(subscriber.Request, subscriber.ResponseType, subscriber.Handler);
                _container.Register(subscriber.Handler);
            }

            return this;
        }

        public HandlerSettings WithDefaultDecorators()
        {
            _container.Register(typeof(RequestLoggingDecorator<,>));
            _container.Register(typeof(RetryableQueryDecorator<,>));
            _container.Register(typeof(FallbackPolicyDecorator<,>));

            return this;
        }
    }
}