using System.Collections.Generic;
using Paramore.Darker;

namespace SampleApi.Ports.Queries
{
    public sealed class GetPeople : IQuery<IReadOnlyDictionary<int, string>>
    {
    }
}