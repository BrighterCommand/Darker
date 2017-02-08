using System.Threading;
using System.Threading.Tasks;
using Darker;
using Darker.Attributes;
using Darker.Policies;
using Darker.RequestLogging;
using SampleApi.Domain;

namespace SampleApi.Ports
{
    public sealed class GetPersonNameQuery : IQuery<string>
    {
        public int PersonId { get; }

        public GetPersonNameQuery(int personId)
        {
            PersonId = personId;
        }
    }

    public sealed class GetPersonQueryHandler : AsyncQueryHandler<GetPersonNameQuery, string>
    {
        [RequestLogging(1)]
        [FallbackPolicy(2)]
        [RetryableQuery(3, Startup.SomethingWentTerriblyWrongCircuitBreakerName)]
        public override async Task<string> ExecuteAsync(GetPersonNameQuery query, CancellationToken cancellationToken = default(CancellationToken))
        {
            var repository = new PersonRepository();
            return await repository.GetNameById(query.PersonId, cancellationToken);
        }

        public override Task<string> FallbackAsync(GetPersonNameQuery query, CancellationToken cancellationToken = default(CancellationToken))
        {
            // this will happen when the circuit is broken or when the id doesn't exist
            return Task.FromResult("Linus Torvalds");
        }
    }
}
