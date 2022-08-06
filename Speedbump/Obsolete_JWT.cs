using Newtonsoft.Json.Linq;

using System.Security.Cryptography;
using System.Text;

namespace Speedbump
{
    [Obsolete]
    public static class JWT
    {
        public static JObject CreatePayload(JObject discord, long expiration = 60 * 60 * 24 * 6 /* 6 days */)
        {
            var now = DateTimeOffset.Now.ToUnixTimeSeconds();

            return new JObject()
            {
                ["iss"] = "Speedbump",
                ["aud"] = "Speedbump",
                ["sub"] = discord["id"],
                ["exp"] = now + expiration,
                ["nbf"] = now,
                ["iat"] = now,
                ["jti"] = Snowflake.Generate().ToString(),
                ["discord"] = discord,
            };
        }

        public static string Issue(JObject payload, string jwtSecret)
        {
            // JWT Issuing
            var header = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";
            var prefix = $"{header}.{Encoding.UTF8.GetBytes(payload.ToString()).Base64UrlEncode()}";
            if (prefix.Contains('='))
            {
                prefix = prefix[..prefix.IndexOf('=')];
            }

            var secret = jwtSecret;
            var signatureBytes = HMACSHA256.HashData(Encoding.ASCII.GetBytes(secret), Encoding.UTF8.GetBytes(prefix));
            var signature = signatureBytes.Base64UrlEncode();
            return $"{prefix}.{signature}";
        }

        public static JWTStatus Validate(string jwt, string jwtSecret)
        {
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length != 3) { return JWTStatus.InvalidSyntax; }

                if (parts[0] != "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9") { return JWTStatus.InvalidSyntax; }

                var signature = parts[2];
                var prefix = jwt[..jwt.LastIndexOf('.')];
                var signatureBytes = HMACSHA256.HashData(Encoding.ASCII.GetBytes(jwtSecret), Encoding.UTF8.GetBytes(prefix));
                if (signatureBytes.Base64UrlEncode() != signature) { return JWTStatus.InvalidSignature; }

                var payloadString = Encoding.UTF8.GetString(parts[1].Base64UrlDecode());
                var payload = JObject.Parse(payloadString);
                var now = DateTimeOffset.Now.ToUnixTimeSeconds();
                if (long.Parse(payload["exp"].Value<string>()) < now ||
                    long.Parse(payload["nbf"].Value<string>()) > now)
                {
                    return JWTStatus.Expired;
                }

                return JWTStatus.Valid;
            }
            catch
            {
                return JWTStatus.InvalidSyntax;
            }
        }

        public static JObject GetPayload(string jwt, string jwtSecret)
        {
            if (Validate(jwt, jwtSecret) != JWTStatus.Valid) { return null; }
            var payloadString = Encoding.UTF8.GetString(jwt.Split('.')[1].Base64UrlDecode());
            return JObject.Parse(payloadString);
        }

        public enum JWTStatus
        {
            Valid,
            InvalidSyntax,
            InvalidSignature,
            Expired,
        }
    }
}
