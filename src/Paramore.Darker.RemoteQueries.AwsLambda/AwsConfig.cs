using System;

namespace Paramore.Darker.RemoteQueries.AwsLambda
{
    public sealed class AwsConfig
    {
        public Uri BaseUri { get; internal set; }
        public string ApiKey { get; internal set; }
    }
}