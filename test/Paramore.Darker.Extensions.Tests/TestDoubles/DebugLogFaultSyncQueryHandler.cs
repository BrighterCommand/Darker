namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>Arms the logging provider as it runs, so the first thing to fault is teardown's own Debug message.</summary>
    public sealed class DebugLogFaultSyncQueryHandler(ThrowingLoggerProvider provider)
        : QueryHandler<DebugLogFaultQuery<DebugLogFaultSyncTag>, DebugLogFaultResult<DebugLogFaultSyncTag>>
    {
        public const string RESULT = "sync-result";

        public override DebugLogFaultResult<DebugLogFaultSyncTag> Execute(DebugLogFaultQuery<DebugLogFaultSyncTag> query)
        {
            provider.Arm();
            return new(RESULT);
        }
    }
}
