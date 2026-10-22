using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class DisposalRecordingAsyncAttribute : QueryHandlerAttributeAsync
    {
        public DisposalRecordingAsyncAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(DisposalRecordingAsyncDecorator<,>);
    }
}
