using System.Collections.Generic;

namespace Darker
{
    public interface IRequestContext
    {
        IPolicyRegistry Policies { get; set; }

        IDictionary<string, object> Bag { get; set; }
    }
}