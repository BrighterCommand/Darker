using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A distinctive exception, so a test can pick the dependency's async fault out of the captured logs.</summary>
    public sealed class DependencyDisposeAsyncFaultException : Exception
    {
        public DependencyDisposeAsyncFaultException() : base("dependency failed to dispose asynchronously") { }
    }

    /// <summary>A service that implements only <see cref="IAsyncDisposable"/> and whose DisposeAsync faults.</summary>
    public sealed class FaultingAsyncDisposeDependency : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            throw new DependencyDisposeAsyncFaultException();
        }
    }

    // Each path has result types of its own, so the static per-TResult logger in the pipeline is not shared.

    public sealed class FaultingAsyncDisposeQuery : IQuery<FaultingAsyncDisposeResult>
    {
    }

    public sealed class FaultingAsyncDisposeResult
    {
        public FaultingAsyncDisposeResult(string value) => Value = value;

        public string Value { get; }
    }

    public sealed class FaultingAsyncDisposeQueryHandler : QueryHandlerAsync<FaultingAsyncDisposeQuery, FaultingAsyncDisposeResult>
    {
        public const string RESULT = "survived-dependency-dispose-async-fault";

        private readonly FaultingAsyncDisposeDependency _dependency;

        public FaultingAsyncDisposeQueryHandler(FaultingAsyncDisposeDependency dependency) => _dependency = dependency;

        public override Task<FaultingAsyncDisposeResult> ExecuteAsync(FaultingAsyncDisposeQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(new FaultingAsyncDisposeResult(_dependency != null ? RESULT : null));
    }

    public sealed class FaultingAsyncDisposeStreamQuery : IStreamQuery<FaultingAsyncDisposeStreamItem>
    {
    }

    public sealed class FaultingAsyncDisposeStreamItem
    {
        public FaultingAsyncDisposeStreamItem(string value) => Value = value;

        public string Value { get; }
    }

    public sealed class FaultingAsyncDisposeStreamQueryHandler
        : StreamQueryHandler<FaultingAsyncDisposeStreamQuery, FaultingAsyncDisposeStreamItem>
    {
        public const string ITEM = "survived-dependency-dispose-async-fault-item";

        private readonly FaultingAsyncDisposeDependency _dependency;

        public FaultingAsyncDisposeStreamQueryHandler(FaultingAsyncDisposeDependency dependency) => _dependency = dependency;

        public override async IAsyncEnumerable<FaultingAsyncDisposeStreamItem> ExecuteAsync(
            FaultingAsyncDisposeStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new FaultingAsyncDisposeStreamItem(_dependency != null ? ITEM : null);
            yield return new FaultingAsyncDisposeStreamItem(ITEM);
        }
    }
}
