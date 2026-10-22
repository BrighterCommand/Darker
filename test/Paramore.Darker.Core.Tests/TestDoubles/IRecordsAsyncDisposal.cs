namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A component that counts its awaited DisposeAsync calls, whether or not they fault.</summary>
    internal interface IRecordsAsyncDisposal
    {
        int DisposeAsyncCount { get; }
    }
}
