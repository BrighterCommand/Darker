using System;
using System.Reflection;
using Paramore.Darker.Builder;

namespace Paramore.Darker.AspNetCore
{
    public interface IDarkerHandlerBuilder
    {
        IQueryProcessorExtensionBuilder HandlersFromAssemblies(params Assembly[] assemblies);
        IQueryProcessorExtensionBuilder Handlers(Action<IQueryHandlerRegistry> registerHandlers);
    }
}