using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Logging;

namespace Paramore.Darker
{
    public abstract class QueryHandlerAsync<TQuery, TResult> : IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private static readonly ILogger _logger = ApplicationLogging.CreateLogger<QueryHandlerAsync<TQuery, TResult>>();

        public IQueryContext Context { get; set; }

        public virtual TResult Execute(TQuery query)
        {
            throw new NotImplementedException($"Please derive from {nameof(QueryHandler<TQuery, TResult>)} if you want to execute queries synchronously.");
        }

        public virtual TResult Fallback(TQuery query)
        {
            throw new NotImplementedException($"Please derive from {nameof(QueryHandler<TQuery, TResult>)} if you want to execute queries synchronously.");
        }

        public abstract Task<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default(CancellationToken));

        public virtual Task<TResult> FallbackAsync(TQuery query, CancellationToken cancellationToken = default(CancellationToken))
        {
            _logger.LogInformation("Executing the default fallback implementation, returning default(TResult)");
            return Task.FromResult(default(TResult));
        }
    }
}