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
    public class RetryableQueryDecoratorAsync<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private static readonly ILogger _logger = ApplicationLogging.CreateLogger<RetryableQueryDecoratorAsync<TQuery, TResult>>();

        private string _policyName;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            _policyName = (string)attributeParams[0];

            if (!GetPolicyRegistry().ContainsKey(_policyName))
                throw new ConfigurationException($"Policy does not exist in policy registry: {_policyName}");
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
            => Context.Policies ?? throw new ConfigurationException("No policy registry is configured. Set a policy registry on the query context or pass one to the QueryProcessor constructor.");
    }
}
