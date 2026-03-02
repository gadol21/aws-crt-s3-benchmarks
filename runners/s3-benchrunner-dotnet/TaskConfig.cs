using System.Text.Json.Serialization;

namespace S3BenchrunnerDotnet;

public class TaskConfig
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }
}
