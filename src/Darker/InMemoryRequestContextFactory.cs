namespace Darker
{
    public class InMemoryRequestContextFactory : IRequestContextFactory
    {
        public IRequestContext Create()
        {
            return new RequestContext();
        }
    }
}