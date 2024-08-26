#pragma warning disable CA2000 // Disposal handled by the test context
using HotChocolate.AspNetCore;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Options;
using HotChocolate.Subscriptions;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Infrastructure;
using LiveDocs.GraphQLApi.Models.Replication;
using LiveDocs.GraphQLApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RxDBDotNet.Extensions;
using RxDBDotNet.Services;
using StackExchange.Redis;
using Query = LiveDocs.GraphQLApi.Models.GraphQL.Query;

namespace RxDBDotNet.Tests.Setup;

public static class WebApplicationFactorySetupUtil
{
    public static WebApplicationFactory<TestProgram> Setup(
        Action<IApplicationBuilder>? configureApp = null,
        Action<IServiceCollection>? configureServices = null,
        Action<IRequestExecutorBuilder>? configureGraphQL = null)
    {
        return new WebApplicationFactory<TestProgram>().WithWebHostBuilder(builder =>
        {
            builder.UseSolutionRelativeContentRoot("example/LiveDocs.GraphQLApi")
                .Configure(app =>
                {
                    if (configureApp != null)
                    {
                        configureApp.Invoke(app);
                    }
                    else
                    {
                        ConfigureAppDefaults(app);
                    }
                })
                .ConfigureServices(services =>
                {
                    if (configureServices != null)
                    {
                        configureServices.Invoke(services);
                    }
                    else
                    {
                        ConfigureServiceDefaults(services);
                    }

                    var graphQLBuilder = services.AddGraphQLServer();

                    if (configureGraphQL != null)
                    {
                        configureGraphQL.Invoke(graphQLBuilder);
                    }
                    else
                    {
                        ConfigureGraphQLDefaults(graphQLBuilder);
                    }
                });
        });
    }

    private static void ConfigureServiceDefaults(IServiceCollection services)
    {
        services.AddProblemDetails();

        // Extend timeout for long-running debugging sessions
        services.Configure<RequestExecutorOptions>(options => options.ExecutionTimeout = TimeSpan.FromMinutes(15));

        // Configure WebSocket options for longer keep-alive
        services.Configure<WebSocketOptions>(options => options.KeepAliveInterval = TimeSpan.FromMinutes(2));

        // Redis is required for Hot Chocolate subscriptions
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost:3333"));

        services.AddDbContext<LiveDocsDbContext>(
            options => options.UseSqlServer(Environment.GetEnvironmentVariable(ConfigKeys.DbConnectionString)
                                            ?? throw new InvalidOperationException($"The '{ConfigKeys.DbConnectionString}' env variable must be set")));

        services.AddScoped<IDocumentService<ReplicatedUser>, UserService>()
            .AddScoped<IDocumentService<ReplicatedWorkspace>, WorkspaceService>()
            .AddScoped<IDocumentService<ReplicatedLiveDoc>, LiveDocService>();
    }

    private static void ConfigureAppDefaults(IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        app.UseDeveloperExceptionPage();
        app.UseWebSockets();
        app.UseRouting();
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

    private static void ConfigureGraphQLDefaults(IRequestExecutorBuilder graphQLBuilder)
    {
        graphQLBuilder
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
            .AddReplicatedDocument<ReplicatedWorkspace>()
            .AddReplicatedDocument<ReplicatedLiveDoc>();
    }

    public static HttpClient CreateHttpClient(this WebApplicationFactory<TestProgram> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var client = factory.CreateClient();

        client.Timeout = TimeSpan.FromMinutes(5);

        return client;
    }
}
