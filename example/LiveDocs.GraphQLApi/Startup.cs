using HotChocolate.AspNetCore;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Infrastructure;
using LiveDocs.GraphQLApi.Models.Replication;
using LiveDocs.GraphQLApi.Models.Shared;
using LiveDocs.GraphQLApi.Services;
using LiveDocs.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Extensions;
using RxDBDotNet.Repositories;
using StackExchange.Redis;
using Query = LiveDocs.GraphQLApi.Models.GraphQL.Query;

namespace LiveDocs.GraphQLApi;

public class Startup
{
    public virtual void ConfigureServices(
        IServiceCollection services,
        IHostEnvironment environment,
        WebApplicationBuilder builder,
        bool isAspireEnvironment)
    {
        // Configure the database context
        ConfigureDbContext(services, builder, isAspireEnvironment);

        // Add service defaults & Aspire components if running with Aspire
        if (isAspireEnvironment)
        {
            builder.AddServiceDefaults();
        }

        // Add services to the container
        services.AddProblemDetails()
            .AddScoped<IDocumentService<Hero>, HeroService>()
            .AddScoped<IDocumentService<ReplicatedUser>, UserService>()
            .AddScoped<IDocumentService<ReplicatedWorkspace>, WorkspaceService>()
            .AddScoped<IDocumentService<ReplicatedLiveDoc>, LiveDocService>();

        ConfigureGraphQLServer(services, builder, isAspireEnvironment);

        // Configure CORS
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
    }

    protected virtual void ConfigureGraphQLServer(IServiceCollection services,
        WebApplicationBuilder builder,
        bool isAspireEnvironment)
    {
        if (isAspireEnvironment)
        {
            builder.AddRedisClient("redis");
        }

        ConfigureDefaultGraphQLServer(services);
    }

    public static void ConfigureDefaultGraphQLServer(IServiceCollection services)
    {
        // Configure the GraphQL server
        services.AddGraphQLServer()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            // Simulate scenario where the library user
            // has already added their own root query type.
            .AddQueryType<Query>()
            .AddReplicationServer()
            .AddReplicatedDocument<ReplicatedUser>()
            .AddReplicatedDocument<ReplicatedWorkspace>()
            .AddReplicatedDocument<ReplicatedLiveDoc>()
            .AddReplicatedDocument<Hero>()
            .AddRedisSubscriptions(provider => provider.GetRequiredService<IConnectionMultiplexer>())
            .AddSubscriptionDiagnostics();
    }

    protected static void ConfigureDbContext(
        IServiceCollection services,
        WebApplicationBuilder builder,
        bool isAspireEnvironment)
    {
        if (isAspireEnvironment)
        {
            // Use Aspire's SQL Server configuration when running with Aspire
            builder.AddSqlServerDbContext<LiveDocsDbContext>("sqldata");
        }
        else
        {
            // Use a standard SQL Server configuration when not running with Aspire
            services.AddDbContext<LiveDocsDbContext>(options =>
                options.UseSqlServer(Environment.GetEnvironmentVariable(ConfigKeys.DbConnectionString)
                                     ?? throw new InvalidOperationException($"The '{ConfigKeys.DbConnectionString}' env variable must be set")));
        }
    }

    public void Configure(WebApplication app, IWebHostEnvironment env)
    {
        app.UseExceptionHandler();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseHsts();
        }

        // Enable CORS
        app.UseCors();

        // Enable WebSockets
        app.UseWebSockets();

        app.MapGraphQL()
            .WithOptions(new GraphQLServerOptions
            {
                EnforceMultipartRequestsPreflightHeader = false,
                Tool =
                {
                    Enable = true,
                },
            });
    }
}
