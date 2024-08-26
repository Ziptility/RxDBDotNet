using System.Security.Claims;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Entities;
using Microsoft.AspNetCore.Http;
using RxDBDotNet.Tests.Helpers;

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
                services
                    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = JwtUtil.GetTokenValidationParameters();

                        options.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = ctx =>
                            {
                                if (ctx.Request.Query.TryGetValue("access_token", out var token))
                                {
                                    ctx.Token = token;
                                }

                                return Task.CompletedTask;
                            },
                            OnTokenValidated = ctx =>
                            {
                                if (ctx.Principal is { Identity: not null })
                                {
                                    var claimIdentity = (ClaimsIdentity)ctx.Principal.Identity;
                                    ctx.HttpContext.User = new ClaimsPrincipal(claimIdentity);
                                }

                                return Task.CompletedTask;
                            },
                            OnAuthenticationFailed = ctx =>
                            {
                                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                ctx.Fail(ctx.Exception);
                                return Task.CompletedTask;
                            },
                            OnForbidden = ctx =>
                            {
                                // var sentry = ctx.HttpContext.RequestServices.GetRequiredService<ISentryClient>();
                                // sentry.CaptureException(new ForbiddenException("Your role does not allow you to use this endpoint"));
                                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                                ctx.Fail(nameof(HttpStatusCode.Forbidden));
                                return Task.CompletedTask;
                            },
                        };
                    });

                // Define the security policy for this test where the user must be in the role of a
                // workspace admin to create a workspace
                //services.AddAuthorization();
                services.AddAuthorizationBuilder()
                    .AddPolicy("IsWorkspaceAdmin", policy => policy.RequireRole(nameof(UserRole.WorkspaceAdmin)));
            }

            void ConfigureReplicatedDocuments(IRequestExecutorBuilder graphQLBuilder)
            {
                graphQLBuilder
                    .AddAuthorization()
                    .AddReplicatedDocument<ReplicatedUser>()
                    .AddReplicatedDocument<ReplicatedWorkspace>(
                        options => options.Security.RequirePolicyToCreate("IsWorkspaceAdmin"))
                    .AddReplicatedDocument<ReplicatedLiveDoc>();
            }

            testContext = await TestSetupUtil.SetupAsync(ConfigureApp, ConfigureServices, ConfigureReplicatedDocuments);

            // 1. Create a workspace
            var dbContext = testContext.ServiceProvider.GetRequiredService<LiveDocsDbContext>();
            var workspace = new Workspace
            {
                Id = Provider.Sql.Create(),
                Name = Strings.CreateString(),
                Topics = [],
                UpdatedAt = DateTimeOffset.Now,
                ReplicatedDocumentId = Guid.NewGuid(),
                IsDeleted = false,
            };
            await dbContext.Workspaces.AddAsync(workspace, testContext.CancellationToken);

            // 2. Create a standard user within the workspace
            var standardReplicatedUser = new ReplicatedUser
            {
                Id = Guid.NewGuid(),
                FirstName = Strings.CreateString(),
                LastName = Strings.CreateString(),
                Email = $"{Strings.CreateString()}@livedocs.example.org",
                JwtAccessToken = null,
                WorkspaceId = workspace.ReplicatedDocumentId,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false,
            };

            var standardUserAccessToken = JwtUtil.GenerateJwtToken(standardReplicatedUser, UserRole.StandardUser);

            var standardUser = new User
            {
                Id = Provider.Sql.Create(),
                FirstName = standardReplicatedUser.FirstName,
                LastName = standardReplicatedUser.LastName,
                Email = standardReplicatedUser.Email,
                JwtAccessToken = standardUserAccessToken,
                WorkspaceId = workspace.Id,
                UpdatedAt = standardReplicatedUser.UpdatedAt,
                IsDeleted = standardReplicatedUser.IsDeleted,
                ReplicatedDocumentId = standardReplicatedUser.Id,
            };

            await dbContext.Users.AddAsync(standardUser, testContext.CancellationToken);

            await dbContext.SaveChangesAsync(testContext.CancellationToken);

            // As a standard user:
            await testContext.HttpClient.CreateNewWorkspaceAsync(
                testContext.CancellationToken,
                jwtAccessToken: standardUserAccessToken);

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
