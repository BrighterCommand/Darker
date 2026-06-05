using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A query carrying a <see cref="Guid"/> <see cref="Id"/> and returning it, used by
    /// the QueryProcessor tests. Distinct in-test analogue of the scannable
    /// <c>Exported.TestQueryA</c> (copied and renamed per ADR 0013, Decision 1).
    /// </summary>
    internal class ProcessorQuery : IQuery<Guid>
    {
        public Guid Id { get; }

        public ProcessorQuery(Guid id)
        {
            Id = id;
        }
    }
}
