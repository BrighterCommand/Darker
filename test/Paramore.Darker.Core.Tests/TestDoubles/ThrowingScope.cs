using System;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// An <see cref="IAmAScope"/> that throws the supplied exception from <see cref="Dispose"/> and
    /// faults the task returned from <see cref="DisposeAsync"/> with it.
    /// </summary>
    internal sealed class ThrowingScope : IAmAScope
    {
        private readonly Exception _fault;

        public ThrowingScope(Exception fault) => _fault = fault;

        public void Dispose() => throw _fault;

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            throw _fault;
        }
    }
}
