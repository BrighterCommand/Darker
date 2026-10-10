using System;
using System.Collections.Generic;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A <see cref="RecordingDisposable"/> that throws the supplied exception from <see cref="Dispose"/>.
    /// </summary>
    internal sealed class ThrowOnDisposeDisposable : RecordingDisposable
    {
        private readonly Exception _fault;

        public ThrowOnDisposeDisposable(string name, List<string> disposalLog, Exception fault)
            : base(name, disposalLog)
        {
            _fault = fault;
        }

        public override void Dispose()
        {
            base.Dispose();
            throw _fault;
        }
    }
}
