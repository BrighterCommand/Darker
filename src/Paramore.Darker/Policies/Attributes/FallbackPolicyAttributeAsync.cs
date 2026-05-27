using System;
using System.Linq;
using Paramore.Darker.Policies.Handlers;

namespace Paramore.Darker.Policies.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class FallbackPolicyAttributeAsync : QueryHandlerAttributeAsync
    {
        private readonly Type[] _exceptions;

        public FallbackPolicyAttributeAsync(int step, params Type[] exceptions) : base(step)
        {
            _exceptions = exceptions;
        }

        public override object[] GetAttributeParams()
        {
            return _exceptions.Cast<object>().ToArray();
        }

        public override Type GetDecoratorType()
        {
            return typeof(FallbackPolicyDecoratorAsync<,>);
        }
    }
}
