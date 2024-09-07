#pragma warning disable CA2000 // Disposal handled by the test context
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;

namespace RxDBDotNet.Tests.Setup;

/// <summary>
/// Provides utility methods for setting up WebApplicationFactory for testing RxDBDotNet.
/// </summary>
public static class WebApplicationFactorySetupUtil
{
    /// <summary>
    /// Sets up a WebApplicationFactory with customizable configurations.
    /// </summary>
    /// <returns>A configured WebApplicationFactory.</returns>
    public static WebApplicationFactory<TestProgram> SetupWithDefaults()
    {
        return new WebApplicationFactory<TestProgram>().WithWebHostBuilder(builder =>
        {
            builder.UseSolutionRelativeContentRoot("example/LiveDocs.GraphQLApi")
                .Configure(app => TestSetupBase.ConfigureAppDefaults(app))
                .ConfigureServices(services =>
                {
                    TestSetupBase.ConfigureServiceDefaults(services);

                    var graphQLBuilder = services.AddGraphQLServer();

                    TestSetupBase.ConfigureGraphQLDefaults(graphQLBuilder);
                });
        });
    }

    /// <summary>
    /// Sets up a WebApplicationFactory with customizable configurations.
    /// </summary>
    /// <param name="configureApp">Optional action to configure the application.</param>
    /// <param name="configureServices">Optional action to configure services.</param>
    /// <param name="configureGraphQL">Optional action to configure GraphQL.</param>
    /// <returns>A configured WebApplicationFactory.</returns>
    public static WebApplicationFactory<TestProgram> Setup(
        Action<IApplicationBuilder> configureApp,
        Action<IServiceCollection> configureServices,
        Action<IRequestExecutorBuilder> configureGraphQL)
    {
        return new WebApplicationFactory<TestProgram>().WithWebHostBuilder(builder =>
        {
            builder.UseSolutionRelativeContentRoot("example/LiveDocs.GraphQLApi")
                .Configure(configureApp)
                .ConfigureServices(services =>
                {
                    configureServices(services);

                    var graphQLBuilder = services.AddGraphQLServer();

                    configureGraphQL(graphQLBuilder);
                });
        });
    }

    /// <summary>
    /// Creates an HttpClient with an extended timeout for the given WebApplicationFactory.
    /// </summary>
    /// <param name="factory">The WebApplicationFactory to create the client from.</param>
    /// <returns>An HttpClient with an extended timeout.</returns>
    public static HttpClient CreateHttpClient(this WebApplicationFactory<TestProgram> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        return client;
    }
}
