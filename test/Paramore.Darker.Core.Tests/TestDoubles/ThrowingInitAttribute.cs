using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    // Decorators for the partial-build tests. Each pipeline kind has a pass-through decorator and a
    // decorator whose InitializeFromAttributeParams throws the fault it was constructed with.
    // The sync pass-through decorator is FirstPassThroughDecorator.

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ThrowingInitAttribute : QueryHandlerAttribute
    {
        public ThrowingInitAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(ThrowingInitDecorator<,>);
    }
}
