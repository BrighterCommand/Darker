using System;
using Darker.Builder;
using Newtonsoft.Json;

namespace Darker.RequestLogging
{
    public static class Constants
    {
        public const string ContextBagKey = "Darker.JsonSerializer";
    }

    public static class QueryProcessorBuilderExtensions
    {
        public static IBuildTheQueryProcessor JsonRequestLogging(this IBuildTheQueryProcessor lastStageBuilder, Action<JsonSerializerSettings> settings = null)
        {
            var builder = lastStageBuilder as QueryProcessorBuilder;
            if (builder == null)
                throw new NotSupportedException($"This extension method only supports the default {nameof(QueryProcessorBuilder)}.");

            JsonSerializerSettings serializerSettings = null;

            if (settings != null)
            {
                serializerSettings = new JsonSerializerSettings();
                settings(serializerSettings);
            }

            return builder.ContextBagItem(Constants.ContextBagKey, new NewtonsftJsonSerializer(serializerSettings));
        }
    }
}