using System;
using System.Linq;
using Darker.Decorators;

namespace Darker.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class FallbackPolicyAttribute : QueryHandlerAttribute
    {
        private readonly Type[] _exceptions;

        public FallbackPolicyAttribute(int step, params Type[] exceptions) : base(step)
        {
            _exceptions = exceptions;
        }

        public override object[] GetAttributeParams()
        {
            return _exceptions.Cast<object>().ToArray();
        }

        public override Type GetDecoratorType()
        {
            return typeof(FallbackPolicyDecorator<,>);
        }
    }
}