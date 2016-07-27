using System;
using System.Collections.Generic;
using Darker.Logging;

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

        public TResponse Execute(TRequest request, Func<TRequest, TResponse> next)
        {
            if (request is IEquatable<TRequest> == false)
                throw new InvalidOperationException("Memoization is only supported for query requests that implement IEquatable<TRequest>");

            if (_cache.ContainsKey(request))
            {
                _logger.InfoFormat("Returning cached result for {0}", request);
                return _cache[request];
            }

            var result = next(request);

            _logger.InfoFormat("Adding result for {0} to cache", request);
            _cache.Add(request, result);

            return result;
        }
    }
}