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
            void ConfigureApp(IApplicationBuilder app)
            {
                app.UseAuthentication();
                app.UseAuthorization();
            }

            void ConfigureServices(IServiceCollection services)
            {
                // Add authentication
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options => options.TokenValidationParameters = JwtUtil.GetTokenValidationParameters());

                // Define the security policy for this test where the user must be in the role of a workspace admin to create a workspace
                services.AddAuthorizationBuilder()
                    .AddPolicy(nameof(UserRole.WorkspaceAdmin), policy => policy.RequireRole(nameof(UserRole.WorkspaceAdmin)));
            }

            void ConfigureReplicatedDocuments(IRequestExecutorBuilder graphQLBuilder)
            {
                graphQLBuilder
                    .AddAuthorization()
                    .AddReplicatedDocument<ReplicatedUser>()
                    .AddReplicatedDocument<ReplicatedWorkspace>(options => options.Security.RequirePolicyToCreate(nameof(UserRole.WorkspaceAdmin)))
                    .AddReplicatedDocument<ReplicatedLiveDoc>();
            }

            testContext = await TestSetupUtil.SetupAsync(ConfigureApp, ConfigureServices, ConfigureReplicatedDocuments);

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
