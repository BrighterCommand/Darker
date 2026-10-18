using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal sealed class FaultOnDisposeAsyncAttribute : QueryHandlerAttributeAsync
    {
        public FaultOnDisposeAsyncAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(FaultOnDisposeAsyncDecorator<,>);
    }
}
