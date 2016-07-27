namespace Darker.Builder
{
    public interface INeedARequestContext
    {
        IBuildTheQueryProcessor RequestContextFactory(IRequestContextFactory requestContextFactory);
        IBuildTheQueryProcessor InMemoryRequestContextFactory();
    }
}