using Paramore.Darker;

namespace SampleApi.Ports.Queries
{
    public sealed class GetAzureGreeting : IRemoteQuery<string>
    {
        public string Name { get; }

        public GetAzureGreeting(string name)
        {
            Name = name;
        }
    }
}