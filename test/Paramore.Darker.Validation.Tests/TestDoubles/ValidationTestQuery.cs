using Paramore.Darker;

namespace Paramore.Darker.Validation.Tests.TestDoubles;

internal sealed class ValidationTestQuery : IQuery<ValidationTestQuery.Result>
{
    internal sealed class Result
    {
        public string Value { get; set; } = string.Empty;
    }
}
