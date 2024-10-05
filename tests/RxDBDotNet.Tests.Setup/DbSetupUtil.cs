using Docker.DotNet;
using Docker.DotNet.Models;
using LiveDocs.GraphQLApi.Infrastructure;
using Testcontainers.MsSql;

namespace RxDBDotNet.Tests.Setup
{
    public static class DbSetupUtil
    {
        private const string DbConnectionString = "Server=127.0.0.1,1445;Database=LiveDocsTestDb;User Id=sa;Password=Admin123!;TrustServerCertificate=True";
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
            Environment.SetEnvironmentVariable(ConfigKeys.DbConnectionString, DbConnectionString);

            const string containerName = "rxdbdotnet_test_db";
            const string saPassword = "Admin123!";

            Console.WriteLine("Starting database setup...");
            using var dockerClientConfiguration = new DockerClientConfiguration();
            using var client = dockerClientConfiguration.CreateClient();

            try
            {
                var existingContainers = await client.Containers.ListContainersAsync(new ContainersListParameters { All = true });
                var existingContainer = existingContainers.FirstOrDefault(c => c.Names.Contains($"/{containerName}", StringComparer.OrdinalIgnoreCase));

                if (existingContainer != null)
                {
                    Console.WriteLine($"Found existing container: {existingContainer.ID}, State: {existingContainer.State}");
                    if (!string.Equals(existingContainer.State, "running", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Starting existing SQL Server container: {existingContainer.ID}");
                        await client.Containers.StartContainerAsync(existingContainer.ID, new ContainerStartParameters());
                        Console.WriteLine($"Started existing SQL Server container: {existingContainer.ID}");
                    }
                    else
                    {
                        Console.WriteLine($"SQL Server container is already running: {existingContainer.ID}");
                    }
                }
                else
                {
                    Console.WriteLine("Creating new SQL Server container...");
                    var sqlServerContainer = new MsSqlBuilder()
                        .WithName(containerName)
                        .WithPassword(saPassword)
                        .WithPortBinding(1445, 1433)
                        .WithCleanUp(false)
                        .Build();

                    await sqlServerContainer.StartAsync();
                    Console.WriteLine("Started new SQL Server container");
                }

                // Wait for the container to be fully started
                await Task.Delay(TimeSpan.FromSeconds(10));

                Console.WriteLine("Checking container status...");
                var containers = await client.Containers.ListContainersAsync(new ContainersListParameters { All = true });
                var container = containers.FirstOrDefault(c => c.Names.Contains($"/{containerName}", StringComparer.OrdinalIgnoreCase));
                if (container != null)
                {
                    Console.WriteLine($"Container status: {container.State}, {container.Status}");
                }
                else
                {
                    Console.WriteLine("Container not found after starting!");
                }

                await InitializeDatabase();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during database setup: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private static async Task InitializeDatabase()
        {
            Console.WriteLine("Initializing database...");
            await LiveDocsDbInitializer.InitializeAsync();
            Console.WriteLine("Database initialized successfully");
        }
    }
}
