using System;
using Newtonsoft.Json;
using Paramore.Darker.Builder;

namespace Paramore.Darker.QueryLogging
{
    public static class Constants
    {
        public const string ContextBagKey = "Darker.JsonSerializer";
    }

    public static class QueryProcessorBuilderExtensions
    {
        public static IBuildTheQueryProcessor JsonQueryLogging(this IBuildTheQueryProcessor lastStageBuilder, Action<JsonSerializerSettings> settings = null)
        {
            var builder = lastStageBuilder.ToQueryProcessorBuilder();

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