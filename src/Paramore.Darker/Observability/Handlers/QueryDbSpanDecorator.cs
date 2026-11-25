using System;

namespace Paramore.Darker.Observability.Handlers
{
    /// <summary>
    /// A sync pipeline decorator that wraps handler execution in a child database span.
    /// Add this decorator by placing <c>[QueryDbSpan(step, system, dbName, dbTable, operation)]</c>
    /// on the handler's <c>Execute</c> method.
    /// </summary>
    /// <remarks>
    /// Reads <see cref="IQueryContext.Tracer"/> and <see cref="IQueryContext.Span"/> from the ambient
    /// query context. When no tracer is configured (zero-overhead path), both properties are null and
    /// the handler executes unchanged. An exception from the wrapped work is recorded on the DB span,
    /// then rethrown; the <c>QueryProcessor</c> records it on the query span, which it owns.
    /// </remarks>
    /// <typeparam name="TQuery">The query type handled by the pipeline.</typeparam>
    /// <typeparam name="TResult">The result type returned by the handler.</typeparam>
    public class QueryDbSpanDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private DbSpanInfo? _spanInfo;

        /// <inheritdoc />
        public IQueryContext Context { get; set; }

        /// <inheritdoc />
        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            var system = (DbSystem)attributeParams[0];
            var dbName = (string)attributeParams[1];
            var dbTable = (string)attributeParams[2];
            var operation = (string)attributeParams[3];
            _spanInfo = new DbSpanInfo(system, dbName, operation, dbTable);
        }

        /// <inheritdoc />
        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
        {
            var dbSpan = Context.Tracer?.CreateDbSpan(_spanInfo!, Context.Span);
            try
            {
                return next(query);
            }
            catch (Exception ex)
            {
                Context.Tracer?.AddExceptionToSpan(dbSpan, ex);
                throw;
            }
            finally
            {
                Context.Tracer?.EndSpan(dbSpan);
            }
        }
    }
}
