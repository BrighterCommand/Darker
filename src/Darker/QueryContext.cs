using System.Collections.Generic;

namespace Darker
{
    public sealed class QueryContext : IQueryContext
    {
        public IDictionary<string, object> Bag { get; set; }
    }
}