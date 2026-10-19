using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class PassThroughStreamAttribute : StreamQueryHandlerAttribute
    {
        public PassThroughStreamAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(PassThroughStreamDecorator<,>);
    }
}
