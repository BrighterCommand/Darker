using Paramore.Darker;
using Paramore.Darker.Logging.Attributes;
using Paramore.Darker.Policies.Attributes;
using SampleMinimalApi.Domain;

namespace SampleMinimalApi.QueryHandlers;

public sealed class GetFlakyResultQuery : IQuery<string>
{
}

public sealed class GetFlakyResultQueryHandler : QueryHandlerAsync<GetFlakyResultQuery, string>
{
    private int _attempts;

    [QueryLoggingAttributeAsync(1)]
    [UseResiliencePipelineAttributeAsync(2, DarkerSettings.RetryPipelineName)]
    public override Task<string> ExecuteAsync(GetFlakyResultQuery query,
        CancellationToken cancellationToken = default)
    {
        _attempts++;

        // Fail on the first attempt to simulate a transient fault. The retry pipeline re-invokes this
        // handler, and we succeed on the next attempt — demonstrating recovery via the pipeline.
        if (_attempts == 1)
            throw new SomethingWentTerriblyWrongException();

        return Task.FromResult($"Succeeded on attempt {_attempts} after a transient failure");
    }
}
