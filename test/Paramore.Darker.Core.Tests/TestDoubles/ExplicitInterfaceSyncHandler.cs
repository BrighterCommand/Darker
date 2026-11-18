using System;
using Paramore.Darker.Core.Tests.Exported;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A sync handler that implements the interface's members only explicitly, so it has no public Execute.</summary>
    internal sealed class ExplicitInterfaceSyncHandler : IQueryHandler<TestQueryA, Guid>
    {
        public IQueryContext Context { get; set; }

        Guid IQueryHandler<TestQueryA, Guid>.Execute(TestQueryA query) => query.Id;

        Guid IQueryHandler<TestQueryA, Guid>.Fallback(TestQueryA query) => throw new NotImplementedException();
    }
}
