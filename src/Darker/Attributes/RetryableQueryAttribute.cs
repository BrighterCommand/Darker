using System;
using Darker.Decorators;

namespace Darker.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RetryableQueryAttribute : QueryHandlerAttribute
    {
        private readonly string _policyName;

        public RetryableQueryAttribute(int step, string policyName = QueryProcessor.RetryPolicyName)
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