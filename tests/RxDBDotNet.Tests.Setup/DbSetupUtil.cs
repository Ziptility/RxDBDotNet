using Docker.DotNet;
using Docker.DotNet.Models;
using LiveDocs.GraphQLApi.Infrastructure;
using Microsoft.Extensions.Logging;
using Polly;
using Testcontainers.MsSql;

namespace RxDBDotNet.Tests.Setup
{
    /// <summary>
    /// Utility class for setting up and initializing the database for RxDBDotNet tests.
    /// </summary>
    public static class DbSetupUtil
    {
        private const string DbConnectionString = "Server=127.0.0.1,1445;Database=LiveDocsTestDb;User Id=sa;Password=Admin123!;TrustServerCertificate=True";
        private static readonly ILogger Logger = LoggingConfig.LoggerFactory.CreateLogger(nameof(DbSetupUtil));
        private static readonly SemaphoreSlim Semaphore = new(1, 1);
        private static volatile bool _isInitialized;

        /// <summary>
        /// Sets up the database environment for testing.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Internal method to set up the database environment.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static Task SetupInternalAsync()
        {
            Environment.SetEnvironmentVariable(ConfigKeys.DbConnectionString, DbConnectionString);

            const string containerName = "rxdbdotnet_test_db";
            const string saPassword = "Admin123!";

            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, _) => Logger.LogWarning(exception, "Error during database setup (Attempt {RetryCount}). Retrying in {RetryTimeSpan}...", retryCount, timeSpan));

            return policy.ExecuteAsync(async () =>
            {
                Logger.LogInformation("Starting database setup...");
                using var dockerClientConfiguration = new DockerClientConfiguration();
                using var client = dockerClientConfiguration.CreateClient();

                var existingContainers = await client.Containers.ListContainersAsync(new ContainersListParameters { All = true });
                var existingContainer = existingContainers.FirstOrDefault(c => c.Names.Contains($"/{containerName}", StringComparer.OrdinalIgnoreCase));

                if (existingContainer != null)
                {
                    if (!string.Equals(existingContainer.State, "running", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.LogInformation("Starting existing SQL Server container: {ContainerId}", existingContainer.ID);
                        await client.Containers.StartContainerAsync(existingContainer.ID, new ContainerStartParameters());
                        Logger.LogInformation("Started existing SQL Server container: {ContainerId}", existingContainer.ID);
                    }
                    else
                    {
                        Logger.LogInformation("SQL Server container is already running: {ContainerId}", existingContainer.ID);
                    }
                }
                else
                {
                    Logger.LogInformation("Creating new SQL Server container...");
                    var sqlServerContainer = new MsSqlBuilder()
                        .WithName(containerName)
                        .WithPassword(saPassword)
                        .WithPortBinding(1445, 1433)
                        .WithCleanUp(false)
                        .Build();

                    await sqlServerContainer.StartAsync();
                    Logger.LogInformation("Started new SQL Server container");
                }

                await InitializeDatabaseWithRetry();
            });
        }

        /// <summary>
        /// Initializes the database with retry logic.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static Task InitializeDatabaseWithRetry()
        {
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, _) => Logger.LogWarning(exception, "Error during database initialization (Attempt {RetryCount}). Retrying in {RetryTimeSpan}...", retryCount, timeSpan));

            return policy.ExecuteAsync(async () =>
            {
                Logger.LogInformation("Initializing database...");
                await LiveDocsDbInitializer.InitializeAsync();
                Logger.LogInformation("Database initialized successfully");
            });
        }
    }
}
