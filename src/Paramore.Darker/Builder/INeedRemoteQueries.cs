namespace Paramore.Darker.Builder
{
    public interface INeedRemoteQueries
    {
        INeedAQueryContext NoRemoteQueries();

        /// <summary>
        /// EXPERIMENTAL: this feature is in early development preview. please only use it if you know what you're doing. expect significant api changes around remote queries!
        /// </summary>
        INeedAQueryContext RemoteQueries(params IRemoteQueryRegistry[] registries);
    }
}