using Paramore.Darker;
using Paramore.Darker.Logging.Attributes;
using Paramore.Darker.Policies.Attributes;
using SampleMinimalApi.Domain;

namespace SampleMinimalApi.QueryHandlers;

public sealed class GetPersonNameQuery : IQuery<string>
{
    public GetPersonNameQuery(int personId)
    {
        PersonId = personId;
    }

    public int PersonId { get; }
}

public sealed class GetPersonQueryHandler : QueryHandlerAsync<GetPersonNameQuery, string>
{
    [QueryLoggingAttributeAsync(1)]
    [FallbackPolicyAttributeAsync(2)]
    [UseResiliencePipelineAttributeAsync(3, DarkerSettings.GeneralPipelineName)]
    public override async Task<string> ExecuteAsync(GetPersonNameQuery query,
        CancellationToken cancellationToken = default)
    {
        var repository = new PersonRepository();
        return await repository.GetNameById(query.PersonId, cancellationToken);
    }

    public override Task<string> FallbackAsync(GetPersonNameQuery query, CancellationToken cancellationToken = default)
    {
        // this will happen when the id doesn't exist
        return Task.FromResult("Linus Torvalds");
    }
}