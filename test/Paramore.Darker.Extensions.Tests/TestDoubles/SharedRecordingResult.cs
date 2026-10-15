using System;
using System.Collections.Generic;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// The result shared by <see cref="FirstSharedResultQuery"/> and <see cref="SecondSharedResultQuery"/>.
    /// The handler and each decorator write the identity of their own instance, and of the dependency they
    /// were given, so a test can tell which instances took part in a query.
    /// </summary>
    public sealed class SharedRecordingResult
    {
        public Guid HandlerId { get; set; }

        public ITrackedDependency HandlerDependency { get; set; }

        public List<Guid> DecoratorIds { get; } = new();

        public List<ITrackedDependency> DecoratorDependencies { get; } = new();
    }
}
