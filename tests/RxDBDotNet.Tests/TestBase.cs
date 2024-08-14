using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RxDBDotNet.Tests.Setup;
using Xunit.Abstractions;

namespace RxDBDotNet.Tests;

public abstract class TestBase(ITestOutputHelper output) : IAsyncLifetime
{
    private AsyncServiceScope _asyncTestServiceScope;

    protected WebApplicationFactory<TestProgram> Factory { get; set; } = null!;

    protected HttpClient HttpClient => Factory.HttpClient();

    protected ITestOutputHelper Output { get; } = output;

    public Task InitializeAsync()
    {
        Factory = WebApplicationFactorySetup.CreateWebApplicationFactory();

        _asyncTestServiceScope = Factory.Services.CreateAsyncScope();

        var applicationLifetime = _asyncTestServiceScope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();

        // Register the application stopping token
        var cancellationToken = applicationLifetime.ApplicationStopping;

        return UnitTestDbUtil.InitializeAsync(_asyncTestServiceScope.ServiceProvider, Output, cancellationToken);
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
    }
}
