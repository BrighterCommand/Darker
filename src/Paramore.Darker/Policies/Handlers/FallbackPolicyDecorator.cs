using Paramore.Darker.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Paramore.Darker.Policies.Handlers
{
    public class FallbackPolicyDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public const string CauseOfFallbackException = "Fallback_Exception_Cause";

        private static readonly ILogger _logger = ApplicationLogging.CreateLogger<FallbackPolicyDecorator<TQuery, TResult>>();

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
                _logger.LogInformation("Executing query with fallback handling");
                return next(query);
            }
            catch (Exception ex)
            {
                if (!_exceptionTypes.Any() || _exceptionTypes.Contains(ex.GetType()))
                {
                    _logger.LogInformation(ex, "Fallback handler caught exception, executing fallback");
                    Context.Bag.Add(CauseOfFallbackException, ex);
                    return fallback(query);
                }

                _logger.LogInformation(ex, "Fallback handler caught exception, but it's not configured to be handled");
                throw;
            }
        }
    }
}