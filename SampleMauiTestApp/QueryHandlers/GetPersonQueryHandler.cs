using Paramore.Darker;
using Paramore.Darker.Attributes;
using Paramore.Darker.Policies;
using Paramore.Darker.QueryLogging;
using SampleMauiTestApp.Domain;

namespace SampleMauiTestApp.QueryHandlers
{
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
        [QueryLogging(1)]
        [FallbackPolicy(2)]
        [RetryableQuery(3, DarkerSettings.SomethingWentTerriblyWrongCircuitBreakerName)]
        public override Task<string> ExecuteAsync(GetPersonNameQuery query,
            CancellationToken cancellationToken = default)
        {
            var repository = new PersonRepository();
            return repository.GetNameById(query.PersonId, cancellationToken);
        }

        public override Task<string> FallbackAsync(GetPersonNameQuery query, CancellationToken cancellationToken = default)
        {
            // this will happen when the circuit is broken or when the id doesn't exist
            return Task.FromResult("Linus Torvalds");
        }
    }
}