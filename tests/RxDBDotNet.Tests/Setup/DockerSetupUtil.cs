namespace RxDBDotNet.Tests.Setup;

public sealed class DockerSetupUtil : IAsyncLifetime
{
    private readonly Lazy<Task> _initializer = new(InitializeAsyncInternal);

    public Task InitializeAsync()
    {
        return _initializer.Value;
    }

    public Task DisposeAsync()
    {
        // since the test containers are expensive to setup
        // we don't want to dispose of them after each test run.
        // Manually delete them when necessary; for example,
        // when the db schema changes.
        return Task.CompletedTask;
    }

    private static async Task InitializeAsyncInternal()
    {
        await RedisSetupUtil.SetupAsync();

        await DbSetupUtil.SetupAsync();
    }
}
