using Microsoft.Extensions.Logging;
using Polly;
using Xunit;

namespace RxDBDotNet.Tests.Setup;

public sealed class DockerSetupUtil : IAsyncLifetime
{
    private static readonly ILogger<DockerSetupUtil> Logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<DockerSetupUtil>();
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    private static volatile bool _isInitialized;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        await Semaphore.WaitAsync();
        try
        {
            if (!_isInitialized)
            {
                await InitializeAsyncInternal();
                _isInitialized = true;
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static Task InitializeAsyncInternal()
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, _) => Logger.LogWarning(exception, "Error during Docker setup (Attempt {RetryCount}). Retrying in {RetryTimeSpan}...", retryCount, timeSpan));

        return policy.ExecuteAsync(async () =>
        {
            await RedisSetupUtil.SetupAsync();
            await DbSetupUtil.SetupAsync();
        });
    }
}
