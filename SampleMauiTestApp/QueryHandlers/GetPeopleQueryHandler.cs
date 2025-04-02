using Paramore.Darker;
using Paramore.Darker.Policies;
using Paramore.Darker.QueryLogging;
using SampleMauiTestApp.Domain;

namespace SampleMauiTestApp.QueryHandlers
{
    public sealed class GetPeopleQuery : IQuery<IReadOnlyDictionary<int, string>>
    {
    }

    public sealed class GetPeopleQueryHandler : QueryHandlerAsync<GetPeopleQuery, IReadOnlyDictionary<int, string>>
    {
        [QueryLogging(1)]
        [RetryableQuery(2, DarkerSettings.SomethingWentTerriblyWrongCircuitBreakerName)]
        public override async Task<IReadOnlyDictionary<int, string>> ExecuteAsync(GetPeopleQuery query,
            CancellationToken cancellationToken = default)
        {
            var repository = new PersonRepository();
            return await repository.GetAll(cancellationToken);
        }
    }
}