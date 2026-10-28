using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>An attribute for a decorator that no test registers with the container.</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class UnregisteredDecoratorAttribute : QueryHandlerAttribute
    {
        public UnregisteredDecoratorAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(UnregisteredDecorator<,>);
    }
}
