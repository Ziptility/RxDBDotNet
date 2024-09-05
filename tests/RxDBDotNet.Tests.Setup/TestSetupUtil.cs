using System.Diagnostics;
using System.Net;
using HotChocolate.Execution.Configuration;
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
using Microsoft.Extensions.Hosting;
using RxDBDotNet.Extensions;
using RxDBDotNet.Security;
using RxDBDotNet.Services;
using StackExchange.Redis;

namespace RxDBDotNet.Tests.Setup;

public static class TestSetupUtil
{
    public static TestContext Setup(
        Action<IApplicationBuilder>? configureApp = null,
        Action<IServiceCollection>? configureServices = null,
        Action<IRequestExecutorBuilder>? configureGraphQL = null,
        bool setupAuthorization = false,
        Action<SecurityOptions<ReplicatedWorkspace>>? configureWorkspaceSecurity = null,
        Action<List<Type>>? configureWorkspaceErrors = null)
    {
        var asyncDisposables = new List<IAsyncDisposable>();
        var disposables = new List<IDisposable>();

        var testTimeout = GetTestTimeout();

        var timeoutTokenSource = new CancellationTokenSource(testTimeout);
        disposables.Add(timeoutTokenSource);

        var timeoutToken = timeoutTokenSource.Token;

        var factory = WebApplicationFactorySetupUtil.Setup(
            app =>
            {
                ConfigureAppDefaults(app, setupAuthorization);
                configureApp?.Invoke(app);
            },
            services =>
            {
                ConfigureServiceDefaults(services, setupAuthorization);
                configureServices?.Invoke(services);
            },
            graphQLBuilder =>
            {
                ConfigureGraphQL(graphQLBuilder, setupAuthorization, configureWorkspaceSecurity, configureWorkspaceErrors);
                configureGraphQL?.Invoke(graphQLBuilder);
            });

        asyncDisposables.Add(factory);

        var asyncTestServiceScope = factory.Services.CreateAsyncScope();
        asyncDisposables.Add(asyncTestServiceScope);

        var applicationStoppingToken = asyncTestServiceScope
            .ServiceProvider
            .GetRequiredService<IHostApplicationLifetime>()
            .ApplicationStopping;

        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken, applicationStoppingToken);
        disposables.Add(linkedTokenSource);

        var linkedToken = linkedTokenSource.Token;

        return new TestContext
        {
            Factory = factory,
            HttpClient = factory.CreateClient(),
            ServiceProvider = asyncTestServiceScope.ServiceProvider,
            CancellationToken = linkedToken,
            AsyncDisposables = asyncDisposables,
            Disposables = disposables,
        };
    }

    private static void ConfigureAppDefaults(IApplicationBuilder app, bool setupAuthorization)
    {
        if (setupAuthorization)
        {
            app.UseAuthentication();
        }

        app.UseWebSockets();
        app.UseRouting();

        if (setupAuthorization)
        {
            app.UseAuthorization();
        }

        app.UseEndpoints(endpoints => endpoints.MapGraphQL());
    }

    private static void ConfigureServiceDefaults(IServiceCollection services, bool setupAuthorization)
    {
        services.AddProblemDetails();
        services.Configure<WebSocketOptions>(options => options.KeepAliveInterval = TimeSpan.FromMinutes(2));
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

    private static void ConfigureGraphQL(
        IRequestExecutorBuilder graphQLBuilder,
        bool setupAuthorization,
        Action<SecurityOptions<ReplicatedWorkspace>>? configureWorkspaceSecurity,
        Action<List<Type>>? configureWorkspaceErrors)
    {
        graphQLBuilder
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .AddReplicationServer()
            .AddRedisSubscriptions()
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

    private static TimeSpan GetTestTimeout()
    {
        return Debugger.IsAttached
            ? TimeSpan.FromMinutes(5)
            : TimeSpan.FromSeconds(10);
    }
}
