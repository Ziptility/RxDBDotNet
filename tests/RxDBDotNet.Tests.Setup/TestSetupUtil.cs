#pragma warning disable CA2000 // items are disposed at the end of the test via `await testContext.DisposeAsync();`
using System.Diagnostics;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RxDBDotNet.Tests.Setup;

public static class TestSetupUtil
{
    public static Task<TestContext> SetupAsync(
        Action<IApplicationBuilder>? configureApp = null,
        Action<IServiceCollection>? configureServices = null,
        Action<IRequestExecutorBuilder>? configureGraphQL = null)
    {
        var asyncDisposables = new List<IAsyncDisposable>();
        var disposables = new List<IDisposable>();

        var testTimeout = GetTestTimeout();

        var timeoutTokenSource = new CancellationTokenSource(testTimeout);
        disposables.Add(timeoutTokenSource);

        var timeoutToken = timeoutTokenSource.Token;

        var factory = WebApplicationFactorySetupUtil.Setup(configureApp, configureServices, configureGraphQL);
        asyncDisposables.Add(factory);

        var asyncTestServiceScope = factory.Services.CreateAsyncScope();
        asyncDisposables.Add(asyncTestServiceScope);

        var applicationStoppingToken = asyncTestServiceScope
            .ServiceProvider
            .GetRequiredService<IHostApplicationLifetime>()
            .ApplicationStopping;

        // Combine the timeoutToken and the applicationStoppingToek into a single token
        // so that the tests will stop if the application is stopped or the timeout is reached
        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken, applicationStoppingToken);
        disposables.Add(timeoutTokenSource);

        var linkedToken = linkedTokenSource.Token;

        return Task.FromResult(new TestContext
        {
            Factory = factory,
            HttpClient = factory.CreateClient(),
            ServiceProvider = asyncTestServiceScope.ServiceProvider,
            CancellationToken = linkedToken,
            AsyncDisposables = asyncDisposables,
            Disposables = disposables,
        });
    }

    private static TimeSpan GetTestTimeout()
    {
        return Debugger.IsAttached
            // If we are debugging, then ensure we don't timeout
            ? TimeSpan.FromMinutes(5)
            : TimeSpan.FromSeconds(10);
    }
}
