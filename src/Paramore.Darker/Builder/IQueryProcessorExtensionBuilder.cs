using System;

namespace Paramore.Darker.Builder
{
    public interface IQueryProcessorExtensionBuilder
    {
        IQueryProcessorExtensionBuilder AddContextBagItem(string key, object item);
        IQueryProcessorExtensionBuilder RegisterDecorator(Type decoratorType);
    }
}