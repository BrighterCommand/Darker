#if NETSTANDARD
namespace Paramore.Darker
{
    public interface IRemoteQuery<out TResult> : IQuery
    {
    }
}
#endif