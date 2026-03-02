namespace S3BenchrunnerDotnet;

public static class Util
{
    public static double NanoToSecs(long nanoseconds)
    {
        return (double)nanoseconds / 1_000_000_000;
    }

    public static double BytesToGigabit(long bytes)
    {
        return (double)bytes * 8 / 1_000_000_000;
    }

    public static void ExitWithError(string msg)
    {
        Console.Error.WriteLine("FAIL - " + msg);
        Environment.Exit(255);
    }

    public static void ExitWithSkipCode(string msg)
    {
        Console.Error.WriteLine("Skipping benchmark - " + msg);
        Environment.Exit(123);
    }

    public static byte[] GenerateRandomData()
    {
        var data = new byte[8 * 1024 * 1024]; // 8 MiB
        Random.Shared.NextBytes(data);
        return data;
    }
}
