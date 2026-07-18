using Paramore.Darker;

namespace Paramore.Darker.Validation.FluentValidation.Tests.TestDoubles;

internal sealed class FvTestQuery : IQuery<FvTestQuery.Result>
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Age must be greater than zero when the query is validated.</summary>
    public int Age { get; set; } = 18;

    internal sealed class Result
    {
        public string Value { get; set; } = string.Empty;
    }
}
