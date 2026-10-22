namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>Records which of Dispose and DisposeAsync was used to tear a component down.</summary>
    internal interface IRecordsDisposal
    {
        /// <summary>"Dispose", "DisposeAsync", or null while the component has not been disposed.</summary>
        string DisposedBy { get; }
    }
}
