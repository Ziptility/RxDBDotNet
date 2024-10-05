using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Testcontainers.Redis;

namespace RxDBDotNet.Tests.Setup;

public static class RedisSetupUtil
{
    private static readonly ILogger Logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger(typeof(RedisSetupUtil));
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    private static volatile bool _isInitialized;

    public static async Task SetupAsync()
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
                await SetupInternalAsync();
                _isInitialized = true;
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private static Task SetupInternalAsync()
    {
        const string containerName = "rxdbdotnet_test_redis";

        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, _) => Logger.LogWarning(exception, "Error during Redis setup (Attempt {RetryCount}). Retrying in {RetryTimeSpan}...", retryCount, timeSpan));

        return policy.ExecuteAsync(async () =>
        {
            using var dockerClientConfiguration = new DockerClientConfiguration();
            using var client = dockerClientConfiguration.CreateClient();

            var existingContainers = await client.Containers.ListContainersAsync(new ContainersListParameters { All = true });
            var existingContainer = existingContainers.FirstOrDefault(c => c.Names.Contains($"/{containerName}", StringComparer.OrdinalIgnoreCase));

            if (existingContainer != null)
            {
                if (!string.Equals(existingContainer.State, "running", StringComparison.OrdinalIgnoreCase))
                {
                    await client.Containers.StartContainerAsync(existingContainer.ID, new ContainerStartParameters());
                    Logger.LogInformation("Started existing Redis container: {ContainerId}", existingContainer.ID);
                }
                else
                {
                    Logger.LogInformation("Redis container is already running: {ContainerId}", existingContainer.ID);
                }
            }
            else
            {
                var redisContainer = new RedisBuilder()
                    .WithName(containerName)
                    .WithImage("redis:7.0")
                    .WithPortBinding(3333, 6379)
                    .WithCleanUp(false)
                    .Build();

                await redisContainer.StartAsync();
                Logger.LogInformation("Started new Redis container");
            }
        });
    }
}
