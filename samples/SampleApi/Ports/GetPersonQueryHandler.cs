using System.Threading;
using System.Threading.Tasks;
using Darker;
using Darker.Attributes;
using Darker.Policies;
using Darker.RequestLogging;
using SampleApi.Domain;

namespace SampleApi.Ports
{
    public sealed class GetPersonQuery : IQueryRequest<GetPersonQuery.Response>
    {
        public int PersonId { get; }

        public GetPersonQuery(int personId)
        {
            PersonId = personId;
        }

        public sealed class Response : IQueryResponse
        {
            public string Name { get; }

            public Response(string name)
            {
                Name = name;
            }
        }
    }

    public sealed class GetPersonQueryHandler : AsyncQueryHandler<GetPersonQuery, GetPersonQuery.Response>
    {
        [RequestLogging(1)]
        [FallbackPolicy(2)]
        [RetryableQuery(3, Startup.SomethingWentTerriblyWrongCircuitBreakerName)]
        public override async Task<GetPersonQuery.Response> ExecuteAsync(GetPersonQuery request, CancellationToken cancellationToken = new CancellationToken())
        {
            var repository = new PersonRepository();
            var person = await repository.GetById(request.PersonId, cancellationToken);

            return new GetPersonQuery.Response(person);
        }

        public override Task<GetPersonQuery.Response> FallbackAsync(GetPersonQuery request, CancellationToken cancellationToken = new CancellationToken())
        {
            // this will happen when the circuit is broken or when the id doesn't exist
            return Task.FromResult(new GetPersonQuery.Response("Linus Torvalds"));
        }
    }
}
