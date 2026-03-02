using System.Diagnostics;

namespace S3BenchrunnerDotnet;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length != 5)
        {
            Console.Error.WriteLine("Expected args: S3_CLIENT WORKLOAD BUCKET REGION TARGET_THROUGHPUT");
            Environment.Exit(1);
        }

        string s3ClientId = args[0];
        string workloadPath = args[1];
        string bucket = args[2];
        string region = args[3];
        double targetThroughputGbps = double.Parse(args[4]);

        BenchmarkConfig config = BenchmarkConfig.FromJson(workloadPath);

        BenchmarkRunner runner = s3ClientId switch
        {
            "sdk-dotnet-client" => new SdkDotnetBenchmarkRunner(config, bucket, region, targetThroughputGbps, useTransferUtility: false),
            "sdk-dotnet-tm" => new SdkDotnetBenchmarkRunner(config, bucket, region, targetThroughputGbps, useTransferUtility: true),
            _ => throw new ArgumentException(
                $"Unsupported S3_CLIENT: {s3ClientId}. Options are: sdk-dotnet-client, sdk-dotnet-tm"),
        };

        long bytesPerRun = config.BytesPerRun();

        // Repeat benchmark until we exceed maxRepeatCount or maxRepeatSecs
        var appStopwatch = Stopwatch.StartNew();
        for (int runI = 0; runI < config.MaxRepeatCount; runI++)
        {
            runner.PrepareRun();

            var runStopwatch = Stopwatch.StartNew();

            await runner.RunAsync();

            runStopwatch.Stop();
            double runSecs = runStopwatch.Elapsed.TotalSeconds;

            Console.WriteLine("Run:{0} Secs:{1:F6} Gb/s:{2:F6}",
                runI + 1,
                runSecs,
                Util.BytesToGigabit(bytesPerRun) / runSecs);

            // break out if we've exceeded maxRepeatSecs
            if (appStopwatch.Elapsed.TotalSeconds >= config.MaxRepeatSecs)
            {
                break;
            }
        }
    }
}
