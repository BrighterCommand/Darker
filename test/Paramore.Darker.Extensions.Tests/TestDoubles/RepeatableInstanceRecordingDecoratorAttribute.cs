using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// Applies <see cref="InstanceRecordingDecorator{TQuery,TResult}"/>, and may be applied more than once
    /// to one handler method, at different steps.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class RepeatableInstanceRecordingDecoratorAttribute : QueryHandlerAttribute
    {
        public RepeatableInstanceRecordingDecoratorAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(InstanceRecordingDecorator<,>);
    }
}
