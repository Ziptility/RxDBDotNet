using LiveDocs.GraphQLApi.Infrastructure;
using Testcontainers.MsSql;
using Xunit.Abstractions;

namespace RxDBDotNet.Tests.Setup;

public static class DbSetupUtil
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private static volatile bool _isInitialized;

    public static async Task SetupAsync(
        IServiceProvider serviceProvider,
        ITestOutputHelper output,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(output);

        await Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!_isInitialized)
            {
                output.WriteLine("Initializing the unit test db");

                // Specify a host port that will be consistent across runs
                // const int hostPort = 58000;
                var sqlServerDockerContainer = new MsSqlBuilder()
                    .WithName("livedocs_test_db")
                    .WithPassword("Password123!")
                    // Bind the container's SQL Server port to the host port
                    // .WithPortBinding(hostPort, containerPort: 1433)
                    .Build();

                await sqlServerDockerContainer.StartAsync(cancellationToken).ConfigureAwait(false);

                var connectionStringToMaster = sqlServerDockerContainer.GetConnectionString();

                // Modify the connection string to use the consistent port
                var connectionStringToTestDb = connectionStringToMaster
                    .Replace("master", "LiveDocsTestDb", StringComparison.OrdinalIgnoreCase);

                Environment.SetEnvironmentVariable(
                    ConfigKeys.DbConnectionString,
                    connectionStringToTestDb);

                await LiveDocsDbInitializer.InitializeAsync(serviceProvider, cancellationToken).ConfigureAwait(false);

                _isInitialized = true;
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }
}
