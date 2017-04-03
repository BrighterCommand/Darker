using System.Collections.Generic;

namespace Paramore.Darker
{
    public interface IQueryContext
    {
        IDictionary<string, object> Bag { get; set; }
    }
}