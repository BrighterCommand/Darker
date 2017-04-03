using System.Collections.Generic;

namespace Paramore.Darker
{
    public sealed class QueryContext : IQueryContext
    {
        public IDictionary<string, object> Bag { get; set; }
    }
}