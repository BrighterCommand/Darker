using Paramore.Darker;
using Paramore.Darker.Logging.Attributes;
using Paramore.Darker.Policies.Attributes;
using SampleMinimalApi.Domain;

namespace SampleMinimalApi.QueryHandlers;

public sealed class GetPeopleQuery : IQuery<IReadOnlyDictionary<int, string>>
{
}

public sealed class GetPeopleQueryHandler : QueryHandlerAsync<GetPeopleQuery, IReadOnlyDictionary<int, string>>
{
    [QueryLoggingAttributeAsync(1)]
    [UseResiliencePipelineAttributeAsync(2, DarkerSettings.GeneralPipelineName)]
    public override async Task<IReadOnlyDictionary<int, string>> ExecuteAsync(GetPeopleQuery query,
        CancellationToken cancellationToken = default)
    {
        var repository = new PersonRepository();
        return await repository.GetAll(cancellationToken);
    }
}