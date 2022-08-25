using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Logging;

namespace Paramore.Darker
{
    public abstract class QueryHandler<TQuery, TResult> : IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private static readonly ILogger _logger = ApplicationLogging.CreateLogger<QueryHandler<TQuery, TResult>>();

        public IQueryContext Context { get; set; }

        public abstract TResult Execute(TQuery query);

        public virtual TResult Fallback(TQuery query)
        {
            _logger.LogInformation("Executing the default fallback implementation, returning default(TResult)");
            return default(TResult);
        }

        public virtual Task<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException($"Please derive from {nameof(QueryHandlerAsync<TQuery, TResult>)} if you want to execute queries asynchronously.");
        }

        public virtual Task<TResult> FallbackAsync(TQuery query, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException($"Please derive from {nameof(QueryHandlerAsync<TQuery, TResult>)} if you want to execute queries asynchronously.");
        }
    }
}