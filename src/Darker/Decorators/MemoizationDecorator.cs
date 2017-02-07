using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darker.Logging;

// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable HeuristicUnreachableCode
namespace Darker.Decorators
{
    /// <summary>
    /// Just a proof of concept, please don't use in prod
    /// </summary>
    public class MemoizationDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private static readonly ILog _logger = LogProvider.GetLogger(typeof(MemoizationDecorator<,>));
        private static readonly IDictionary<TQuery, TResult> _cache = new Dictionary<TQuery, TResult>(); 

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            // nothing to do
        }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
        {
            if (query is IEquatable<TQuery> == false)
                throw new InvalidOperationException("Memoization is only supported for queries that implement IEquatable<TQuery>");

            if (_cache.ContainsKey(query))
            {
                _logger.InfoFormat("Returning cached result for {Query}", query);
                return _cache[query];
            }

            var result = next(query);

            _logger.InfoFormat("Adding result for {Query} to cache", query);
            _cache.Add(query, result);

            return result;
        }

        public Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}