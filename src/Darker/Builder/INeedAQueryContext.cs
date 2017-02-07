namespace Darker.Builder
{
    public interface INeedAQueryContext
    {
        IBuildTheQueryProcessor QueryContextFactory(IQueryContextFactory queryContextFactory);
        IBuildTheQueryProcessor InMemoryQueryContextFactory();
    }
}