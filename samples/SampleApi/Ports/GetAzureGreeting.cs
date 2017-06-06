using Paramore.Darker;

namespace SampleApi.Ports
{
    public sealed class GetAzureGreeting : IQuery<string>
    {
        public string Name { get; }

        public GetAzureGreeting(string name)
        {
            Name = name;
        }
    }
}