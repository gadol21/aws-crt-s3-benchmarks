using System.Text.Json;
using System.Text.Json.Serialization;

namespace S3BenchrunnerDotnet;

[JsonSerializable(typeof(BenchmarkConfig))]
internal partial class BenchmarkConfigContext : JsonSerializerContext { }

public class BenchmarkConfig
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("filesOnDisk")]
    public bool FilesOnDisk { get; set; }

    [JsonPropertyName("checksum")]
    public string? Checksum { get; set; }

    [JsonPropertyName("maxRepeatCount")]
    public int MaxRepeatCount { get; set; }

    [JsonPropertyName("maxRepeatSecs")]
    public double MaxRepeatSecs { get; set; }

    [JsonPropertyName("tasks")]
    public List<TaskConfig> Tasks { get; set; } = new();

    public static BenchmarkConfig FromJson(string jsonFilepath)
    {
        string jsonString = File.ReadAllText(jsonFilepath);

        var config = JsonSerializer.Deserialize(jsonString, BenchmarkConfigContext.Default.BenchmarkConfig);
        if (config == null)
        {
            Util.ExitWithSkipCode($"Can't parse '{jsonFilepath}'");
            throw new InvalidOperationException(); // unreachable
        }

        if (config.Version != 2)
        {
            Util.ExitWithSkipCode($"Workload version not supported: {config.Version}");
        }

        return config;
    }

    public long BytesPerRun()
    {
        long bytes = 0;
        foreach (var task in Tasks)
        {
            bytes += task.Size;
        }
        return bytes;
    }
}
