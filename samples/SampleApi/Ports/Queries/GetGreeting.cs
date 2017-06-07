using Paramore.Darker;

namespace SampleApi.Ports.Queries
{
    public sealed class GetGreeting : IQuery<string>
    {
        public string Name { get; }

        public GetGreeting(string name)
        {
            Name = name;
        }
    }
}