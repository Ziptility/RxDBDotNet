using Docker.DotNet;
using Docker.DotNet.Models;
using Testcontainers.Redis;

namespace RxDBDotNet.Tests.Setup;

public static class RedisSetupUtil
{
    public static async Task SetupAsync()
    {
        const string containerName = "rxdbdotnet_test_redis";

        // Check if the container already exists
        using var dockerClientConfiguration = new DockerClientConfiguration();
        using var client = dockerClientConfiguration.CreateClient();
        var existingContainers = await client.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true,
        });
        var existingContainer = existingContainers.FirstOrDefault(c => c.Names.Contains($"/{containerName}", StringComparer.OrdinalIgnoreCase));

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
                .WithImage("redis:7.0")
                .WithPortBinding(3333, 6379)
                .WithCleanUp(false)
                .Build();

            await redisContainer.StartAsync();
        }
    }
}
