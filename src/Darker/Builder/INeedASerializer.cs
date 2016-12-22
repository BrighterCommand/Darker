using Darker.Serialization;

namespace Darker.Builder
{
    public interface INeedASerializer
    {
        IBuildTheQueryProcessor NoSerializer();
        IBuildTheQueryProcessor Serializer(ISerializer serializer);
    }
}