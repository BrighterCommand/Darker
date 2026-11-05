using System;
using System.Collections.Generic;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>Records its step number into a shared log on entry, then calls the next step.</summary>
    internal sealed class StepOrderRecordingDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly List<string> _log;
        private int _step;

        public StepOrderRecordingDecorator(List<string> log) => _log = log;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) => _step = (int)attributeParams[0];

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
        {
            _log.Add($"step {_step}");
            return next(query);
        }
    }
}
