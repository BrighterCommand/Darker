using System;
using System.Reflection;
using Paramore.Darker.Builder;

namespace Paramore.Darker.AspNetCore
{
    public interface IDarkerHandlerBuilder : IQueryProcessorExtensionBuilder
    {
        IDarkerHandlerBuilder AddHandlersFromAssemblies(params Assembly[] assemblies);
        IDarkerHandlerBuilder AddHandlers(Action<IQueryHandlerRegistry> registerHandlers);
    }
}