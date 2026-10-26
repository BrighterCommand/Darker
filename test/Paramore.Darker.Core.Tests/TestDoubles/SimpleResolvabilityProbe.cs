using System;
using Paramore.Darker.PipelineValidation;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A resolvability probe that answers with a delegate.</summary>
    internal sealed class SimpleResolvabilityProbe(Func<Type, bool> resolves) : IAmAComponentResolvabilityProbe
    {
        public bool Resolves(Type type) => resolves(type);
    }
}
