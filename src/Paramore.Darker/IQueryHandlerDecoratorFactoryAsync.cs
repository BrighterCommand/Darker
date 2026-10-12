using System;
using System.Threading.Tasks;

namespace Paramore.Darker
{
    public interface IQueryHandlerDecoratorFactoryAsync
    {
        T Create<T>(Type decoratorType, IAmALifetime lifetime) where T : IQueryHandlerDecorator;
        ValueTask ReleaseAsync<T>(T handler, IAmALifetime lifetime) where T : IQueryHandlerDecorator;
    }
}
