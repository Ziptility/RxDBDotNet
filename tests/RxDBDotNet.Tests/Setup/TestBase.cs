using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RxDBDotNet.Tests.Utils;
using Xunit.Abstractions;

namespace RxDBDotNet.Tests.Setup;

public abstract class TestBase(ITestOutputHelper output) : IAsyncLifetime
{
    private static readonly SemaphoreSlim Semaphore = InitializeSemaphore();

    private AsyncServiceScope _asyncTestServiceScope;

    protected IServiceProvider TestServiceProvider => _asyncTestServiceScope.ServiceProvider;

    private WebApplicationFactory<TestProgram> Factory { get; set; } = null!;

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

        await UnitTestDbUtil.InitializeAsync(Output);

        // Disposed in call to FinishAsync()
#pragma warning disable CA2000
        Factory = new WebApplicationFactory<TestProgram>().WithWebHostBuilder(builder => builder
#pragma warning restore CA2000
            .UseSolutionRelativeContentRoot("LiveDocs.GraphQLApi"));

        _asyncTestServiceScope = Factory.Services.CreateAsyncScope();
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
