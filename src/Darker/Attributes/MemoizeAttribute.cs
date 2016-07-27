using System;
using Darker.Decorators;

namespace Darker.Attributes
{
    /// <summary>
    /// Just a proof of concept, please don't use in prod
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MemoizeAttribute : QueryHandlerAttribute
    {
        public MemoizeAttribute(int step) : base(step)
        {
        }

        public override object[] GetAttributeParams()
        {
            return new object[0];
        }

        public override Type GetDecoratorType()
        {
            return typeof(MemoizationDecorator<,>);
        }
    }
}