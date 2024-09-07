using System.Diagnostics;
using HotChocolate.Execution.Configuration;
using LiveDocs.GraphQLApi.Models.Replication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RxDBDotNet.Security;

namespace RxDBDotNet.Tests.Setup;

/// <summary>
/// Provides utility methods for setting up test environments in RxDBDotNet.
/// </summary>
public static class TestSetupUtil
{
    /// <summary>
    /// Sets up a test environment with default configurations.
    /// </summary>
    /// <returns>A TestContext containing the configured test environment with default settings.</returns>
    public static TestContext Setup()
    {
        return Setup(new TestSetupOptions());
    }

    /// <summary>
    /// Sets up a test environment with custom configurations while applying defaults.
    /// </summary>
    /// <param name="configureApp">Optional action to configure the application.</param>
    /// <param name="configureServices">Optional action to configure services.</param>
    /// <param name="configureGraphQL">Optional action to configure GraphQL.</param>
    /// <param name="setupAuthorization">Whether to set up authorization.</param>
    /// <param name="configureWorkspaceSecurity">Optional action to configure workspace security.</param>
    /// <param name="configureWorkspaceErrors">Optional action to configure workspace errors.</param>
    /// <returns>A TestContext containing the configured test environment.</returns>
    public static TestContext SetupWithDefaultsAndCustomConfig(
        Action<IApplicationBuilder>? configureApp = null,
        Action<IServiceCollection>? configureServices = null,
        Action<IRequestExecutorBuilder>? configureGraphQL = null,
        bool setupAuthorization = false,
        Action<SecurityOptions<ReplicatedWorkspace>>? configureWorkspaceSecurity = null,
        Action<List<Type>>? configureWorkspaceErrors = null)
    {
        return Setup(new TestSetupOptions
        {
            ConfigureApp = configureApp,
            ConfigureServices = configureServices,
            ConfigureGraphQL = configureGraphQL,
            SetupAuthorization = setupAuthorization,
            ConfigureWorkspaceSecurity = configureWorkspaceSecurity,
            ConfigureWorkspaceErrors = configureWorkspaceErrors,
        });
    }

    /// <summary>
    /// Sets up a test environment with customizable configurations.
    /// </summary>
    /// <param name="options">The options to configure the test setup.</param>
    /// <returns>A TestContext containing the configured test environment.</returns>
    private static TestContext Setup(TestSetupOptions options)
    {
        var asyncDisposables = new List<IAsyncDisposable>();
        var disposables = new List<IDisposable>();

        var testTimeout = Debugger.IsAttached ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(10);

        var timeoutTokenSource = new CancellationTokenSource(testTimeout);
        disposables.Add(timeoutTokenSource);

        var timeoutToken = timeoutTokenSource.Token;

        var factory = SetupWebApplicationFactory(options);

        asyncDisposables.Add(factory);

        var asyncTestServiceScope = factory.Services.CreateAsyncScope();
        asyncDisposables.Add(asyncTestServiceScope);

        var applicationStoppingToken = asyncTestServiceScope
            .ServiceProvider
            .GetRequiredService<IHostApplicationLifetime>()
            .ApplicationStopping;

        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken, applicationStoppingToken);
        disposables.Add(linkedTokenSource);

        var linkedToken = linkedTokenSource.Token;

        return new TestContext
        {
            Factory = factory,
            HttpClient = factory.CreateHttpClient(),
            ServiceProvider = asyncTestServiceScope.ServiceProvider,
            CancellationToken = linkedToken,
            AsyncDisposables = asyncDisposables,
            Disposables = disposables,
        };
    }

    private static WebApplicationFactory<TestProgram> SetupWebApplicationFactory(TestSetupOptions options)
    {
        return WebApplicationFactorySetupUtil.Setup(
            app =>
            {
                if (options.ApplyDefaultAppConfiguration)
                {
                    TestSetupBase.ConfigureAppDefaults(app, options.SetupAuthorization);
                }
                options.ConfigureApp?.Invoke(app);
            },
            services =>
            {
                if (options.ApplyDefaultServiceConfiguration)
                {
                    TestSetupBase.ConfigureServiceDefaults(services, options.SetupAuthorization);
                }
                options.ConfigureServices?.Invoke(services);
            },
            graphQLBuilder =>
            {
                if (options.ApplyDefaultGraphQLConfiguration)
                {
                    TestSetupBase.ConfigureGraphQLDefaults(graphQLBuilder, options.SetupAuthorization, options.ConfigureWorkspaceSecurity, options.ConfigureWorkspaceErrors);
                }
                options.ConfigureGraphQL?.Invoke(graphQLBuilder);
            }
        );
    }
}
