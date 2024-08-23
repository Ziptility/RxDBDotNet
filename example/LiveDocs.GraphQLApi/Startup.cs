using HotChocolate.AspNetCore;
using HotChocolate.Subscriptions;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Replication;
using LiveDocs.GraphQLApi.Models.Shared;
using LiveDocs.GraphQLApi.Services;
using LiveDocs.ServiceDefaults;
using RxDBDotNet.Extensions;
using RxDBDotNet.Repositories;
using StackExchange.Redis;

namespace LiveDocs.GraphQLApi;

public sealed class Startup
{
    public static void ConfigureServices(
        IServiceCollection services,
        WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();

        services.AddProblemDetails();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(corsPolicyBuilder =>
            {
                corsPolicyBuilder.WithOrigins(
                        "http://localhost:3000",
                        "http://127.0.0.1:8888")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        builder.AddSqlServerDbContext<LiveDocsDbContext>("sqldata");

        AddDependencies(services, builder);

        AddGraphQL(services);
    }

    private static void AddDependencies(IServiceCollection services, WebApplicationBuilder builder)
    {
        // Redis is required for Hot Chocolate subscriptions
        builder.AddRedisClient("redis");

        services
            .AddScoped<IDocumentService<Hero>, HeroService>()
            .AddScoped<IDocumentService<ReplicatedUser>, UserService>()
            .AddScoped<IDocumentService<ReplicatedWorkspace>, WorkspaceService>()
            .AddScoped<IDocumentService<ReplicatedLiveDoc>, LiveDocService>();
    }

    private static void AddGraphQL(IServiceCollection services)
    {
        services.AddGraphQLServer()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .AddReplicationServer()
            .AddSubscriptionDiagnostics()
            .AddReplicatedDocument<Hero>()
            .AddReplicatedDocument<ReplicatedUser>()
            .AddReplicatedDocument<ReplicatedWorkspace>()
            .AddReplicatedDocument<ReplicatedLiveDoc>()
            .AddRedisSubscriptions(provider => provider.GetRequiredService<IConnectionMultiplexer>(), new SubscriptionOptions
            {
                TopicPrefix = null,
            });
    }

    public static void Configure(WebApplication app)
    {
        app.UseExceptionHandler();

        app.UseDeveloperExceptionPage();

        app.UseCors();

        app.UseWebSockets();

        app.MapGraphQL()
            .WithOptions(new GraphQLServerOptions
            {
                Tool =
                {
                    Enable = true,
                },
            });
    }
}
