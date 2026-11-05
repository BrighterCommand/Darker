using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public open-generic async decorator whose only constructor takes an <see cref="IServiceProvider"/>.</summary>
    public sealed class ServiceProviderDecorator<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public ServiceProviderDecorator(IServiceProvider serviceProvider)
        {
        }

        public IQueryContext Context { get; set; } = null!;

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
        }

        public Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default) => next(query, cancellationToken);
    }
}
