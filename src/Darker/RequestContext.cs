using System.Collections.Generic;

namespace Darker
{
    public sealed class RequestContext : IRequestContext
    {
        public IDictionary<string, object> Bag { get; set; }
    }
}