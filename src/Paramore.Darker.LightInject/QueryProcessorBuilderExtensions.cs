using System;
using LightInject;
using Paramore.Darker.Builder;

namespace Paramore.Darker.LightInject
{
    public static class QueryProcessorBuilderExtensions
    {
        public static INeedAQueryContext LightInjectHandlers(this INeedHandlers handlerBuilder, ServiceContainer container, Action<HandlerSettings> settings = null)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            var factory = new LightInjectHandlerFactory(container);
            var registry = new LightInjectHandlerRegistry(container);
            var handlerSettings = new HandlerSettings(registry);
            settings?.Invoke(handlerSettings);

            return handlerBuilder.Handlers(registry, factory, registry, factory);
        }
    }
}