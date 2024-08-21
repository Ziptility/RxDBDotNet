using LiveDocs.GraphQLApi.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace RxDBDotNet.Tests.Setup;

public static class TestSetupUtil
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private static volatile bool _areDockerContainersInitialized;

    public static async Task<TestContext> SetupAsync(TimeSpan? testTimeout = null, Action<IServiceCollection>? configureGraphQLServer = null)
    {
        testTimeout ??= TimeSpan.FromSeconds(5);

        using var timeoutTokenSource = new CancellationTokenSource(testTimeout.Value);

        var timeoutToken = timeoutTokenSource.Token;

        var disposables = new List<IAsyncDisposable>();

        await Semaphore.WaitAsync(timeoutToken);

        try
        {
            if (!_areDockerContainersInitialized)
            {
                disposables.Add(await RedisSetupUtil.SetupAsync(timeoutToken));

                disposables.Add(await DbSetupUtil.SetupAsync(timeoutToken));

                _areDockerContainersInitialized = true;
            }
        }
        finally
        {
            Semaphore.Release();
        }

        var factory = WebApplicationFactorySetupUtil.Setup(configureGraphQLServer);
        disposables.Add(factory);

        var asyncTestServiceScope = factory.Services.CreateAsyncScope();
        disposables.Add(asyncTestServiceScope);

        await LiveDocsDbInitializer.InitializeAsync(asyncTestServiceScope.ServiceProvider, timeoutToken);

        return new TestContext
        {
            Factory = factory,
            HttpClient = factory.CreateClient(),
            TestTimeoutToken = timeoutToken,
            Disposables = disposables,
        };
    }
}
