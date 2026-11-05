using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>Records its step number into a shared log on entry, then calls the next step.</summary>
    internal sealed class StepOrderRecordingAsyncDecorator<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly List<string> _log;
        private int _step;

        public StepOrderRecordingAsyncDecorator(List<string> log) => _log = log;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) => _step = (int)attributeParams[0];

        public Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default)
        {
            _log.Add($"step {_step}");
            return next(query, cancellationToken);
        }
    }
}
