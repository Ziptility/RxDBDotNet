using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace RxDBDotNet.Tests.Setup;

public static class WebApplicationFactorySetup
{
    public static WebApplicationFactory<TestProgram> CreateWebApplicationFactory()
    {
#pragma warning disable CA2000 // caller handles disposal
        return new WebApplicationFactory<TestProgram>().WithWebHostBuilder(builder =>
        {
            builder.UseSolutionRelativeContentRoot("example/LiveDocs.GraphQLApi")
                .ConfigureServices(_ =>
            {
                // Add any additional service configuration here
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
