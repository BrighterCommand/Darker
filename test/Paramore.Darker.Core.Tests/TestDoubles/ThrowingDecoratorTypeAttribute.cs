using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// An async decorator attribute whose GetDecoratorType throws.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ThrowingDecoratorTypeAttribute : QueryHandlerAttributeAsync
    {
        public const string FaultMessage = "The decorator type could not be determined.";

        public ThrowingDecoratorTypeAttribute() : base(1) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => throw new InvalidOperationException(FaultMessage);
    }
}
