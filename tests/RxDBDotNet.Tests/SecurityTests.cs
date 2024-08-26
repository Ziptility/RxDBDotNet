using HotChocolate.AspNetCore;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Entities;
using Microsoft.AspNetCore.Http;
using RxDBDotNet.Security;
using RxDBDotNet.Tests.Helpers;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class SecurityTests
{
    [Fact]
    public async Task AWorkspaceAdminShouldBeAbleToCreateAWorkspace()
    {
        TestContext? testContext = null;

        try
        {
            // Arrange
            // Configure security for this test
            void ConfigureApp(IApplicationBuilder app)
            {
                app.UseExceptionHandler();
                app.UseDeveloperExceptionPage();
                app.UseAuthentication();
                app.UseWebSockets();
                app.UseRouting();
                // UseAuthorization must be between UseRouting and UseEndpoints
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGraphQL()
                        .WithOptions(new GraphQLServerOptions
                    {
                        Tool =
                        {
                            Enable = true,
                        },
                    });
                });
            }

            void ConfigureServices(IServiceCollection services)
            {
                services.AddScoped<AuthorizationHelper>();

                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.Audience = JwtUtil.Audience;
                        options.IncludeErrorDetails = true;
                        options.RequireHttpsMetadata = false;

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
                            OnAuthenticationFailed = ctx =>
                            {
                                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                ctx.Fail(ctx.Exception);
                                return Task.CompletedTask;
                            },
                            OnForbidden = ctx =>
                            {
                                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                                ctx.Fail(nameof(HttpStatusCode.Forbidden));
                                return Task.CompletedTask;
                            },
                        };
                    });

                // Define the security policy for this test where the user must be in the role of a
                // workspace admin to create a workspace
                services.AddAuthorizationBuilder()
                    .AddPolicy("IsWorkspaceAdmin", policy => policy.RequireRole(nameof(UserRole.WorkspaceAdmin)));
            }

            testContext = await TestSetupUtil.SetupAsync(ConfigureApp, ConfigureServices, configureReplicatedDocuments: null);

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
            var workspaceAdminReplicatedUser = new ReplicatedUser
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

            var workspaceAdminToken = JwtUtil.GenerateJwtToken(workspaceAdminReplicatedUser, UserRole.WorkspaceAdmin);

            var workspaceAdminUser = new User
            {
                Id = Provider.Sql.Create(),
                FirstName = workspaceAdminReplicatedUser.FirstName,
                LastName = workspaceAdminReplicatedUser.LastName,
                Email = workspaceAdminReplicatedUser.Email,
                JwtAccessToken = workspaceAdminToken,
                WorkspaceId = workspace.Id,
                UpdatedAt = workspaceAdminReplicatedUser.UpdatedAt,
                IsDeleted = workspaceAdminReplicatedUser.IsDeleted,
                ReplicatedDocumentId = workspaceAdminReplicatedUser.Id,
            };

            await dbContext.Users.AddAsync(workspaceAdminUser, testContext.CancellationToken);

            await dbContext.SaveChangesAsync(testContext.CancellationToken);

            // As a standard user:
            await testContext.HttpClient.CreateNewWorkspaceAsync(
                testContext.CancellationToken,
                jwtAccessToken: workspaceAdminToken);

            // Attempt to create a workspace

            // Act

            // Assert
            // The response should not contain an unauthorized error
        }
        finally
        {
            if (testContext != null)
            {
                await testContext.DisposeAsync();
            }
        }
    }

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
                app.UseExceptionHandler();
                app.UseDeveloperExceptionPage();
                app.UseAuthentication();
                app.UseWebSockets();
                app.UseRouting();
                // UseAuthorization must be between UseRouting and UseEndpoints
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGraphQL()
                        .WithOptions(new GraphQLServerOptions
                        {
                            Tool =
                        {
                            Enable = true,
                        },
                        });
                });
            }

            void ConfigureServices(IServiceCollection services)
            {
                services.AddScoped<AuthorizationHelper>();

                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.Audience = JwtUtil.Audience;
                        options.IncludeErrorDetails = true;
                        options.RequireHttpsMetadata = false;

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
                            OnAuthenticationFailed = ctx =>
                            {
                                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                ctx.Fail(ctx.Exception);
                                return Task.CompletedTask;
                            },
                            OnForbidden = ctx =>
                            {
                                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                                ctx.Fail(nameof(HttpStatusCode.Forbidden));
                                return Task.CompletedTask;
                            },
                        };
                    });

                // Define the security policy for this test where the user must be in the role of a
                // workspace admin to create a workspace
                services.AddAuthorizationBuilder()
                    .AddPolicy("IsWorkspaceAdmin", policy => policy.RequireRole(nameof(UserRole.WorkspaceAdmin)));
            }

            testContext = await TestSetupUtil.SetupAsync(ConfigureApp, ConfigureServices, configureReplicatedDocuments: null);

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
            var standardUserReplicatedUser = new ReplicatedUser
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

            var standardUserToken = JwtUtil.GenerateJwtToken(standardUserReplicatedUser, UserRole.StandardUser);

            var standardUserUser = new User
            {
                Id = Provider.Sql.Create(),
                FirstName = standardUserReplicatedUser.FirstName,
                LastName = standardUserReplicatedUser.LastName,
                Email = standardUserReplicatedUser.Email,
                JwtAccessToken = standardUserToken,
                WorkspaceId = workspace.Id,
                UpdatedAt = standardUserReplicatedUser.UpdatedAt,
                IsDeleted = standardUserReplicatedUser.IsDeleted,
                ReplicatedDocumentId = standardUserReplicatedUser.Id,
            };

            await dbContext.Users.AddAsync(standardUserUser, testContext.CancellationToken);

            await dbContext.SaveChangesAsync(testContext.CancellationToken);

            // As a standard user:
            await testContext.HttpClient.CreateNewWorkspaceAsync(
                testContext.CancellationToken,
                jwtAccessToken: standardUserToken);

            // Attempt to create a workspace

            // Act

            // Assert
            // The response should not contain an unauthorized error
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
