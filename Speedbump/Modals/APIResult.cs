using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System.Net;

namespace Speedbump
{
    public class APIResult
    {
        public long RequestReceived { get; set; }
        public long ResponseSent { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public HttpStatusCode Status { get; set; }
        public object Response { get; set; }
    }
}
