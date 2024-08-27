using HotChocolate.AspNetCore;
using HotChocolate.Execution.Options;
using HotChocolate.Subscriptions;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Entities;
using LiveDocs.GraphQLApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Security;
using RxDBDotNet.Services;
using RxDBDotNet.Tests.Helpers;
using StackExchange.Redis;

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
                services.AddProblemDetails();

                // Extend timeout for long-running debugging sessions
                services.Configure<RequestExecutorOptions>(options => options.ExecutionTimeout = TimeSpan.FromMinutes(15));

                // Configure WebSocket options for longer keep-alive
                services.Configure<WebSocketOptions>(options => options.KeepAliveInterval = TimeSpan.FromMinutes(2));

                // Redis is required for Hot Chocolate subscriptions
                services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost:3333"));

                services.AddDbContext<LiveDocsDbContext>(options =>
                    options.UseSqlServer(Environment.GetEnvironmentVariable(ConfigKeys.DbConnectionString)
                                         ?? throw new InvalidOperationException($"The '{ConfigKeys.DbConnectionString}' env variable must be set")));

                services
                    .AddScoped<IDocumentService<ReplicatedUser>, UserService>()
                    .AddScoped<IDocumentService<ReplicatedWorkspace>, WorkspaceService>()
                    .AddScoped<IDocumentService<ReplicatedLiveDoc>, LiveDocService>();

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

            void ConfigureGraphQL(IRequestExecutorBuilder graphQLBuilder)
            {
                graphQLBuilder
                    .AddAuthorization()
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    // Simulate scenario where the library user
                    // has already added their own root query type.
                    .AddQueryType<Query>()
                    .AddReplicationServer()
                    .AddRedisSubscriptions(provider => provider.GetRequiredService<IConnectionMultiplexer>(), new SubscriptionOptions
                    {
                        // Make redis topics unique per unit test
                        TopicPrefix = Guid.NewGuid()
                            .ToString(),
                    })
                    .AddSubscriptionDiagnostics()
                    .AddReplicatedDocument<ReplicatedUser>()
                    .AddReplicatedDocument<ReplicatedWorkspace>(options => options.Security.RequirePolicyToCreate("IsWorkspaceAdmin"))
                    .AddReplicatedDocument<ReplicatedLiveDoc>();
            }

            testContext = await TestSetupUtil.SetupAsync(ConfigureApp, ConfigureServices, ConfigureGraphQL);

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

            // Act
            var response = await testContext.HttpClient.CreateNewWorkspaceAsync(
                testContext.CancellationToken,
                jwtAccessToken: workspaceAdminToken);

            // Assert
            await testContext.HttpClient.VerifyWorkspaceAsync(response.workspaceInputGql, testContext.CancellationToken);
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
                services.AddProblemDetails();

                // Extend timeout for long-running debugging sessions
                services.Configure<RequestExecutorOptions>(options => options.ExecutionTimeout = TimeSpan.FromMinutes(15));

                // Configure WebSocket options for longer keep-alive
                services.Configure<WebSocketOptions>(options => options.KeepAliveInterval = TimeSpan.FromMinutes(2));

                // Redis is required for Hot Chocolate subscriptions
                services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost:3333"));

                services.AddDbContext<LiveDocsDbContext>(options =>
                    options.UseSqlServer(Environment.GetEnvironmentVariable(ConfigKeys.DbConnectionString)
                                         ?? throw new InvalidOperationException($"The '{ConfigKeys.DbConnectionString}' env variable must be set")));

                services
                    .AddScoped<IDocumentService<ReplicatedUser>, UserService>()
                    .AddScoped<IDocumentService<ReplicatedWorkspace>, WorkspaceService>()
                    .AddScoped<IDocumentService<ReplicatedLiveDoc>, LiveDocService>();

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

            void ConfigureGraphQL(IRequestExecutorBuilder graphQLBuilder)
            {
                graphQLBuilder
                    .AddAuthorization()
                    .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    // Simulate scenario where the library user
                    // has already added their own root query type.
                    .AddQueryType<Query>()
                    .AddReplicationServer()
                    .AddRedisSubscriptions(provider => provider.GetRequiredService<IConnectionMultiplexer>(), new SubscriptionOptions
                    {
                        // Make redis topics unique per unit test
                        TopicPrefix = Guid.NewGuid()
                            .ToString(),
                    })
                    .AddSubscriptionDiagnostics()
                    .AddReplicatedDocument<ReplicatedUser>()
                    .AddReplicatedDocument<ReplicatedWorkspace>(options => options.Security.RequirePolicyToCreate("IsWorkspaceAdmin"))
                    .AddReplicatedDocument<ReplicatedLiveDoc>();
            }

            testContext = await TestSetupUtil.SetupAsync(ConfigureApp, ConfigureServices, ConfigureGraphQL);

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

            // Act
            var result = await testContext.HttpClient.CreateNewWorkspaceAsync(
                testContext.CancellationToken,
                jwtAccessToken: standardUserToken);

            // Assert
            result.response.Data.PushWorkspace.Should()
                .HaveCount(1, "Since the user was unauthorized, the new workspace should be returned");
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
