using System;

namespace Darker.Builder
{
    public static class QueryProcessorBuilderExtensions
    {
        /// <summary>
        /// Converts the last builder stage to the full builder or throws if the builder instance is not a QueryProcessorBuilder.
        /// This is useful for extending the builder with custom methods.
        /// </summary>
        /// <param name="lastStageBuilder">The builder instance.</param>
        /// <returns></returns>
        public static QueryProcessorBuilder ToQueryProcessorBuilder(this IBuildTheQueryProcessor lastStageBuilder)
        {
            var builder = lastStageBuilder as QueryProcessorBuilder;
            if (builder == null)
                throw new NotSupportedException($"This extension method only supports the default {nameof(QueryProcessorBuilder)}.");

            return builder;
        }
    }
}