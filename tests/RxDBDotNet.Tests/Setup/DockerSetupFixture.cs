namespace RxDBDotNet.Tests.Setup;

public sealed class DockerSetupFixture
{
    private readonly Lazy<Task> _initializer = new(InitializeAsyncInternal);

    public Task InitializeAsync()
    {
        return _initializer.Value;
    }

    private static async Task InitializeAsyncInternal()
    {
        await RedisSetupUtil.SetupAsync();

        await DbSetupUtil.SetupAsync();
    }
}
