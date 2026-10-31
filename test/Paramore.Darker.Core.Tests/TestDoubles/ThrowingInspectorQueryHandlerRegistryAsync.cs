using System;
using System.Collections.Generic;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>An async registry whose inspector throws, to pin that validation survives a registry that cannot be read.</summary>
    public sealed class ThrowingInspectorQueryHandlerRegistryAsync : QueryHandlerRegistryAsync, IAmAHandlerRegistryInspector
    {
        public const string FAULT_MESSAGE = "async inspector fault";

        IReadOnlyList<HandlerRegistration> IAmAHandlerRegistryInspector.GetRegistrations() =>
            throw new InvalidOperationException(FAULT_MESSAGE);
    }
}
