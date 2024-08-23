#pragma warning disable CA2000 // Disposal handled by the test context
using HotChocolate.Execution.Configuration;
using HotChocolate.Subscriptions;
using LiveDocs.GraphQLApi.Models.Replication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RxDBDotNet.Extensions;
using StackExchange.Redis;
using Query = LiveDocs.GraphQLApi.Models.GraphQL.Query;

namespace RxDBDotNet.Tests.Setup;

public static class WebApplicationFactorySetupUtil
{
    public static WebApplicationFactory<TestProgram> Setup(
        Action<IServiceCollection>? configureServices = null,
        Action<IRequestExecutorBuilder>? configureReplicatedDocuments = null)
    {
        return new WebApplicationFactory<TestProgram>().WithWebHostBuilder(builder =>
        {
            builder.UseSolutionRelativeContentRoot("example/LiveDocs.GraphQLApi")
                .ConfigureServices(serviceCollection =>
                {
                    configureServices?.Invoke(serviceCollection);

                    string? topicPrefix = Guid.NewGuid().ToString();

                    // Configure the GraphQL server
                    var graphQLBuilder1 = serviceCollection.AddGraphQLServer()
                        .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                        // Simulate scenario where the library user
                        // has already added their own root query type.
                        .AddQueryType<Query>()
                        .AddReplicationServer()
                        .AddSubscriptionDiagnostics();

                    graphQLBuilder1 = graphQLBuilder1
                        .AddReplicatedDocument<ReplicatedUser>()
                        .AddReplicatedDocument<ReplicatedWorkspace>()
                        .AddReplicatedDocument<ReplicatedLiveDoc>();

                    graphQLBuilder1.AddRedisSubscriptions(provider => provider.GetRequiredService<IConnectionMultiplexer>(), new SubscriptionOptions
                    {
                        TopicPrefix = topicPrefix,
                    });
                    var graphQLBuilder = graphQLBuilder1;

                    if (configureReplicatedDocuments != null)
                    {
                        // This enables configuration of replicated documents on a per test basis
                        configureReplicatedDocuments.Invoke(graphQLBuilder);
                    }
                    else
                    {
                        // Configure using the default configuration defined in the Startup class
                        graphQLBuilder
                            .AddReplicatedDocument<ReplicatedUser>()
                            .AddReplicatedDocument<ReplicatedWorkspace>()
                            .AddReplicatedDocument<ReplicatedLiveDoc>();
                    }
                });
        });
    }

    public static HttpClient CreateHttpClient(this WebApplicationFactory<TestProgram> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var client = factory.CreateClient();

        client.Timeout = TimeSpan.FromMinutes(5);

        return client;
    }
}
