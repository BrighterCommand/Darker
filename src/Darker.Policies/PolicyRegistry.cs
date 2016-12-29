using System;
using System.Collections;
using System.Collections.Generic;
using Polly;

namespace Darker.Policies
{
    public sealed class PolicyRegistry : IPolicyRegistry, IEnumerable<KeyValuePair<string, Policy>>
    {
        private readonly IDictionary<string, Policy> _policies = new Dictionary<string, Policy>();

        public void Add(string policyName, Policy policy)
        {
            if (policyName == null)
                throw new ArgumentNullException(nameof(policyName));
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            _policies.Add(policyName, policy);
        }

        public Policy Get(string policyName)
        {
            if (_policies.ContainsKey(policyName))
                return _policies[policyName];

            throw new ArgumentException($"There is no policy for {policyName}", nameof(policyName));
        }

        public bool Has(string policyName) => _policies.ContainsKey(policyName);

        public IEnumerator<KeyValuePair<string, Policy>> GetEnumerator() => _policies.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}