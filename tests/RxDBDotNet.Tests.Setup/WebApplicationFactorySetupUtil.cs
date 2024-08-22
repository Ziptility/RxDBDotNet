#pragma warning disable CA2000 // Disposal handled by the test context
using HotChocolate.Execution.Configuration;
using LiveDocs.GraphQLApi;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

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

                    var graphQLBuilder = Startup.ConfigureBaseGraphQLServer(serviceCollection, Guid.NewGuid().ToString());

                    if (configureReplicatedDocuments != null)
                    {
                        // This enables configuration of replicated documents on a per test basis
                        configureReplicatedDocuments.Invoke(graphQLBuilder);
                    }
                    else
                    {
                        // Configure using the default configuration defined in the Startup class
                        Startup.ConfigureDefaultReplicatedDocuments(graphQLBuilder);
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
