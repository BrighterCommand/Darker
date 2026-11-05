using System;
using System.Collections.Generic;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A sync registry whose inspector throws, to pin that validation survives a registry that cannot be read.</summary>
    public sealed class ThrowingInspectorQueryHandlerRegistry : QueryHandlerRegistry, IAmAHandlerRegistryInspector
    {
        public const string FAULT_MESSAGE = "sync inspector fault";

        IReadOnlyList<HandlerRegistration> IAmAHandlerRegistryInspector.GetRegistrations() =>
            throw new InvalidOperationException(FAULT_MESSAGE);
    }
}
