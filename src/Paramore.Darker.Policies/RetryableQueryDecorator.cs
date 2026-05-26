using System;
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

        private IPolicyRegistry<string> GetPolicyRegistry()
            => Context.Policies ?? throw new ConfigurationException("No policy registry is configured. Set a policy registry on the query context or pass one to the QueryProcessor constructor.");
    }
}