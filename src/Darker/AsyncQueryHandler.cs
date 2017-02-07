using System;
using System.Threading;
using System.Threading.Tasks;
using Darker.Logging;

namespace Darker
{
    public abstract class AsyncQueryHandler<TQuery, TResult> : IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private static readonly ILog _logger = LogProvider.For<AsyncQueryHandler<TQuery, TResult>>();

        public IQueryContext Context { get; set; }

        public virtual TResult Execute(TQuery query)
        {
            throw new NotImplementedException("Please derive from AsyncQueryHandler if you want to execute queries sync.");
        }

        public virtual TResult Fallback(TQuery query)
        {
            throw new NotImplementedException("Please derive from AsyncQueryHandler if you want to execute queries sync.");
        }

        public abstract Task<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default(CancellationToken));

        public virtual Task<TResult> FallbackAsync(TQuery query, CancellationToken cancellationToken = default(CancellationToken))
        {
            _logger.InfoFormat("Executing the default fallback implementation, returning default(TResult)");
            return Task.FromResult(default(TResult));
        }
    }
}