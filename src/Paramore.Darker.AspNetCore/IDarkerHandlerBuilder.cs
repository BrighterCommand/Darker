using System;
using System.Reflection;
using Paramore.Darker.Builder;

namespace Paramore.Darker.AspNetCore
{
    public interface IDarkerHandlerBuilder
    {
        IQueryProcessorExtensionBuilder AddHandlersFromAssemblies(params Assembly[] assemblies);
        IQueryProcessorExtensionBuilder AddHandlers(Action<IQueryHandlerRegistry> registerHandlers);
    }
}