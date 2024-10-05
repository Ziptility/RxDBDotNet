using Docker.DotNet;
using Docker.DotNet.Models;
using LiveDocs.GraphQLApi.Infrastructure;
using Polly;
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
            if (_isInitialized) return;

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
            Environment.SetEnvironmentVariable(ConfigKeys.DbConnectionString, DbConnectionString);

            const string containerName = "rxdbdotnet_test_db";
            const string saPassword = "Admin123!";

            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, _) => Console.WriteLine($"Error during database setup (Attempt {retryCount}). Retrying in {timeSpan}... Error: {exception.Message}"));

            return policy.ExecuteAsync(async () =>
            {
                Console.WriteLine("Starting database setup...");
                using var dockerClientConfiguration = new DockerClientConfiguration();
                using var client = dockerClientConfiguration.CreateClient();

                var existingContainers = await client.Containers.ListContainersAsync(new ContainersListParameters { All = true });
                var existingContainer = existingContainers.FirstOrDefault(c => c.Names.Contains($"/{containerName}", StringComparer.OrdinalIgnoreCase));

                if (existingContainer != null)
                {
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

                await InitializeDatabaseWithRetry();
            });
        }

        private static Task InitializeDatabaseWithRetry()
        {
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, _) => Console.WriteLine($"Error during database initialization (Attempt {retryCount}). Retrying in {timeSpan}... Error: {exception.Message}"));

            return policy.ExecuteAsync(async () =>
            {
                Console.WriteLine("Initializing database...");
                await LiveDocsDbInitializer.InitializeAsync();
                Console.WriteLine("Database initialized successfully");
            });
        }
    }
}
