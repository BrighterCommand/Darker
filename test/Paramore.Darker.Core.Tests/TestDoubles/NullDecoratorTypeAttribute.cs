using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// An async decorator attribute whose decorator type is null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class NullDecoratorTypeAttribute : QueryHandlerAttributeAsync
    {
        public NullDecoratorTypeAttribute() : base(1) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => null!;
    }
}
