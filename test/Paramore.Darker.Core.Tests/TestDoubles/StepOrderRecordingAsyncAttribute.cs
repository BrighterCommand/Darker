using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>Wires <see cref="StepOrderRecordingAsyncDecorator{TQuery,TResult}"/> into the async pipeline at the given step.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal sealed class StepOrderRecordingAsyncAttribute : QueryHandlerAttributeAsync
    {
        public StepOrderRecordingAsyncAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [Step];

        public override Type GetDecoratorType() => typeof(StepOrderRecordingAsyncDecorator<,>);
    }
}
