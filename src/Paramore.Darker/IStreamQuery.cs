namespace Paramore.Darker
{
    /// <summary>
    /// Marker interface for queries that yield results incrementally as an async stream.
    /// TResult is the item type, not the enumerable.
    /// </summary>
    public interface IStreamQuery<out TResult> : IQuery<TResult>
    {
    }
}
