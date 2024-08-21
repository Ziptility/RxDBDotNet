using Docker.DotNet.Models;
using Docker.DotNet;
using LiveDocs.GraphQLApi.Infrastructure;
using Testcontainers.MsSql;

namespace RxDBDotNet.Tests.Setup;

public static class DbSetupUtil
{
    public static async Task SetupAsync()
    {
        const string containerName = "rxdbdotnet_test_db";
        const string saPassword = "Password123!";

        // Check if the container already exists
        using var dockerClientConfiguration = new DockerClientConfiguration();
        using var client = dockerClientConfiguration.CreateClient();
        var existingContainers = await client.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true,
        });
        var existingContainer = existingContainers
            .FirstOrDefault(c => c.Names.Contains($"/{containerName}", StringComparer.OrdinalIgnoreCase));

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
            var sqlServerDockerContainer = new MsSqlBuilder().WithName(containerName)
                .WithPassword(saPassword)
                .WithPortBinding(1445, 1433)
                .WithCleanUp(false)
                .Build();

            await sqlServerDockerContainer.StartAsync();
        }

        Environment.SetEnvironmentVariable(
            ConfigKeys.DbConnectionString,
            "Server=127.0.0.1,1445;Database=LiveDocsTestDb;User Id=sa;Password=Password123!;TrustServerCertificate=True");
    }
}
