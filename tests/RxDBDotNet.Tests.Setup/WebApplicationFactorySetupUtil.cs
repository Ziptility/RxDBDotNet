#pragma warning disable CA2000 // Disposal handled by the test context
using HotChocolate.AspNetCore;
using HotChocolate.Execution.Configuration;
using HotChocolate.Subscriptions;
using LiveDocs.GraphQLApi.Models.Replication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RxDBDotNet.Extensions;
using StackExchange.Redis;
using Query = LiveDocs.GraphQLApi.Models.GraphQL.Query;

namespace RxDBDotNet.Tests.Setup;

public static class WebApplicationFactorySetupUtil
{
    public static WebApplicationFactory<Program> Setup(
        Action<IApplicationBuilder>? configureApp = null,
        Action<IServiceCollection>? configureServices = null,
        Action<IRequestExecutorBuilder>? configureReplicatedDocuments = null)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
        {
            builder
                .UseSolutionRelativeContentRoot("example/LiveDocs.GraphQLApi")
                .Configure(app =>
                {
                    ConfigureAppDefaults(app);

                    configureApp?.Invoke(app);
                })
                .ConfigureServices(services =>
                {
                    configureServices?.Invoke(services);

                    var graphQLBuilder = ConfigureGraphQLDefaults(services);

                    if (configureReplicatedDocuments != null)
                    {
                        // This enables configuration of replicated documents on a per test basis
                        configureReplicatedDocuments.Invoke(graphQLBuilder);
                    }
                    else
                    {
                        ConfigureReplicatedDocumentDefaults(graphQLBuilder);
                    }
                });
        });
    }

    private static void ConfigureAppDefaults(IApplicationBuilder app)
    {
        app.UseExceptionHandler();

        app.UseRouting();

        app.UseDeveloperExceptionPage();

        app.UseWebSockets();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGraphQL().WithOptions(new GraphQLServerOptions
            {
                Tool =
                {
                    Enable = true,
                },
            });
        });
    }

    private static IRequestExecutorBuilder ConfigureGraphQLDefaults(IServiceCollection services)
    {
        return services.AddGraphQLServer()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            // Simulate scenario where the library user
            // has already added their own root query type.
            .AddQueryType<Query>()
            .AddReplicationServer()
            .AddRedisSubscriptions(provider => provider.GetRequiredService<IConnectionMultiplexer>(), new SubscriptionOptions
            {
                // Make redis topics unique per unit test
                TopicPrefix = Guid.NewGuid().ToString(),
            })
            .AddSubscriptionDiagnostics();
    }

    private static void ConfigureReplicatedDocumentDefaults(IRequestExecutorBuilder graphQLBuilder)
    {
        graphQLBuilder
            .AddReplicatedDocument<ReplicatedUser>()
            .AddReplicatedDocument<ReplicatedWorkspace>()
            .AddReplicatedDocument<ReplicatedLiveDoc>();
    }

    public static HttpClient CreateHttpClient(this WebApplicationFactory<Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var client = factory.CreateClient();

        client.Timeout = TimeSpan.FromMinutes(5);

        return client;
    }
}
