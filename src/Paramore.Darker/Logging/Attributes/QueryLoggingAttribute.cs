using System;
using Paramore.Darker.Logging.Handlers;

namespace Paramore.Darker.Logging.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class QueryLoggingAttribute : QueryHandlerAttribute
    {
        public QueryLoggingAttribute(int step) : base(step)
        {
        }

        public override object[] GetAttributeParams()
        {
            return new object[0];
        }

        public override Type GetDecoratorType()
        {
            return typeof(QueryLoggingDecorator<,>);
        }
    }
}