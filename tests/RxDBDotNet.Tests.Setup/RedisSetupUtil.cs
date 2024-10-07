using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Testcontainers.Redis;

namespace RxDBDotNet.Tests.Setup;

public static class RedisSetupUtil
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private static volatile bool _isInitialized;

    public static async Task SetupAsync()
    {
        await Semaphore.WaitAsync();

        try
        {
            if (!_isInitialized)
            {
                const string containerName = "rxdbdotnet_test_redis";

                // Check if the container already exists
                using var dockerClientConfiguration = new DockerClientConfiguration();
                using var client = dockerClientConfiguration.CreateClient();
                var existingContainers = await client.Containers.ListContainersAsync(new ContainersListParameters
                {
                    All = true,
                });
                var existingContainer =
                    existingContainers.FirstOrDefault(c => c.Names.Contains($"/{containerName}", StringComparer.OrdinalIgnoreCase));

                if (existingContainer != null)
                {
                    if (!string.Equals(existingContainer.State, "running", StringComparison.OrdinalIgnoreCase))
                    {
                        // Start the existing container
                        await client.Containers.StartContainerAsync(existingContainer.ID, new ContainerStartParameters());
                    }
                }
                else
                {
                    var redisContainer = new RedisBuilder().WithName(containerName)
                        .WithImage("redis:latest")
                        .WithPortBinding(3333, 6379)
                        .WithCleanUp(false)
                        .Build();

                    await redisContainer.StartAsync();
                }

                _isInitialized = true;
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }
}
