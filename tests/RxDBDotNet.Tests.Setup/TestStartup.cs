using HotChocolate.Execution.Options;
using LiveDocs.GraphQLApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

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

        // Extend timeout for long-running operations
        services.Configure<RequestExecutorOptions>(options => options.ExecutionTimeout = TimeSpan.FromHours(1));

        // Configure WebSocket options for longer keep-alive
        services.Configure<WebSocketOptions>(options => options.KeepAliveInterval = TimeSpan.FromMinutes(2));

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost:3333"));

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

    protected override void ConfigureGraphQLServer(
        IServiceCollection services,
        WebApplicationBuilder builder,
        bool isAspireEnvironment)
    {
        // This override prevents the base class from configuring the GraphQL server.
        // Each unit test can configure the server as needed via TestSetupUtil.SetupAsync().
    }
}
