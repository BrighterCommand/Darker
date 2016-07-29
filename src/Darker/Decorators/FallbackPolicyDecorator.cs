using System;
using System.Collections.Generic;
using System.Linq;
using Darker.Exceptions;
using Darker.Logging;

namespace Darker.Decorators
{
    public class FallbackPolicyDecorator<TRequest, TResponse> : IQueryHandlerDecorator<TRequest, TResponse>
        where TRequest : IQueryRequest<TResponse>
        where TResponse : IQueryResponse
    {
        public const string CauseOfFallbackException = "Fallback_Exception_Cause";

        private static readonly ILog _logger = LogProvider.GetLogger(typeof(FallbackPolicyDecorator<,>));

        private IEnumerable<Type> _exceptionTypes;

        public IRequestContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            _exceptionTypes = attributeParams.Cast<Type>();
        }

        public TResponse Execute(TRequest request, Func<TRequest, TResponse> next, Func<TRequest, TResponse> fallback)
        {
            try
            {
                _logger.Info("Executing query with fallback handling");
                return next(request);
            }
            catch (Exception ex)
            {
                if (!_exceptionTypes.Any() || _exceptionTypes.Contains(ex.GetType()))
                {
                    _logger.InfoException("Fallback handler caught exception, executing fallback", ex);
                    Context.Bag.Add(CauseOfFallbackException, ex);
                    return fallback(request);
                }

                _logger.InfoException("Fallback handler caught exception, but it's not configured to be handled", ex);
                throw;
            }
        }
    }
}