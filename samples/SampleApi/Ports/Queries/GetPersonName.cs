using Paramore.Darker;

namespace SampleApi.Ports.Queries
{
    public sealed class GetPersonName : IQuery<string>
    {
        public int PersonId { get; }

        public GetPersonName(int personId)
        {
            PersonId = personId;
        }
    }
}