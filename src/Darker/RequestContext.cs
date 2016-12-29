using System.Collections.Generic;
using Darker.Serialization;

namespace Darker
{
    public sealed class RequestContext : IRequestContext
    {
        public ISerializer Serializer { get; set; }
        public IDictionary<string, object> Bag { get; set; }
    }
}