using System;
using System.Collections.Generic;
using Paramore.Darker.PipelineValidation;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A resolvability probe that answers with a delegate and records every type it is asked about.</summary>
    internal sealed class SimpleResolvabilityProbe(Func<Type, bool> resolves) : IAmAComponentResolvabilityProbe
    {
        /// <summary>Every type the probe was asked about, in order, recorded before the delegate runs.</summary>
        public List<Type?> AskedAbout { get; } = [];

        public bool Resolves(Type type)
        {
            AskedAbout.Add(type);
            return resolves(type);
        }
    }
}
