using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>Records which of Dispose and DisposeAsync was used to tear a component down.</summary>
    internal interface IRecordsDisposal
    {
        /// <summary>"Dispose", "DisposeAsync", or null while the component has not been disposed.</summary>
        string DisposedBy { get; }
    }

    internal sealed class DisposalRecord
    {
        public string DisposedBy { get; private set; }

        public void Sync() => DisposedBy = nameof(IDisposable.Dispose);

        public void Async() => DisposedBy = nameof(IAsyncDisposable.DisposeAsync);
    }

    // The async path and the stream path have result types of their own.

    internal sealed class DisposalRecordingQuery : IQuery<DisposalRecordingResult>
    {
    }

    internal sealed class DisposalRecordingResult
    {
        public DisposalRecordingResult(string value) => Value = value;

        public string Value { get; }
    }

    internal sealed class DisposalRecordingStreamQuery : IStreamQuery<DisposalRecordingStreamItem>
    {
    }

    internal sealed class DisposalRecordingStreamItem
    {
        public DisposalRecordingStreamItem(string value) => Value = value;

        public string Value { get; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class DisposalRecordingAsyncAttribute : QueryHandlerAttributeAsync
    {
        public DisposalRecordingAsyncAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(DisposalRecordingAsyncDecorator<,>);
    }

    internal sealed class DisposalRecordingAsyncDecorator<TQuery, TResult>
        : IQueryHandlerDecoratorAsync<TQuery, TResult>, IRecordsDisposal, IDisposable, IAsyncDisposable
        where TQuery : IQuery<TResult>
    {
        private readonly DisposalRecord _record = new DisposalRecord();

        public IQueryContext Context { get; set; }

        public string DisposedBy => _record.DisposedBy;

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default)
            => next(query, cancellationToken);

        public void Dispose() => _record.Sync();

        public ValueTask DisposeAsync()
        {
            _record.Async();
            return default;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class DisposalRecordingStreamAttribute : StreamQueryHandlerAttribute
    {
        public DisposalRecordingStreamAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(DisposalRecordingStreamDecorator<,>);
    }

    internal sealed class DisposalRecordingStreamDecorator<TQuery, TResult>
        : IStreamQueryHandlerDecorator<TQuery, TResult>, IRecordsDisposal, IDisposable, IAsyncDisposable
        where TQuery : IStreamQuery<TResult>
    {
        private readonly DisposalRecord _record = new DisposalRecord();

        public IQueryContext Context { get; set; }

        public string DisposedBy => _record.DisposedBy;

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public IAsyncEnumerable<TResult> Execute(TQuery query,
            Func<TQuery, CancellationToken, IAsyncEnumerable<TResult>> next,
            CancellationToken cancellationToken)
            => next(query, cancellationToken);

        public void Dispose() => _record.Sync();

        public ValueTask DisposeAsync()
        {
            _record.Async();
            return default;
        }
    }

    internal sealed class DisposalRecordingAsyncHandler
        : QueryHandlerAsync<DisposalRecordingQuery, DisposalRecordingResult>, IRecordsDisposal, IDisposable, IAsyncDisposable
    {
        public const string RESULT = "disposal-recording";

        private readonly DisposalRecord _record = new DisposalRecord();

        public string DisposedBy => _record.DisposedBy;

        [DisposalRecordingAsync(step: 1)]
        public override Task<DisposalRecordingResult> ExecuteAsync(DisposalRecordingQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(new DisposalRecordingResult(RESULT));

        public void Dispose() => _record.Sync();

        public ValueTask DisposeAsync()
        {
            _record.Async();
            return default;
        }
    }

    internal sealed class DisposalRecordingStreamHandler
        : IStreamQueryHandler<DisposalRecordingStreamQuery, DisposalRecordingStreamItem>, IRecordsDisposal, IDisposable, IAsyncDisposable
    {
        public const string ITEM = "disposal-recording-item";

        private readonly DisposalRecord _record = new DisposalRecord();

        public IQueryContext Context { get; set; }

        public string DisposedBy => _record.DisposedBy;

        [DisposalRecordingStream(step: 1)]
        public async IAsyncEnumerable<DisposalRecordingStreamItem> ExecuteAsync(
            DisposalRecordingStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new DisposalRecordingStreamItem(ITEM);
        }

        public void Dispose() => _record.Sync();

        public ValueTask DisposeAsync()
        {
            _record.Async();
            return default;
        }
    }
}
