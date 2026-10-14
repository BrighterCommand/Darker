using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    // Each path has a result type of its own, so the static per-TResult logger in the pipeline is not shared.

    internal sealed class ThrowingScopeQuery : IQuery<ThrowingScopeResult>
    {
    }

    internal sealed class ThrowingScopeResult
    {
        public ThrowingScopeResult(string value) => Value = value;

        public string Value { get; }
    }

    internal sealed class ThrowingScopeQueryHandler : QueryHandler<ThrowingScopeQuery, ThrowingScopeResult>
    {
        public const string RESULT = "throwing-scope-sync";

        public override ThrowingScopeResult Execute(ThrowingScopeQuery query) => new ThrowingScopeResult(RESULT);
    }

    internal sealed class ThrowingScopeAsyncQuery : IQuery<ThrowingScopeAsyncResult>
    {
    }

    internal sealed class ThrowingScopeAsyncResult
    {
        public ThrowingScopeAsyncResult(string value) => Value = value;

        public string Value { get; }
    }

    internal sealed class ThrowingScopeAsyncQueryHandler : QueryHandlerAsync<ThrowingScopeAsyncQuery, ThrowingScopeAsyncResult>
    {
        public const string RESULT = "throwing-scope-async";

        public override Task<ThrowingScopeAsyncResult> ExecuteAsync(ThrowingScopeAsyncQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(new ThrowingScopeAsyncResult(RESULT));
    }

    internal sealed class ThrowingScopeStreamQuery : IStreamQuery<ThrowingScopeStreamItem>
    {
    }

    internal sealed class ThrowingScopeStreamItem
    {
        public ThrowingScopeStreamItem(string value) => Value = value;

        public string Value { get; }
    }

    internal sealed class ThrowingScopeStreamQueryHandler : StreamQueryHandler<ThrowingScopeStreamQuery, ThrowingScopeStreamItem>
    {
        public const string ITEM = "throwing-scope-stream";

        public override async IAsyncEnumerable<ThrowingScopeStreamItem> ExecuteAsync(
            ThrowingScopeStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new ThrowingScopeStreamItem(ITEM);
        }
    }
}
