using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    // Closed over a tag type, the query, result and item types are distinct per fact, so the static
    // per-TResult logger in the pipeline is created against the logger factory current when that fact first runs.
    public sealed class LifetimeFaultTag { }

    public sealed class ReleaseFaultTag { }

    public sealed class LoggingFaultAsyncQuery<TTag> : IQuery<LoggingFaultAsyncResult<TTag>>
    {
    }

    public sealed class LoggingFaultAsyncResult<TTag>
    {
        public LoggingFaultAsyncResult(string value) => Value = value;

        public string Value { get; }
    }

    public sealed class LoggingFaultStreamQuery<TTag> : IStreamQuery<LoggingFaultStreamItem<TTag>>
    {
    }

    public sealed class LoggingFaultStreamItem<TTag>
    {
        public LoggingFaultStreamItem(string value) => Value = value;

        public string Value { get; }
    }

    /// <summary>A handler with a Scoped <see cref="FaultingAsyncDisposeDependency"/>, so tearing the scope down faults.</summary>
    public sealed class LoggingFaultAsyncLifetimeHandler
        : QueryHandlerAsync<LoggingFaultAsyncQuery<LifetimeFaultTag>, LoggingFaultAsyncResult<LifetimeFaultTag>>
    {
        public const string RESULT = "survived-async-lifetime-and-logging-faults";

        private readonly FaultingAsyncDisposeDependency _dependency;

        public LoggingFaultAsyncLifetimeHandler(FaultingAsyncDisposeDependency dependency) => _dependency = dependency;

        public override Task<LoggingFaultAsyncResult<LifetimeFaultTag>> ExecuteAsync(
            LoggingFaultAsyncQuery<LifetimeFaultTag> query, CancellationToken cancellationToken = default)
            => Task.FromResult(new LoggingFaultAsyncResult<LifetimeFaultTag>(_dependency != null ? RESULT : null));
    }

    public sealed class LoggingFaultStreamLifetimeHandler
        : StreamQueryHandler<LoggingFaultStreamQuery<LifetimeFaultTag>, LoggingFaultStreamItem<LifetimeFaultTag>>
    {
        public const string ITEM = "survived-stream-lifetime-and-logging-faults";

        private readonly FaultingAsyncDisposeDependency _dependency;

        public LoggingFaultStreamLifetimeHandler(FaultingAsyncDisposeDependency dependency) => _dependency = dependency;

        public override async IAsyncEnumerable<LoggingFaultStreamItem<LifetimeFaultTag>> ExecuteAsync(
            LoggingFaultStreamQuery<LifetimeFaultTag> query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new LoggingFaultStreamItem<LifetimeFaultTag>(_dependency != null ? ITEM : null);
            yield return new LoggingFaultStreamItem<LifetimeFaultTag>(ITEM);
        }
    }

    /// <summary>A handler whose <see cref="DisposeAsync"/> faults, so releasing it makes the pipeline log a fault.</summary>
    public sealed class LoggingFaultAsyncReleaseHandler
        : QueryHandlerAsync<LoggingFaultAsyncQuery<ReleaseFaultTag>, LoggingFaultAsyncResult<ReleaseFaultTag>>, IAsyncDisposable
    {
        public const string RESULT = "survived-async-release-and-logging-faults";

        public override Task<LoggingFaultAsyncResult<ReleaseFaultTag>> ExecuteAsync(
            LoggingFaultAsyncQuery<ReleaseFaultTag> query, CancellationToken cancellationToken = default)
            => Task.FromResult(new LoggingFaultAsyncResult<ReleaseFaultTag>(RESULT));

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            throw new InvalidOperationException("handler failed to dispose asynchronously");
        }
    }

    public sealed class LoggingFaultStreamReleaseHandler
        : IStreamQueryHandler<LoggingFaultStreamQuery<ReleaseFaultTag>, LoggingFaultStreamItem<ReleaseFaultTag>>, IAsyncDisposable
    {
        public const string ITEM = "survived-stream-release-and-logging-faults";

        public IQueryContext Context { get; set; }

        public async IAsyncEnumerable<LoggingFaultStreamItem<ReleaseFaultTag>> ExecuteAsync(
            LoggingFaultStreamQuery<ReleaseFaultTag> query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new LoggingFaultStreamItem<ReleaseFaultTag>(ITEM);
            yield return new LoggingFaultStreamItem<ReleaseFaultTag>(ITEM);
        }

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            throw new InvalidOperationException("stream handler failed to dispose asynchronously");
        }
    }
}
