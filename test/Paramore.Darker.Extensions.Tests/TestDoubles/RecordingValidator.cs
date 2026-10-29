using System;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A validator that records when it runs and finds nothing.</summary>
    public sealed class RecordingValidator : IAmAPipelineValidator
    {
        /// <summary>The entry this validator records in the startup sequence.</summary>
        public const string ENTRY = "validation";

        private readonly StartupSequence _sequence;

        public RecordingValidator(StartupSequence sequence) => _sequence = sequence;

        public PipelineValidationResult Validate()
        {
            _sequence.Record(ENTRY);
            return new PipelineValidationResult(Array.Empty<ValidationError>(), Array.Empty<ValidationError>());
        }
    }
}
