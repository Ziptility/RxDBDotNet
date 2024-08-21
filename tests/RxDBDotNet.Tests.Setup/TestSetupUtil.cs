using System.Diagnostics;
using LiveDocs.GraphQLApi.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RxDBDotNet.Tests.Setup;

public static class TestSetupUtil
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private static volatile bool _areDockerContainersInitialized;

    public static async Task<TestContext> SetupAsync(Action<IServiceCollection>? configureGraphQLServer = null)
    {
        var asyncDisposables = new List<IAsyncDisposable>();
        var disposables = new List<IDisposable>();

        try
        {
            await Semaphore.WaitAsync();

            if (!_areDockerContainersInitialized)
            {
                await RedisSetupUtil.SetupAsync();

                await DbSetupUtil.SetupAsync();

                _areDockerContainersInitialized = true;
            }
        }
        finally
        {
            Semaphore.Release();
        }

        var testTimeout = GetTestTimeout();

#pragma warning disable CA2000 // dispose at end of test
        var timeoutTokenSource = new CancellationTokenSource(testTimeout);
        disposables.Add(timeoutTokenSource);
#pragma warning restore CA2000

        var timeoutToken = timeoutTokenSource.Token;

        var factory = WebApplicationFactorySetupUtil.Setup(configureGraphQLServer);
        asyncDisposables.Add(factory);

        var asyncTestServiceScope = factory.Services.CreateAsyncScope();
        asyncDisposables.Add(asyncTestServiceScope);

        var applicationStoppingToken = asyncTestServiceScope
            .ServiceProvider
            .GetRequiredService<IHostApplicationLifetime>()
            .ApplicationStopping;

#pragma warning disable CA2000 // dispose at end of test
        // Combine the token into a single token
        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken, applicationStoppingToken);
        disposables.Add(timeoutTokenSource);
#pragma warning restore CA2000
        var linkedToken = linkedTokenSource.Token;

        await LiveDocsDbInitializer.InitializeAsync(asyncTestServiceScope.ServiceProvider, linkedToken);

        return new TestContext
        {
            Factory = factory,
            HttpClient = factory.CreateClient(),
            CancellationToken = linkedToken,
            AsyncDisposables = asyncDisposables,
            Disposables = disposables,
        };
    }

    private static TimeSpan GetTestTimeout()
    {
        return Debugger.IsAttached ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(10);
    }
}
