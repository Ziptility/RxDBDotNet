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

        var startup = new TestStartup(builder.Configuration);
        startup.ConfigureServices(builder.Services, builder.Environment, builder, isAspireEnvironment: false);

        var webApplication = builder.Build();
        startup.Configure(webApplication, webApplication.Environment);

        return webApplication.RunAsync();
    }
}
