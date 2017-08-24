using System;
using Paramore.Darker.Builder;
using SimpleInjector;

namespace Paramore.Darker.SimpleInjector
{
    public static class QueryProcessorBuilderExtensions
    {
        public static INeedAQueryContext SimpleInjectorHandlers(this INeedHandlers handlerBuilder, Container container, Action<HandlerSettings> settings = null)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            var factory = new SimpleInjectorHandlerFactory(container);
            var registry = new SimpleInjectorHandlerRegistry(container);
            var handlerSettings = new HandlerSettings(registry);
            settings?.Invoke(handlerSettings);

            return handlerBuilder.Handlers(registry, factory, registry, factory);
        }
    }
}