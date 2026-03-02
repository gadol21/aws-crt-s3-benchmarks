namespace S3BenchrunnerDotnet;

public abstract class BenchmarkRunner
{
    public BenchmarkConfig Config { get; }
    public string Bucket { get; }
    public string Region { get; }
    public byte[]? RandomDataForUpload { get; }

    protected BenchmarkRunner(BenchmarkConfig config, string bucket, string region)
    {
        Config = config;
        Bucket = bucket;
        Region = region;

        if (!config.FilesOnDisk)
        {
            RandomDataForUpload = Util.GenerateRandomData();
        }
    }

    public abstract Task RunAsync();

    public void PrepareRun()
    {
        if (!Config.FilesOnDisk)
            return;

        foreach (var task in Config.Tasks)
        {
            if (task.Action == "download")
            {
                var dir = Path.GetDirectoryName(task.Key);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                if (File.Exists(task.Key))
                {
                    File.Delete(task.Key);
                }
            }
            else if (task.Action == "upload")
            {
                if (!File.Exists(task.Key))
                {
                    Util.ExitWithError($"File not found: {task.Key}");
                }
            }
        }
    }
}
