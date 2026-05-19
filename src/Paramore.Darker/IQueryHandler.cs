namespace Paramore.Darker
{
    public interface IQueryHandler
    {
        IQueryContext Context { get; set; }
    }

    public interface IQueryHandler<in TQuery, TResult> : IQueryHandler
        where TQuery : IQuery<TResult>
    {
        TResult Execute(TQuery query);

        TResult Fallback(TQuery query);
    }
}