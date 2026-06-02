using System;
using System.Text.Json;
using Paramore.Darker.Logging;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    public static class QueryLoggingDIExtensions
    {
        public static IDarkerHandlerBuilder AddJsonQueryLogging(this IDarkerHandlerBuilder builder, Action<JsonSerializerOptions> configure = null)
            => QueryProcessorBuilderExtensions.AddJsonQueryLogging<IDarkerHandlerBuilder>(builder, configure);
    }
}
