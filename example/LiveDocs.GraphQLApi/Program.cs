using HotChocolate.AspNetCore;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Infrastructure;
using LiveDocs.GraphQLApi.Models.Replication;
using LiveDocs.GraphQLApi.Services;
using LiveDocs.ServiceDefaults;
using RxDBDotNet.Extensions;
using RxDBDotNet.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder);

ConfigureGraphQL(builder);

var app = builder.Build();

ConfigureApp(app);

await InitializeLiveDocsDbAsync();

await app.RunAsync();

return;

static void ConfigureServices(WebApplicationBuilder builder)
{
    builder.AddServiceDefaults();

    builder.Services.AddProblemDetails();

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(corsPolicyBuilder =>
        {
            corsPolicyBuilder.WithOrigins(
                    "http://localhost:3000",
                    "http://127.0.0.1:8888")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    builder.AddSqlServerDbContext<LiveDocsDbContext>("sqldata");

    // Redis is required for Hot Chocolate subscriptions
    builder.AddRedisClient("redis");

    builder.Services
        .AddScoped<IDocumentService<ReplicatedUser>, UserService>()
        .AddScoped<IDocumentService<ReplicatedWorkspace>, WorkspaceService>()
        .AddScoped<IDocumentService<ReplicatedLiveDoc>, LiveDocService>();
}

static void ConfigureGraphQL(WebApplicationBuilder builder)
{
    builder.Services.AddGraphQLServer()
        .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
        // Mutation conventions must be enabled for replication to work
        .AddMutationConventions()
        .AddReplication()
        .AddSubscriptionDiagnostics()
        .AddReplicatedDocument<ReplicatedUser>(options => options.Security.RequirePolicyToCreate())
        .AddReplicatedDocument<ReplicatedWorkspace>()
        .AddReplicatedDocument<ReplicatedLiveDoc>()
        .AddRedisSubscriptions(provider => provider.GetRequiredService<IConnectionMultiplexer>());
}

static Task InitializeLiveDocsDbAsync()
{
    const string dbConnectionString = "Server=127.0.0.1,1146;Database=LiveDocsDb;User Id=sa;Password=Admin123!;TrustServerCertificate=True";

    Environment.SetEnvironmentVariable(ConfigKeys.DbConnectionString, dbConnectionString);

    return LiveDocsDbInitializer.InitializeAsync();
}

void ConfigureApp(WebApplication webApplication)
{
    webApplication.UseExceptionHandler();

    webApplication.UseDeveloperExceptionPage();

    webApplication.UseCors();

    webApplication.UseWebSockets();

    webApplication.MapGraphQL()
        .WithOptions(new GraphQLServerOptions
        {
            Tool =
            {
                Enable = true,
            },
        });
}
