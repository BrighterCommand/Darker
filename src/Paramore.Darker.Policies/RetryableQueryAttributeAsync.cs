using System;
using Paramore.Darker.Attributes;

namespace Paramore.Darker.Policies
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RetryableQueryAttributeAsync : QueryHandlerAttributeAsync
    {
        private readonly string _policyName;

        public RetryableQueryAttributeAsync(int step, string policyName = Constants.RetryPolicyName)
            : base(step)
        {
            _policyName = policyName;
        }

        public override object[] GetAttributeParams()
        {
            return new object[] { _policyName };
        }

        public override Type GetDecoratorType()
        {
            return typeof(RetryableQueryDecoratorAsync<,>);
        }
    }
}
