using Microsoft.AspNetCore.Builder;

namespace RxDBDotNet.Tests.Setup;

public sealed class TestProgram
{
    public static Task Main(string[] args)
    {
        Environment.SetEnvironmentVariable(
            "DOTNET_USE_POLLING_FILE_WATCHER",
            "true");
        Environment.SetEnvironmentVariable(
            "DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE",
            "false");

        var builder = WebApplication.CreateBuilder(args);

        TestStartup.ConfigureServices(builder.Services, builder);

        var webApplication = builder.Build();

        TestStartup.Configure(webApplication);

        return webApplication.RunAsync();
    }
}
