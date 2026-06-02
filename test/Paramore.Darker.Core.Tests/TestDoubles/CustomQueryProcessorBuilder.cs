using Paramore.Darker.Builder;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A custom <see cref="IBuildTheQueryProcessor"/> that is deliberately NOT a
    /// <c>QueryProcessorBuilder</c>, used to pin the documented limitation that the builder-surface
    /// JSON-logging entry point only supports the in-box builder (ADR 0012 Decision step 5).
    /// </summary>
    internal sealed class CustomQueryProcessorBuilder : IBuildTheQueryProcessor
    {
        public IQueryProcessor Build() => null!;
    }
}
