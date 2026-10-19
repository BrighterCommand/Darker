namespace Paramore.Darker.Core.Tests.TestDoubles
{
    // Each handler carries a pass-through decorator at step 2 (created first) and a decorator whose
    // initialisation throws at step 1 (created second).

    internal sealed class PartialBuildQuery : IQuery<string>
    {
    }
}
