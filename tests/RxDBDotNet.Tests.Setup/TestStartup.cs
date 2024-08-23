using HotChocolate.Execution.Options;
using HotChocolate.Subscriptions;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Infrastructure;
using LiveDocs.GraphQLApi.Models.Replication;
using LiveDocs.GraphQLApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RxDBDotNet.Extensions;
using RxDBDotNet.Repositories;
using StackExchange.Redis;

namespace RxDBDotNet.Tests.Setup;

public sealed class TestStartup
{
    public static void ConfigureServices(
        IServiceCollection services,
        WebApplicationBuilder builder)
    {
        services.AddProblemDetails();

        // Extend timeout for long-running debugging sessions
        services.Configure<RequestExecutorOptions>(options => options.ExecutionTimeout = TimeSpan.FromMinutes(15));

        // Configure WebSocket options for longer keep-alive
        services.Configure<WebSocketOptions>(options => options.KeepAliveInterval = TimeSpan.FromMinutes(2));

        ConfigureLogging(builder);

        AddDependencies(services);

        AddDbContext(services);

        AddGraphQL(services);
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        builder.Logging.AddFilter(
            "Microsoft.EntityFrameworkCore.Database.Command",
            LogLevel.Critical);
        builder.Logging.AddFilter(
            "Microsoft.EntityFrameworkCore.Infrastructure",
            LogLevel.Critical);
        builder.Logging.AddFilter(
            "Microsoft.AspNetCore",
            LogLevel.Critical);
        builder.Logging.AddFilter(
            "Microsoft.AspNetCore.SignalR",
            LogLevel.Critical);
        builder.Logging.AddFilter(
            "Microsoft.AspNetCore.Http.Connections",
            LogLevel.Critical);
        builder.Logging.SetMinimumLevel(LogLevel.Critical);
    }

    private static void AddDependencies(IServiceCollection services)
    {
        // Redis is required for Hot Chocolate subscriptions
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost:3333"));

        services
            .AddScoped<IDocumentService<ReplicatedUser>, UserService>()
            .AddScoped<IDocumentService<ReplicatedWorkspace>, WorkspaceService>()
            .AddScoped<IDocumentService<ReplicatedLiveDoc>, LiveDocService>();
    }

    private static void AddDbContext(IServiceCollection services)
    {
        // Use a standard SQL Server configuration when not running with Aspire
        services.AddDbContext<LiveDocsDbContext>(options =>
            options.UseSqlServer(Environment.GetEnvironmentVariable(ConfigKeys.DbConnectionString)
                                 ?? throw new InvalidOperationException($"The '{ConfigKeys.DbConnectionString}' env variable must be set")));
    }

    private static void AddGraphQL(IServiceCollection services)
    {
        services.AddGraphQLServer()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            // Simulate scenario where the library user
            // has already added their own root query type.
            .AddQueryType<LiveDocs.GraphQLApi.Models.GraphQL.Query>()
            .AddReplicationServer()
            .AddReplicatedDocument<ReplicatedUser>()
            .AddReplicatedDocument<ReplicatedWorkspace>()
            .AddReplicatedDocument<ReplicatedLiveDoc>()
            .AddRedisSubscriptions(provider => provider.GetRequiredService<IConnectionMultiplexer>(), new SubscriptionOptions
            {
                TopicPrefix = null,
            })
            .AddSubscriptionDiagnostics();
    }

    public static void Configure(WebApplication app)
    {
        app.UseExceptionHandler();

        app.UseDeveloperExceptionPage();

        app.UseWebSockets();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGraphQL();
    }
}
