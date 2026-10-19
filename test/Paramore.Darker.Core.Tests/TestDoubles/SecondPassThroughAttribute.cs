using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class SecondPassThroughAttribute : QueryHandlerAttribute
    {
        public SecondPassThroughAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(SecondPassThroughDecorator<,>);
    }
}
