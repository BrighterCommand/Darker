using Paramore.Darker.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Paramore.Darker.Decorators
{
    public class FallbackPolicyDecoratorAsync<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public const string CauseOfFallbackException = "Fallback_Exception_Cause";

        private static readonly ILogger _logger = ApplicationLogging.CreateLogger<FallbackPolicyDecoratorAsync<TQuery, TResult>>();

        private IEnumerable<Type> _exceptionTypes;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            _exceptionTypes = attributeParams.Cast<Type>();
        }

        public async Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                _logger.LogInformation("Executing async query with fallback handling");
                return await next(query, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!_exceptionTypes.Any() || _exceptionTypes.Contains(ex.GetType()))
                {
                    _logger.LogInformation(ex, "Fallback handler caught exception, executing fallback");
                    Context.Bag.Add(CauseOfFallbackException, ex);
                    return await fallback(query, cancellationToken).ConfigureAwait(false);
                }

                _logger.LogInformation(ex, "Fallback handler caught exception, but it's not configured to be handled");
                throw;
            }
        }
    }
}
