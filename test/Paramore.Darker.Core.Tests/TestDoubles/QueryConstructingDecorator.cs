namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A decorator whose query type must have a parameterless constructor, so it closes over a concrete query class
    /// but never over the query interface the pipeline builder closes decorators over.
    /// </summary>
    internal sealed class QueryConstructingDecorator<TQuery, TResult> where TQuery : new()
    {
    }
}
