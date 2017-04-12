using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Paramore.Darker.QueryLogging
{
    public sealed class NewtonsftJsonSerializer
    {
        private readonly JsonSerializerSettings _serializerSettings;

        public NewtonsftJsonSerializer(JsonSerializerSettings serializerSettings = null)
        {
            _serializerSettings = serializerSettings ?? new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Include,
                DateFormatString = "O", // ISO 8601: 2009-06-15T13:45:30.0000000-07:00
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                Converters = new List<JsonConverter>
                {
                    new StringEnumConverter()
                }
            };
        }

        public string Serialize<T>(T value)
        {
            return JsonConvert.SerializeObject(value, _serializerSettings);
        }
    }
}