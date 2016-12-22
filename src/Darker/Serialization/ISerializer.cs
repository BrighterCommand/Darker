namespace Darker.Serialization
{
    public interface ISerializer
    {
        string Serialize<T>(T value);
    }
}