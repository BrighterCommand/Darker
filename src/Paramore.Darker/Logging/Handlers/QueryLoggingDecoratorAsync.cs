using System;
using System.Diagnostics;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Policies.Handlers;

namespace Paramore.Darker.Logging.Handlers
{
    public class QueryLoggingDecoratorAsync<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private static readonly ILogger Logger = ApplicationLogging.CreateLogger<QueryLoggingDecoratorAsync<TQuery, TResult>>();

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            // nothing to do
        }

        public async Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var sw = Stopwatch.StartNew();

            var queryName = query.GetType().Name;
            Logger.LogInformation("Executing async query {QueryName}: {Query}", queryName, Serialize(query));

            var result = await next(query, cancellationToken).ConfigureAwait(false);

            var withFallback = Context.Bag.ContainsKey(FallbackPolicyDecoratorAsync<TQuery, TResult>.CauseOfFallbackException)
                ? " (with fallback)"
                : string.Empty;

            Logger.LogInformation("Async execution of query {QueryName} completed in {Elapsed}ms" + withFallback, queryName, sw.Elapsed.TotalMilliseconds);

            return result;
        }

        // Runtime-type overload: the pipeline closes this decorator over IQuery<TResult>, so the generic
        // Serialize<T>(value, options) overload would serialise the bare interface and emit "{}".
        // value.GetType() restores Newtonsoft's runtime-type behaviour and composes with a
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
