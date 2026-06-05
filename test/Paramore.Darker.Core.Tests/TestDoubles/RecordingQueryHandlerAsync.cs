using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// The async sibling of <see cref="RecordingQueryHandler{TQuery,TResult}"/>. Runs a
    /// supplied delegate as its async body and records how it was invoked. The
    /// <see cref="CancellationToken"/> is threaded through to the delegate but is
    /// invariantly <c>default</c> in the migrated tests and is not recorded
    /// (ADR 0013, Decision 3 — behaviour-equivalent).
    /// </summary>
    internal class RecordingQueryHandlerAsync<TQuery, TResult> : QueryHandlerAsync<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly Func<TQuery, CancellationToken, Task<TResult>> _execute;

        public RecordingQueryHandlerAsync(Func<TQuery, TResult> execute)
        {
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            _execute = (query, _) => Task.FromResult(execute(query));
        }

        public RecordingQueryHandlerAsync(Func<TQuery, CancellationToken, Task<TResult>> execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public int ExecuteCount { get; private set; }
        public int FallbackCount { get; private set; }
        public TQuery LastQuery { get; private set; }

        public override Task<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default(CancellationToken))
        {
            ExecuteCount++;
            LastQuery = query;
            return _execute(query, cancellationToken);
        }

        public override Task<TResult> FallbackAsync(TQuery query, CancellationToken cancellationToken = default(CancellationToken))
        {
            FallbackCount++;
            return base.FallbackAsync(query, cancellationToken);
        }
    }
}
