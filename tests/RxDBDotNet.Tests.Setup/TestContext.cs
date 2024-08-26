using Microsoft.AspNetCore.Mvc.Testing;

namespace RxDBDotNet.Tests.Setup;

public sealed class TestContext : IAsyncDisposable
{
    public required WebApplicationFactory<TestProgram> Factory { get; init; }

    public required HttpClient HttpClient { get; init; }

    public required List<IAsyncDisposable> AsyncDisposables { get; init; }

    public required List<IDisposable> Disposables { get; init; }

    public required CancellationToken CancellationToken { get; init; }

    public required IServiceProvider ServiceProvider { get; init; }

    public async ValueTask DisposeAsync()
    {
        try
        {
            foreach (var asyncDisposable in AsyncDisposables)
            {
                await asyncDisposable.DisposeAsync();
            }

            foreach (var disposable in Disposables)
            {
                disposable.Dispose();
            }
        }
        catch
        {
            // Don't fail when finishing a test
        }
    }
}
