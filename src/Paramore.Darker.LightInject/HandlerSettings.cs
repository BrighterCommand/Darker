using System.Linq;
using System.Reflection;
using LightInject;

namespace Paramore.Darker.LightInject
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
                from t in assembly.ExportedTypes
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
                _handlerRegistry.Register(subscriber.QueryType, subscriber.ResultType, subscriber.HandlerType);
                _container.Register(subscriber.HandlerType);
            }

            return this;
        }
    }
}