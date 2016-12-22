using System.Collections.Generic;
using Darker.Serialization;

namespace Darker
{
    public interface IRequestContext
    {
        IPolicyRegistry Policies { get; set; }
        ISerializer Serializer { get; set; }
        IDictionary<string, object> Bag { get; set; }
    }
}