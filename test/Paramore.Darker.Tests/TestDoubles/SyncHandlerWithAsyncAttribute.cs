using System;
using Paramore.Darker.Attributes;

namespace Paramore.Darker.Tests.TestDoubles
{
    /// <summary>
    /// A sync handler that incorrectly uses an async attribute on Execute.
    /// This should cause a ConfigurationException at pipeline build time.
    /// </summary>
    public class SyncHandlerWithAsyncAttribute : QueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        [FallbackPolicyAttributeAsync(1)]
        public override SyncTestQuery.Result Execute(SyncTestQuery query)
        {
            return new SyncTestQuery.Result { Value = query.Id };
        }
    }
}
