using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Builder;

namespace Paramore.Darker.AspNetCore
{
    public interface IQueryProcessorAspNetExtensionBuilder : IQueryProcessorExtensionBuilder
    {
        IServiceCollection Services { get; }
    }
}