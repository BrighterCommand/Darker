using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A query handler double that runs a supplied delegate as its body and records
    /// how it was invoked. Replaces Moq interaction verification with state the test
    /// can assert on (ExecuteCount/FallbackCount/LastQuery), while still preferring the
    /// returned result as the primary evidence the handler ran (ADR 0013, Decision 3).
    /// The delegate may throw, to exercise exception paths.
    /// </summary>
    internal class RecordingQueryHandler<TQuery, TResult> : QueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly Func<TQuery, TResult> _execute;

        public RecordingQueryHandler(Func<TQuery, TResult> execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public int ExecuteCount { get; private set; }
        public int FallbackCount { get; private set; }
        public TQuery LastQuery { get; private set; }

        public override TResult Execute(TQuery query)
        {
            ExecuteCount++;
            LastQuery = query;
            return _execute(query);
        }

        public override TResult Fallback(TQuery query)
        {
            FallbackCount++;
            return base.Fallback(query);
        }
    }
}
