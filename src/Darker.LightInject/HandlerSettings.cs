using System.Linq;
using System.Reflection;
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
                select new { QueryType = i.GetGenericArguments().First(), ResultType = i.GetGenericArguments().ElementAt(1), HandlerType = t };

            foreach (var subscriber in subscribers)
            {
                _handlerRegistry.Register(subscriber.QueryType, subscriber.ResultType, subscriber.HandlerType);
                _container.Register(subscriber.HandlerType);
            }

            return this;
        }
    }
}