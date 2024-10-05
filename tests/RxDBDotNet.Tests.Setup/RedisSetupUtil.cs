using Docker.DotNet;
using Docker.DotNet.Models;
using Testcontainers.Redis;

namespace RxDBDotNet.Tests.Setup
{
    public static class RedisSetupUtil
    {
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

        private static async Task SetupInternalAsync()
        {
            const string containerName = "rxdbdotnet_test_redis";

            Console.WriteLine("Starting Redis setup...");
            using var dockerClientConfiguration = new DockerClientConfiguration();
            using var client = dockerClientConfiguration.CreateClient();

            var existingContainers = await client.Containers.ListContainersAsync(new ContainersListParameters { All = true });
            var existingContainer = existingContainers.FirstOrDefault(c => c.Names.Contains($"/{containerName}", StringComparer.OrdinalIgnoreCase));

            if (existingContainer != null)
            {
                if (!string.Equals(existingContainer.State, "running", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Starting existing Redis container: {existingContainer.ID}");
                    await client.Containers.StartContainerAsync(existingContainer.ID, new ContainerStartParameters());
                    Console.WriteLine($"Started existing Redis container: {existingContainer.ID}");
                }
                else
                {
                    Console.WriteLine($"Redis container is already running: {existingContainer.ID}");
                }
            }
            else
            {
                Console.WriteLine("Creating new Redis container...");
                var redisContainer = new RedisBuilder()
                    .WithName(containerName)
                    .WithImage("redis:7.0")
                    .WithPortBinding(3333, 6379)
                    .WithCleanUp(false)
                    .Build();

                await redisContainer.StartAsync();
                Console.WriteLine("Started new Redis container");
            }
        }
    }
}
