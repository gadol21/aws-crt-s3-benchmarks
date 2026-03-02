# s3-benchrunner-dotnet

s3-benchrunner for [AWS SDK for .NET](https://github.com/aws/aws-sdk-net) (AWSSDK.S3).

## Requirements

*   .NET 9.0 SDK

## Building

```sh
cd aws-crt-s3-benchmarks/runners/s3-benchrunner-dotnet
dotnet publish -c Release
```

This produces a native AOT executable at: `bin/Release/net9.0/<RID>/publish/s3-benchrunner-dotnet` (e.g. `linux-x64`, `linux-arm64`)

## Running

The `RUNNER_CMD` is:

`dotnet run --project path/to/s3-benchrunner-dotnet.csproj -c Release -- [args...]`

or if you published the binary:

`path/to/s3-benchrunner-dotnet [args...]`

The args are described [here](../../README.md#run-a-benchmark).

### S3 Clients

| S3_CLIENT | Description |
|-----------|-------------|
| `sdk-dotnet-client` | Uses `AmazonS3Client` directly for each transfer |
| `sdk-dotnet-tm` | Uses `TransferUtility` for high-level managed transfers |
