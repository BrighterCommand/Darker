using Paramore.Darker;

namespace SampleApi.Ports
{
    public sealed class GetAwsGreeting : IQuery<string>
    {
        public string Name { get; }

        public GetAwsGreeting(string name)
        {
            Name = name;
        }
    }
}