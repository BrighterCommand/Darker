using System.Linq;
using System.Reflection;
using SimpleInjector;

namespace Darker.SimpleInjector
{
    public sealed class HandlerSettings
    {
        private readonly Container _container;
        private readonly IQueryHandlerRegistry _handlerRegistry;

        public HandlerSettings(Container container, IQueryHandlerRegistry handlerRegistry)
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
                select new { Request = i.GetGenericArguments().First(), ResultType = i.GetGenericArguments().ElementAt(1), Handler = t };

            foreach (var subscriber in subscribers)
            {
                _handlerRegistry.Register(subscriber.Request, subscriber.ResultType, subscriber.Handler);
                _container.Register(subscriber.Handler);
            }

            return this;
        }
    }
}