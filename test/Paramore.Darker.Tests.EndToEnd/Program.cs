// End-to-end verification harness for Paramore.Darker (ADR 0027).
//
// This is a plain console application (not an xunit host) that references only
// the product libraries and Microsoft.Extensions.Hosting - no ASP.NET Core.
//
// Contract: scenarios run in order; the process stops at the first scenario that
// returns a non-zero result and exits with that result; it exits 0 when every
// scenario passes.

using Paramore.Darker.Tests.EndToEnd.Scenarios;

var result = await ValidConfigurationRunsSyncAsyncAndStreamQueries.RunAsync();
if (result != 0)
{
    return result;
}

result = await RetryPipelineRecoversATransientFailure.RunAsync();
if (result != 0)
{
    return result;
}

result = await ScopedDependencyIsProvidedAndDisposed.RunAsync();
if (result != 0)
{
    return result;
}

result = await InvalidConfigurationFailsAtHostStart.RunAsync();
if (result != 0)
{
    return result;
}

return 0;
