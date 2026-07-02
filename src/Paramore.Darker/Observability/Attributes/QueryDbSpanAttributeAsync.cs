using System;
using Paramore.Darker.Observability.Handlers;

namespace Paramore.Darker.Observability.Attributes
{
    /// <summary>
    /// Decorates an async handler's <c>ExecuteAsync</c> method to weave a <see cref="QueryDbSpanDecoratorAsync{TQuery,TResult}"/>
    /// that opens a child database span (OTel <c>Client</c> kind) around the awaited handler invocation.
    /// </summary>
    /// <remarks>
    /// The five constructor parameters map to <see cref="DbSpanInfo"/> fields and are passed to
    /// <see cref="QueryDbSpanDecoratorAsync{TQuery,TResult}.InitializeFromAttributeParams"/> by the
    /// <c>PipelineBuilder</c> at query-execution time.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class QueryDbSpanAttributeAsync : QueryHandlerAttributeAsync
    {
        private readonly DbSystem _system;
        private readonly string _dbName;
        private readonly string _dbTable;
        private readonly string _operation;

        /// <summary>
        /// Initialises a new <see cref="QueryDbSpanAttributeAsync"/>.
        /// </summary>
        /// <param name="step">The pipeline step order (higher executes first, outermost).</param>
        /// <param name="system">The database system (e.g. <see cref="DbSystem.MsSql"/>).</param>
        /// <param name="dbName">The name of the database being accessed.</param>
        /// <param name="dbTable">The name of the table or collection being accessed.</param>
        /// <param name="operation">The operation being performed (e.g. <c>"select"</c>).</param>
        public QueryDbSpanAttributeAsync(int step, DbSystem system, string dbName, string dbTable, string operation)
            : base(step)
        {
            _system = system;
            _dbName = dbName;
            _dbTable = dbTable;
            _operation = operation;
        }

        /// <inheritdoc />
        public override object[] GetAttributeParams() =>
            new object[] { _system, _dbName, _dbTable, _operation };

        /// <inheritdoc />
        public override Type GetDecoratorType() =>
            typeof(QueryDbSpanDecoratorAsync<,>);
    }
}
