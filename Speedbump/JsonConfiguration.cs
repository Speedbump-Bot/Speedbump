using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Speedbump
{
    public class JsonConfiguration : IConfiguration
    {
        public JObject Config { get; private set; }

        public JsonConfiguration(Lifetime lifetime)
        {
            if (!File.Exists("config.json"))
            {
                throw new FileNotFoundException("config.json does not exist.");
            }
            var data = File.ReadAllText("config.json");

            lifetime.Add((_) =>
            {
                Config = null;
            }, Lifetime.ExitOrder.Configuration);
            Config = JObject.Parse(data);
        }

        public T Get<T>(string path)
        {
            var token = Config.SelectToken(path);
            return token.Value<T>();
        }
    }
}
