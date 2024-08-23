using HotChocolate.Execution.Options;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Infrastructure;
using LiveDocs.GraphQLApi.Models.Replication;
using LiveDocs.GraphQLApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RxDBDotNet.Repositories;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

ConfigureLogging(builder);

ConfigureServices(builder);

var app = builder.Build();

await app.RunAsync();

return;

static void ConfigureLogging(WebApplicationBuilder builder)
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

static void ConfigureServices(WebApplicationBuilder builder)
{
    builder.Services.AddProblemDetails();

    // Extend timeout for long-running debugging sessions
    builder.Services.Configure<RequestExecutorOptions>(options => options.ExecutionTimeout = TimeSpan.FromMinutes(15));

    // Configure WebSocket options for longer keep-alive
    builder.Services.Configure<WebSocketOptions>(options => options.KeepAliveInterval = TimeSpan.FromMinutes(2));

    // Redis is required for Hot Chocolate subscriptions
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost:3333"));

    builder.Services.AddDbContext<LiveDocsDbContext>(options =>
        options.UseSqlServer(Environment.GetEnvironmentVariable(ConfigKeys.DbConnectionString)
                             ?? throw new InvalidOperationException($"The '{ConfigKeys.DbConnectionString}' env variable must be set")));

    builder.Services
        .AddScoped<IDocumentService<ReplicatedUser>, UserService>()
        .AddScoped<IDocumentService<ReplicatedWorkspace>, WorkspaceService>()
        .AddScoped<IDocumentService<ReplicatedLiveDoc>, LiveDocService>();
}

// Here to make this class public so it can be used as a type parameter in WebApplicationFactory<Program>
namespace RxDBDotNet.Tests.Setup
{
    public sealed class Program;
}
