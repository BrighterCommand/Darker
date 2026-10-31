using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Builder;
using Paramore.Darker.Extensions.DependencyInjection;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// An <see cref="IDarkerHandlerBuilder"/> that AddDarker did not create, so it exposes no registries.
    /// </summary>
    public class ForeignDarkerHandlerBuilder : IDarkerHandlerBuilder
    {
        public IServiceCollection Services { get; } = new ServiceCollection();

        public IDarkerHandlerBuilder AddHandlersFromAssemblies(params Assembly[] assemblies) => throw new NotImplementedException();

        public IDarkerHandlerBuilder AddHandlers(Action<IQueryHandlerRegistry> registerHandlers) => throw new NotImplementedException();

        public IDarkerHandlerBuilder AddAsyncHandlers(Action<IQueryHandlerRegistryAsync> registerHandlers) => throw new NotImplementedException();

        public IDarkerHandlerBuilder AddStreamHandlers(Action<IStreamQueryHandlerRegistry> registerHandlers) => throw new NotImplementedException();

        public IQueryProcessorExtensionBuilder RegisterDecorator(Type decoratorType) => throw new NotImplementedException();
    }
}
