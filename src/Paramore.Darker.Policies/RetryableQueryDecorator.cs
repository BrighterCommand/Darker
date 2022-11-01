using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Logging;
using Polly;
using Polly.Registry;

namespace Paramore.Darker.Policies
{
    public class RetryableQueryDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private static readonly ILogger _logger = ApplicationLogging.CreateLogger<RetryableQueryDecorator<TQuery, TResult>>();

        private string _policyName;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            _policyName = (string)attributeParams[0];

            if (!GetPolicyRegistry().ContainsKey(_policyName))
                throw new ConfigurationException($"Policy does not exist in policy registry: {_policyName}");
        }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
        {
            _logger.LogInformation("Executing query with policy: {PolicyName}", _policyName);

            return GetPolicyRegistry().Get<ISyncPolicy>(_policyName).Execute(() => next(query));
        }

        
        public async Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            _logger.LogInformation("Executing async query with policy: {PolicyName}", _policyName);

            return await GetPolicyRegistry().Get<IAsyncPolicy>(_policyName)
                .ExecuteAsync(ct => next(query, ct), cancellationToken, false)
                .ConfigureAwait(false);
        }

        private IPolicyRegistry<string> GetPolicyRegistry()
        {
            if (!Context.Bag.ContainsKey(Constants.ContextBagKey))
                throw new ConfigurationException($"Policy registry does not exist in context bag with key {Constants.ContextBagKey}.");

            var policyRegistry = Context.Bag[Constants.ContextBagKey] as IPolicyRegistry<string>;
            if (policyRegistry == null)
                throw new ConfigurationException($"The policy registry in the context bag (with key {Constants.ContextBagKey}) must be of type {nameof(IPolicyRegistry<string>)}, but is {Context.Bag[Constants.ContextBagKey].GetType()}.");

            return policyRegistry;
        }
    }
}