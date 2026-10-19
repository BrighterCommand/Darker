using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class FirstPassThroughAttribute : QueryHandlerAttribute
    {
        public FirstPassThroughAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(FirstPassThroughDecorator<,>);
    }
}
