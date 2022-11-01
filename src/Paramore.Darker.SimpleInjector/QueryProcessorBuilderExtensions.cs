using System;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Builder;
using Paramore.Darker.Logging;
using SimpleInjector;

namespace Paramore.Darker.SimpleInjector
{
    public static class QueryProcessorBuilderExtensions
    {
        public static INeedAQueryContext SimpleInjectorHandlers(this INeedHandlers handlerBuilder, Container container, Action<HandlerSettings> settings = null)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));


            container.RegisterInitializer<ILoggerFactory>(loggerFactory =>
            {
                ApplicationLogging.LoggerFactory = loggerFactory;
            });

            var factory = new SimpleInjectorHandlerFactory(container);
            var registry = new SimpleInjectorHandlerRegistry(container);
            var handlerSettings = new HandlerSettings(registry);
            settings?.Invoke(handlerSettings);

            return handlerBuilder.Handlers(registry, factory, registry, factory);
        }
    }
}