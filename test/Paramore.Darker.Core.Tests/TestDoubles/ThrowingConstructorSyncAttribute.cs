using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A sync decorator attribute whose constructor throws, so reading it from a method fails.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ThrowingConstructorSyncAttribute : QueryHandlerAttribute
    {
        public const string FaultMessage = "The sync attribute constructor failed.";

        public ThrowingConstructorSyncAttribute() : base(1)
        {
            throw new InvalidOperationException(FaultMessage);
        }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(ThrowingInitDecorator<,>);
    }
}
