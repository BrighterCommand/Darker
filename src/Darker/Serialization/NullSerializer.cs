namespace Darker.Serialization
{
    public sealed class NullSerializer : ISerializer
    {
        public string Serialize<T>(T value)
        {
            return string.Empty;
        }
    }
}