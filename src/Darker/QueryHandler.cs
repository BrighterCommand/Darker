using System;
using System.Threading;
using System.Threading.Tasks;
using Darker.Logging;

namespace Darker
{
    public abstract class QueryHandler<TQuery, TResult> : IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private static readonly ILog _logger = LogProvider.For<QueryHandler<TQuery, TResult>>();

        public IQueryContext Context { get; set; }

        public abstract TResult Execute(TQuery query);

        public virtual TResult Fallback(TQuery query)
        {
            _logger.InfoFormat("Executing the default fallback implementation, returning default(TResult)");
            return default(TResult);
        }

        public virtual Task<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException("Please derive from AsyncQueryHandler if you want to execute queries async.");
        }

        public virtual Task<TResult> FallbackAsync(TQuery query, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException("Please derive from AsyncQueryHandler if you want to execute queries async.");
        }
    }
}