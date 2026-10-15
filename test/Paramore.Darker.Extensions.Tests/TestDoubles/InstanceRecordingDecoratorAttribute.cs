using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class InstanceRecordingDecoratorAttribute : QueryHandlerAttribute
    {
        public InstanceRecordingDecoratorAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(InstanceRecordingDecorator<,>);
    }
}
