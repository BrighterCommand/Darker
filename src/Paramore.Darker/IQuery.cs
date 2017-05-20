namespace Paramore.Darker
{
    public interface IQuery
    {
    }

    public interface IQuery<out TResult> : IQuery
    {
    }
}