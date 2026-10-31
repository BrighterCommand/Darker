using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>Wires <see cref="OuterDouble{TQuery,TResult}"/> into the pipeline.</summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class OuterDoubleAttribute : QueryHandlerAttribute
    {
        public OuterDoubleAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(OuterDouble<,>);
    }
}
