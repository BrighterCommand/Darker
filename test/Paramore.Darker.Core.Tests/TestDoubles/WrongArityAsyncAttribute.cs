using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// An async decorator attribute naming a decorator with the wrong generic arity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class WrongArityAsyncAttribute : QueryHandlerAttributeAsync
    {
        public WrongArityAsyncAttribute() : base(1) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(WrongArityDecorator<>);
    }
}
