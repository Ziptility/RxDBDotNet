using LiveDocs.GraphQLApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RxDBDotNet.Tests.Setup;

public class TestStartup : Startup
{
    public override void ConfigureServices(
        IServiceCollection services,
        IHostEnvironment environment,
        WebApplicationBuilder builder,
        bool isAspireEnvironment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(builder);

        base.ConfigureServices(services, environment, builder, isAspireEnvironment);

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

        builder.Configuration.AddEnvironmentVariables();
    }
}
