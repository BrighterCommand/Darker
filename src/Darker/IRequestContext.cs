using System.Collections.Generic;

namespace Darker
{
    public interface IRequestContext
    {
        IDictionary<string, object> Bag { get; set; }
    }
}