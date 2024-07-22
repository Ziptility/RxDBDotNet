using Microsoft.AspNetCore.Mvc.Testing;

namespace RxDBDotNet.Tests.Setup;

public static class WebApplicationFactoryExtensions
{
    public static HttpClient HttpClient(this WebApplicationFactory<TestProgram> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var httpClient = factory.CreateClient();

        httpClient.Timeout = TimeSpan.FromMinutes(5);

        return httpClient;
    }
}
