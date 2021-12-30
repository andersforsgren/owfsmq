using System.IO;
using System.Text.Json;

namespace owfsmq
{
    public interface IConfigProvider
    {
        Config GetConfig();
    }

    public class JsonConfigProvider : IConfigProvider
    {
        private readonly string path;
        private Config config;

        public JsonConfigProvider(string path)
        {
            this.path = path;
        }

        public Config GetConfig()
        {
            if (config == null)
            {
                var json = File.ReadAllText(path);
                this.config = JsonSerializer.Deserialize<Config>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
            return config;
        }
    }
}
