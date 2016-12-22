using System;
using Darker.Builder;
using Newtonsoft.Json;

namespace Darker.Serialization.NewtonsoftJson
{
    public static class QueryProcessorBuilderExtensions
    {
        public static IBuildTheQueryProcessor NewtonsoftJsonSerializer(this INeedASerializer serializerBuilder, Action<JsonSerializerSettings> settings = null)
        {
            var builder = serializerBuilder as QueryProcessorBuilder;
            if (builder == null)
                throw new NotSupportedException($"This extension method only supports the default {nameof(QueryProcessorBuilder)}.");

            JsonSerializerSettings serializerSettings = null;

            if (settings != null)
            {
                serializerSettings = new JsonSerializerSettings();
                settings(serializerSettings);
            }

            return builder.Serializer(new NewtonsftJsonSerializer(serializerSettings));
        }
    }
}