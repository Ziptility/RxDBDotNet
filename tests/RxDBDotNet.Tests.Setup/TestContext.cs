using Microsoft.AspNetCore.Mvc.Testing;

namespace RxDBDotNet.Tests.Setup;

public sealed class TestContext : IAsyncDisposable
{
    public required WebApplicationFactory<TestProgram> Factory { get; init; }

    public required HttpClient HttpClient { get; init; }

    public required List<IAsyncDisposable> Disposables { get; init; }

    public required CancellationToken TestTimeoutToken { get; init; }

    public async ValueTask DisposeAsync()
    {
        try
        {
            foreach (var asyncDisposable in Disposables)
            {
                await asyncDisposable.DisposeAsync();
            }
        }
        catch
        {
            // Don't fail when finishing a test
        }
    }
}
