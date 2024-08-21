using DotNet.Testcontainers.Containers;
using LiveDocs.GraphQLApi.Infrastructure;
using Testcontainers.MsSql;

namespace RxDBDotNet.Tests.Setup;

public static class DbSetupUtil
{
    public static async Task<DockerContainer> SetupAsync(CancellationToken cancellationToken)
    {
        var sqlServerDockerContainer = new MsSqlBuilder().WithName("livedocs_test_db")
            .WithPassword("Password123!")
            .WithPortBinding(58000, 1433)
            .Build();

        await sqlServerDockerContainer.StartAsync(cancellationToken);

        var connectionStringToMaster = sqlServerDockerContainer.GetConnectionString();

        // Get the connection string to the LiveDocs test database
        var connectionStringToTestDb = connectionStringToMaster.Replace("master", "LiveDocsTestDb", StringComparison.OrdinalIgnoreCase);

        Environment.SetEnvironmentVariable(ConfigKeys.DbConnectionString, connectionStringToTestDb);

        return sqlServerDockerContainer;
    }
}
