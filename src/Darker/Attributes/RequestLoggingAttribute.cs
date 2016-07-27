using System;
using Darker.Decorators;

namespace Darker.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RequestLoggingAttribute : QueryHandlerAttribute
    {
        public RequestLoggingAttribute(int step) : base(step)
        {
        }

        public override object[] GetAttributeParams()
        {
            return new object[0];
        }

        public override Type GetDecoratorType()
        {
            return typeof(RequestLoggingDecorator<,>);
        }
    }
}