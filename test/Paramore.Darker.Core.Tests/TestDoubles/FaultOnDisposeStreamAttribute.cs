using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal sealed class FaultOnDisposeStreamAttribute : StreamQueryHandlerAttribute
    {
        public FaultOnDisposeStreamAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(FaultOnDisposeStreamDecorator<,>);
    }
}
