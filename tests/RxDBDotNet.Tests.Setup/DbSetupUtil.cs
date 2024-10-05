using Docker.DotNet;
using Docker.DotNet.Models;
using LiveDocs.GraphQLApi.Infrastructure;
using Microsoft.Extensions.Logging;
using Polly;
using Testcontainers.MsSql;

namespace RxDBDotNet.Tests.Setup;

public static class DbSetupUtil
{
    private const string DbConnectionString = "Server=127.0.0.1,1445;Database=LiveDocsTestDb;User Id=sa;Password=Admin123!;TrustServerCertificate=True";
    private static readonly ILogger Logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger(typeof(DbSetupUtil));
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
            using var dockerClientConfiguration = new DockerClientConfiguration();
            using var client = dockerClientConfiguration.CreateClient();

            var existingContainers = await client.Containers.ListContainersAsync(new ContainersListParameters { All = true });
            var existingContainer = existingContainers.FirstOrDefault(c => c.Names.Contains($"/{containerName}", StringComparer.OrdinalIgnoreCase));

            if (existingContainer != null)
            {
                if (!string.Equals(existingContainer.State, "running", StringComparison.OrdinalIgnoreCase))
                {
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

    private static Task InitializeDatabaseWithRetry()
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, _) => Logger.LogWarning(exception, "Error during database initialization (Attempt {RetryCount}). Retrying in {RetryTimeSpan}...", retryCount, timeSpan));

        return policy.ExecuteAsync(async () =>
        {
            await LiveDocsDbInitializer.InitializeAsync();
            Logger.LogInformation("Database initialized successfully");
        });
    }
}
