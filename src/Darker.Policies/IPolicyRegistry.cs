using Polly;

namespace Darker.Policies
{
    public interface IPolicyRegistry
    {
        void Add(string policyName, Policy policy);
        Policy Get(string policyName);
        bool Has(string policyName);
    }
}