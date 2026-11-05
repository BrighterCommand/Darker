using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>Wires <see cref="InnerDouble{TQuery,TResult}"/> into the pipeline.</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class InnerDoubleAttribute : QueryHandlerAttribute
    {
        public InnerDoubleAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(InnerDouble<,>);
    }
}
