using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A <see cref="RecordingAsyncDisposable"/> that throws the supplied exception from <see cref="DisposeAsync"/>.
    /// </summary>
    internal sealed class ThrowOnDisposeAsyncDisposable : RecordingAsyncDisposable
    {
        private readonly Exception _fault;

        public ThrowOnDisposeAsyncDisposable(string name, List<string> disposalLog, Exception fault)
            : base(name, disposalLog)
        {
            _fault = fault;
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            throw _fault;
        }
    }
}
