using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Darker.Logging;

// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable HeuristicUnreachableCode
namespace Darker.Decorators
{
    /// <summary>
    /// Just a proof of concept, please don't use in prod
    /// </summary>
    public class MemoizationDecorator<TRequest, TResponse> : IQueryHandlerDecorator<TRequest, TResponse>
        where TRequest : IQueryRequest<TResponse>
        where TResponse : IQueryResponse
    {
        private static readonly ILog _logger = LogProvider.GetLogger(typeof(MemoizationDecorator<,>));
        private static readonly IDictionary<TRequest, TResponse> _cache = new Dictionary<TRequest, TResponse>(); 

        public IRequestContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            // nothing to do
        }

        public TResponse Execute(TRequest request, Func<TRequest, TResponse> next, Func<TRequest, TResponse> fallback)
        {
            if (request is IEquatable<TRequest> == false)
                throw new InvalidOperationException("Memoization is only supported for query requests that implement IEquatable<TRequest>");

            if (_cache.ContainsKey(request))
            {
                _logger.InfoFormat("Returning cached result for {Request}", request);
                return _cache[request];
            }

            var result = next(request);

            _logger.InfoFormat("Adding result for {Request} to cache", request);
            _cache.Add(request, result);

            return result;
        }

        public Task<TResponse> ExecuteAsync(TRequest request, Func<TRequest, Task<TResponse>> next, Func<TRequest, Task<TResponse>> fallback)
        {
            throw new NotImplementedException();
        }
    }
}