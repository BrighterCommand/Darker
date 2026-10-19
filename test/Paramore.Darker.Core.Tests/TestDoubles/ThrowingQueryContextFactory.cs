using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A query context factory whose <see cref="Create"/> throws the supplied exception, so the pipeline never builds.</summary>
    internal sealed class ThrowingQueryContextFactory : IQueryContextFactory
    {
        private readonly Exception _fault;

        public ThrowingQueryContextFactory(Exception fault) => _fault = fault;

        public IQueryContext Create() => throw _fault;
    }
}
