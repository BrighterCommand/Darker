using System;
using System.Threading.Tasks;
using Darker.Logging;

namespace Darker
{
    public abstract class AsyncQueryHandler<TRequest, TResponse> : IQueryHandler<TRequest, TResponse>
        where TRequest : IQueryRequest<TResponse>
        where TResponse : IQueryResponse
    {
        private static readonly ILog _logger = LogProvider.For<AsyncQueryHandler<TRequest, TResponse>>();

        public IRequestContext Context { get; set; }

        public virtual TResponse Execute(TRequest request)
        {
            throw new NotImplementedException("Please derive from AsyncQueryHandler if you want to execute queries sync.");
        }

        public virtual TResponse Fallback(TRequest request)
        {
            throw new NotImplementedException("Please derive from AsyncQueryHandler if you want to execute queries sync.");
        }

        public abstract Task<TResponse> ExecuteAsync(TRequest request);

        public virtual Task<TResponse> FallbackAsync(TRequest request)
        {
            _logger.InfoFormat("Executing the default fallback implementation, returning default(TResponse)");
            return Task.FromResult(default(TResponse));
        }
    }
}