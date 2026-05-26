using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Builder;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    public interface IDarkerHandlerBuilder : IQueryProcessorExtensionBuilder
    {
        IServiceCollection Services { get; }
        IDarkerHandlerBuilder AddHandlersFromAssemblies(params Assembly[] assemblies);
        IDarkerHandlerBuilder AddHandlers(Action<IQueryHandlerRegistry> registerHandlers);
        IDarkerHandlerBuilder AddAsyncHandlers(Action<IQueryHandlerRegistryAsync> registerHandlers);
    }
}