using LiveDocs.GraphQLApi;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace RxDBDotNet.Tests.Setup;

public static class WebApplicationFactorySetupUtil
{
    public static WebApplicationFactory<TestProgram> Setup(Action<IServiceCollection>? configureGraphQLServer = null)
    {
#pragma warning disable CA2000 // caller handles disposal
        return new WebApplicationFactory<TestProgram>().WithWebHostBuilder(builder =>
        {
            builder.UseSolutionRelativeContentRoot("example/LiveDocs.GraphQLApi")
                .ConfigureServices(serviceCollection =>
            {
                if (configureGraphQLServer != null)
                {
                    configureGraphQLServer.Invoke(serviceCollection);
                }
                else
                {
                    Startup.ConfigureDefaultGraphQLServer(serviceCollection);
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
