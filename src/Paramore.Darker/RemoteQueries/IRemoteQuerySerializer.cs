#if NETSTANDARD
using System.IO;

namespace Paramore.Darker.RemoteQueries
{
    public interface IRemoteQuerySerializer
    {
        string MediaType { get; }
        
        // todo: use stream instead
        string Serialize<T>(T query);
        
        T Deserialize<T>(Stream stream);
    }
}
#endif