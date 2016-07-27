using Darker.Logging;

namespace Darker
{
    public abstract class QueryHandler<TRequest, TResponse> : IQueryHandler<TRequest, TResponse>
        where TRequest : IQueryRequest<TResponse>
        where TResponse : IQueryResponse
    {
        private static readonly ILog _logger = LogProvider.For<QueryHandler<TRequest, TResponse>>();

        public IRequestContext Context { get; set; }

        public abstract TResponse Execute(TRequest request);

        public virtual TResponse Fallback(TRequest request)
        {
            _logger.InfoFormat("Executing the default fallback implementation, returning default(TResponse)");
            return default(TResponse);
        }
    }
}