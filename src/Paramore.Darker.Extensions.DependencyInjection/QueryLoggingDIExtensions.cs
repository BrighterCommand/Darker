using System;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Paramore.Darker.QueryLogging;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    public static class QueryLoggingDIExtensions
    {
        public static IDarkerHandlerBuilder AddJsonQueryLogging(this IDarkerHandlerBuilder builder, Action<JsonSerializerSettings> configure = null)
        {
            var settings = new JsonSerializerSettings();
            configure?.Invoke(settings);

            QueryProcessorBuilderExtensions.AddJsonQueryLogging(builder, configure);
            builder.Services.AddSingleton(settings);

            return builder;
        }
    }
}
