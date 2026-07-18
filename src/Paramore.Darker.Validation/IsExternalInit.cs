// Polyfill that allows 'init' accessors (C# 9) to compile on netstandard2.0.
// The compiler emits a modreq to this type; providing it here satisfies that requirement
// without taking a dependency on a newer runtime.
// See: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/init

#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
