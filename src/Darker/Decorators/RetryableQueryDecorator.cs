using System;
using System.Threading.Tasks;
using Darker.Exceptions;
using Darker.Logging;

namespace Darker.Decorators
{
    public class RetryableQueryDecorator<TRequest, TResponse> : IQueryHandlerDecorator<TRequest, TResponse>
        where TRequest : IQueryRequest<TResponse>
        where TResponse : IQueryResponse
    {
        private static readonly ILog _logger = LogProvider.GetLogger(typeof(RetryableQueryDecorator<,>));

        private string _policyName;

        public IRequestContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            _policyName = (string)attributeParams[0];

            if (!Context.Policies.Has(_policyName))
                throw new ConfigurationException($"Policy does not exist in policy registry: {_policyName}");
        }

        public TResponse Execute(TRequest request, Func<TRequest, TResponse> next, Func<TRequest, TResponse> fallback)
        {
            _logger.InfoFormat("Executing query with policy: {PolicyName}", _policyName);

            return Context.Policies.Get(_policyName).Execute(() => next(request));
        }

        public async Task<TResponse> ExecuteAsync(TRequest request, Func<TRequest, Task<TResponse>> next, Func<TRequest, Task<TResponse>> fallback)
        {
            _logger.InfoFormat("Executing async query with policy: {PolicyName}", _policyName);

            return await Context.Policies.Get(_policyName).ExecuteAsync(() => next(request)).ConfigureAwait(false);
        }
    }
}