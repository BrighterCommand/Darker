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
    }
}