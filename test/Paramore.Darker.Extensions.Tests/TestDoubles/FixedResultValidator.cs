using Paramore.Darker.PipelineValidation;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A validator that returns the result it was given.</summary>
    public sealed class FixedResultValidator : IAmAPipelineValidator
    {
        private readonly PipelineValidationResult _result;

        public FixedResultValidator(PipelineValidationResult result) => _result = result;

        public PipelineValidationResult Validate() => _result;
    }
}
