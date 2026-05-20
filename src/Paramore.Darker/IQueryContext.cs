using System.Collections.Generic;
using Polly.Registry;

namespace Paramore.Darker
{
    public interface IQueryContext
    {
        IDictionary<string, object> Bag { get; set; }
        IPolicyRegistry<string> Policies { get; set; }
    }
}