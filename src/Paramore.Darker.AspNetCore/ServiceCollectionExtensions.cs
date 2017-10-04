using System;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Builder;

namespace Paramore.Darker.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static IDarkerHandlerBuilder AddDarker(this IServiceCollection services, Action<DarkerOptions> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var options = new DarkerOptions();
            configure?.Invoke(options);

            var registry = new AspNetHandlerRegistry(services);
            var factory = new AspNetHandlerFactory(services);

            var builder = QueryProcessorBuilder.With()
                .Handlers(registry, factory, registry, factory)
                .QueryContextFactory(options.QueryContextFactory);

            var queryProcessor = builder.Build();

            services.AddSingleton(queryProcessor);

            return new AspNetHandlerBuilder(services, registry, (QueryProcessorBuilder)builder);
        }
    }
}