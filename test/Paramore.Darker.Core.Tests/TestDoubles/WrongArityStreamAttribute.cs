using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A stream decorator attribute naming a decorator with the wrong generic arity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class WrongArityStreamAttribute : StreamQueryHandlerAttribute
    {
        public WrongArityStreamAttribute() : base(1) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(WrongArityDecorator<>);
    }
}
