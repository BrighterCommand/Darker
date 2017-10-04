using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.AspNetCore
{
    public interface IDarkerHandlerBuilder
    {
        IServiceCollection Services { get; }

        IQueryProcessorAspNetExtensionBuilder AddHandlersFromAssemblies(params Assembly[] assemblies);
        IQueryProcessorAspNetExtensionBuilder AddHandlers(Action<IQueryHandlerRegistry> registerHandlers);
    }
}