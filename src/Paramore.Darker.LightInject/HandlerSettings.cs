using System.Reflection;

namespace Paramore.Darker.LightInject
{
    public sealed class HandlerSettings
    {
        private readonly LightInjectHandlerRegistry _registry;

        internal HandlerSettings(LightInjectHandlerRegistry registry)
        {
            _registry = registry;
        }

        public HandlerSettings WithQueriesAndHandlersFromAssembly(Assembly assembly)
        {
            _registry.RegisterFromAssemblies(new[] { assembly });
            return this;
        }
    }
}