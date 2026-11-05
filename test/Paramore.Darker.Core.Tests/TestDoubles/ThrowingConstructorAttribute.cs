using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// An async decorator attribute whose constructor throws, so reading it from a method fails.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ThrowingConstructorAttribute : QueryHandlerAttributeAsync
    {
        public const string FaultMessage = "The attribute constructor failed.";

        public ThrowingConstructorAttribute() : base(1)
        {
            throw new InvalidOperationException(FaultMessage);
        }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(ThrowingInitAsyncDecorator<,>);
    }
}
