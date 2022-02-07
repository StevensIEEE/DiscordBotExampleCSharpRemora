using System.Text.Json;

namespace BotExample
{
    internal class Configuration
    {
        public string Token { get; set; } = null!;
        public string? TestServerId { get; set; }
        public string? InviteLink { get; set; }

        public static Configuration ReadConfig(string configPath = "config.json")
        {
            string jsonString = File.ReadAllText(configPath);
            JsonSerializerOptions? options = new()
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                WriteIndented = true
            };
            Configuration configuration = JsonSerializer.Deserialize<Configuration>(jsonString, options) ?? throw new InvalidOperationException("Configuration cannot be null");
            return configuration;
        }
    }
}
