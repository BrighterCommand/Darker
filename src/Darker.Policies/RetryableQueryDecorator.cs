using System;
using System.Threading;
using System.Threading.Tasks;
using Darker.Exceptions;
using Darker.Policies.Logging;

namespace Darker.Policies
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

            if (!GetPolicyRegistry().Has(_policyName))
                throw new ConfigurationException($"Policy does not exist in policy registry: {_policyName}");
        }

        public TResponse Execute(TRequest request, Func<TRequest, TResponse> next, Func<TRequest, TResponse> fallback)
        {
            _logger.InfoFormat("Executing query with policy: {PolicyName}", _policyName);

            return GetPolicyRegistry().Get(_policyName).Execute(() => next(request));
        }

        public async Task<TResponse> ExecuteAsync(TRequest request,
            Func<TRequest, CancellationToken, Task<TResponse>> next,
            Func<TRequest, CancellationToken, Task<TResponse>> fallback,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            _logger.InfoFormat("Executing async query with policy: {PolicyName}", _policyName);

            return await GetPolicyRegistry().Get(_policyName)
                .ExecuteAsync(ct => next(request, ct), cancellationToken, false)
                .ConfigureAwait(false);
        }

        private IPolicyRegistry GetPolicyRegistry()
        {
            if (!Context.Bag.ContainsKey(Constants.ContextBagKey))
                throw new ConfigurationException($"Policy registry does not exist in context bag with key {Constants.ContextBagKey}.");

            var policyRegistry = Context.Bag[Constants.ContextBagKey] as IPolicyRegistry;
            if (policyRegistry == null)
                throw new ConfigurationException($"The policy registry in the context bag (with key {Constants.ContextBagKey}) must be of type {nameof(IPolicyRegistry)}, but is {Context.Bag[Constants.ContextBagKey].GetType()}.");

            return policyRegistry;
        }
    }
}