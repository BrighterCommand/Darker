using System;
using System.Diagnostics;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Policies.Handlers;

namespace Paramore.Darker.Logging.Handlers
{
    public class QueryLoggingDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private static readonly ILogger Logger = ApplicationLogging.CreateLogger<QueryLoggingDecorator<TQuery, TResult>>();

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            // nothing to do
        }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
        {
            var sw = Stopwatch.StartNew();

            var queryName = query.GetType().Name;
            Logger.LogInformation("Executing query {QueryName}: {Query}", queryName, Serialize(query));

            var result = next(query);

            var withFallback = Context.Bag.ContainsKey(FallbackPolicyDecorator<TQuery, TResult>.CauseOfFallbackException)
                ? " (with fallback)"
                : string.Empty;

            Logger.LogInformation("Execution of query {QueryName} completed in {Elapsed}ms" + withFallback, queryName, sw.Elapsed.TotalMilliseconds);

            return result;
        }

        // The pipeline closes this decorator over IQuery<TResult> (PipelineBuilder closes the open
        // generic with typeof(IQuery<TResult>)), so the runtime-type overload is required: the generic
        // Serialize<T>(value, options) overload would serialise the bare IQuery<TResult> interface and
        // emit "{}". value.GetType() restores Newtonsoft's runtime-type behaviour and composes with a
        // source-generated TypeInfoResolver under AOT.
#if NET8_0_OR_GREATER
        [UnconditionalSuppressMessage(
            "Trimming", "IL2026:RequiresUnreferencedCodeAttribute",
            Justification = "Consumers supply their own JsonSerializerOptions. AOT/trim-safe usage is documented as the consumer responsibility (NFR2). Source-gen TypeInfoResolver is the supported escape hatch.")]
        [UnconditionalSuppressMessage(
            "AOT", "IL3050:RequiresDynamicCodeAttribute",
            Justification = "Same as IL2026 — call site is unavoidable without erasing the public Serialize API. Consumers supply source-gen TypeInfoResolver for full AOT safety.")]
#endif
        private string Serialize<T>(T value) =>
            JsonSerializer.Serialize(value, value.GetType(), QueryLoggingJsonOptions.Options);
    }
}
