using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>Wires <see cref="StepOrderRecordingDecorator{TQuery,TResult}"/> into the sync pipeline at the given step.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal sealed class StepOrderRecordingAttribute : QueryHandlerAttribute
    {
        public StepOrderRecordingAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [Step];

        public override Type GetDecoratorType() => typeof(StepOrderRecordingDecorator<,>);
    }
}
