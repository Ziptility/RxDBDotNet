using Testcontainers.MsSql;
using Xunit.Abstractions;

namespace RxDBDotNet.Tests.Setup;

public static class UnitTestDbUtil
{
    private static readonly SemaphoreSlim Semaphore = new(
        1,
        1);

    private static volatile bool _isInitialized;

    public static async Task InitializeAsync(ITestOutputHelper output)
    {
        ArgumentNullException.ThrowIfNull(output);

        await Semaphore.WaitAsync();

        try
        {
            if (!_isInitialized)
            {
                output.WriteLine("Initializing the unit test db");

                var sqlServerDockerContainer = new MsSqlBuilder()
                    .WithName("livedocs_test_db")
                    .WithPassword("Password123!")
                    .Build();

                await sqlServerDockerContainer.StartAsync();

                var connectionStringToMaster = sqlServerDockerContainer.GetConnectionString();

                var connectionStringToTestDb = connectionStringToMaster.Replace("master", "LiveDocsTestDb");

                // Environment.SetEnvironmentVariable(
                //     ConfigKeys.DbConnectionString,
                //     connectionStringToTestDb);

                // var dockerPortId = await DockerSqlDatabaseUtilities
                //     .EnsureDockerStartedAndGetContainerIdAndPortAsync();
                //
                // const string dbName = "UnitTestDatabase";
                //
                // var sqlConnectionString = DockerSqlDatabaseUtilities.GetSqlConnectionStringToTestDb(
                //     dbName,
                //     dockerPortId);
                //
                // Environment.SetEnvironmentVariable(
                //     ConfigKeys.DbConnectionString,
                //     sqlConnectionString);
                //
                // var databaseUpgradeResult = DbUpScriptRunner.Run(
                //     sqlConnectionString,
                //     ZiptilityEnvironment.UnitTest);
                //
                // if (!databaseUpgradeResult.Successful)
                // {
                //     throw new InvalidStateException(
                //         "Database migration failed. See inner exception for details",
                //         databaseUpgradeResult.Error);
                // }

                _isInitialized = true;
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }
}
