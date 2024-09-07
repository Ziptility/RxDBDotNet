using HotChocolate.AspNetCore;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Options;
using HotChocolate.Subscriptions;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Infrastructure;
using LiveDocs.GraphQLApi.Models.Replication;
using LiveDocs.GraphQLApi.Models.Shared;
using LiveDocs.GraphQLApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RxDBDotNet.Extensions;
using RxDBDotNet.Security;
using RxDBDotNet.Services;
using StackExchange.Redis;
using System.Net;

namespace RxDBDotNet.Tests.Setup;

/// <summary>
/// Provides base configuration methods for setting up test environments in RxDBDotNet.
/// </summary>
public static class TestSetupBase
{
    /// <summary>
    /// Configures the default application settings.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="setupAuthorization">Whether to set up authorization.</param>
    public static void ConfigureAppDefaults(IApplicationBuilder app, bool setupAuthorization = false)
    {
        if (setupAuthorization)
        {
            app.UseAuthentication();
        }

        app.UseExceptionHandler();
        app.UseDeveloperExceptionPage();
        app.UseWebSockets();
        app.UseRouting();

        if (setupAuthorization)
        {
            app.UseAuthorization();
        }

        app.UseEndpoints(endpoints => endpoints.MapGraphQL().WithOptions(new GraphQLServerOptions
        {
            Tool = { Enable = true },
        }));
    }

    /// <summary>
    /// Configures the default service settings.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="setupAuthorization">Whether to set up authorization.</param>
    public static void ConfigureServiceDefaults(IServiceCollection services, bool setupAuthorization = false)
    {
        services.AddProblemDetails();
        services.Configure<WebSocketOptions>(options => options.KeepAliveInterval = TimeSpan.FromMinutes(2));
        services.Configure<RequestExecutorOptions>(options => options.ExecutionTimeout = TimeSpan.FromMinutes(15));
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost:3333"));
        services.AddDbContext<LiveDocsDbContext>(options =>
            options.UseSqlServer(Environment.GetEnvironmentVariable(ConfigKeys.DbConnectionString)
                                 ?? throw new InvalidOperationException($"The '{ConfigKeys.DbConnectionString}' env variable must be set")));

        services.AddScoped<IDocumentService<ReplicatedUser>, UserService>()
            .AddScoped<IDocumentService<ReplicatedWorkspace>, WorkspaceService>()
            .AddScoped<IDocumentService<ReplicatedLiveDoc>, LiveDocService>();

        if (setupAuthorization)
        {
            services.AddScoped<AuthorizationHelper>();
            ConfigureAuthentication(services);
            ConfigureAuthorization(services);
        }
    }

    /// <summary>
    /// Configures the default GraphQL settings.
    /// </summary>
    /// <param name="graphQLBuilder">The GraphQL builder.</param>
    /// <param name="setupAuthorization">Whether to set up authorization.</param>
    /// <param name="configureWorkspaceSecurity">Action to configure workspace security.</param>
    /// <param name="configureWorkspaceErrors">Action to configure workspace errors.</param>
    public static void ConfigureGraphQLDefaults(
        IRequestExecutorBuilder graphQLBuilder,
        bool setupAuthorization = false,
        Action<SecurityOptions<ReplicatedWorkspace>>? configureWorkspaceSecurity = null,
        Action<List<Type>>? configureWorkspaceErrors = null)
    {
        graphQLBuilder
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .AddReplicationServer()
            .AddRedisSubscriptions(provider => provider.GetRequiredService<IConnectionMultiplexer>(), new SubscriptionOptions
            {
                TopicPrefix = Guid.NewGuid().ToString(),
            })
            .AddSubscriptionDiagnostics()
            .AddReplicatedDocument<ReplicatedUser>()
            .AddReplicatedDocument<ReplicatedWorkspace>(options =>
            {
                configureWorkspaceSecurity?.Invoke(options.Security);
                configureWorkspaceErrors?.Invoke(options.Errors);
            })
            .AddReplicatedDocument<ReplicatedLiveDoc>();

        if (setupAuthorization)
        {
            graphQLBuilder.AddAuthorization();
        }
    }

    private static void ConfigureAuthentication(IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Audience = JwtUtil.Audience;
                options.IncludeErrorDetails = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = JwtUtil.GetTokenValidationParameters();
                ConfigureJwtBearerEvents(options);
            });
    }

    private static void ConfigureJwtBearerEvents(JwtBearerOptions options)
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = _ => Task.CompletedTask,
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
    }

    private static void ConfigureAuthorization(IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy("IsWorkspaceAdmin", policy => policy.RequireRole(nameof(UserRole.WorkspaceAdmin)))
            .AddPolicy("IsSystemAdmin", policy => policy.RequireRole(nameof(UserRole.SystemAdmin)));
    }
}
