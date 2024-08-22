using HotChocolate.Execution.Configuration;
using LiveDocs.GraphQLApi.Infrastructure;
using LiveDocs.GraphQLApi.Models.Replication;
using LiveDocs.GraphQLApi.Models.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using RxDBDotNet.Extensions;
using RxDBDotNet.Security;
using RxDBDotNet.Tests.Setup;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class SecurityTests
{
    [Fact]
    public async Task AStandardUserShouldNotBeAbleToCreateAWorkspace()
    {
        TestContext? testContext = null;

        try
        {
            // Arrange
            // Configure security for this test
            void ConfigureServices(IServiceCollection services)
            {
                // Add authentication
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options => options.TokenValidationParameters = JwtUtil.GetTokenValidationParameters());
            }

            void ConfigureReplicatedDocuments(IRequestExecutorBuilder graphQLBuilder)
            {
                graphQLBuilder
                    .AddReplicatedDocument<ReplicatedUser>()
                    .AddReplicatedDocument<ReplicatedWorkspace>(options => options.Security.RequireMinimumRoleToWrite(UserRole.WorkspaceAdmin))
                    .AddReplicatedDocument<ReplicatedLiveDoc>();
            }

            testContext = await TestSetupUtil.SetupAsync(ConfigureServices, ConfigureReplicatedDocuments);

            // As a system admin user:
            // 1. Create a workspace

            // 2. Create a standard user within the workspace

            // As a standard user:

            // Attempt to create a workspace

            // Act

            // Assert
            // The response should contain an unauthorized error
        }
        finally
        {
            if (testContext != null)
            {
                await testContext.DisposeAsync();
            }
        }
    }
}
