using System;
using Paramore.Darker.Attributes;

namespace Paramore.Darker.QueryLogging
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