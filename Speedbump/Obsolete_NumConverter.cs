using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Speedbump
{
    [Obsolete]
    public class NumConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return JToken.ReadFrom(reader).Value<ulong?>();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.ToString());
        }
    }
}
