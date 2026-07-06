using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Observability.Handlers
{
    /// <summary>
    /// An async pipeline decorator that wraps handler execution in a child database span.
    /// Add this decorator by placing <c>[QueryDbSpanAsync(step, system, dbName, dbTable, operation)]</c>
    /// on the handler's <c>ExecuteAsync</c> method.
    /// </summary>
    /// <remarks>
    /// Reads <see cref="IQueryContext.Tracer"/> and <see cref="IQueryContext.Span"/> from the ambient
    /// query context. When no tracer is configured (zero-overhead path), both properties are null and
    /// the handler executes unchanged. Exception recording is left to the <c>QueryProcessor</c>
    /// (the span owner) to avoid duplicate exception events.
    /// </remarks>
    /// <typeparam name="TQuery">The query type handled by the pipeline.</typeparam>
    /// <typeparam name="TResult">The result type returned by the handler.</typeparam>
    public class QueryDbSpanDecoratorAsync<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>
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
        public async Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default)
        {
            var dbSpan = Context.Tracer?.CreateDbSpan(_spanInfo!, Context.Span);
            try
            {
                return await next(query, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Context.Tracer?.EndSpan(dbSpan);
            }
        }
    }
}
