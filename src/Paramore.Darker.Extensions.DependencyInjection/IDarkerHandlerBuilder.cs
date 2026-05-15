using System;
using System.Reflection;
using Paramore.Darker.Builder;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    public interface IDarkerHandlerBuilder : IQueryProcessorExtensionBuilder
    {
        IDarkerHandlerBuilder AddHandlersFromAssemblies(params Assembly[] assemblies);
        IDarkerHandlerBuilder AddHandlers(Action<IQueryHandlerRegistry> registerHandlers);
    }
}