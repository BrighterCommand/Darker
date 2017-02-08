using System.Collections.Generic;

namespace Darker
{
    public interface IQueryContext
    {
        IDictionary<string, object> Bag { get; set; }
    }
}