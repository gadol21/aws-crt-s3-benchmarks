using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace S3BenchrunnerDotnet;

public class SdkDotnetBenchmarkRunner : BenchmarkRunner
{
    private const int MaxConcurrency = 1000;
    private readonly SemaphoreSlim _semaphore = new(MaxConcurrency);
    private readonly IAmazonS3 _s3Client;
    private readonly TransferUtility? _transferUtility;
    private readonly bool _useTransferUtility;

    public SdkDotnetBenchmarkRunner(
        BenchmarkConfig config,
        string bucket,
        string region,
        double targetThroughputGbps,
        bool useTransferUtility)
        : base(config, bucket, region)
    {
        _useTransferUtility = useTransferUtility;

        var s3Config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(region),
        };

        _s3Client = new AmazonS3Client(s3Config);

        if (useTransferUtility)
        {
            if (!config.FilesOnDisk)
            {
                Util.ExitWithSkipCode("TransferUtility cannot run tasks unless they're on disk");
            }

            _transferUtility = new TransferUtility(_s3Client);
        }
    }

    public override async Task RunAsync()
    {
        if (_useTransferUtility)
        {
            await RunWithTransferUtilityAsync();
        }
        else
        {
            await RunWithClientAsync();
        }
    }

    private async Task RunWithClientAsync()
    {
        var tasks = new List<Task>(Config.Tasks.Count);
        foreach (var taskConfig in Config.Tasks)
        {
            if (taskConfig.Action == "upload")
            {
                tasks.Add(UploadWithClientAsync(taskConfig));
            }
            else if (taskConfig.Action == "download")
            {
                tasks.Add(DownloadWithClientAsync(taskConfig));
            }
            else
            {
                throw new InvalidOperationException($"Unknown task action: {taskConfig.Action}");
            }
        }
        await Task.WhenAll(tasks);
    }

    private async Task UploadWithClientAsync(TaskConfig taskConfig)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (Config.FilesOnDisk)
            {
                var request = new PutObjectRequest
                {
                    BucketName = Bucket,
                    Key = taskConfig.Key,
                    FilePath = taskConfig.Key,
                };
                await _s3Client.PutObjectAsync(request);
            }
            else
            {
                using var stream = new RandomDataStream(RandomDataForUpload!, taskConfig.Size);
                var request = new PutObjectRequest
                {
                    BucketName = Bucket,
                    Key = taskConfig.Key,
                    InputStream = stream,
                };
                await _s3Client.PutObjectAsync(request);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task DownloadWithClientAsync(TaskConfig taskConfig)
    {
        await _semaphore.WaitAsync();
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = Bucket,
                Key = taskConfig.Key,
            };

            using var response = await _s3Client.GetObjectAsync(request);
            if (Config.FilesOnDisk)
            {
                await response.WriteResponseStreamToFileAsync(taskConfig.Key, false, CancellationToken.None);
            }
            else
            {
                // Read and discard the data
                var buffer = new byte[8 * 1024 * 1024];
                while (await response.ResponseStream.ReadAsync(buffer) > 0) { }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task RunWithTransferUtilityAsync()
    {
        var tasks = new List<Task>(Config.Tasks.Count);
        foreach (var taskConfig in Config.Tasks)
        {
            if (taskConfig.Action == "upload")
            {
                tasks.Add(_transferUtility!.UploadAsync(taskConfig.Key, Bucket, taskConfig.Key));
            }
            else if (taskConfig.Action == "download")
            {
                tasks.Add(_transferUtility!.DownloadAsync(taskConfig.Key, Bucket, taskConfig.Key));
            }
            else
            {
                throw new InvalidOperationException($"Unknown task action: {taskConfig.Action}");
            }
        }
        await Task.WhenAll(tasks);
    }
}

/// <summary>
/// A stream that produces random data by repeating a small buffer,
/// to avoid allocating a huge buffer for large uploads.
/// </summary>
internal class RandomDataStream : Stream
{
    private readonly byte[] _buffer;
    private readonly long _length;
    private long _position;

    public RandomDataStream(byte[] buffer, long length)
    {
        _buffer = buffer;
        _length = length;
        _position = 0;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set => _position = value;
    }

    public override int Read(byte[] dest, int offset, int count)
    {
        long remaining = _length - _position;
        if (remaining <= 0)
            return 0;

        int toRead = (int)Math.Min(count, remaining);
        int totalRead = 0;

        while (totalRead < toRead)
        {
            int bufferOffset = (int)(_position % _buffer.Length);
            int chunkSize = Math.Min(toRead - totalRead, _buffer.Length - bufferOffset);
            Buffer.BlockCopy(_buffer, bufferOffset, dest, offset + totalRead, chunkSize);
            _position += chunkSize;
            totalRead += chunkSize;
        }

        return totalRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        _position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };
        return _position;
    }

    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
