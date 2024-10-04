// tests\RxDBDotNet.Tests.Setup\TestProgram.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace RxDBDotNet.Tests.Setup;

public sealed class TestProgram
{
    public static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureLogging(builder);

        var app = builder.Build();

        return app.RunAsync();
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        builder.Logging.AddFilter(
            "Microsoft.EntityFrameworkCore.Database.Command",
            LogLevel.Critical);
        builder.Logging.AddFilter(
            "Microsoft.EntityFrameworkCore.Infrastructure",
            LogLevel.Critical);
        builder.Logging.AddFilter(
            "Microsoft.AspNetCore",
            LogLevel.Critical);
        builder.Logging.AddFilter(
            "Microsoft.AspNetCore.SignalR",
            LogLevel.Critical);
        builder.Logging.AddFilter(
            "Microsoft.AspNetCore.Http.Connections",
            LogLevel.Critical);
        builder.Logging.SetMinimumLevel(LogLevel.Critical);
    }
}
