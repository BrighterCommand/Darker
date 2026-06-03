// Native-AOT verification harness for Paramore.Darker query logging.
//
// This is a plain console application (not an xunit host) so that it can be
// published under PublishAot=true while referencing only the product libraries
// — see ADR 0012's "Implementation-time correction (AOT verification harness)".
//
// Each FR11 scenario routine returns 0 on success and non-zero (printing a diff)
// on failure; the process exits with the first non-zero result so a failed
// scenario fails the AOT verification.

using Paramore.Darker.Tests.AOT.Scenarios;

return await Case1PropertyBearingJson.RunAsync();
