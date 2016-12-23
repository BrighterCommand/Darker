namespace Darker
{
    public class InMemoryRequestContextFactory : IRequestContextFactory
    {
        public IRequestContext Create() => new RequestContext();
    }
}