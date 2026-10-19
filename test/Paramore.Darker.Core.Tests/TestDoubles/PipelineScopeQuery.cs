namespace Paramore.Darker.Core.Tests.TestDoubles
{
    // One query and handler per path, each carrying a single pass-through decorator, so that both the
    // handler factory and the decorator factory are asked to create a component.

    internal sealed class PipelineScopeQuery : IQuery<string>
    {
    }
}
