using Testcontainers.Redis;
using Xunit.Abstractions;

namespace RxDBDotNet.Tests.Setup;

public static class RedisSetupUtil
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private static volatile bool _isInitialized;

    public static async Task SetupAsync(
        ITestOutputHelper output,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(output);

        await Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!_isInitialized)
            {
                output.WriteLine("Initializing redis");

                var redisContainer = new RedisBuilder()
                    .WithImage("redis:7.0")
                    .WithPortBinding(3333, 6379)
                    .Build();

                await redisContainer.StartAsync(cancellationToken).ConfigureAwait(false);

                _isInitialized = true;
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }
}
