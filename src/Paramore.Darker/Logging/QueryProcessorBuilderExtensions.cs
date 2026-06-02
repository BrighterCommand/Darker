using System;
using System.Text.Json;
using Paramore.Darker.Builder;
using Paramore.Darker.Logging.Handlers;

namespace Paramore.Darker.Logging
{
    public static class QueryProcessorBuilderExtensions
    {
        public static IBuildTheQueryProcessor JsonQueryLogging(this IBuildTheQueryProcessor builder, Action<JsonSerializerOptions> configure = null)
        {
            var queryProcessorBuilder = builder as QueryProcessorBuilder;
            if (queryProcessorBuilder == null)
                throw new NotSupportedException($"This extension method only supports the default {nameof(QueryProcessorBuilder)}.");

            return AddJsonQueryLogging(queryProcessorBuilder, configure);
        }

        public static TBuilder AddJsonQueryLogging<TBuilder>(this TBuilder builder, Action<JsonSerializerOptions> configure = null)
            where TBuilder : IQueryProcessorExtensionBuilder
        {
            configure?.Invoke(QueryLoggingJsonOptions.Options);

            builder.RegisterDecorator(typeof(QueryLoggingDecorator<,>));
            builder.RegisterDecorator(typeof(QueryLoggingDecoratorAsync<,>));

            return builder;
        }
    }
}
