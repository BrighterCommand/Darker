using System;

namespace Paramore.Darker.Builder
{
    public interface IQueryProcessorExtensionBuilder
    {
        IQueryProcessorExtensionBuilder RegisterDecorator(Type decoratorType);
    }
}