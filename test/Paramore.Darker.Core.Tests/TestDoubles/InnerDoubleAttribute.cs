using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>Wires <see cref="InnerDouble{TQuery,TResult}"/> into the pipeline.</summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class InnerDoubleAttribute : QueryHandlerAttribute
    {
        public InnerDoubleAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(InnerDouble<,>);
    }
}
