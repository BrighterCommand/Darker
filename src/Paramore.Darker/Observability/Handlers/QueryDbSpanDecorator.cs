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
    /// the handler executes unchanged. Exception recording is left to the <c>QueryProcessor</c>
    /// (the span owner) to avoid duplicate exception events.
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
            finally
            {
                Context.Tracer?.EndSpan(dbSpan);
            }
        }
    }
}
