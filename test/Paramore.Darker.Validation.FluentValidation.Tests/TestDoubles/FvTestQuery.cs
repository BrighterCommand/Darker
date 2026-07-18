using Paramore.Darker;

namespace Paramore.Darker.Validation.FluentValidation.Tests.TestDoubles;

internal sealed class FvTestQuery : IQuery<FvTestQuery.Result>
{
    public string Name { get; set; } = string.Empty;

    internal sealed class Result
    {
        public string Value { get; set; } = string.Empty;
    }
}
