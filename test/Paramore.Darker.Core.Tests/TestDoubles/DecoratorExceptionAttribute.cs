using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DecoratorExceptionAttribute : QueryHandlerAttribute
    {

        public DecoratorExceptionAttribute(int step) : base(step)
        {
        }

        public override object[] GetAttributeParams()
        {
            return [];
        }

        public override Type GetDecoratorType()
        {
            return typeof(TestExceptionDecorator<,>);
        }
    }
}
