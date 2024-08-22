using Docker.DotNet.Models;
using Docker.DotNet;
using LiveDocs.GraphQLApi.Infrastructure;
using Testcontainers.MsSql;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Entities;
using LiveDocs.GraphQLApi.Models.Replication;
using LiveDocs.GraphQLApi.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace RxDBDotNet.Tests.Setup;

public static class DbSetupUtil
{
    private const string DbConnectionString = "Server=127.0.0.1,1445;Database=LiveDocsTestDb;User Id=sa;Password=Password123!;TrustServerCertificate=True";

    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private static volatile bool _isInitialized;

    public static async Task SetupAsync()
    {
        await Semaphore.WaitAsync();
        try
        {
            if (!_isInitialized)
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
                var existingContainer =
                    existingContainers.FirstOrDefault(c => c.Names.Contains($"/{containerName}", StringComparer.OrdinalIgnoreCase));

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

                Environment.SetEnvironmentVariable(ConfigKeys.DbConnectionString, DbConnectionString);

                await using var dbContext = new LiveDocsDbContext();

                await dbContext.Database.EnsureCreatedAsync();

                await SeedDataAsync(dbContext);

                _isInitialized = true;
            }
        }
        finally
        {
            Semaphore.Release();
        }

        Environment.SetEnvironmentVariable(ConfigKeys.DbConnectionString, DbConnectionString);
    }

    private static async Task SeedDataAsync(LiveDocsDbContext dbContext)
    {
        if (await dbContext.Workspaces.AnyAsync())
        {
            return; // Data has already been seeded
        }

        var workspacePk = RT.Comb.Provider.Sql.Create();
        var liveDocsWorkspace = new Workspace
        {
            Id = workspacePk,
            Name = "LiveDocs Org Workspace",
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
            ReplicatedDocumentId = workspacePk,
        };

        await dbContext.Workspaces.AddAsync(liveDocsWorkspace);

        var userPk = RT.Comb.Provider.Sql.Create();
        var systemAdminReplicatedUser = new ReplicatedUser
        {
            Id = userPk,
            FirstName = "System",
            LastName = "Admin",
            Email = "superadmin@livedocs.example.org",
            JwtAccessToken = null,
            WorkspaceId = liveDocsWorkspace.Id,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
        };

        var jwtAccessToken = JwtUtil.GenerateJwtToken(systemAdminReplicatedUser, UserRole.SystemAdmin);

        var superAdminUser = new User
        {
            Id = userPk,
            FirstName = "System",
            LastName = "Admin",
            Email = "systemadmin@livedocs.example.org",
            JwtAccessToken = jwtAccessToken,
            WorkspaceId = liveDocsWorkspace.Id,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
            ReplicatedDocumentId = userPk,
        };

        await dbContext.Users.AddAsync(superAdminUser);

        await dbContext.SaveChangesAsync();
    }
}
