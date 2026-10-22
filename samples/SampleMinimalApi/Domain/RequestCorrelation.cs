namespace SampleMinimalApi.Domain;

public sealed class RequestCorrelation
{
    public Guid Id { get; } = Guid.NewGuid();
}
