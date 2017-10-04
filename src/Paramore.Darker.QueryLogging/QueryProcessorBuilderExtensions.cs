using System;
using Newtonsoft.Json;
using Paramore.Darker.Builder;

namespace Paramore.Darker.QueryLogging
{
   public static class QueryProcessorBuilderExtensions
    {
        public static IBuildTheQueryProcessor JsonQueryLogging(this IBuildTheQueryProcessor builder, Action<JsonSerializerSettings> configure = null)
        {
            var queryProcessorBuilder = builder as QueryProcessorBuilder;
            if (queryProcessorBuilder == null)
                throw new NotSupportedException($"This extension method only supports the default {nameof(QueryProcessorBuilder)}.");

            AddJsonQueryLogging(queryProcessorBuilder, configure);

            return queryProcessorBuilder;
        }
        
        public static TBuilder AddJsonQueryLogging<TBuilder>(this TBuilder builder, Action<JsonSerializerSettings> settings = null)
            where TBuilder : IQueryProcessorExtensionBuilder
        {
            JsonSerializerSettings serializerSettings = null;

            if (settings != null)
            {
                serializerSettings = new JsonSerializerSettings();
                settings(serializerSettings);
            }

            builder.RegisterDecorator(typeof(QueryLoggingDecorator<,>));
            builder.AddContextBagItem(Constants.ContextBagKey, new NewtonsftJsonSerializer(serializerSettings));

            return builder;
        }
    }
}