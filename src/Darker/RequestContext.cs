using System.Collections.Generic;

namespace Darker
{
    public sealed class RequestContext : IRequestContext
    {
        public IPolicyRegistry Policies { get; set; }

        public IDictionary<string, object> Bag { get; set; }
    }
}