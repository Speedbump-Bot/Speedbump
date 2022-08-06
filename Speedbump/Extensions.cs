using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Net;

namespace Speedbump
{
    public static class Extensions
    {
        public static string Base64UrlEncode(this byte[] arr) =>
            Convert.ToBase64String(arr).Replace("+", "-").Replace("/", "_").Replace("=", "");
        public static byte[] Base64UrlDecode(this string s)
        {
            var a = s.Replace("-", "+").Replace("_", "/");
            a = a + new string('=', (4 - (a.Length % 4)) % 4);
            return Convert.FromBase64String(a);
        }

        public static JObject User(this ControllerBase con) => con.HttpContext.Items["userData"] as JObject;
        public static JObject Discord(this ControllerBase con) => (con.HttpContext.Items["userData"] as JObject)["discord"] as JObject;
        public static ContentResult Respond(this ControllerBase con, object response, HttpStatusCode status)
        {
            APIResult modal;

            if (status == HttpStatusCode.Unauthorized)
            {
                modal = new APIResult()
                {
                    Status = status,
                    RequestReceived = -1,
                    ResponseSent = -1,
                    Response = null,
                };
            }
            else
            {
                modal = new APIResult()
                {
                    RequestReceived = ((DateTimeOffset)con.HttpContext.Items["RequestTime"]).ToUnixTimeMilliseconds(),
                    ResponseSent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Status = status,
                    Response = response,
                };
            }

            return new ContentResult()
            {
                Content = JsonConvert.SerializeObject(modal),
                ContentType = "application/json",
                StatusCode = (int)status,
            };
        }

        public static Stream StreamFromString(this string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
