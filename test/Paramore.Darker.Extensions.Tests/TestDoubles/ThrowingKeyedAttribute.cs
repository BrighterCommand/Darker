using System;
using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A <see cref="FromKeyedServicesAttribute"/> whose constructor throws, so reading it by reflection fails.</summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ThrowingKeyedAttribute : FromKeyedServicesAttribute
    {
        public ThrowingKeyedAttribute() : base("k")
        {
            throw new InvalidOperationException("The attribute constructor failed.");
        }
    }
}
