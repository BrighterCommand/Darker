namespace Darker.Builder
{
    public interface INeedARequestContext
    {
        INeedASerializer RequestContextFactory(IRequestContextFactory requestContextFactory);
        INeedASerializer InMemoryRequestContextFactory();
    }
}