using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>Arms the logging provider as it runs, so the first thing to fault is teardown's own Debug message.</summary>
    public sealed class DebugLogFaultAsyncQueryHandler(ThrowingLoggerProvider provider)
        : QueryHandlerAsync<DebugLogFaultQuery<DebugLogFaultAsyncTag>, DebugLogFaultResult<DebugLogFaultAsyncTag>>
    {
        public const string RESULT = "async-result";

        public override Task<DebugLogFaultResult<DebugLogFaultAsyncTag>> ExecuteAsync(
            DebugLogFaultQuery<DebugLogFaultAsyncTag> query, CancellationToken cancellationToken = default)
        {
            provider.Arm();
            return Task.FromResult(new DebugLogFaultResult<DebugLogFaultAsyncTag>(RESULT));
        }
    }
}
