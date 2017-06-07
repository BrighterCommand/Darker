using Paramore.Darker;

namespace SampleApi.Ports.Queries
{
    public sealed class GetAwsGreeting : IRemoteQuery<string>
    {
        public string Name { get; }

        public GetAwsGreeting(string name)
        {
            Name = name;
        }
    }
}