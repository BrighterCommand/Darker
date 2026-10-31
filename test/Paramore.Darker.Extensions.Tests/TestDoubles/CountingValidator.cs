using System.Threading;
using Paramore.Darker.PipelineValidation;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A validator that counts how many times it was asked to validate and finds nothing.</summary>
    public sealed class CountingValidator : IAmAPipelineValidator
    {
        private int _count;

        /// <summary>The number of times <see cref="Validate"/> was called.</summary>
        public int Count => Volatile.Read(ref _count);

        public PipelineValidationResult Validate()
        {
            Interlocked.Increment(ref _count);
            return new PipelineValidationResult([], []);
        }
    }
}
