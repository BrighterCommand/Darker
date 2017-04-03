using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Logging;

namespace Paramore.Darker.Decorators
{
    public class FallbackPolicyDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public const string CauseOfFallbackException = "Fallback_Exception_Cause";

        private static readonly ILog _logger = LogProvider.GetLogger(typeof(FallbackPolicyDecorator<,>));

        private IEnumerable<Type> _exceptionTypes;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            _exceptionTypes = attributeParams.Cast<Type>();
        }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
        {
            try
            {
                _logger.Info("Executing query with fallback handling");
                return next(query);
            }
            catch (Exception ex)
            {
                if (!_exceptionTypes.Any() || _exceptionTypes.Contains(ex.GetType()))
                {
                    _logger.InfoException("Fallback handler caught exception, executing fallback", ex);
                    Context.Bag.Add(CauseOfFallbackException, ex);
                    return fallback(query);
                }

                _logger.InfoException("Fallback handler caught exception, but it's not configured to be handled", ex);
                throw;
            }
        }

        public async Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                _logger.Info("Executing async query with fallback handling");
                return await next(query, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!_exceptionTypes.Any() || _exceptionTypes.Contains(ex.GetType()))
                {
                    _logger.InfoException("Fallback handler caught exception, executing fallback", ex);
                    Context.Bag.Add(CauseOfFallbackException, ex);
                    return await fallback(query, cancellationToken).ConfigureAwait(false);
                }

                _logger.InfoException("Fallback handler caught exception, but it's not configured to be handled", ex);
                throw;
            }
        }
    }
}