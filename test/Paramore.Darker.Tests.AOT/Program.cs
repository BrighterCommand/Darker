// Native-AOT verification harness for Paramore.Darker query logging.
//
// This is a plain console application (not an xunit host) so that it can be
// published under PublishAot=true while referencing only the product libraries
// — see ADR 0012's "Implementation-time correction (AOT verification harness)".
//
// The FR11 scenario routines (property-bearing source-generated JSON; cycle-bearing
// IgnoreCycles) are added by the behavioural TEST + IMPLEMENT tasks of Step 9. Until
// then this entry point is an intentional placeholder so the project compiles and
// AOT-publishes, confirming the NETSDK1150 blocker is resolved.
return 0;
