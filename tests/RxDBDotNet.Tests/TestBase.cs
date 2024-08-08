using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RxDBDotNet.Tests.Helpers;
using RxDBDotNet.Tests.Setup;
using Xunit.Abstractions;

namespace RxDBDotNet.Tests;

public abstract class TestBase(ITestOutputHelper output) : IAsyncLifetime
{
    private static readonly SemaphoreSlim Semaphore = InitializeSemaphore();

    private AsyncServiceScope _asyncTestServiceScope;

    protected IServiceProvider TestServiceProvider => _asyncTestServiceScope.ServiceProvider;

    protected WebApplicationFactory<TestProgram> Factory { get; set; } = null!;

    protected TestServer Server => Factory.Server;

    protected HttpClient HttpClient => Factory.HttpClient();

    protected ITestOutputHelper Output { get; } = output;

    protected static string CreateString(int? length = null)
    {
        length ??= 10;

        return Strings.CreateString(length.Value);
    }

    protected T GetService<T>() where T : notnull
    {
        return _asyncTestServiceScope.ServiceProvider.GetRequiredService<T>();
    }

    private static SemaphoreSlim InitializeSemaphore()
    {
        return new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
    }

    public async Task InitializeAsync()
    {
        await Semaphore.WaitAsync();

        Factory = WebApplicationFactorySetup.CreateWebApplicationFactory();

        _asyncTestServiceScope = Factory.Services.CreateAsyncScope();

        var applicationLifetime = _asyncTestServiceScope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();

        // Register the application stopping token
        var cancellationToken = applicationLifetime.ApplicationStopping;

        await UnitTestDbUtil.InitializeAsync(_asyncTestServiceScope.ServiceProvider, Output, cancellationToken);
    }

    public async Task DisposeAsync()
    {
        try
        {
            await Factory.DisposeAsync();
            await _asyncTestServiceScope.DisposeAsync();
        }
        catch
        {
            // Don't fail when finishing a test
        }

        Semaphore.Release();
    }
}
