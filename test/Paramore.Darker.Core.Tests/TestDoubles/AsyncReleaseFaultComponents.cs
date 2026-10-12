using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A component that counts its awaited DisposeAsync calls, whether or not they fault.</summary>
    internal interface IRecordsAsyncDisposal
    {
        int DisposeAsyncCount { get; }
    }

    // Closed over a tag type, the query, result and item types are distinct per fact, so the static
    // per-TResult logger in the pipeline is not shared between facts.
    internal sealed class HandlerFaultsTag { }

    internal sealed class EveryComponentFaultsTag { }

    internal sealed class StreamHandlerFaultsTag { }

    /// <summary>Counts a DisposeAsync that completes only after a yield, then throws the fault it was given (if any).</summary>
    internal sealed class FaultingAsyncDisposal
    {
        private readonly Exception _fault;

        public FaultingAsyncDisposal(Exception fault) => _fault = fault;

        public int Count { get; private set; }

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            Count++;
            if (_fault != null)
                throw _fault;
        }
    }

    internal sealed class AsyncReleaseFaultQuery<TTag> : IQuery<AsyncReleaseFaultResult<TTag>>
    {
    }

    internal sealed class AsyncReleaseFaultResult<TTag>
    {
        public AsyncReleaseFaultResult(string value) => Value = value;

        public string Value { get; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal sealed class FaultOnDisposeAsyncAttribute : QueryHandlerAttributeAsync
    {
        public FaultOnDisposeAsyncAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(FaultOnDisposeAsyncDecorator<,>);
    }

    internal sealed class FaultOnDisposeAsyncDecorator<TQuery, TResult>
        : IQueryHandlerDecoratorAsync<TQuery, TResult>, IRecordsAsyncDisposal, IAsyncDisposable
        where TQuery : IQuery<TResult>
    {
        private readonly FaultingAsyncDisposal _disposal;

        public FaultOnDisposeAsyncDecorator(Exception fault) => _disposal = new FaultingAsyncDisposal(fault);

        public IQueryContext Context { get; set; }

        public int DisposeAsyncCount => _disposal.Count;

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default)
            => next(query, cancellationToken);

        public ValueTask DisposeAsync() => _disposal.DisposeAsync();
    }

    /// <summary>An async handler carrying two faulting-on-dispose decorators; its own DisposeAsync faults if given a fault.</summary>
    internal sealed class AsyncReleaseFaultHandler<TTag>
        : QueryHandlerAsync<AsyncReleaseFaultQuery<TTag>, AsyncReleaseFaultResult<TTag>>, IRecordsAsyncDisposal, IAsyncDisposable
    {
        public const string RESULT = "survived-async-release-faults";

        private readonly FaultingAsyncDisposal _disposal;

        public AsyncReleaseFaultHandler(Exception fault) => _disposal = new FaultingAsyncDisposal(fault);

        public int DisposeAsyncCount => _disposal.Count;

        [FaultOnDisposeAsync(step: 1)]
        [FaultOnDisposeAsync(step: 2)]
        public override Task<AsyncReleaseFaultResult<TTag>> ExecuteAsync(
            AsyncReleaseFaultQuery<TTag> query, CancellationToken cancellationToken = default)
            => Task.FromResult(new AsyncReleaseFaultResult<TTag>(RESULT));

        public ValueTask DisposeAsync() => _disposal.DisposeAsync();
    }

    internal sealed class StreamReleaseFaultQuery<TTag> : IStreamQuery<StreamReleaseFaultItem<TTag>>
    {
    }

    internal sealed class StreamReleaseFaultItem<TTag>
    {
        public StreamReleaseFaultItem(string value) => Value = value;

        public string Value { get; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal sealed class FaultOnDisposeStreamAttribute : StreamQueryHandlerAttribute
    {
        public FaultOnDisposeStreamAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(FaultOnDisposeStreamDecorator<,>);
    }

    internal sealed class FaultOnDisposeStreamDecorator<TQuery, TResult>
        : IStreamQueryHandlerDecorator<TQuery, TResult>, IRecordsAsyncDisposal, IAsyncDisposable
        where TQuery : IStreamQuery<TResult>
    {
        private readonly FaultingAsyncDisposal _disposal;

        public FaultOnDisposeStreamDecorator(Exception fault) => _disposal = new FaultingAsyncDisposal(fault);

        public IQueryContext Context { get; set; }

        public int DisposeAsyncCount => _disposal.Count;

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public IAsyncEnumerable<TResult> Execute(TQuery query,
            Func<TQuery, CancellationToken, IAsyncEnumerable<TResult>> next,
            CancellationToken cancellationToken)
            => next(query, cancellationToken);

        public ValueTask DisposeAsync() => _disposal.DisposeAsync();
    }

    internal sealed class StreamReleaseFaultHandler<TTag>
        : IStreamQueryHandler<StreamReleaseFaultQuery<TTag>, StreamReleaseFaultItem<TTag>>, IRecordsAsyncDisposal, IAsyncDisposable
    {
        public const string ITEM = "survived-stream-release-faults";

        private readonly FaultingAsyncDisposal _disposal;

        public StreamReleaseFaultHandler(Exception fault) => _disposal = new FaultingAsyncDisposal(fault);

        public IQueryContext Context { get; set; }

        public int DisposeAsyncCount => _disposal.Count;

        [FaultOnDisposeStream(step: 1)]
        [FaultOnDisposeStream(step: 2)]
        public async IAsyncEnumerable<StreamReleaseFaultItem<TTag>> ExecuteAsync(
            StreamReleaseFaultQuery<TTag> query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new StreamReleaseFaultItem<TTag>(ITEM);
            yield return new StreamReleaseFaultItem<TTag>(ITEM);
        }

        public ValueTask DisposeAsync() => _disposal.DisposeAsync();
    }
}
