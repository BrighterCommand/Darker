using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Decorators;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Logging;

namespace Paramore.Darker.QueryLogging
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
            Logger.LogInformation("Executing query {QueryName}: {Query}", queryName, GetSerializer().Serialize(query));

            var result = next(query);

            var withFallback = Context.Bag.ContainsKey(FallbackPolicyDecorator<TQuery, TResult>.CauseOfFallbackException)
                ? " (with fallback)"
                : string.Empty;

            Logger.LogInformation("Execution of query {QueryName} completed in {Elapsed}ms" + withFallback, queryName, sw.Elapsed.TotalMilliseconds);

            return result;
        }

        private NewtonsoftJsonSerializer GetSerializer()
        {
            if (!Context.Bag.ContainsKey(Constants.ContextBagKey))
                throw new ConfigurationException($"Serializer does not exist in context bag with key {Constants.ContextBagKey}.");

            var serializer = Context.Bag[Constants.ContextBagKey] as NewtonsoftJsonSerializer;
            if (serializer == null)
                throw new ConfigurationException($"The serializer in the context bag (with key {Constants.ContextBagKey}) must be of type {nameof(NewtonsoftJsonSerializer)}, but is {Context.Bag[Constants.ContextBagKey].GetType()}.");

            return serializer;
        }
    }
}