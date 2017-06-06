using System;

namespace Paramore.Darker.RemoteQueries.AzureFunctions
{
    public sealed class AzureConfig
    {
        public Uri BaseUri { get; internal set; }
        public string FunctionsKey { get; internal set; }
    }
}