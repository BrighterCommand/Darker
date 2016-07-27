namespace Darker.Builder
{
    public interface INeedPolicies
    {
        INeedARequestContext Policies(IPolicyRegistry policyRegistry);
        INeedARequestContext DefaultPolicies();
    }
}