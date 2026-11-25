using System.Diagnostics.CodeAnalysis;

namespace Paramore.Darker
{
#if !NETSTANDARD2_0
    // The pipeline inspects a resolved handler's interfaces to select the closed query contract.
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#endif
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