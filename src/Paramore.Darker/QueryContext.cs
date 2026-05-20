using System.Collections.Generic;
using Polly.Registry;

namespace Paramore.Darker
{
    public sealed class QueryContext : IQueryContext
    {
        public IDictionary<string, object> Bag { get; set; }
        public IPolicyRegistry<string> Policies { get; set; }
    }
}