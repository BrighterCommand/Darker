using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Paramore.Darker.RemoteQueries.JsonSerialization
{
    public sealed class JsonRemoteQuerySerializer : IRemoteQuerySerializer
    {
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public string MediaType => "application/json";

        public string Serialize<T>(T query)
        {
            return JsonConvert.SerializeObject(query, _serializerSettings);
        }
        
        public T Deserialize<T>(Stream stream)
        {
            using (var streamReader = new StreamReader(stream))
            using (var reader = new JsonTextReader(streamReader))
            {
                return new JsonSerializer().Deserialize<T>(reader);
            }
        }
    }
}
