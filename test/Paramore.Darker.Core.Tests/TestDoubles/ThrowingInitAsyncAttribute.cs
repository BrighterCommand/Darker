using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ThrowingInitAsyncAttribute : QueryHandlerAttributeAsync
    {
        public ThrowingInitAsyncAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(ThrowingInitAsyncDecorator<,>);
    }
}
