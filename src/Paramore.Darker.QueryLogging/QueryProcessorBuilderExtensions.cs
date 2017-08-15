using System;
using Newtonsoft.Json;
using Paramore.Darker.Builder;

namespace Paramore.Darker.QueryLogging
{
   public static class QueryProcessorBuilderExtensions
    {
        public static IBuildTheQueryProcessor JsonQueryLogging(this IBuildTheQueryProcessor builder, Action<JsonSerializerSettings> settings = null)
        {
            var queryProcessorBuilder = builder as QueryProcessorBuilder;
            if (queryProcessorBuilder == null)
                throw new NotSupportedException($"This extension method only supports the default {nameof(QueryProcessorBuilder)}.");

            AddJsonQueryLogging(queryProcessorBuilder, settings);

            return queryProcessorBuilder;
        }
        
        public static IQueryProcessorExtensionBuilder AddJsonQueryLogging(this IQueryProcessorExtensionBuilder builder, Action<JsonSerializerSettings> settings = null)
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