using System;
using Paramore.Darker.Attributes;

namespace Paramore.Darker.Policies
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RetryableQueryAttribute : QueryHandlerAttribute
    {
        private readonly string _policyName;

        public RetryableQueryAttribute(int step, string policyName = Constants.RetryPolicyName)
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
            return typeof(RetryableQueryDecorator<,>);
        }
    }
}